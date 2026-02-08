# 전투 및 라운드 설계 (Combat & Round Design)

본 문서는 `PentaShield` 프로젝트의 핵심 전투 흐름과 라운드 시스템의 설계 및 구현 방식을 설명합니다.

## 1. 개요 (Overview)

전투는 하나의 긴 흐름이 아닌 **라운드(Wave)** 단위로 진행됩니다. 각 라운드는 명확한 시작과 끝이 있으며, 라운드 종료 시 플레이어에게 보상과 성장의 기회를 제공하여 진행감을 부여합니다.

### 핵심 설계 목표
*   **명확한 페이즈 구분**: 준비 -> 전투 -> 보상 -> 업그레이드 -> 다음 전투
*   **데이터 주도 스폰**: 하드코딩된 로직이 아닌, 데이터(CSV/ScriptableObject)에 기반한 적 스폰
*   **리소스 관리**: 오브젝트 풀링과 비동기 로딩을 통해 모바일 환경에서의 성능 최적화

---

## 2. 주요 시스템 구조

### 2.1 라운드 시스템 ([RoundSystem.cs](Contents/RoundSystem/RoundSystem.cs))
게임의 전체적인 수명 주기를 관리하는 중앙 컨트롤러입니다.

| 단계 | 주요 역할 | 관련 메소드 |
| :--- | :--- | :--- |
| **입장 (Entry)** | 스테이지 초기화, UI 세팅, 첫 원소 스폰 | `InitializeAsync` |
| **준비 (Prepare)** | 카운트다운, 스포너 초기화 | `BeginRoundCountdown`, `PrepareRoundSpawn` |
| **진행 (Battle)** | 제한 시간 체크, 게임 로직 실행 | `StartRound`, `ResumeRound` |
| **종료 (End)** | 결과 저장, 오브젝트 정리, 업그레이드 UI 호출 | `CompleteRound`, `CleanupGameSession` |

<details>
<summary>📄 RoundSystem.cs 코드 확인하기</summary>

```csharp
// RoundSystem.cs
private async UniTask CompleteRound()
{
    if (!IsRoundActive || OngameOver) return;

    IsRoundActive = false;
    SetGamePaused(true); // 게임 일시정지 (TimeScale = 0)

    // 1. 이벤트 전파 (스포너 정리 등)
    OnRoundEnd?.Invoke(currentRound);

    // 2. 다음 라운드 데이터 확인
    if (!ValidateNextRound())
    {
        await TriggerGameComplete(); // 게임 클리어
        return;
    }

    // 3. 업그레이드 화면 표시
    ShowUpgradeScreen();
}
```

</details>

### 2.2 시간 관리 ([GameTimer.cs](Contents/RoundSystem/GameTimer.cs))
라운드의 제한 시간을 관리하며, 시간이 종료되면 라운드 승리 처리를 돕습니다.

*   `RoundSystem`이 `Stop/Resume`을 제어합니다.
*   타이머가 종료(`onTimerComplete`)되면 `RoundSystem.CompleteRound()`를 호출합니다.

### 2.3 적 스폰 시스템 ([EnemySpawnBase.cs](Contents/Enemy/EnemySpawnBase.cs))
각 라운드 별로 정의된 스폰 테이블(`SpawnInfo`)에 따라 적을 생성합니다.

*   **SpawnOperation**: [어떤 적]을, [몇 마리], [몇 초 간격]으로 소환할지 정의한 단위 데이터입니다.
*   **동적 스폰 포인트**: 맵의 바닥 면적(`Renderer.bounds`)을 계산하여 그리드 형태로 안전한 스폰 위치를 자동 생성합니다.

<details>
<summary>📄 EnemySpawnBase.cs 코드 확인하기</summary>

```csharp
// EnemySpawnBase.cs
protected virtual void Update()
{
    foreach (SpawnOperation op in SpawnInfo.SpawnOperations)
    {
        op.TimerUpdate(Time.deltaTime);
        if (op.IsSpawnable)
        {
            // 스폰 실행 (Object Pooling 권장)
            Instantiate(op.SpawnPrefab, ...);
            op.curSpawnCount++;
            op.TimerReset();
        }
    }
}
```

</details>

---

## 3. 최적화 및 안정성 (Optimization & Stability)

### 3.1 오브젝트 생명주기 관리
모바일 환경에서의 성능 스파이크를 방지하기 위해 생성/파괴 비용을 최소화해야 합니다.

*   **스폰 정리 (`DestroyAllSpawnedEnemies`)**: 
    라운드 종료, 재시작, 게임 오버 시 `RoundSystem`이 필드에 남아있는 모든 적과 투사체, 이펙트를 정리합니다.
    `EnemySpawnBase`를 상속받은 모든 스포너의 `DestroySpawnedObject`를 호출하여 깔끔하게 제거합니다.

### 3.2 안전한 코루틴/비동기 관리
*   게임 종료나 씬 전환 시 실행 중인 코루틴이 오류를 일으키지 않도록 `RoundSystem`에서 중앙 관리합니다.
*   비동기 작업(`UniTask`) 사용 시 `OnDestroy`나 `Cancellation` 토큰을 체크하여 안전하게 취소합니다.

---

## 4. 데이터 저장 흐름

라운드 종료 시점은 유저 데이터가 저장되는 중요한 타이밍입니다.

1.  **라운드 클리어/실패**
2.  `RoundSystem.SaveGameResult()` 호출
3.  **스테이지 데이터 생성**: 현재 라운드, 점수, 플레이 타임 기록
4.  **유저 데이터 갱신**: [UserData.cs](UserData/UserData.cs)의 `StageDatas`에 추가하고 `HighestWave` 갱신
5.  **영구 저장**: 로컬 파일 암호화 저장 및 Firebase 클라우드 동기화 수행 (`PersistUserData`)

---

## 5. 결론

본 설계는 전투의 호흡을 라운드 단위로 끊어가며 유저에게 명확한 목표(다음 웨이브)와 보상(업그레이드)을 제공합니다. 또한 시스템적으로는 스폰과 게임 로직을 분리하고, 중앙(`RoundSystem`)에서 상태를 철저히 관리하여 안정성을 확보했습니다.
