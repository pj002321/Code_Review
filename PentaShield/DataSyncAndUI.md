# 데이터 비동기 저장과 UI 동기화 (Data Sync & UI Synchronization)

본 문서는 `PentaShield` 프로젝트에서 사용자 데이터의 비동기 저장 처리와 그에 따른 UI 동기화 문제를 해결한 방식(Observer Pattern)에 대해 설명합니다.

## 1. 문제 상황 (Problem)

### 현상
*   게임 내 활동(상점 구매, 아이템 획득 등)으로 인해 사용자 데이터(`UserData`)가 변경됩니다.
*   데이터 변경 후 **저장(I/O)**과 **UI 갱신**이 필요합니다.
*   저장 작업(로컬 파일 암호화/쓰기 + Firebase 네트워크 통신)은 시간이 소요되는 **비동기 작업**입니다.

### 문제점
1.  **UI 반응성 저하**: 저장이 완료될 때까지 기다렸다가 UI를 갱신하면 게임이 멈추거나 반응이 느려집니다.
2.  **데이터 불일치**: 각 UI가 개별적으로 데이터를 조회하여 갱신할 경우, 갱신 시점에 따라 서로 다른 값을 표시할 위험이 있습니다 (예: 상단 재화 바는 차감되었는데, 인벤토리는 그대로인 경우).
3.  **코드 복잡도 증가**: 데이터가 변경되는 모든 지점에서 관련된 모든 UI(로비, 상점, 인벤토리 등)를 직접 참조하여 갱신 함수를 호출하는 것은 유지보수가 어렵습니다.

---

## 2. 해결 방안 (Solution)

### 핵심 아키텍처: Observer Pattern & Async Queue
데이터 변경과 저장을 분리하고, 이벤트 기반으로 UI를 동기화하여 문제를 해결했습니다.

1.  **단일 진실 공급원 (Single Source of Truth)**: [UserDataManager.cs](UserData/UserDataManager.cs)가 관리하는 [UserData.cs](UserData/UserData.cs) 객체가 유일한 원본입니다.
2.  **Observer Pattern (UI 동기화)**: 데이터가 변경되면 `OnDataUpdated` 이벤트를 발행합니다. UI들은 이 이벤트를 구독하고 있다가, 이벤트 발생 시 즉시 자신의 최신 상태로 갱신합니다.
3.  **비동기 저장 큐 (Async Save Queue)**: 저장 요청은 `SaveRequest`로 캡슐화되어 큐(`Queue`)에 쌓이고, 별도의 비동기 루프(`ProcessSaveQueue`)에서 순차적으로 처리됩니다.

---

## 3. 상세 구현 (Implementation)

### 3.1 데이터 매니저 (Subject)

[UserDataManager.cs](UserData/UserDataManager.cs)는 데이터 변경 알림을 담당하는 주체입니다.

<details>
<summary>📄 UserDataManager.cs 코드 확인하기</summary>

```csharp
// UserDataManager.cs
public class UserDataManager : MonoBehaviourSingleton<UserDataManager>
{
    // 데이터 변경 이벤트 정의
    public event Action<UserData> OnDataUpdated;

    // 데이터 변경 알림 메서드
    public void NotifyDataUpdated()
    {
        if (Data == null) return;
        OnDataUpdated?.Invoke(Data);
    }
    
    // 저장 요청 (비동기 큐에 추가)
    public void SaveImportant(string reason)
    {
        saveQueue.Enqueue(new SaveRequest { ... });
        ProcessSaveQueue().Forget();
    }
}
```

</details>

### 3.2 UI 컴포넌트 (Observer)

각 UI는 활성화(`OnEnable`) 시 이벤트를 구독하고, 비활성화(`OnDisable`) 시 구독을 해제합니다.

#### 예시: 재화 표시 UI ([ShopGameMoneyUIBase.cs](Contents/ItemShop/ShopGameMoneyUIBase.cs))
<details>
<summary>📄 ShopGameMoneyUIBase.cs 코드 확인하기</summary>

```csharp
// ShopGameMoneyUIBase.cs
private void OnEnable()
{
    // 이벤트 구독
    if (UserDataManager.Shared != null)
    {
        UserDataManager.Shared.OnDataUpdated += HandleUserDataUpdated;
    }
    // 초기화 시 한번 갱신
    UpdateText().Forget();
}

private void OnDisable()
{
    // 이벤트 구독 해제 (메모리 누수 방지)
    if (UserDataManager.Shared != null)
    {
        UserDataManager.Shared.OnDataUpdated -= HandleUserDataUpdated;
    }
}

// 이벤트 핸들러: 데이터가 변경되면 호출됨
private void HandleUserDataUpdated(UserData _)
{
    UpdateText().Forget(); // 최신 값으로 UI 갱신
}
```

</details>

### 3.3 비동기 저장 프로세스

UI 갱신과 별개로 저장은 백그라운드에서 실행됩니다.

1.  **요청**: `UserDataManager.Shared.SaveImportant("Item Purchase")` 호출.
2.  **큐잉**: `SaveRequest` 객체가 큐에 추가됨.
3.  **처리**: `ProcessSaveQueue`가 돌면서 순차적으로 로컬 저장(암호화) 및 클라우드 동기화를 수행.
4.  **결과**: 저장 성공/실패 여부가 로그로 남지만, UI는 이미 갱신되었으므로 유저는 대기할 필요가 없음.

---

## 4. 개선된 프로세스 흐름

| 단계 | 동작 | 스레드/타이밍 |
| :--- | :--- | :--- |
| **1. Action** | 유저가 아이템 구매 버튼 클릭 | Main Thread |
| **2. Data Update** | `UserData.Stone -= cost;` (메모리 상 데이터 즉시 변경) | Main Thread |
| **3. UI Notify** | `NotifyDataUpdated()` 호출 -> 모든 구독 UI 즉시 갱신 | Main Thread (즉시) |
| **4. Save Request** | `SaveImportant()` 호출 -> 큐에 저장 요청 추가 | Main Thread |
| **5. Async Save** | 큐에서 요청 꺼내어 파일 쓰기 및 네트워크 전송 | **Background (Async)** |

## 5. 결론

이 구조를 통해 다음과 같은 이점을 얻었습니다.
*   **반응성**: 저장 지연 없이 UI가 즉각 반응합니다.
*   **일관성**: 모든 UI가 동일한 이벤트(`OnDataUpdated`)를 통해 갱신되므로 데이터 불일치가 사라졌습니다.
*   **결합도 감소**: 상점 로직이 인벤토리 UI를 알 필요가 없습니다. 오로지 데이터만 수정하면 됩니다.
