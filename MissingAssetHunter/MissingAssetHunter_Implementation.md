# MissingAssetHunter 구현 및 동작 방식 정리

MissingAssetHunter는 유니티 프로젝트 내에서 누락된 에셋(스크립트, 머티리얼, 프리팹)과 다양한 설정 오류를 찾아내고 분석하는 에디터 툴 모음입니다. 이 문서는 각 기능의 구현 방식과 동작 원리를 설명합니다.

## 1. 핵심 아키텍처 (Core Architecture)

모든 분석 도구는 `BaseFinderBehaviour` 클래스를 상속받아 구현되었습니다.

### BaseFinderBehaviour.cs
- **공통 UI 스타일**: `UIColors`, `UIStyles`를 통해 일관된 디자인(다크 모드, 카드 스타일 UI)을 제공합니다.
- **씬/오브젝트 탐색**: 
  - `FindInScene`: 특정 씬을 열고 오브젝트를 찾아 하이라이트하는 기능을 캡슐화했습니다.
  - 씬이 닫혀있어도 경로를 추적하여 자동으로 열고 해당 오브젝트를 선택합니다.
- **결과 표시**: 분석 결과를 리스트 형태로 보여주고, 선택(Select) 및 제거(Remove) 기능을 제공하는 템플릿 메서드를 포함합니다.

---

## 2. 주요 분석 도구 (Analyzers)

### A. Scene Analyzer (씬 분석기)
현재 활성화된 씬 또는 특정 씬을 종합적으로 분석합니다.

**주요 기능 및 구현:**
1.  **단계별 분석 (UpdateAnalysis)**:
    - 대규모 씬 분석 시 에디터가 멈추지 않도록 `EditorApplication.update`를 사용하여 분석을 여러 프레임에 나누어 처리합니다.
    - 단계: GameObjects -> Components -> Environment -> Errors -> Snapshot -> Finalize.
2.  **환경 분석 (Environment Info)**:
    - 조명(Lighing), 카메라(Camera), 지형(Terrain), 포스트 프로세싱(Post Processing) 설정을 수집합니다.
    - 특히 포스트 프로세싱 볼륨의 프로필과 활성/비활성 설정을 리플렉션(Reflection)을 통해 분석합니다.
3.  **에러 검출**:
    - Missing Script, Missing Material, Error Shader, Missing Prefab 등을 한 번에 검사합니다.
    - 성능 이슈(버텍스 10,000개 이상의 고폴리곤 매쉬)도 감지합니다.
4.  **씬 스냅샷 (Scene Snapshot)**:
    - `TempSnapshotCamera`를 생성하여 씬의 전체적인 모습을 렌더링하고 `RenderTexture`를 통해 이미지를 캡처하여 보고서에 포함합니다.

### B. Prefab Analyzer (프리팹 분석기)
프로젝트 내의 프리팹을 심층 분석합니다.

**주요 기능 및 구현:**
1.  **드래그 앤 드롭 (Drag & Drop)**:
    - `HandleDragAndDrop` 메서드에서 이벤트 타입을 확인하여 `.prefab` 파일만 허용하고, FBX 모델 프리팹은 필터링합니다.
2.  **본 대칭 분석 (Transform Symmetry)**:
    - 캐릭터 모델 등의 좌우 대칭(Left/Right 네이밍 규칙)을 분석하여 스케일 차이가 0.1 이상 나거나 비정상적인 스케일 값(0.5~2.0 범위를 벗어남)을 가진 본을 찾아냅니다.
3.  **심층 연결 검사**:
    - 프리팹 내부의 모든 컴포넌트, 머티리얼, 텍스처, 애니메이션 클립뿐만 아니라, 다른 프리팹과의 참조 관계까지 추적합니다.
    - `SerializedObject`를 사용하여 프리팹 내의 깨진 참조(Missing Prefab Reference)를 찾아냅니다.

---

## 3. 전문 탐색 도구 (Specialized Finders)

각각의 문제 유형에 특화된 탐색 로직을 제공합니다.

### A. Missing Material Finder
머티리얼 누락 및 셰이더 오류를 정밀하게 진단합니다.

**구현 디테일 (`IsErrorMaterial`):**
1.  **기본 검사**: `material.shader == null` 또는 Unity의 내장 에러 셰이더("Hidden/InternalErrorShader", 마젠타 색상)를 감지합니다.
2.  **렌더 파이프라인 불일치 (RP Mismatch)**:
    - 현재 프로젝트의 RP(HDRP, URP, Built-in)와 셰이더의 호환성을 검사합니다.
    - 예: HDRP 프로젝트에서 `Standard` 셰이더나 `URP` 셰이더가 사용된 경우를 에러로 처리합니다.
3.  **셰이더 코드 검증**:
    - 커스텀 셰이더의 경우, 소스 코드를 읽어 정규표현식(Regex)으로 구조적 결함을 검사합니다.
    - `#pragma vertex/fragment` 선언과 실제 함수명의 일치 여부 확인.
    - `CGPROGRAM`과 `ENDCG` 블록의 짝이 맞는지, `Pass` 블록이 존재하는지 확인합니다.

### B. Missing Script Finder
GameObject에 연결된 스크립트가 누락된 경우(MonoScript가 삭제되거나 GUID가 변경된 경우)를 찾습니다.

**구현 디테일:**
1.  **Fake Null 감지**:
    - `GetComponents<Component>()`로 가져온 배열에서 `components[i] == null`인 요소를 찾습니다. 유니티에서 스크립트가 깨지면 컴포넌트 슬롯은 남지만 실제 객체는 null로 반환되는 점을 이용합니다.
2.  **프리팹 연결 검증 (`ValidatePrefabInstanceScriptConnection`)**:
    - 프리팹 인스턴스에서 스크립트가 깨진 경우, 이것이 원본 프리팹의 문제인지 인스턴스 오버라이드 과정에서의 문제인지 구분합니다.
    - `PrefabUtility.GetCorrespondingObjectFromSource`로 원본을 찾아 비교 분석합니다.

### C. Missing Prefab Finder
중첩된 프리팹(Nested Prefab)이나 씬에 배치된 프리팹 인스턴스의 연결이 끊어진 경우를 찾습니다.

**구현 디테일:**
1.  **PrefabUtility 활용**:
    - `PrefabUtility.IsPrefabAssetMissing(obj)`: 프리팹 에셋 자체가 삭제된 경우.
    - `PrefabUtility.IsPartOfPrefabInstance(obj)` + `PrefabUtility.GetCorrespondingObjectFromSource(obj) == null`: 프리팹 인스턴스이지만 원본과의 연결이 끊어진 경우(Broken Prefab Connection).
2.  **재귀적 탐색**:
    - 씬의 모든 루트 오브젝트부터 시작하여 자식 트랜스폼을 순회하며 깨진 프리팹을 찾아냅니다.

---

## 4. 요약

| 도구 | 주요 대상 | 특징적 구현 |
| :--- | :--- | :--- |
| **SceneAnalyzer** | 씬 전체 | 비동기 분할 분석, 씬 스냅샷, 환경 설정(Light/PostProcessing) 분석 |
| **PrefabAnalyzer** | 프리팹 에셋 | 본(Bone) 대칭 분석, 드래그 앤 드롭 지원, 심층 참조 추적 |
| **MissingMaterial** | 머티리얼/셰이더 | 렌더 파이프라인 호환성 검사, 셰이더 소스 코드 Regex 정밀 검사 |
| **MissingScript** | 컴포넌트 | Fake Null 감지, 프리팹 원본 vs 인스턴스 상태 비교 |
| **MissingPrefab** | 프리팹 인스턴스 | 유니티 PrefabUtility API를 활용한 연결 상태 정밀 진단 |

이 도구들은 단순한 Null 체크를 넘어, 렌더 파이프라인 호환성이나 셰이더 코드 레벨의 오류, 프리팹의 구조적 결함까지 찾아내어 프로젝트의 무결성을 유지하는 데 도움을 줍니다.
