# HuntVerse VFX 시스템 구조 분석

이 문서는 프로젝트의 VFX 시스템이 어떻게 구성되어 있고, 데이터가 흐르는지를 설명합니다.  
**Editor(툴)** 에서 데이터를 설정하고, **Data(프리셋)** 에 저장하며, **Runtime(인게임)** 에서 이를 재생하는 구조입니다.

---

## 1. 전체 구조 (Architecture)

```mermaid
graph TD
    %% Editor Layer
    Editor[HuntPresetEditor.cs] -->|편집 & 저장| Data[CharacterFxPreset.cs]
    
    %% Runtime Layer
    subgraph InGame [인게임 런타임]
        Actor[ActorFxController] -->|참조| Data
        Actor -->|애니메이션 상태 감지| Animator
        Actor -->|VFX 생성 요청 (PlayOneShot)| Manager[VfxManager.cs]
        
        Manager -->|프리팹 로드 & 풀링| VfxObject[VfxObject (Pool)]
    end
```

### 핵심 구성 요소
1.  **Data (`CharacterFxPreset.cs`)**: 어떤 캐릭터의 어떤 애니메이션 클립에서, 언제(Timing), 어떤 이펙트/사운드를 재생할지 정의하는 데이터.
2.  **Tool (`HuntPresetEditor.cs`)**: 개발자가 이 데이터를 직관적으로 입력할 수 있도록 돕는 Odin 기반의 에디터 윈도우. (타임라인 프리뷰, 자동 추출 등)
3.  **Service (`VfxManager.cs`)**: 실제 이펙트 오브젝트(GameObject)를 관리하는 매니저. Addressables 로드, 오브젝트 풀링, 위치/회전 제어, 클립 기반 자동 재생 로직 등을 담당.

---

## 2. 파일별 상세 분석 및 코드 콜아웃

### A. 데이터 정의: `CharacterFxPreset.cs`
캐릭터별로 존재하는 설정 파일(ScriptableObject)로, 각 애니메이션 클립마다의 VFX/SFX 타이밍을 저장합니다.

> **핵심 역할**: 애니메이션 이름(`clipName`)과 타이밍 데이터(`FxTiming`)의 매핑 저장소.

```csharp
// [Assets/Script/Tool/FXPreset/Editor/CharacterFxPreset.cs]

[CreateAssetMenu(fileName = "CharacterFxPreset", menuName = "Hunt/CharacterFxPreset")]
public class CharacterFxPreset : ScriptableObject
{
    [Header("액터 정보")]
    public GameObject characterPrefab; // 미리보기 및 연동용
    
    [Header("클립별 FX 설정")]
    public List<ClipFxData> clipFxDataList = new List<ClipFxData>();
}

[Serializable]
public class ClipFxData
{
    public string clipName; // "Attack01"
    public List<FxTiming> fxTimings = new List<FxTiming>();
}

[Serializable]
public class FxTiming
{
    public float timeInSeconds;   // "0.5초 지점"
    public VfxType vfxType;       // "SlashEffect" (Enum 등)
    public AudioType audioType;   // "SwingSound"
    public bool attachHit;        // 캐릭터/부모에 붙어서 따라다닐지 여부
}
```

---

## 3. 에디터 툴: `HuntPresetEditor.cs`
데이터를 쉽고 정확하게 편집하기 위한 커스텀 에디터입니다. Odin Inspector를 사용하며, 애니메이션 타임라인을 직접 제어하며 타이밍을 잡을 수 있습니다.

> **핵심 기능**:
> *   **타임라인 프리뷰**: 슬라이더를 움직여 애니메이션 특정 프레임을 미리보기 (`SampleAnimation`).
> *   **자동 추출**: Animator Controller의 모든 클립을 가져와 목록을 자동 생성 (`AutoExtractClipsFromAnimator`).
> *   **Addressable 자동 등록**: 프리셋 생성/저장 시 자동으로 Addressables 그룹에 등록하여 런타임 로드 지원.

```csharp
// [Assets/Script/Tool/FXPreset/Editor/HuntPresetEditor.cs]

// 미리보기용 캐릭터 생성 및 애니메이션 샘플링
private void SampleAnimation()
{
    if (_previewInstance != null && _currentClip != null)
    {
        // 에디터 상에서 시간을 변경하며 포즈를 강제로 업데이트
        _currentClip.SampleAnimation(_previewInstance, _previewTime);
    }
}

// "Add Event Here" 버튼: 현재 미리보기 시간(_previewTime)에 이벤트 추가
private void AddEventAtCurrentTime()
{
    // ...
    clipData.fxTimings.Add(new FxTiming
    {
        timeInSeconds = _previewTime, // 현재 슬라이더 시간 자동 입력
        vfxType = VfxType.None
    });
    // ...
}

// 프리셋 저장 시 Addressable 자동 등록 (런타임 로드 보장)
private void RegisterToAddressable(string assetPath)
{
    // ...
    var entry = settings.CreateOrMoveEntry(guid, group);
    entry.SetAddress(System.IO.Path.GetFileNameWithoutExtension(assetPath));
}
```

---

## 4. 런타임 매니저: `VfxManager.cs` (`Service/Manage/`)
게임 내에서 실제 이펙트를 생성하고 관리하는 싱글톤 매니저입니다. 데이터(`CharacterFxPreset`)를 직접 참조하기보다는, **요청(Key, Position)** 에 따라 리소스를 로드하고 풀링하여 재생합니다. 또한, 애니메이션 클립의 이벤트를 런타임에 파싱하여 자동 재생하는 기능도 포함하고 있습니다.

> **핵심 기능**:
> *   **비동기 리소스 로드**: `GetOrLoadVfxObject` (Addressables 사용).
> *   **오브젝트 풀링**: `GetPool` / `ObjectPool<VfxObject>` 사용으로 성능 최적화.
> *   **클립 이벤트 파싱**: `ReadSpansFromClipEvents`를 통해 애니메이션 클립 자체에 심어진 이벤트도 처리 가능.

```csharp
// [Assets/Script/Service/Manage/VfxManager.cs]

// 1. 단발성 이펙트 재생 요청
public async UniTask<VfxHandle> PlayOneShot(string key, Vector3 pos, Quaternion rot, Transform parent = null)
{
    // A. 프리팹 비동기 로드 (캐싱됨)
    var vfxObj = await GetOrLoadVfxObject(key);
    
    // B. 풀에서 가져오기
    var pool = GetPool(key, vfxObj);
    var vfxInstance = pool.Get();

    // C. 위치/회전/부모 설정
    if (parent != null)
    {
        vfxInstance.transform.SetParent(parent);
        // ... 로컬 좌표/회전 계산
    }
    else
    {
        vfxInstance.transform.position = pos + rot * spawnOffset;
        vfxInstance.transform.rotation = rot;
    }

    // D. 초기화 및 반환 (종료 시 Release 호출)
    vfxInstance.Init(() => { pool.Release(vfxInstance); });

    return new VfxHandle(vfxInstance);
}

// 2. 클립 이벤트 기반 재생 구간(Span) 계산
public List<VfxSpan> GetSpansForClip(AnimationClip clip)
{
    // ... 오버라이드 확인
    
    // 캐시 없으면 클립 이벤트 파싱
    var spans = ReadSpansFromClipEvents(clip);
    _clipSpansCache[clipName] = spans;
    return spans;
}
```

---

## 요약: 작업 흐름 (Workflow)

1.  **설정 (Editor)**:
    *   `CharacterFxPreset` 생성 후 작업할 캐릭터의 `CharacterFxPreset`을 엽니다.
    *   `HuntPresetEditor`에서 캐릭터 프리팹을 선택하고, 애니메이션 클립을 로드합니다.
    *   타임라인 슬라이더를 움직여 공격 순간(예: 0.3초)을 찾고 "Add Event Here"를 눌러 `Slash` 이펙트를 추가합니다.
    *   저장하면 `CharacterFxPreset.asset` 파일이 갱신되고, Addressables에 자동 등록됩니다.

2.  **실행 (Runtime)**:
    *   인게임의 캐릭터 컨트롤러(`ActorFxController` 등)는 자신의 `CharacterFxPreset` 데이터를 읽습니다.
    *   애니메이션 재생 시간을 모니터링하다가 설정된 시간(0.3초)이 되면 `VfxManager.Shared.PlayOneShot("Slash", ...)`을 호출합니다.
    *   `VfxManager`는 해당 키("Slash")의 프리팹이 로드되어 있는지 확인하고(없으면 로드), 풀에서 꺼내 지정된 위치에 이펙트를 표시합니다.
