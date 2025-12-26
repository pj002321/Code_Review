using Firebase.Firestore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace chaos
{
    /// <summary> 출석 보상 타입 </summary>
    public enum DailyRewardType
    {
        Eli,
        Stone,
        GlobalItem
    }

    /// <summary> 하루치 출석 보상 정보 </summary>
    [Serializable]
    [FirestoreData]
    public class DailyReward
    {
        [FirestoreProperty]
        [JsonProperty("Day")] public int Day { get; set; }

        [FirestoreProperty]
        [JsonProperty("RewardType")] public DailyRewardType RewardType { get; set; }

        [FirestoreProperty]
        [JsonProperty("ItemType")] public ItemType ItemType { get; set; }

        [FirestoreProperty]
        [JsonProperty("Amount")] public int Amount { get; set; }

        public DailyReward()
        {
        }

        public DailyReward(int day, DailyRewardType rewardType, int amount, chaos.ItemType itemType = chaos.ItemType.Other)
        {
            Day = day;
            RewardType = rewardType;
            Amount = amount;
            ItemType = itemType;
        }
    }

    /// <summary>
    /// 출석 미션 데이터
    /// - 출석 체크 및 사이클 관리
    /// - 보상 리스트 관리
    /// </summary>
    [Serializable]
    [FirestoreData]
    public class DailyRewardData
    {
        [FirestoreProperty]
        [JsonProperty("LastCheckDate")] public DateTime LastCheckDate { get; set; }

        [FirestoreProperty]
        [JsonProperty("CurrentDay")] public int CurrentDay { get; set; }

        [FirestoreProperty]
        [JsonProperty("TotalAttendanceDays")] public int TotalAttendanceDays { get; set; }

        [FirestoreProperty]
        [JsonProperty("CycleStartDate")] public DateTime CycleStartDate { get; set; }

        [FirestoreProperty]
        [JsonProperty("CurrentCycleId")] public string CurrentCycleId { get; set; }

        [FirestoreProperty]
        [JsonProperty("CycleVersion")] public int CycleVersion { get; set; }

        [FirestoreProperty]
        [JsonProperty("Rewards")] public List<DailyReward> Rewards { get; set; }

        public DailyRewardData()
        {
            LastCheckDate = DateTime.MinValue;
            CurrentDay = 0;
            TotalAttendanceDays = 0;
            CycleStartDate = DateTime.UtcNow;
            CurrentCycleId = "";
            CycleVersion = 0;
            Rewards = new List<DailyReward>();
        }

        /// <summary> 오늘 출석 체크 가능 여부 확인 </summary>
        public bool CanCheckToday()
        {
            DateTime today = DateTime.UtcNow.Date;
            DateTime lastCheck = LastCheckDate.Date;
            return lastCheck < today;
        }

        /// <summary> 연속 출석 여부 확인 </summary>
        public bool IsContinuous()
        {
            DateTime today = DateTime.UtcNow.Date;
            DateTime yesterday = today.AddDays(-1);
            DateTime lastCheck = LastCheckDate.Date;
            return lastCheck == yesterday;
        }

        /// <summary> 2주 사이클 완료 여부 확인 </summary>
        public bool IsCycleComplete()
        {
            return CurrentDay >= 14;
        }

        /// <summary> 사이클 초기화 </summary>
        public void ResetCycle()
        {
            CurrentDay = 0;
            CycleStartDate = DateTime.UtcNow;
            CurrentCycleId = "";
            CycleVersion = 0;
            Rewards.Clear();
        }
    }
}
