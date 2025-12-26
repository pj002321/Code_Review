using Cysharp.Threading.Tasks;
using UnityEngine.Networking;

namespace penta
{
    /// <summary>
    /// 출석 보상 뷰어 (주요 로직)
    /// - 출석 체크 처리
    /// - 보상 지급
    /// - 사이클 관리 및 슬롯 생성
    /// </summary>
    public class DailyRewardViewer : MonoBehaviourSingleton<DailyRewardViewer>
    {
        private const int REWARD_COUNT = 14;
        private const int MAX_DAYS_BREAK = 2;

        [Header("UI")]
        [SerializeField] private Transform slotContainer;
        [SerializeField] private DailyRewardSlot slotPrefab;
        [SerializeField] private Button checkInButton;
        [SerializeField] private TextMeshProUGUI totalDaysText;
        [SerializeField] private GameObject anonymousUserNotice;
        [SerializeField] private GameObject networkErrorNotice;

        private List<DailyRewardSlot> dailyMissionSlots = new List<DailyRewardSlot>();
        private DailyRewardData missionData;

        protected override void Awake()
        {
            base.Awake();
            checkInButton.onClick.AddListener(OnCheckInButtonClicked);
        }

        /// <summary> 출석 보상 시스템 초기화 </summary>
        public async UniTask InitializeDailyMission()
        {
            await WaitForUserDataManager();
            await WaitForUserDataInitialization();

            missionData = UserDataManager.Shared.Data.DailyMission;
            if (missionData == null) return;

            CheckAndResetCycle();
            await SyncRewardsFromFirebase();
            await CreateSlots();
            await UpdateCheckInButton();
            UpdateTotalDaysText();
        }

        private async UniTask WaitForUserDataManager()
        {
            if (UserDataManager.Shared == null)
            {
                await UniTask.WaitUntil(() => UserDataManager.Shared != null);
            }
        }

        private async UniTask WaitForUserDataInitialization()
        {
            if (UserDataManager.Shared.Data == null)
            {
                await UniTask.WaitUntil(() => UserDataManager.Shared.IsInitialized);
            }
        }

        /// <summary> 사이클 체크 및 리셋 </summary>
        private void CheckAndResetCycle()
        {
            bool cycleWasReset = false;

            if (missionData.IsCycleComplete())
            {
                ResetCycle("Daily Mission Cycle Reset");
                cycleWasReset = true;
            }
            else if (IsAttendanceBroken())
            {
                ResetCycle("Daily Mission Attendance Broken");
                cycleWasReset = true;
            }

            if (cycleWasReset && IsRewardsEmpty())
            {
                LoadDefaultRewards();
            }
        }

        private bool IsAttendanceBroken()
        {
            if (missionData.LastCheckDate == DateTime.MinValue || missionData.CurrentDay == 0)
            {
                return false;
            }

            DateTime today = DateTime.UtcNow.Date;
            DateTime lastCheck = missionData.LastCheckDate.Date;
            int daysPassed = (today - lastCheck).Days;

            return daysPassed > MAX_DAYS_BREAK;
        }

        private void ResetCycle(string saveReason)
        {
            missionData.ResetCycle();
            UserDataManager.Shared.SaveImportant(saveReason);
        }

        /// <summary> Firebase에서 보상 동기화 </summary>
        private async UniTask SyncRewardsFromFirebase()
        {
            await WaitForFirebaseDailyRewardManager();

            try
            {
                bool wasSynced = await FirebaseDailyRewardManager.Shared.SyncRewardsIfNeededAsync(missionData);

                if (wasSynced)
                {
                    UserDataManager.Shared.SaveImportant("Daily Mission Rewards Synced from Firebase");
                }

                EnsureValidRewards();
            }
            catch (Exception e)
            {
                $"[DailyRewardViewer] Firebase sync failed: {e.Message}".DError();
                LoadDefaultRewards();
            }
        }

        private async UniTask WaitForFirebaseDailyRewardManager()
        {
            if (FirebaseDailyRewardManager.Shared == null)
            {
                await UniTask.WaitUntil(() => FirebaseDailyRewardManager.Shared != null);
            }
        }

        private void EnsureValidRewards()
        {
            if (IsRewardsEmpty() || !IsRewardCountValid())
            {
                LoadDefaultRewards();
            }
        }

        private bool IsRewardsEmpty()
        {
            return missionData.Rewards == null || missionData.Rewards.Count == 0;
        }

        private bool IsRewardCountValid()
        {
            return missionData.Rewards != null && missionData.Rewards.Count == REWARD_COUNT;
        }

        /// <summary> 기본 보상 리스트 로드 </summary>
        private void LoadDefaultRewards()
        {
            if (missionData == null) return;

            if (missionData.Rewards == null)
            {
                missionData.Rewards = new List<DailyReward>();
            }
            missionData.Rewards.Clear();

            InitializeDefaultRewardList();
            UserDataManager.Shared.SaveImportant("Daily Mission Default Rewards Loaded");
        }

        private void InitializeDefaultRewardList()
        {
            missionData.Rewards.Add(new DailyReward(1, DailyRewardType.Eli, 1));
            missionData.Rewards.Add(new DailyReward(2, DailyRewardType.Stone, 50));
            missionData.Rewards.Add(new DailyReward(3, DailyRewardType.Eli, 1));
            missionData.Rewards.Add(new DailyReward(4, DailyRewardType.GlobalItem, 1, ItemType.Potion));
            missionData.Rewards.Add(new DailyReward(5, DailyRewardType.Eli, 2));
            missionData.Rewards.Add(new DailyReward(6, DailyRewardType.Stone, 100));
            missionData.Rewards.Add(new DailyReward(7, DailyRewardType.Eli, 3));
            missionData.Rewards.Add(new DailyReward(8, DailyRewardType.GlobalItem, 1, ItemType.RandomCard));
            missionData.Rewards.Add(new DailyReward(9, DailyRewardType.Eli, 1));
            missionData.Rewards.Add(new DailyReward(10, DailyRewardType.Stone, 150));
            missionData.Rewards.Add(new DailyReward(11, DailyRewardType.Eli, 4));
            missionData.Rewards.Add(new DailyReward(12, DailyRewardType.GlobalItem, 2, ItemType.RandomBox));
            missionData.Rewards.Add(new DailyReward(13, DailyRewardType.Stone, 2));
            missionData.Rewards.Add(new DailyReward(14, DailyRewardType.Eli, 5));
        }

        /// <summary> 보상 슬롯 생성 </summary>
        private async UniTask CreateSlots()
        {
            ClearExistingSlots();

            if (slotPrefab == null || missionData == null) return;

            EnsureRewardsForSlotCreation();

            try
            {
                await CreateMissionSlots();
            }
            catch (Exception e)
            {
                $"[DailyRewardViewer] CreateSlots failed: {e.Message}".DError();
            }
        }

        private void ClearExistingSlots()
        {
            foreach (var slot in dailyMissionSlots)
            {
                if (slot != null)
                {
                    Destroy(slot.gameObject);
                }
            }
            dailyMissionSlots.Clear();

            for (int i = slotContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(slotContainer.GetChild(i).gameObject);
            }
        }

        private void EnsureRewardsForSlotCreation()
        {
            if (IsRewardsEmpty() || !IsRewardCountValid())
            {
                LoadDefaultRewards();
            }
        }

        private async UniTask CreateMissionSlots()
        {
            for (int i = 0; i < missionData.Rewards.Count; i++)
            {
                int dayIndex = i + 1;
                DailyReward reward = missionData.Rewards[i];
                bool isReceived = dayIndex <= missionData.CurrentDay;

                DailyRewardSlot slot = Instantiate(slotPrefab, slotContainer);

                try
                {
                    await slot.InitializeAsync(dayIndex, reward, isReceived);
                    dailyMissionSlots.Add(slot);
                }
                catch (Exception e)
                {
                    $"[DailyRewardViewer] Slot {dayIndex} initialization failed: {e.Message}".DError();
                }
            }
        }

        /// <summary> 출석 체크 버튼 클릭 처리 </summary>
        private void OnCheckInButtonClicked()
        {
            ProcessCheckIn().Forget();
        }

        /// <summary> 출석 체크 처리 </summary>
        private async UniTaskVoid ProcessCheckIn()
        {
            if (!ValidateCheckInPrerequisites())
            {
                return;
            }

            DateTime? serverTime = await GetServerTime();
            if (!serverTime.HasValue)
            {
                await ShowNetworkErrorNotification();
                return;
            }

            if (!CanCheckInToday(serverTime.Value))
            {
                return;
            }

            if (await SaveCheckIn(serverTime.Value))
            {
                CompleteCheckIn(serverTime.Value);
            }
        }

        private bool ValidateCheckInPrerequisites()
        {
            if (UserDataManager.Shared.IsAnonymouseUser())
            {
                ShowLoginRequiredNotification();
                return false;
            }

            if (!IsNetworkAvailable())
            {
                $"[DailyRewardViewer] Network connection required".DError();
                return false;
            }

            return true;
        }

        private async UniTask<DateTime?> GetServerTime()
        {
            try
            {
                if (PentaFirebase.Shared?.PfireStore != null)
                {
                    return await PentaFirebase.Shared.PfireStore.GetServerTimestampAsync();
                }
            }
            catch (Exception e)
            {
                $"[DailyRewardViewer] Failed to get server timestamp: {e.Message}".DError();
            }

            return null;
        }

        private bool CanCheckInToday(DateTime serverTime)
        {
            DateTime serverToday = serverTime.Date;
            DateTime lastCheck = missionData.LastCheckDate.Date;

            if (lastCheck >= serverToday)
            {
                return false;
            }

            return true;
        }

        /// <summary> 출석 체크 저장 </summary>
        private async UniTask<bool> SaveCheckIn(DateTime serverTime)
        {
            DateTime lastCheck = missionData.LastCheckDate;

            try
            {
                missionData.CurrentDay++;
                missionData.TotalAttendanceDays++;
                missionData.LastCheckDate = serverTime;

                bool saveSuccess = await UserDataManager.Shared.SaveImportantAsync($"Daily Mission Check-in Day {missionData.CurrentDay}");

                if (!saveSuccess)
                {
                    RollbackCheckIn(lastCheck);
                    await ShowNetworkErrorNotification();
                    return false;
                }

                return true;
            }
            catch (Exception e)
            {
                RollbackCheckIn(lastCheck);
                $"[DailyRewardViewer] Exception during check-in save: {e.Message}".DError();
                await ShowNetworkErrorNotification();
                return false;
            }
        }

        private void RollbackCheckIn(DateTime lastCheck)
        {
            missionData.CurrentDay--;
            missionData.TotalAttendanceDays--;
            missionData.LastCheckDate = lastCheck;
        }

        /// <summary> 출석 체크 완료 처리 </summary>
        private void CompleteCheckIn(DateTime serverTime)
        {
            GiveReward(missionData.CurrentDay);
            UpdateSlotUI(missionData.CurrentDay);
            UpdateCheckInButton();
            UpdateTotalDaysText();
        }

        /// <summary> 보상 지급 </summary>
        private void GiveReward(int day)
        {
            if (day < 1 || day > missionData.Rewards.Count) return;

            DailyReward reward = missionData.Rewards[day - 1];
            UserData userData = UserDataManager.Shared.Data;

            switch (reward.RewardType)
            {
                case DailyRewardType.Eli:
                    userData.Eli += reward.Amount;
                    break;
                case DailyRewardType.Stone:
                    userData.Stone += reward.Amount;
                    break;
                case DailyRewardType.GlobalItem:
                    userData.Item.AddItem(reward.ItemType, reward.Amount);
                    break;
            }
        }

        /// <summary> 슬롯 UI 업데이트 </summary>
        private void UpdateSlotUI(int day)
        {
            if (day < 1 || day > dailyMissionSlots.Count) return;
            dailyMissionSlots[day - 1].MarkAsReceived();
        }

        /// <summary> 출석 체크 버튼 상태 업데이트 </summary>
        private async UniTask UpdateCheckInButton()
        {
            if (checkInButton == null) return;

            if (!IsNetworkAvailable())
            {
                checkInButton.interactable = false;
                ShowNetworkErrorNotification().Forget();
                return;
            }

            bool canCheckIn = await DetermineCheckInAvailability();
            checkInButton.interactable = canCheckIn;
        }

        private async UniTask<bool> DetermineCheckInAvailability()
        {
            if (PentaFirebase.Shared?.PfireStore == null)
            {
                return false;
            }

            try
            {
                var serverTime = await PentaFirebase.Shared.PfireStore.GetServerTimestampAsync();
                if (serverTime.HasValue)
                {
                    DateTime serverToday = serverTime.Value.Date;
                    DateTime lastCheck = missionData.LastCheckDate.Date;
                    return (lastCheck < serverToday) && !missionData.IsCycleComplete();
                }
            }
            catch
            {
            }

            return missionData.CanCheckToday() && !missionData.IsCycleComplete();
        }

        /// <summary> 누적 출석 일수 텍스트 업데이트 </summary>
        private void UpdateTotalDaysText()
        {
            if (totalDaysText == null) return;

            if (missionData == null)
            {
                totalDaysText.text = "TOTAL : 0 day";
                return;
            }

            totalDaysText.text = $"TOTAL : {missionData.TotalAttendanceDays} day";
        }

        /// <summary> 네트워크 에러 알림 표시 </summary>
        private async UniTask ShowNetworkErrorNotification()
        {
            if (networkErrorNotice == null) return;

            CanvasGroup canvasGroup = GetOrAddCanvasGroup(networkErrorNotice);
            networkErrorNotice.SetActive(true);
            canvasGroup.alpha = 1f;

            await UniTask.Delay(2000);
            await FadeOutNotification(canvasGroup);
            networkErrorNotice.SetActive(false);
        }

        private CanvasGroup GetOrAddCanvasGroup(GameObject target)
        {
            CanvasGroup canvasGroup = target.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = target.AddComponent<CanvasGroup>();
            }
            return canvasGroup;
        }

        private async UniTask FadeOutNotification(CanvasGroup canvasGroup)
        {
            const float fadeDuration = 0.5f;
            float elapsedTime = 0f;

            while (elapsedTime < fadeDuration)
            {
                elapsedTime += Time.deltaTime;
                canvasGroup.alpha = 1f - (elapsedTime / fadeDuration);
                await UniTask.Yield();
            }

            canvasGroup.alpha = 0f;
        }

        private void ShowLoginRequiredNotification()
        {
            if (anonymousUserNotice != null)
            {
                anonymousUserNotice.SetActive(true);
            }
        }

        private bool IsNetworkAvailable()
        {
            return Application.internetReachability != NetworkReachability.NotReachable;
        }

        public int GetCurrentDay()
        {
            return missionData?.CurrentDay ?? 0;
        }

        public bool CanCheckInToday()
        {
            return missionData?.CanCheckToday() ?? false;
        }
    }
}
