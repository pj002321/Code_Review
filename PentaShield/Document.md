# PentaShield 프로젝트 구조

## 📁 코드 구조 개요

### 1. Boot/
게임 부팅 및 초기화 프로세스를 담당합니다.

- **BootLoader.cs**: 게임 부팅 메인 로직. Addressable 다운로드, Firebase 초기화, 점검/업데이트 확인, 프로그레스바 관리
- **BootingGuide.cs**: 부팅 가이드 UI 컴포넌트
- **BootProgress.cs**: 부팅 진행 상태 표시

---

### 2. Addressables/
Addressable Asset System 관리 및 Firebase 연동을 담당합니다.

- **AbHelper.cs**: Addressable 에셋 로드 헬퍼
- **AddressableDownloadManager.cs**: Addressable 다운로드 관리. Firebase Storage에서 에셋 다운로드
- **AddressableSystemManager.cs**: Addressable 시스템 초기화 및 갱신. InternalIdTransform 설정, 카탈로그 업데이트 관리
- **AddressabpeFirebaseUploader.cs**: Firebase Storage로 Addressable 에셋 업로드

---

### 3. Firebase/
Firebase 초기화 및 서비스 관리입니다.

- **PentaFirebase.cs**: Firebase 통합 관리자. Auth, Firestore, Realtime Database 초기화
- **PFireAuth.cs**: Firebase Authentication 래퍼. Google/Apple 로그인, 익명 로그인, 계정 삭제
- **PFireStore.cs**: Firestore 데이터베이스 래퍼
- **PRealTimeDb.cs**: Realtime Database 래퍼. 점검 플래그, 일일 보상 데이터 관리
- **FirebaseConfig.cs**: Firebase 설정 (Database URL, Storage Bucket)
- **FirebaseStorageClient.cs**: Firebase Storage 클라이언트

---

### 4. Google_Apple_Sign/
소셜 로그인 기능을 담당합니다.

- **LoginUI.cs**: 로그인 UI 관리. Google/Apple 로그인, 로그아웃, 계정 삭제 처리
- **AuthInfoUI.cs**: 인증 정보 UI 표시
- **NameEditButton.cs**: 사용자 이름 편집 버튼
- **GoogleSignIn.h / GoogleSignIn.mm**: iOS용 Google 로그인 네이티브 플러그인
- **GoogleSignInAppController.h / GoogleSignInAppController.mm**: iOS용 Google 로그인 App Controller

---

### 5. DailyReward/
일일 보상 시스템을 관리합니다.

- **FirebaseDailyRewardManager.cs**: Firebase Realtime Database에서 일일 보상 사이클 정보 로드. 보상 동기화 및 사이클 관리
- **DailyRewardData.cs**: 일일 보상 데이터 모델
- **DailyRewardSlot.cs**: 보상 슬롯 UI
- **DailyRewardViewer.cs**: 보상 뷰어 UI

---

### 6. Common/
공통 유틸리티 및 상수 정의입니다.

- **PentaConst.cs**: 게임 전반의 상수 정의 (애니메이션 키, 씬 이름 등)
- **RegionConst.cs**: 지역/국가 코드 상수

#### Helper/Audio/
- **AudioHelper.cs**: 오디오 재생 헬퍼
- **AudioManager.cs**: 오디오 매니저

#### Helper/Effect/
- 이펙트 관련 헬퍼

---

### 7. Screen/
UI 화면 관리를 담당합니다.

- **MainMenuScreen.cs**: 메인 메뉴 화면 메인 로직
- **MainMenuScreen.UI.cs**: 메인 메뉴 UI 부분 클래스
- **InGameScreen.cs**: 인게임 화면

#### GameHub/
- **IMainMenuScoreText.cs**: 메인 메뉴 스코어 텍스트 인터페이스
- **ISceneChangedUpdate.cs**: 씬 변경 업데이트 인터페이스
- **ScoreTextBase.cs**: 스코어 텍스트 기본 클래스
- **StageSelectPanel.cs**: 스테이지 선택 패널
- **StageWaveText.cs**: 스테이지 웨이브 텍스트

#### UserRank/
- **MainMenuRankStageUI.cs**: 메인 메뉴 랭킹 스테이지 UI
- **RankHudUI.cs**: 랭킹 HUD UI
- **UserRankBoardUI.cs**: 유저 랭킹 보드 UI

---

### 8. Contents/
게임 콘텐츠 로직을 담당합니다.

#### Combat/
전투 시스템 핵심 로직입니다.

- **IDamageable.cs**: 데미지를 받을 수 있는 객체 인터페이스
- **IDamager.cs**: 데미지를 주는 객체 인터페이스
- **Projectile.cs**: 발사체 기본 클래스

##### Elemental/Base/
- **Elemental.cs**: 엘리멘탈 기본 클래스. 궤도 이동, 공격, 레벨업, 강화 시스템
- **IElemental.cs**: 엘리멘탈 인터페이스

##### Elemental Types/
각 속성별 엘리멘탈 구현:
- **Flame.cs / Flame.Attack.cs**: 화염 엘리멘탈 및 공격
- **Water.cs / Water.Attack.cs**: 물 엘리멘탈 및 공격
- **Thunder.cs / Thunder.Attack.cs**: 번개 엘리멘탈 및 공격
- **Stone.cs / Stone.Attack.cs**: 돌 엘리멘탈 및 공격
- **Curse.cs / Curse.Attack.cs**: 저주 엘리멘탈 및 공격

#### Enemy/
적 시스템입니다.

- **Enemy.cs**: 적 기본 클래스
- **Dummy.cs**: 더미 적
- **EnemySpawnBase.cs**: 적 스폰 기본 클래스

#### Player/
플레이어 시스템입니다.

- **PlayerController.cs**: 플레이어 컨트롤러. 조이스틱 입력, 이동, 회전, 애니메이션, 체력 관리
- **PlayerBehaviour.cs**: 플레이어 행동 로직
- **PlayerReward.cs**: 플레이어 보상 처리

#### Items/
아이템 시스템입니다.

- **GlobalItem.cs**: 글로벌 아이템 기본 클래스
- **FireGlobalItemObject.cs**: 화염 글로벌 아이템
- **IceGlobalItemObject.cs**: 얼음 글로벌 아이템
- **StoneGlobalItemObject.cs**: 돌 글로벌 아이템
- **ThunderGlobalItemObject.cs**: 번개 글로벌 아이템
- **CurseGlobalItemObject.cs**: 저주 글로벌 아이템
- **LevelUpRewardItem.cs**: 레벨업 보상 아이템

#### ItemShop/
상점 시스템입니다.

- **ItemData.cs**: 아이템 데이터
- **SellableItemInfo.cs**: 판매 가능 아이템 정보
- **ShopItemView.cs**: 상점 아이템 뷰
- **ShopItemConfirmUI.cs**: 상점 아이템 구매 확인 UI
- **MyItemView.cs**: 보유 아이템 뷰
- **ShopUserEliUI.cs**: 유저 Eli(화폐) UI
- **ShopUserStoneUI.cs**: 유저 Stone(화폐) UI
- **CacheCharge.cs**: 캐시 충전

#### Reward/
보상 시스템입니다.

- **RewardUI.cs**: 보상 UI 관리. 점수, 경험치 표시

#### RoundSystem/
라운드 기반 게임 시스템입니다.

- **RoundSystem.cs**: 라운드 시스템 메인 로직. 라운드 진행/전환, 게임오버/클리어, 데이터 저장
- **GameTimer.cs**: 게임 타이머
- **RoundSpawnData.cs**: 라운드 스폰 데이터
- **InGameResultWindowUI.cs**: 인게임 결과 창 UI

##### Upgrade/
업그레이드 시스템입니다.

- **UpgradeTable.cs**: 업그레이드 테이블 관리
- **UpgradeViewer.cs**: 업그레이드 선택 UI
- **BaseUpgrade.cs**: 업그레이드 기본 클래스
- **ElementalUpgrade.cs**: 엘리멘탈 업그레이드
- **GuardUpgrade.cs**: 가드 업그레이드
- **PlayerUpgrade.cs**: 플레이어 업그레이드

---

## 🎮 게임 플로우

### 부팅 플로우
```
BootLoader → Firebase 초기화 → 점검 확인 → 카탈로그 버전 확인
          → Addressable 다운로드 → AddressableSystemManager 갱신
          → MainMenuScreen
```

### 로그인 플로우
```
MainMenuScreen → LoginUI → PFireAuth (Google/Apple 로그인)
               → UserDataManager.SyncWithFirebase
               → Firestore/RealtimeDB 동기화
```

### 게임 시작 플로우
```
StageSelectPanel → InGameScreen → RoundSystem 초기화
                → UpgradeTable 초기화 → Elemental 스폰
                → RoundSystem.StartRound
```

### 라운드 진행
```
RoundSystem → Enemy 스폰 → 전투 → GameTimer 종료
           → UpgradeViewer 표시 → 업그레이드 선택
           → 다음 라운드 or 게임 클리어
```

### 게임오버/클리어
```
RoundSystem.GameOver → 결과 저장 (UserDataManager)
                    → Firebase 동기화 (익명/로그인 유저 분기)
                    → InGameResultWindowUI 표시
```

### 구매 시스템 플로우
```
MainMenuScreen → ItemShop → ShopItemView 선택
              → ShopItemConfirmUI (구매 확인)
              → UserDataManager (Eli/Stone 차감)
              → 아이템 지급
              → Firebase 동기화
```

**캐시 충전 플로우:**
```
ItemShop → CacheCharge → 인앱 결제 처리
        → UserDataManager (Stone 지급)
        → Firebase 동기화
```

### 출석보상 플로우
```
게임 시작 → FirebaseDailyRewardManager 초기화
         → Realtime DB에서 CurrentCycle 확인
         → 사이클 동기화 (필요시)
         → DailyRewardViewer 표시
         → DailyRewardSlot 클릭
         → UserDataManager (마지막 출석일 확인)
         → 보상 지급 (Eli/Stone/GlobalItem)
         → Firebase 동기화
```

**사이클 관리:**
```
FirebaseDailyRewardManager → CurrentCycle 만료 확인
                          → 다음 사이클로 자동 전환
                          → 보상 데이터 재로드
                          → Fallback: 이전/이후 사이클 검색
```

---

## 🌐 플랫폼 지원

- **iOS**: Google Sign-In 네이티브 플러그인, Apple Sign-In
- **Android**: Google Sign-In 



