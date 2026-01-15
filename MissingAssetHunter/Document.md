# MissingAssetHunter - Unity 에디터 도구

## 📋 개요

MissingAssetHunter는 Unity 프로젝트에서 누락되거나 손상된 에셋을 찾아내는 에디터 확장 도구입니다. 씬과 프리팹을 분석하여 Missing Script, Missing Material, Broken Prefab 등의 문제를 탐지하고 상세한 분석 리포트를 제공합니다.

---

## 📁 코드 구조

### 1. BaseFinderBehaviour.cs (728 lines)
모든 Finder 클래스의 기본이 되는 추상 클래스입니다.

**주요 기능:**
- **UI 스타일 시스템**: 모던한 에디터 GUI 스타일 제공
  - `UIColors`: Primary, Success, Warning, Danger, Info 등 다양한 색상 팔레트
  - `UIStyles`: Card, Header, Button, Title, Subtitle 등 재사용 가능한 스타일
- **공통 UI 컴포넌트**:
  - 검색 영역 (타겟 선택, 검색 버튼)
  - 결과 표시 영역 (스크롤뷰, 필터링)
  - 액션 버튼 (Select, Fix, Remove)
- **추상 메소드**:
  - `DrawUI()`: 각 Finder의 UI 렌더링
  - `ClearResults()`: 검색 결과 초기화

**핵심 UI 컴포넌트:**
```csharp
- DrawTitle(): 헤더 타이틀 렌더링
- DrawSearchArea(): 검색 타겟 선택 영역
- DrawResultsArea(): 검색 결과 표시 영역
- DrawActionButtons(): 액션 버튼 그룹
```

---

### 2. MissingScriptFinder.cs (246 lines)
누락되거나 손상된 스크립트 컴포넌트를 찾는 핵심 로직입니다.

**핵심 검사 알고리즘:**

#### ① Fake Null 검출
```csharp
Component[] components = obj.GetComponents<Component>();
for (int i = 0; i < components.Length; i++)
{
    // 컴포넌트 슬롯은 존재하지만 null인 상태 (Fake Null)
    if (components[i] == null)
    {
        // Missing Script 발견
    }
}
```

#### ② Prefab Instance 검증
```csharp
- 원본 프리팹 연결 확인
- 원본 프리팹의 해당 인덱스 컴포넌트 상태 확인
- 씬 오버라이드로 인한 손상 감지
- 원본도 손상된 경우와 인스턴스만 손상된 경우 구분
```

**검사 모드:**
- **Scene Mode**: 현재 열린 씬 또는 지정된 씬들 검사
- **Prefab Mode**: 지정된 프리팹들 검사

**검사 범위:**
- GameObject 및 모든 자식 재귀 탐색
- Prefab Instance의 원본 연결 상태 검증
- 컴포넌트 인덱스 추적

---

### 3. MissingMaterialFinder.cs (506 lines)
누락되거나 손상된 Material 및 Shader를 찾는 클래스입니다.

**검사 대상 Renderer:**
- MeshRenderer
- SkinnedMeshRenderer
- ParticleSystemRenderer
- LineRenderer
- TrailRenderer
- SpriteRenderer

**검사 항목:**

#### ① Material 검증
```csharp
- Material이 null인 경우
- Error Material 상태 확인 (핑크색 에러 머티리얼)
- Material 경로 유효성 검사
```

#### ② Shader 검증
```csharp
- Shader가 null인 경우
- Shader 파일 존재 여부 확인
- Shader 지원 여부 (isSupported)
- Shader 컴파일 상태 (passCount > 0)
```

#### ③ Texture 검증
```csharp
- Material의 모든 Texture Property 순회
- Texture null 체크
- Texture 파일 존재 여부 확인
```

**특수 케이스:**
- URP/HDRP Error Shader 감지
- Built-in Shader Missing 감지
- Shader Graph 참조 손상 감지

---

### 4. MissingPrefabFinder.cs (189 lines)
손상되거나 연결이 끊어진 Prefab을 찾는 클래스입니다.

**검사 항목:**

#### ① Prefab Asset Missing
```csharp
if (PrefabUtility.IsPrefabAssetMissing(obj))
{
    // 프리팹 에셋 파일이 삭제된 경우
}
```

#### ② Broken Prefab Instance
```csharp
var prefabAsset = PrefabUtility.GetCorrespondingObjectFromSource(obj);
if (prefabAsset == null)
{
    // 원본 프리팹 연결이 끊어진 경우
}
```

**에러 타입:**
- **Missing Prefab Asset**: 프리팹 파일 자체가 삭제됨
- **Broken Prefab Instance**: 원본 연결이 끊어짐

---

### 5. PrefabAnalyzer.cs (1695 lines)
프리팹을 심층 분석하여 의존성, 최적화, 문제점을 찾는 클래스입니다.

**분석 모드:**

#### ① Dependency Analysis (의존성 분석)
```
프리팹 구조 분석
    ├── Component 목록 (종류별 통계)
    ├── Material 의존성
    ├── Texture 의존성
    ├── Script 의존성
    └── Nested Prefab 의존성
```

#### ② Performance Analysis (성능 분석)
```
- High Poly Mesh 감지 (>10,000 vertices)
- 과도한 Renderer 수
- 많은 Material/Texture 사용
- 복잡한 Hierarchy 깊이
- Missing Component 영향도
```

#### ③ Issue Detection (문제 감지)
```
- Missing Scripts
- Missing Materials
- Missing Textures
- Broken Nested Prefabs
- Error Shaders
- Optimization Suggestions
```

**통계 수집:**
- 총 프리팹 수
- 문제가 있는 프리팹 수
- 총 컴포넌트 수
- 총 Material/Texture 수

**폴더 스캔 기능:**
- 지정된 폴더의 모든 프리팹 자동 스캔
- 하위 폴더 포함 옵션
- 배치 분석 지원

---

### 6. SceneAnalyzer.cs (1348 lines)
씬 전체를 분석하여 GameObject, 환경 설정, 에러를 찾는 클래스입니다.

**분석 카테고리:**

#### ① GameObject Analysis
```
- 총 오브젝트 수 (활성/비활성)
- Component 통계 (종류별 개수)
- Script 사용 현황
- Renderer 통계
- Material 사용 현황
```

#### ② Environment Analysis
```
Lighting:
    ├── Light 목록 (종류, 강도, 범위)
    ├── Lightmap 정보
    └── Reflection Mode
    
Cameras:
    ├── 카메라 설정 (FOV, Clipping)
    └── Clear Flags, Background Color
    
Terrain:
    ├── Heightmap Resolution
    ├── Detail Resolution
    └── Alphamap Resolution
    
Post Processing:
    ├── Volume 설정
    ├── Active Settings
    └── Priority, Weight
```

#### ③ Error Detection
```
- Missing Scripts
- Missing Materials
- Missing Prefabs
- Broken Prefab Connections
- Performance Issues
- Lighting Issues
- Camera Issues
```

**에러 심각도:**
- **Critical**: 즉시 수정 필요
- **High**: 높은 우선순위
- **Medium**: 중간 우선순위
- **Low**: 낮은 우선순위

**씬 스냅샷:**
- 512x512 해상도의 씬 미리보기 생성
- 분석 결과와 함께 저장

---

### 7. SceneAnalyzer.Data.cs (180 lines)
씬 분석 결과를 저장하는 데이터 구조 정의입니다.

**주요 데이터 클래스:**

```csharp
SceneAnalysisResult
    ├── 씬 기본 정보 (이름, 경로, 분석 시간)
    ├── 통계 (오브젝트 수, 컴포넌트 수, 에러 수)
    ├── GameObject 목록 (GameObjectInfo[])
    ├── Component 타입 통계 (Dictionary)
    ├── 환경 정보 (EnvironmentInfo)
    └── 에러 목록 (SceneError[])

GameObjectInfo
    ├── GameObject 참조
    ├── 기본 정보 (이름, 활성 여부, 레이어, 태그)
    ├── 자식 수
    └── 컴포넌트 목록 (ComponentInfo[])

EnvironmentInfo
    ├── 조명 정보 (LightInfo[])
    ├── 카메라 정보 (CameraInfo[])
    ├── 터레인 정보 (TerrainInfo[])
    └── 포스트 프로세싱 (PostProcessingInfo[])

SceneError
    ├── 에러 타입 (Enum)
    ├── 심각도 (Enum)
    ├── GameObject/Component 참조
    ├── 에러 메시지
    └── 추가 정보 (인덱스 등)
```

## 🔍 사용 시나리오

### 1. 프로젝트 정리 전 체크
```
씬 분석 → 에러 목록 확인 → 우선순위별 수정 → 재분석
```

### 2. 프리팹 리팩토링
```
프리팹 분석 → 의존성 파악 → 최적화 포인트 확인 → 수정 → 재분석
```

### 3. 빌드 전 검증
```
전체 씬 스캔 → Missing Asset 확인 → 수정 → 클린 빌드
```

### 4. 협업 중 에셋 무결성 확인
```
Git Pull → 씬/프리팹 검사 → Broken Reference 수정 → Commit
```

## 🎯 주요 사용 사례

### 1. 대규모 프로젝트 마이그레이션
- Unity 버전 업그레이드 후 Broken Reference 일괄 확인
- 폴더 구조 변경 후 Missing Asset 검증

### 2. 에셋 스토어 퀄리티 체크
- 출시 전 모든 프리팹/씬 검증
- 의존성 무결성 확인

### 3. 팀 협업 품질 관리
- Git 충돌 후 씬 무결성 확인
- 리뷰 전 자동 검사 프로세스

### 4. 최적화 작업
- 성능 이슈 프리팹 식별
- 불필요한 Material/Texture 정리

---

