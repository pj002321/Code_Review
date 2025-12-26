using Firebase.Firestore;
using Newtonsoft.Json;
using System;

namespace penta
{
    /// <summary> 아이템 타입 열거형 </summary>
    public enum ItemType
    {
        RandomBox,
        Potion,
        Haste,
        God,
        RandomCard,
        Fiver,
        RandomCacheBox,
        GoldenBox,
        Eli,
        Stone,
        Other
    }

    /// <summary>
    /// 아이템 데이터 관리 (주요 로직)
    /// - 아이템 개수 조회/수정
    /// - 아이템 추가/사용/설정
    /// </summary>
    [FirestoreData]
    [Serializable]
    public class ItemData
    {
        [FirestoreProperty]
        [JsonProperty("RandomBox")] public int randomboxes { get; set; }

        [FirestoreProperty]
        [JsonProperty("Potion")] public int potions { get; set; }

        [FirestoreProperty]
        [JsonProperty("Haste")] public int hastes { get; set; }

        [FirestoreProperty]
        [JsonProperty("God")] public int gods { get; set; }

        [FirestoreProperty]
        [JsonProperty("RandomCard")] public int randomCard { get; set; }

        [FirestoreProperty]
        [JsonProperty("Fever")] public int fever { get; set; }

        [FirestoreProperty]
        [JsonProperty("RandomCacheBox")] public int randomCacheBox { get; set; }

        [FirestoreProperty]
        [JsonProperty("GoldenBox")] public int goldenBox { get; set; }

        public event Action<ItemType, int> OnItemCountChanged;

        public ItemData()
        {
        }

        /// <summary> 아이템 개수 조회 </summary>
        public int GetItemCount(ItemType _type)
        {
            return _type switch
            {
                ItemType.RandomBox => randomboxes,
                ItemType.Potion => potions,
                ItemType.Haste => hastes,
                ItemType.God => gods,
                ItemType.RandomCard => randomCard,
                ItemType.Fiver => fever,
                ItemType.RandomCacheBox => randomCacheBox,
                ItemType.GoldenBox => goldenBox,
                ItemType.Other => -1,
                _ => -1
            };
        }

        /// <summary> 아이템 개수 수정 </summary>
        public bool ModifyItemCount(ItemType type, int amount, bool allowNegative = false)
        {
            int currentCount = GetItemCount(type);
            int resultCount = currentCount + amount;

            if (!allowNegative && resultCount < 0)
            {
                return false;
            }

            switch (type)
            {
                case ItemType.RandomBox:
                    randomboxes += amount;
                    break;
                case ItemType.Potion:
                    potions += amount;
                    break;
                case ItemType.Haste:
                    hastes += amount;
                    break;
                case ItemType.God:
                    gods += amount;
                    break;
                case ItemType.RandomCard:
                    randomCard += amount;
                    break;
                case ItemType.Fiver:
                    fever += amount;
                    break;
                case ItemType.RandomCacheBox:
                    randomCacheBox += amount;
                    break;
                case ItemType.GoldenBox:
                    goldenBox += amount;
                    break;
                case ItemType.Other:
                default:
                    return false;
            }

            OnItemCountChanged?.Invoke(type, GetItemCount(type));
            return true;
        }

        /// <summary> 아이템 추가 </summary>
        public bool AddItem(ItemType type, int amount)
        {
            if (amount < 0) return false;
            return ModifyItemCount(type, amount);
        }

        /// <summary> 아이템 사용 </summary>
        public bool UseItem(ItemType type, int amount)
        {
            if (amount < 0) return false;
            return ModifyItemCount(type, -amount);
        }

        /// <summary> 아이템 개수 설정 </summary>
        public bool SetItemCount(ItemType type, int count)
        {
            if (count < 0) return false;

            int currentCount = GetItemCount(type);
            int difference = count - currentCount;
            return ModifyItemCount(type, difference, false);
        }
    }
}
