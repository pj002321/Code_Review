# UINodeGraph 구현 및 동작 방식 정리

`UINodeGraph`는 유니티 UI 흐름과 상호작용을 노드 기반으로 시각화하고 편집할 수 있는 도구입니다. 
런타임 성능을 최적화하기 위해, 편집 시점의 노드 그래프 데이터를 **Bake(굽기)** 과정을 통해 선형적인 실행 데이터(`UIGraphRuntimeData`)로 변환하여 사용합니다.

---

## 1. 핵심 아키텍처 (Core Architecture)

### 1.1 데이터 구조
*   **Editor 데이터 (`UINodeGraph`)**: `ScriptableObject`로 저장되며, 노드(`UINode`)와 연결(`UINodeConnection`) 정보를 포함합니다. 이 데이터는 에디터에서 그래프를 그리고 편집하는 데 사용됩니다.
*   **Runtime 데이터 (`UIGraphRuntimeData`)**: 게임 실행 시 사용되는 최적화된 데이터입니다. 노드 간의 복잡한 참조 대신, 실행 순서대로 정렬된 `UIGraphExecutionStep` 리스트를 가집니다.
*   **식별자 시스템 (`UIGraphTarget`)**: 씬 내의 GameObject를 그래프에서 참조하기 위해 고유 ID(`TargetId`)를 부여하는 컴포넌트입니다. Bake 및 런타임 실행 시 이 ID를 통해 대상 오브젝트를 찾습니다.

### 1.2 Bake 프로세스 (최적화)
`UINodeGraph.Bake()` 메서드는 그래프 데이터를 런타임용으로 변환합니다.

```csharp
// UINodeGraph.cs
public void Bake()
{
#if UNITY_EDITOR
    // 1. 초기화 및 기존 데이터 정리
    bakedData = new UIGraphRuntimeData();
    CleanupOldBakedEvents();
    
    // 2. 노드 베이킹 (실행 순서 계산 및 데이터 추출)
    BakeNodes();
    
    // 3. 이벤트 컴포넌트 자동 부착 (Button, Keyboard 등)
    BakeButtonEvents();
    
    EditorUtility.SetDirty(this);
    AssetDatabase.SaveAssets();
#endif
}
```

1.  **위상 정렬 (Topological Sort)**: 노드 간의 연결 관계를 분석하여 실행 순서를 계산합니다.
2.  **이벤트 베이킹 (Event Baking)**:
    *   **Button**: `ButtonClickNode`와 연결된 버튼에는 `UIGraphBakedEvent` 컴포넌트를 자동으로 추가하고 `onClick` 이벤트에 등록합니다.
    *   **Keyboard**: `KeyboardInputNode`와 연결된 오브젝트에는 `UIGraphBakedKeyboardEvent` 컴포넌트를 추가하여 키 입력을 감지하도록 합니다.
3.  **데이터 직렬화**: 각 노드의 `CreateExecutionStep`을 호출하여 런타임에 필요한 파라미터(대상 오브젝트 ID, 지연 시간, 메서드 이름 등)만 추출하여 저장합니다.

---

## 2. 주요 컴포넌트 및 클래스

### 2.1 Core
*   **`UINodeGraph.cs`**: 그래프 데이터의 컨테이너이자 Bake 로직의 진입점입니다. Editor 관련 코드(`SetDirty`, `AssetDatabase`)를 포함하여 데이터 저장 및 갱신을 관리합니다.
*   **`UIGraphRuntimeData.cs`**: 실행 단계(`UIGraphExecutionStep`) 리스트를 담고 있으며, 직렬화 가능한 딕셔너리(`SerializableDictionary`)를 통해 다양한 타입의 파라미터를 저장합니다.

```csharp
// UIGraphRuntimeData.cs
[Serializable]
public class UIGraphExecutionStep
{
    public UINodeType nodeType;
    public string nodeGuid;
    // 런타임에 필요한 파라미터만 저장 (최적화)
    public string[] gameObjectIds; // TargetId (Bake된 고유 ID)
    public SerializableDictionary<string, string> stringParams;
    public SerializableDictionary<string, int> intParams;
    public SerializableDictionary<string, float> floatParams;
}
```

### 2.2 Runtime Components
*   **`UIGraphBakedEvent`**: Bake 시점에 버튼에 자동 추가됩니다. 버튼 클릭 시 `UIManager`를 통해 그래프 실행을 트리거합니다.

```csharp
// UIGraphBakedEvent.cs
public void OnButtonClick()
{
    // ... 유효성 검사 생략
    
    // UIManager에 그래프 실행 요청 (Fire & Forget)
    UIManager.Shared.ExecuteGraphFromNode(_graph, _startNodeGuid).Forget();
}
```

*   **`UIGraphTarget`**: 그래프에서 참조하는 GameObject에 자동 추가되며, GUID 기반의 고유 식별자를 제공하여 씬 로드/언로드 시에도 객체를 다시 찾을 수 있게 합니다.

### 2.3 Editor Tool
*   **`UINodeGraphEditor.cs`**: 커스텀 EditorWindow로 구현된 그래프 편집기입니다.
    *   **노드 시각화**: `DrawNodeWindow`를 통해 각 노드 타입에 맞는 Custom Inspector UI를 제공합니다.
    *   **연결 관리**: 베지에 곡선(Bezier Curve)을 사용하여 노드 간 연결을 시각화하고 관리합니다.
    *   **데이터 복원**: 씬이 열리거나 플레이 모드가 변경될 때, `GUID`를 기반으로 씬 내의 실제 GameObject 참조를 자동으로 복원(`RestoreNodeReferences`)하는 기능을 포함합니다.
    *   **드래그 앤 드롭**: Hierarchy의 GameObject를 그래프로 드래그하여 즉시 노드로 생성할 수 있습니다.

---

## 3. 노드 타입 (Node Types)

각 노드는 `UINode`를 상속받으며, `CreateExecutionStep`을 통해 런타임 데이터로 변환됩니다.

```csharp
// Example: ExecuteMethodNode.cs
public override UIGraphExecutionStep CreateExecutionStep(UINodeGraph graph)
{
    var step = new UIGraphExecutionStep { nodeType = UINodeType.ExecuteMethod, nodeGuid = guid };
    
    // 런타임에 오브젝트를 찾기 위해 TargetID 사용
    if (targetObject != null && graph != null)
    {
        step.gameObjectIds = new[] { graph.GetOrCreateTargetId(targetObject) };
        step.stringParams.Add("componentType", componentTypeName ?? "");
        step.stringParams.Add("methodName", methodName ?? "");
    }
    return step;
}
```

| 노드 타입 | 기능 | 파라미터 |
| :--- | :--- | :--- |
| **ButtonClick** | 그래프 실행의 시작점 (버튼 클릭 트리거) | Target Button (GameObject) |
| **KeyboardInput** | 지정된 키 입력 시 실행 트리거 | Target Object, KeyCode |
| **Delay** | 다음 노드 실행 전 대기 | Delay Seconds (float) |
| **Active Control** | 오브젝트/레이어 활성화 제어 (Show/Hide/Toggle) | Target Objects (Array) or Layers (Enum Array) |
| **ExecuteMethod** | 특정 컴포넌트의 메서드 호출 (매개변수 없는 void) | Target Object, Component Type, Method Name |

---

## 4. 실행 흐름 (Execution Flow)

1.  **Trigger**: 사용자가 버튼을 클릭하거나 키를 입력하면 `UIGraphBakedEvent`가 `startNodeGuid`와 함께 `UIManager`에 실행 요청을 보냅니다.
2.  **Graph Execution**: 
    - `UIManager`는 해당 그래프의 `bakedData.executionSteps`를 참조합니다.
    - 요청된 `startNodeGuid`에 해당하는 단계부터 순차적으로 실행을 시작합니다.
3.  **Action**: 각 단계(`ExecutionStep`)는 `nodeType`에 따라 적절한 로직(활성화/비활성, 대기, 메서드 호출 등)을 수행합니다. 
    - 이때 `TargetId`를 사용하여 런타임 오브젝트를 실시간으로 조회하여 조작합니다.

이러한 구조는 UI 로직과 코드를 분리하여 기획자나 아티스트가 유니티 에디터 상에서 UI 인터랙션을 시각적으로 설계할 수 있게 해주며, 런타임에는 최적화된 데이터로 실행되어 성능 부하를 최소화합니다.
