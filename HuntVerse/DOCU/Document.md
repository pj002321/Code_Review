# HuntVerse 프로젝트 구조

작업 중인 프로젝트이므로 항시 코드의 내용과 구조가 바뀔 수 있습니다.



## 📁 코드 구조 개요

### 1. Common/
공통 유틸리티 및 기초 시스템을 관리합니다.

#### Pool/
- **IPoolable.cs**: 풀링 가능한 객체의 인터페이스
- **PoolManager.cs**: Unity의 ObjectPool을 활용한 오브젝트 풀링 매니저. 프리팹별로 독립적인 풀을 관리하며 Spawn/Despawn 기능 제공

#### Scene/
- **SceneLoadHelper.cs**: 씬 로딩을 보조하는 헬퍼 클래스

#### Vfx/
- **IVfxMover.cs**: VFX 이동 인터페이스
- **VfxHandle.cs**: VFX 인스턴스를 제어하기 위한 핸들
- **VfxHelper.cs**: VFX 생성 및 관리를 위한 헬퍼
- **VfxObject.cs**: VFX 오브젝트 컴포넌트

#### 기타
- **NotiConst.cs**: 알림 메시지 상수 정의
- **ResourceKeyConst.cs**: 리소스 키 상수 정의

---

### 2. Contents/
게임 콘텐츠 로직을 담당합니다.

#### Combat/
- **IProjectile.cs**: 발사체 인터페이스
- **ProjectileBase.cs**: 발사체 기본 클래스

#### Dialog/
- **DialogChoiceButton.cs**: 대화 선택지 버튼
- **DialogData.cs**: 대화 데이터 (노드 기반 대화 시스템)
- **DialogManager.cs**: 대화 시스템 관리자. 타이핑 효과, 선택지 처리, 노드 히스토리 관리
- **DialogPanel.cs**: 대화 UI 패널
- **DialogState.cs**: 대화 상태 (None, Typing, WaitingForInput, ShowingChoices, ProcessingChoice, Completed)

#### NPC/
- **NPCBase.cs**: NPC 기본 클래스
- **NPCData.cs**: NPC 데이터
- **NPCType.cs**: NPC 타입 정의

#### Object/
- 게임 내 오브젝트 관련 스크립트

---

### 3. Screen/
UI 화면들을 관리합니다.

#### CharacterSelect/
- **CharacterSetupController.cs**: 캐릭터 설정 컨트롤러
- **CharInfoField.cs**: 캐릭터 정보 필드
- **CreateCharDocuPanel.cs**: 캐릭터 생성 문서 패널
- **CreateCharPreviewSlot.cs**: 캐릭터 미리보기 슬롯
- **CreateCharProfile.cs**: 캐릭터 생성 프로필
- **UserCharProfilePanel.cs**: 유저 캐릭터 프로필 패널

#### LogIn/
- **LogInScreen.cs**: 로그인 화면. ID/PW 입력, 계정 생성, 유효성 검증, CapsLock 감지 등 처리

#### Menu/
- **MainMenuScreen.cs**: 메인 메뉴 화면
- **SelectMenuAction.cs**: 메뉴 선택 액션
- **SelectMenuField.cs**: 메뉴 선택 필드

#### Village/
- **InGameHud.cs**: 인게임 HUD 메인 컨트롤러

##### Panel/
- **HudCharInventoryPanel.cs**: 캐릭터 인벤토리 패널
- **HudCharStatPanel.cs**: 캐릭터 스탯 패널
- **HudChatPanel.cs**: 채팅 패널
- **HudSettingPanel.cs**: 설정 패널
- **HudStagePanel.cs**: 스테이지 패널
- **HudUserPanel.cs**: 유저 정보 패널
- **UserQuickSlot.cs**: 퀵슬롯

#### World/
- **GameWorldController.cs**: 게임 월드 컨트롤러
- **GameWorldField.cs**: 게임 월드 필드

---

### 4. Tool/
유니티 에디터 도구 및 개발 편의 기능입니다.

- **SceneToolMenu.cs**: 씬 도구 메뉴
- **SteamBuildPostProcessor.cs**: 스팀 빌드 후처리

#### UINodeGraph/
노드 기반 UI 이벤트 시스템. 버튼 클릭, 키보드 입력 등을 노드로 연결하여 UI 플로우를 비주얼하게 구성

##### Editor/
- **UINodeGraphEditor.cs**: 노드 그래프 에디터 윈도우 (651줄)
- **UINodeGraphInspector.cs**: 노드 그래프 인스펙터

##### Node/
- **ButtonClickNode.cs**: 버튼 클릭 노드
- **DelayNode.cs**: 딜레이 노드
- **ExecuteMethodNode.cs**: 메소드 실행 노드
- **HideGameObjectNode.cs**: GameObject 숨김 노드
- **HideLayerNode.cs**: 레이어 숨김 노드
- **KeyboardInputNode.cs**: 키보드 입력 노드
- **ShowGameObjectNode.cs**: GameObject 표시 노드
- **ShowLayerNode.cs**: 레이어 표시 노드
- **ToggleGameObjectNode.cs**: GameObject 토글 노드
- **ToggleLayerNode.cs**: 레이어 토글 노드

##### 기타
- **UIGraphBakedEvent.cs**: 베이크된 UI 그래프 이벤트 (런타임 실행)
- **UIGraphBakedKeyboardEvent.cs**: 베이크된 키보드 이벤트
- **UIGraphRuntimeData.cs**: 그래프 런타임 데이터
- **UIGraphTarget.cs**: UI 그래프 타겟 (GameObject 식별용)
- **UINode.cs**: UI 노드 기본 클래스
- **UINodeConnection.cs**: 노드 간 연결
- **UINodeGraph.cs**: 노드 그래프 ScriptableObject. Bake 시 노드를 실행 순서로 정렬하고 런타임 데이터 생성

---

### 5. Extention/
확장 기능 및 유틸리티 클래스입니다.

- **MonoBehaviourSingleton.cs**: 싱글톤 패턴 기본 클래스. DontDestroyOnLoad 옵션 지원
- **ObjectExtension.cs**: Object 확장 메소드
- **StringExtension.cs**: String 확장 메소드

#### UI/
- **PentagonBalanceUI.cs**: 오각형 밸런스 UI (스탯 차트)
- **UIButtonAudio.cs**: 버튼 오디오
- **UIButtonClickCount.cs**: 버튼 클릭 카운트
- **UIButtonControlBase.cs**: 버튼 컨트롤 기본 클래스
- **UIControlBase.cs**: UI 컨트롤 기본 클래스
- **UIDragWidget.cs**: UI 드래그 위젯
- **UIEffect.cs**: UI 효과 (페이드 등)

---

### 6. Service/
핵심 서비스 및 매니저들을 포함합니다.

#### Boot/
- **ContentsDownloader.cs**: 콘텐츠 다운로더
- **LoadingIndicator.cs**: 로딩 인디케이터
- **SystemBoot.cs**: 시스템 부팅 및 초기화. 로그인 서버 연결, Steam 초기화, 리소스 다운로드 등 처리

#### Manage/
- **AbLoader.cs**: AssetBundle 로더
- **AudioManager.cs**: 오디오 매니저
- **InputManager.cs**: 입력 매니저
- **UIEventManager.cs**: UI 이벤트 매니저
- **UIManager.cs**: UI 시스템 통합 관리자. UILayerManager, UIGraphExecutor, UIEventManager 등 서브시스템 조율
- **VfxManager.cs**: VFX 매니저. 키별 독립 풀 관리, 프리로드, PlayOneShot 등 VFX 생명주기 관리

---

### 7. Network/
네트워크 통신 관련 로직입니다.

#### Auth/
- **LoginService.cs**: 로그인 서버 네트워크 요청/응답 처리. 로그인, 계정 생성, ID/닉네임 중복 확인, 캐릭터 생성 등

#### Login Flow

https://app.diagrams.net/#G12uZP_RN9W6zS4QThJRKT3N5bUo_b3E-f#%7B%22pageId%22%3A%22dfulZH3nUIs-t-_39o08%22%7D

---

#### Character/
- **CharacterFieldListRequst.cs**: 캐릭터 필드 리스트 요청
- **CharModel.cs**: 캐릭터 모델 데이터

#### Data/
- **TableDataBuilder.cs**: 테이블 데이터 빌더
- **TableDataManager.cs**: 테이블 데이터 매니저

#### Session/
- **GameSession.cs**: 게임 세션 관리. 로그인 서버/게임 서버 연결 관리, 캐릭터 정보, 월드 정보 캐싱

#### World/
- **WorldListRequest.cs**: 월드 리스트 요청
- **WorldModel.cs**: 월드 모델 데이터

---

### 8. User/
플레이어 캐릭터 관련 로직입니다.

- **UserCharacter.cs**: 유저 캐릭터 컴포넌트. GameSession에서 선택된 캐릭터 정보를 기반으로 모델 로드 및 초기화

#### Player/
- **IPlayer.cs**: 플레이어 인터페이스
- **UserCharLoco.cs**: 유저 캐릭터 이동/점프/공격 로직. Rigidbody2D 기반 2D 플랫포머 컨트롤러

##### Animation/
- **AttackBehaviour.cs**: 공격 애니메이션 Behaviour
- **JumpBehaviour.cs**: 점프 애니메이션 Behaviour

