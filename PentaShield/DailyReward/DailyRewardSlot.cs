using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PentaShield;
using Cysharp.Threading.Tasks;

namespace chaos
{
    /// <summary>
    /// 출석 보상 슬롯 UI
    /// - 보상 정보 표시 (아이콘, 일수, 수량)
    /// - 수령 완료 상태 표시
    /// </summary>
    public class DailyRewardSlot : MonoBehaviour
    {
        [Header("SLOT UI")]
        [SerializeField] private Image iconImg;
        [SerializeField] private TextMeshProUGUI dayText;
        [SerializeField] private TextMeshProUGUI itemCountText;
        [Header("CLAIMED OVERLAY")]
        [SerializeField] private Image overlayImg;
        [SerializeField] private GameObject checkMarkIcon;

        private int day;
        private DailyReward rewardData;
        private bool isReceived = false;

        /// <summary> 슬롯 초기화 </summary>
        public async UniTask InitializeAsync(int dayIndex, DailyReward reward, bool received)
        {
            day = dayIndex;
            rewardData = reward;
            isReceived = received;
            await UpdateSlotUIAsync();
        }

        /// <summary> 슬롯 UI 업데이트 </summary>
        private async UniTask UpdateSlotUIAsync()
        {
            if (dayText != null)
            {
                dayText.text = $"{day} Day";
            }

            if (itemCountText != null && rewardData != null)
            {
                itemCountText.text = $"x{rewardData.Amount}";
            }

            if (iconImg != null && rewardData != null)
            {
                Sprite rewardSprite = await LoadRewardSpriteAsync(rewardData);
                if (rewardSprite != null)
                {
                    iconImg.sprite = rewardSprite;
                }
            }

            if (overlayImg != null)
            {
                overlayImg.gameObject.SetActive(isReceived);
            }

            if (checkMarkIcon != null)
            {
                checkMarkIcon.SetActive(isReceived);
            }
        }

        /// <summary> 수령 완료 상태로 표시 </summary>
        public void MarkAsReceived()
        {
            isReceived = true;
            UpdateSlotUIAsync().Forget();
        }

        public DailyReward GetRewardData()
        {
            return rewardData;
        }

        public int GetDay()
        {
            return day;
        }

        /// <summary> 보상 스프라이트 로드 </summary>
        private async UniTask<Sprite> LoadRewardSpriteAsync(DailyReward reward)
        {
            if (reward == null) return null;

            string spriteKey = GetSpriteKey(reward);
            if (string.IsNullOrEmpty(spriteKey)) return null;

            return await AbHelper.Shared.LoadAssetAsync<Sprite>(spriteKey);
        }

        /// <summary> 보상 타입별 스프라이트 키 반환 </summary>
        private string GetSpriteKey(DailyReward reward)
        {
            if (reward == null) return null;

            switch (reward.RewardType)
            {
                case DailyRewardType.Eli:
                    return PentaConst.kIconEli;
                case DailyRewardType.Stone:
                    return PentaConst.kIconStone;
                case DailyRewardType.GlobalItem:
                    return GetGlobalItemSpriteKey(reward.ItemType);
                default:
                    return null;
            }
        }

        private string GetGlobalItemSpriteKey(ItemType itemType)
        {
            switch (itemType)
            {
                case ItemType.RandomBox:
                    return PentaConst.KGIconRandomBox;
                case ItemType.Potion:
                    return PentaConst.KGIconHeal;
                case ItemType.Haste:
                    return PentaConst.KGIconHaste;
                case ItemType.God:
                    return PentaConst.KGIconGod;
                case ItemType.RandomCard:
                    return PentaConst.KGIconRandomCard;
                case ItemType.Fiver:
                    return PentaConst.KGIconFever;
                default:
                    return null;
            }
        }
    }
}
