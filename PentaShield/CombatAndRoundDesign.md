# 전투 및 라운드 설계 (Combat & Round Design)

본 문서는 `PentaShield` 프로젝트의 핵심 전투 흐름, 라운드 시스템, 아이템/적 상태 관리, 그리고 **게임 시간, 보상 및 데이터 저장**의 설계와 구현 방식을 설명합니다.

## 1. 개요 (Overview)

전투는 하나의 긴 흐름이 아닌 **라운드(Wave)** 단위로 진행됩니다. 각 라운드는 명확한 시작과 끝이 있으며, 라운드 종료 시 플레이어에게 보상과 성장의 기회를 제공합니다.

### 핵심 설계 목표
*   **데이터 주도형 전투**: 라운드 별 스폰, 아이템 효과 등이 데이터를 기반으로 동작합니다.
*   **명확한 보상 체계**: 적 처치 시 즉각적인 피드백(Orb)과 라운드 종료 시의 영구적 보상(Data Save)을 분리합니다.
*   **안정적인 데이터 저장**: 전투 결과가 유실되지 않도록 로컬 및 클라우드에 이중으로 저장합니다.

---

## 2. 라운드 및 시간 관리 (Round & Time Management)

### 2.1 라운드 매니저 ([RoundSystem.cs](Contents/RoundSystem/RoundSystem.cs))
게임의 전체적인 수명 주기를 관리하는 중앙 컨트롤러입니다.

| 단계 | 주요 역할 | 관련 메소드 |
| :--- | :--- | :--- |
| **입장 (Entry)** | 스테이지 초기화, UI 세팅, 첫 원소 스폰 | `InitializeAsync` |
| **준비 (Prepare)** | 카운트다운(3초), 스포너 초기화 | `BeginRoundCountdown` |
| **진행 (Battle)** | 제한 시간 체크, 게임 로직 실행 | `StartRound`, `ResumeRound` |
| **종료 (End)** | 결과 저장, 오브젝트 정리, 업그레이드 UI 호출 | `CompleteRound` |

### 2.2 게임 타이머 ([GameTimer.cs](Contents/RoundSystem/GameTimer.cs))
각 라운드의 제한 시간을 관리합니다.
*   **동작 방식**: `RoundSystem`에 의해 시작/정지되며, `Update` 문에서 시간을 체크합니다.
*   **라운드 종료 트리거**: 제한 시간(기본 30초/1분 등)이 지나면 `onTimerComplete` 이벤트를 발생시켜 `RoundSystem.CompleteRound()`를 호출합니다.
*   **UI 동기화**: 남은 시간을 `mm:ss` 포맷으로 변환하여 상단바 UI에 실시간으로 표시합니다.

---

## 3. 적 스폰 및 상태 관리 (Enemy Spawn & State)

### 3.1 적 스폰 시스템 ([EnemySpawnBase.cs](Contents/Enemy/EnemySpawnBase.cs))
`SpawnInfo` 데이터에 기반하여 적을 생성합니다.
*   **SpawnOperation**: [어떤 적]을, [몇 마리], [몇 초 간격]으로 소환할지 정의합니다.
*   **동적 스폰 포인트**: 맵의 바닥(`Renderer.bounds`)을 격자(Grid)로 나누어 안전한 스폰 위치를 계산합니다.

### 3.2 적 상태 관리 (State Management)
적은 다양한 상태를 통해 피격 반응과 군중 제어(CC)를 구현합니다.
*   **피격 (Hit Stun)**: 피격 시 일시적으로 이동을 멈춤 (`StartHitStun`).
*   **석화 (Petrify)**: 행동 정지 + 외형 변화 (Y축 스케일 조정).
*   **넉백 (Knockback)**: 물리 엔진(`Rigidbody`)을 활성화하여 밀려나는 연출.
*   **슬로우 (Slow)**: 이동/애니메이션 속도 감소.

---

## 4. 보상 시스템 (Reward System)

적을 처치하거나 라운드를 클리어했을 때 유저에게 주어지는 보상 체계입니다.

### 4.1 인게임 드랍 (`Enemy.OnDie`)
적이 사망할 때 즉시 보상을 생성합니다.
*   **경험치 오브 (ExperienceOrb)**: 플레이어([Guard](Contents/Player/PlayerController.cs))에게 날아가 경험치를 제공. 레벨업 시 스킬 선택 기회 부여.
*   **코인 오브 (CoinOrb)**: 획득 시 재화(`Gold`) 증가.
*   **점수 (Score)**: 적 처치 시 [RewardUI.cs](Contents/Reward/RewardUI.cs)를 통해 점수 집계 및 UI 갱신.

### 4.2 레벨업 보상 (Level Up Rewards)
플레이어가 경험치를 획득하여 레벨업하면 추가적인 보상이 주어집니다.
*   **체력 증가**: [PlayerReward.cs](Contents/Player/PlayerReward.cs)에서 레벨업 시 플레이어의 최대 체력을 즉시 증가시킵니다.
*   **랜덤 아이템 박스**: [LevelUpRewardItem.cs](Contents/Items/LevelUpRewardItem.cs)를 통해 맵의 지정된 위치 중 한 곳에 **랜덤 아이템 박스**가 생성됩니다. 플레이어는 이를 획득하여 글로벌 아이템(God, Fever 등)이나 추가 자원을 얻을 수 있습니다.

### 4.3 글로벌 아이템 ([GlobalItem.cs](Contents/Items/GlobalItem.cs))
전투 중 사용하여 전황을 바꾸는 특수 스킬입니다.
*   **종류**: God(전체 정지), Fever(무적/공격), Haste(이속 증가), Meteors(광역 공격) 등.
*   **구현**: [GlobalItem.cs](Contents/Items/GlobalItem.cs) 클래스에서 쿨타임과 코루틴을 통해 효과를 제어하며, 게임 오버 시 즉시 정리(`CleanupOnGameOver`)됩니다.

<details>
<summary>📄 GlobalItem.cs (God 아이템 사용 예시) 코드 확인하기</summary>

```csharp
// GlobalItem.cs
public IEnumerator Co_PlayerGod(bool _islevelupreward = false)
{
    if (!_islevelupreward) ReduceItemCount(ItemType.God);
    IsSkillIcon.Shared.OnCreatePlayerIcon(PentaConst.KGIconGod).Forget();
    AudioManager.Shared.PlaySfx(AudioConst.GLOBAL_ITEM_GOD,2f);
    
    // ... (중략) ...

    // Enemy 타입 적들 멈춤
    foreach (var enemy in FindObjectsOfType<Enemy>())
    {
        if (enemy != null && enemy.gameObject != null)
        {
            enemy.SetBehaviourStop();
            // ...
        }
    }

    yield return new WaitForSeconds(waitTimeforEnemyGod);

    // ... (적 행동 재개) ...
}
```

</details>

---

## 5. 데이터 저장 (Data Persistence)

라운드 종료 또는 게임 오버 시, 유저의 진행 상황을 안전하게 저장합니다.

### 5.1 저장 시점 (`RoundSystem.CompleteRound` / `GameOver`)
전투가 끝나는 즉시 저장이 트리거됩니다. 이는 유저가 강제로 앱을 종료하더라도 보상을 잃지 않게 하기 위함입니다.

### 5.2 저장 데이터 (`StageData`)
라운드 결과를 하나의 데이터 객체로 캡슐화하여 저장합니다.
<details>
<summary>📄 UserData.cs (StageData 구조) 코드 확인하기</summary>

```csharp
// UserData.cs에 포함된 StageData
[Serializable]
public class StageData
{
    public int Round;       // 도달한 라운드
    public int Score;       // 획득 점수
    public string StageName; // 플레이한 스테이지
    public DateTime SaveTime; // 저장 시간
}
```

</details>

### 5.3 저장 프로세스
1.  **로컬 저장 + 백업**: [UserDataManager.cs](UserData/UserDataManager.cs)를 통해 로컬 파일에 암호화하여 저장합니다. 이때 백업 파일(`bak`)을 먼저 생성하여 파일 깨짐을 방지합니다.
2.  **클라우드 동기화 (Firebase)**: 로컬 저장이 완료되면 Firebase Firestore에 비동기로 데이터를 업로드합니다.
3.  **UI 갱신**: 저장이 완료되면 `NotifyDataUpdated` 이벤트를 발생시켜 로비나 상점 UI가 최신 재화/점수를 표시하도록 합니다.

---

### 5.4 아이템 사용 기록 저장
아이템 사용은 전투 흐름과 별개로 **사용 즉시 저장**됩니다.
*   **실시간 차감**: [GlobalItem.cs](Contents/Items/GlobalItem.cs)을 사용하여 아이템 개수가 줄어들면, 즉시 `UserDataManager.UpdateUserDataAsync()`가 호출되어 로컬 및 DB에 반영됩니다.
*   **이유**: 게임 강제 종료 등으로 인해 사용한 아이템이 복구되거나 소모되지 않는 어뷰징을 방지하기 위함입니다.
*   **구조**: [UserData.cs](UserData/UserData.cs)의 `ItemData` (JSON) -> `ModifyItemCount` -> `Auto Save`

---

## 6. 최적화 및 안정성

*   **오브젝트 풀링/정리**: 라운드 종료 시 `DestroyAllSpawnedEnemies`를 통해 필드의 모든 적과 아이템을 정리하여 메모리 누수를 방지합니다.
*   **안전한 코루틴 종료**: 씬 전환이나 리스타트 시 `GlobalItem`과 `RoundSystem`에서 실행 중인 모든 비동기 작업(`UniTask`, `Coroutine`)을 취소하여 오류를 예방합니다.
