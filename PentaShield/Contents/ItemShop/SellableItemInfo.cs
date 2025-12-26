using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using penta;

namespace penta
{
    /// <summary>
    /// 판매 가능한 아이템 정보 (주요 로직)
    /// - 아이템 정보 및 가격 관리
    /// - 구매 가능 여부 확인
    /// </summary>
    public class SellableItemInfo : MonoBehaviour
    {
        public ItemType itemType;

        [Header("ITEM INFO")]
        public int itemId;
        public int itemCounts;
        public Image itemSprite;
        [TextArea(2, 4)]
        public string description;
        public int currentPurchaseCount = 1;
        [SerializeField] private IsPrice price;

        [Header("EVENTS")]
        public UnityEvent<SellableItemInfo> OnItemClicked;

        private void Awake()
        {
            if (price == null)
            {
                price = GetComponentInChildren<IsPrice>();
            }
            currentPurchaseCount = itemCounts;

            Button button = GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(() => OnItemClicked?.Invoke(this));
            }
        }

        public int GetPurchaseCount() => currentPurchaseCount;
        public int GetEliCost() => price?.eliPrice ?? 0;
        public int GetStoneCost() => price?.stonePrice ?? 0;
        public bool CanPurchase() => true;
    }
}
