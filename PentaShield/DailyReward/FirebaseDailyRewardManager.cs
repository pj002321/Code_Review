using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using penta;
using Firebase.Database;

namespace penta
{
    /// <summary>
    /// Firebase 일일 보상 매니저 (주요 로직)
    /// - Firebase Realtime Database에서 일일 보상 사이클 정보 로드
    /// - 보상 데이터 동기화
    /// </summary>
    public class FirebaseDailyRewardManager : MonoBehaviourSingleton<FirebaseDailyRewardManager>
    {
        private PRealTimeDb realTimeDb;
        private const string CURRENT_CYCLE_PATH = "DailyReward/CurrentCycle";
        private const string CYCLES_PATH = "DailyReward/Cycles";
        private const string DATE_FORMAT = "yyyy-MM-dd";

        protected override void Awake()
        {
            base.Awake();
            InitializeDatabaseAsync().Forget();
        }

        /// <summary> Firebase 초기화 </summary>
        private async UniTaskVoid InitializeDatabaseAsync()
        {
            try
            {
                await EnsureFirebaseReady();
                realTimeDb = PentaFirebase.Shared.PRealTimeDb;
                if (realTimeDb == null || !realTimeDb.IsInitialized)
                {
                    return;
                }
            }
            catch (Exception e)
            {
            }
        }

        private static async UniTask EnsureFirebaseReady()
        {
            if (PentaFirebase.Shared == null)
            {
                await UniTask.WaitUntil(() => PentaFirebase.Shared != null);
            }

            if (!PentaFirebase.Shared.IsInitialized)
            {
                await UniTask.WaitUntil(() => PentaFirebase.Shared.IsInitialized);
            }
        }

        private bool IsDbReady()
        {
            return realTimeDb != null && realTimeDb.IsInitialized;
        }

        private DateTime? ParseDate(string dateString)
        {
            if (string.IsNullOrEmpty(dateString))
                return null;

            if (DateTime.TryParseExact(dateString, DATE_FORMAT, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result))
            {
                return result.Date;
            }

            return null;
        }

        private async UniTask<List<string>> GetAllAvailableCyclesAsync()
        {
            if (!IsDbReady())
                return new List<string>();

            try
            {
                var cyclesData = await realTimeDb.GetDataAsync<Dictionary<string, object>>(CYCLES_PATH);
                if (cyclesData == null)
                    return new List<string>();

                return cyclesData.Keys
                    .OrderBy(key => key)
                    .ToList();
            }
            catch (Exception e)
            {
                return new List<string>();
            }
        }

        /// <summary> 현재 사이클 정보 가져오기 </summary>
        public async UniTask<CurrentCycleInfo> GetCurrentCycleInfoAsync()
        {
            if (!IsDbReady())
            {
                return null;
            }

            try
            {
                var cycleInfo = await realTimeDb.GetDataAsync<CurrentCycleInfo>(CURRENT_CYCLE_PATH);

                if (cycleInfo == null)
                {
                    return null;
                }

                DateTime today = DateTime.UtcNow.Date;
                DateTime? startDate = ParseDate(cycleInfo.startDate);
                DateTime? endDate = ParseDate(cycleInfo.endDate);

                if (startDate.HasValue && endDate.HasValue)
                {
                    await ValidateCycleDateRange(cycleInfo, today, startDate.Value, endDate.Value);
                }

                return cycleInfo;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        private async UniTask ValidateCycleDateRange(CurrentCycleInfo cycleInfo, DateTime today, DateTime startDate, DateTime endDate)
        {
            if (today > endDate)
            {
                ...
            }
        }

        private int ExtractCycleNumber(string cycleId)
        {
            if (string.IsNullOrEmpty(cycleId))
                return 0;

            if (cycleId.StartsWith("cycle_"))
            {
                string numberPart = cycleId.Substring("cycle_".Length);
                if (int.TryParse(numberPart, out int number))
                {
                    return number;
                }
            }

            return 0;
        }

        private int ParseIndex(string key)
        {
            if (int.TryParse(key, out int result))
            {
                return result;
            }
            return int.MaxValue;
        }

        /// <summary> 사이클 보상 목록 가져오기 </summary>
        public async UniTask<List<DailyReward>> GetCycleRewardsAsync(string cycleId)
        {
            if (!IsDbReady())
            {
                return null;
            }

            try
            {
                string rewardsPath = $"{CYCLES_PATH}/{cycleId}/rewards";
                List<FirebaseRewardData> rewardsData = await realTimeDb.GetDataAsync<List<FirebaseRewardData>>(rewardsPath);

                if (rewardsData == null || rewardsData.Count == 0)
                {
                    var rewardsDict = await realTimeDb.GetDataAsync<Dictionary<string, FirebaseRewardData>>(rewardsPath);

                    if (rewardsDict != null && rewardsDict.Count > 0)
                    {
                        rewardsData = rewardsDict
                            .OrderBy(pair => ParseIndex(pair.Key))
                            .Select(pair => pair.Value)
                            .ToList();
                    }
                }

                if (rewardsData == null || rewardsData.Count == 0)
                {
                    return null;
                }

                List<DailyReward> rewards = new List<DailyReward>();

                foreach (var rewardData in rewardsData)
                {
                    if (rewardData == null)
                    {
                        continue;
                    }

                    DailyRewardType rewardType = ParseRewardType(rewardData.item);
                    ItemType itemType = ParseItemType(rewardData.item);

                    rewards.Add(new DailyReward(
                        rewardData.day,
                        rewardType,
                        rewardData.amount,
                        itemType
                    ));
                }

                return rewards;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        /// <summary> 보상 동기화 (필요시) </summary>
        public async UniTask<bool> SyncRewardsIfNeededAsync(DailyRewardData missionData)
        {
            if (missionData == null)
            {
                return false;
            }

            var currentCycleInfo = await GetCurrentCycleInfoAsync();

            if (currentCycleInfo == null)
            {
                return false;
            }

            bool needsSync = NeedsSync(missionData, currentCycleInfo);

            if (!needsSync)
            {
                return false;
            }

            var newRewards = await DownloadRewardsWithFallback(currentCycleInfo);

            if (newRewards == null || newRewards.Count == 0)
            {
                return false;
            }

            missionData.Rewards = newRewards;
            missionData.CurrentCycleId = currentCycleInfo.cycleId;
            missionData.CycleVersion = currentCycleInfo.version;

            return true;
        }

        private bool NeedsSync(DailyRewardData missionData, CurrentCycleInfo currentCycleInfo)
        {
            bool rewardsEmpty = missionData.Rewards == null || missionData.Rewards.Count == 0;

            return rewardsEmpty ||
                   string.IsNullOrEmpty(missionData.CurrentCycleId) ||
                   missionData.CurrentCycleId != currentCycleInfo.cycleId ||
                   missionData.CycleVersion != currentCycleInfo.version;
        }

        private async UniTask<List<DailyReward>> DownloadRewardsWithFallback(CurrentCycleInfo currentCycleInfo)
        {
            var rewards = await GetCycleRewardsAsync(currentCycleInfo.cycleId);

            if (rewards != null && rewards.Count > 0)
            {
                return rewards;
            }

            var allCycles = await GetAllAvailableCyclesAsync();
            int currentCycleNumber = ExtractCycleNumber(currentCycleInfo.cycleId);

            rewards = await TryFindRewardsInCycles(allCycles.Where(c => ExtractCycleNumber(c) > currentCycleNumber).OrderByDescending(ExtractCycleNumber), currentCycleInfo);

            if (rewards != null && rewards.Count > 0)
            {
                return rewards;
            }

            rewards = await TryFindRewardsInCycles(allCycles.Where(c => ExtractCycleNumber(c) < currentCycleNumber).OrderByDescending(ExtractCycleNumber), currentCycleInfo);

            return rewards;
        }

        private async UniTask<List<DailyReward>> TryFindRewardsInCycles(IEnumerable<string> cycleIds, CurrentCycleInfo currentCycleInfo)
        {
            foreach (var cycleId in cycleIds)
            {
                var rewards = await GetCycleRewardsAsync(cycleId);
                if (rewards != null && rewards.Count > 0)
                {
                    currentCycleInfo.cycleId = cycleId;
                    return rewards;
                }
            }

            return null;
        }

        private DailyRewardType ParseRewardType(string itemName)
        {
            if (string.IsNullOrEmpty(itemName)) return DailyRewardType.Eli;
            if (itemName.Equals("Eli", StringComparison.OrdinalIgnoreCase)) return DailyRewardType.Eli;
            if (itemName.Equals("Stone", StringComparison.OrdinalIgnoreCase)) return DailyRewardType.Stone;
            return DailyRewardType.GlobalItem;
        }

        private ItemType ParseItemType(string itemName)
        {
            if (string.IsNullOrEmpty(itemName)) return ItemType.Other;
            if (Enum.TryParse<ItemType>(itemName, true, out ItemType result))
            {
                return result;
            }
            return ItemType.Other;
        }
    }

    [Serializable]
    public class CurrentCycleInfo
    {
        public string cycleId;
        public int version;
        public string startDate;
        public string endDate;
    }

    [Serializable]
    public class FirebaseRewardData
    {
        public int day;
        public string item;
        public int amount;
    }
}

