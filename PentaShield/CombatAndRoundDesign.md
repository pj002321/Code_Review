# 전투 및 라운드 설계 (Combat & Round Design)

본 문서는 `PentaShield` 프로젝트의 핵심 전투 흐름, 라운드 시스템, 글로벌 아이템 및 적 상태 관리의 설계와 구현 방식을 설명합니다.

## 1. 개요 (Overview)

전투는 하나의 긴 흐름이 아닌 **라운드(Wave)** 단위로 진행됩니다. 각 라운드는 명확한 시작과 끝이 있으며, 라운드 종료 시 플레이어에게 보상과 성장의 기회를 제공하여 진행감을 부여합니다.

### 핵심 설계 목표
*   **명확한 페이즈 구분**: 준비 -> 전투 -> 보상 -> 업그레이드 -> 다음 전투
*   **데이터 주도 스폰**: 하드코딩된 로직이 아닌, 데이터(CSV/ScriptableObject)에 기반한 적 스폰
*   **전략적 아이템 사용**: 글로벌 아이템을 통해 전황을 뒤집는 변수 창출
*   **상태 기반 적 AI**: 피격, 상태 이상(석화, 정지 등)에 따른 제어

---

## 2. 라운드 시스템 구조

### 2.1 라운드 매니저 (`RoundSystem`)
게임의 전체적인 수명 주기를 관리하는 중앙 컨트롤러입니다.

| 단계 | 주요 역할 | 관련 메소드 |
| :--- | :--- | :--- |
| **입장 (Entry)** | 스테이지 초기화, UI 세팅, 첫 원소 스폰 | `InitializeAsync` |
| **준비 (Prepare)** | 카운트다운, 스포너 초기화 | `BeginRoundCountdown`, `PrepareRoundSpawn` |
| **진행 (Battle)** | 제한 시간 체크, 게임 로직 실행 | `StartRound`, `ResumeRound` |
| **종료 (End)** | 결과 저장, 오브젝트 정리, 업그레이드 UI 호출 | `CompleteRound`, `CleanupGameSession` |

### 2.2 적 스폰 시스템 (`EnemySpawnBase`)
각 라운드 별로 정의된 스폰 테이블(`SpawnInfo`)에 따라 적을 생성합니다.
*   **SpawnOperation**: [어떤 적]을, [몇 마리], [몇 초 간격]으로 소환할지 정의한 단위 데이터
*   **동적 스폰 포인트**: 맵의 바닥 면적(`Renderer.bounds`)을 계산하여 그리드 형태로 안전한 스폰 위치 자동 생성

---

## 3. 글로벌 아이템 (Global Item)

플레이어가 전투 중 사용하여 전황을 유리하게 이끄는 특수 스킬 시스템입니다. `GlobalItem` 싱글톤 클래스가 쿨타임과 실행 로직을 관리합니다.

### 주요 아이템 및 효과
| 아이템 | 효과 설명 | 구현 로직 (`GlobalItem.cs`) |
| :--- | :--- | :--- |
| **God (신성)** | 모든 적의 행동을 일정 시간 정지 | `Co_PlayerGod`: 모든 `Enemy`/`Dummy`를 찾아 `SetBehaviourStop()` 호출 |
| **Fever (피버)** | 무적 + 체력 증가 + 원소 회전 공격 | `Co_PlayerFever`: Player Status 변경 및 `FeverElementalController` 생성 |
| **Haste (신속)** | 플레이어 이동 속도 증가 | `Co_PlayerHaste`: `PlayerController.moveSpeed` 2배 증가 |
| **Heal (회복)** | 체력 지속 회복 | `Co_PlayerHeal`: 코루틴으로 일정 시간 동안 체력 선형 보간 회복 |
| **Ice/Fire Meteo** | 광역 공격 (빙결/화염) | `Co_IceMeteo`/`Co_FireMeteo`: 적 위치 또는 랜덤 위치에 투사체 낙하 |
| **Thunder** | 단일 적 벼락 공격 | `Co_ThuderCrash`: 화면 내 모든 적에게 즉시 벼락 오브젝트 생성 |
| **MultiCurse** | 다수 적 저주 부여 | `Co_MultiCurse`: 랜덤한 적들에게 저주 오브젝트 부착 |

### 아이템 오브젝트 (예: `FireGlobalItemObject`)
*   **낙하 및 폭발**: 사선으로 낙하하여 지면이나 적 충돌 시 폭발 (`OnTriggerEnter`)
*   **DOT 데미지**: 폭발 후 장판을 생성하여 범위 내 적에게 지속 피해 부여 (`StartDotDamage` 코루틴)

---

## 4. 적 상태 관리 (Enemy State Management)

`Enemy` 클래스는 단순한 이동/공격 외에 다양한 상태(State)를 관리하여 피격감과 군중 제어(CC) 효과를 구현합니다.

### 4.1 피격 및 히트 스턴 (Hit Stun)
적이 공격을 받았을 때의 반응입니다.
*   **OnHit**: 데미지 처리, 피격 사운드, 피격 이펙트(Flash) 재생.
*   **Hit Stun**: 일반(`Normal`) 또는 번개(`Thunder`) 공격 피격 시, `StartHitStun`을 호출하여 일시적으로 이동(`speed = 0`)을 멈춥니다. 단, DOT 데미지에는 반응하지 않습니다.

### 4.2 상태 이상 (Status Effects)
글로벌 아이템이나 스킬에 의해 부여되는 특수 상태입니다.

*   **행동 정지 (Stop)**: `SetBehaviourStop()`
    *   이동 속도와 애니메이션 속도를 0으로 설정.
    *   God 아이템 사용 시 적용됨.
*   **석화 (Petrify)**: `StartPetrify()`
    *   행동 정지 상태가 되며, Y축 스케일을 조절하여 돌처럼 굳은 연출 적용.
    *   지속 시간 종료 후 원래 상태로 복구.
*   **넉백 (Knockback)**: `StartKnockback()`
    *   `Kinematic`을 끄고 물리(`Rigidbody`)를 활성화하여 물리력에 의해 밀려나도록 처리.
    *   다시 땅에 착지(`OnLandedAfterKnockback`)하면 물리 비활성화 및 복귀.
*   **슬로우 (Slow)**: `SlowMove()`
    *   이동 속도와 애니메이션 속도를 50%로 감소.

### 4.3 사망 처리 (Death)
`OnDie` 실행 시:
1.  **Orbs Spawn**: 경험치(`ExperienceOrb`)와 코인(`CoinOrb`)을 주변에 흩뿌림.
2.  **Score**: 점수 집계 UI(`RewardUI`)에 점수 반영.
3.  **Clean Up**: 씬에서 오브젝트 제거.

---

## 5. 최적화 및 안정성 (Optimization & Stability)

### 5.1 오브젝트 정리 (Cleanup)
*   **게임 오버/종료 시**: `GlobalItem.CleanupOnGameOver`가 호출되어 실행 중인 모든 스킬 코루틴(`UniTask` 포함)을 취소하고, 소환된 스킬 오브젝트(메테오 등)를 즉시 파괴합니다.
*   **라운드 종료 시**: `RoundSystem`이 맵에 남은 모든 적을 찾아 파괴하여 다음 라운드를 쾌적하게 시작할 수 있도록 합니다.

### 5.2 안전한 비동기 처리
*   `CancellationToken`을 사용하여 씬이 파괴되거나 게임이 종료될 때 `UniTask` 기반의 스킬 로직(메테오 생성 루프 등)이 안전하게 중단되도록 구현되었습니다.
