using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using penta;
using Cysharp.Threading.Tasks;

namespace penta
{
    /// <summary>
    /// 상점 아이템 구매 확인 UI (주요 로직)
    /// - 아이템 구매 처리
    /// - 구매 수량 조절
    /// - 가격 계산 및 표시
    /// </summary>
    public class ShopItemConfirmUI : MonoBehaviour
    {
        private const string ZeroText = "0";

        public event Action onPurchaseSuccess;

        [Header("NOTIY UI")]
        [SerializeField] private ShopGameMoneyUIBase eliUI;
        [SerializeField] private ShopGameMoneyUIBase stoneUI;
        [Space(5)]
        [SerializeField] private Button stoneSellbtn;
        [SerializeField] private Button eliSellbtn;
        [SerializeField] private Button cancelbtn;
        [SerializeField] private Button itemCountUp;
        [SerializeField] private Button itemCountDown;
        [SerializeField] private Image itemImage;
        [SerializeField] private SelledItemView selledItemView;
        [SerializeField] private TextMeshProUGUI itemCountText;
        [SerializeField] private TextMeshProUGUI eliPriceText;
        [SerializeField] private TextMeshProUGUI stonePriceText;
        [Space(5)]
        [SerializeField] private GameObject purchaseSuccessNoti;
        [SerializeField] private GameObject purchaseFailNoti;
        [SerializeField] private float notiDisplayDuration = 2.0f;

        private SellableItemInfo currentSelectedItem;
        private int currentEliPrice;
        private int currentStonePrice;
        public int LastPurchaseCount { get; private set; }
        private int baseCount;
        private int baseEliPrice;
        private int baseStonePrice;
        private int itemCount = 0;

        private void Awake()
        {
            SetupButtonEvents();
        }

        private void OnDisable()
        {
            CancelInvoke(nameof(HidePurchaseFailNoti));
            CancelInvoke(nameof(HidePurchaseSuccessNoti));
        }

        /// <summary> 아이템 정보 동기화 </summary>
        public void SyncWithItem(SellableItemInfo selectedItem)
        {
            if (selectedItem == null || selectedItem.price == null) return;

            currentSelectedItem = selectedItem;

            baseCount = Mathf.Max(1, selectedItem.currentPurchaseCount);
            baseEliPrice = selectedItem.price.eliPrice;
            baseStonePrice = selectedItem.price.stonePrice;
            itemCount = baseCount;
            UpdateItemCountText();

            currentEliPrice = baseEliPrice;
            currentStonePrice = baseStonePrice;
            LastPurchaseCount = 0;

            if (itemImage != null && selectedItem.itemSprite != null)
            {
                itemImage.sprite = selectedItem.itemSprite.sprite;
                selledItemView.SetIcon(currentSelectedItem.GetComponentInChildren<IsItemSprite>());
            }

            UpdatePriceDisplay();
        }

        private void SetupButtonEvents()
        {
            itemCountUp.onClick.AddListener(() => AdjustCount(1));
            itemCountDown.onClick.AddListener(() => AdjustCount(-1));
            cancelbtn.onClick.AddListener(OnCancelClicked);
            eliSellbtn.onClick.AddListener(OnEliPurchaseClicked);
            stoneSellbtn.onClick.AddListener(OnStonePurchaseClicked);
        }

        /// <summary> 구매 수량 조절 </summary>
        private void AdjustCount(int deltaBundle)
        {
            if (currentSelectedItem == null) return;

            int currentBundle = Mathf.Max(1, itemCount / Mathf.Max(1, baseCount));
            int nextBundle = Mathf.Max(1, currentBundle + deltaBundle);

            itemCount = baseCount * nextBundle;
            currentEliPrice = baseEliPrice * nextBundle;
            currentStonePrice = baseStonePrice * nextBundle;

            UpdateItemCountText();
            UpdatePriceDisplay();
        }

        private void UpdatePriceDisplay()
        {
            if (eliPriceText != null) eliPriceText.text = $"{currentEliPrice}";
            if (stonePriceText != null) stonePriceText.text = $"{currentStonePrice}";
        }

        private void UpdateItemCountText()
        {
            if (itemCountText != null)
            {
                itemCountText.text = itemCount.ToString();
            }
        }

        /// <summary> 취소 버튼 클릭 처리 </summary>
        private void OnCancelClicked()
        {
            ResetUI();
            gameObject.SetActive(false);
        }

        private void OnEliPurchaseClicked()
        {
            TryPurchase(isEli: true);
        }

        private void OnStonePurchaseClicked()
        {
            TryPurchase(isEli: false);
        }

        /// <summary> 구매 처리 </summary>
        private void TryPurchase(bool isEli)
        {
            if (currentSelectedItem == null) return;

            UserData userData = UserDataManager.Shared.Data;
            if (userData == null) return;

            int price = isEli ? currentEliPrice : currentStonePrice;
            int balance = isEli ? userData.Eli : userData.Stone;

            if (balance < price)
            {
                ShowPurchaseFailNoti();
                return;
            }

            if (isEli)
            {
                userData.Eli -= price;
            }
            else
            {
                userData.Stone -= price;
            }

            int actualPurchaseCount = itemCount;
            ItemData itemData = UserDataManager.Shared.ItemData;

            if (itemData != null && currentSelectedItem.itemType != ItemType.Eli && currentSelectedItem.itemType != ItemType.Stone)
            {
                itemData.AddItem(currentSelectedItem.itemType, actualPurchaseCount);
            }

            UserDataManager.Shared.SaveImportant($"아이템 구매 ({(isEli ? "Eli" : "Stone")})");

            UpdateUserCacheUI();
            UpdateSelledItemView(actualPurchaseCount);
            ShowPurchaseSuccessNoti();

            LastPurchaseCount = actualPurchaseCount;
            onPurchaseSuccess?.Invoke();
        }

        private void UpdateSelledItemView(int purchaseCount)
        {
            if (selledItemView != null && currentSelectedItem != null)
            {
                var itemSprite = currentSelectedItem.GetComponentInChildren<IsItemSprite>();
                if (itemSprite != null)
                {
                    selledItemView.SetIcon(itemSprite, purchaseCount);
                }
            }
        }

        private void ShowPurchaseSuccessNoti()
        {
            if (purchaseSuccessNoti != null)
            {
                purchaseSuccessNoti.SetActive(true);
                CancelInvoke(nameof(HidePurchaseSuccessNoti));
                Invoke(nameof(HidePurchaseSuccessNoti), notiDisplayDuration);
            }
        }

        private void ResetUI()
        {
            currentSelectedItem = null;
            currentEliPrice = 0;
            currentStonePrice = 0;
            itemCount = 0;
            LastPurchaseCount = 0;
            baseCount = 0;
            baseEliPrice = 0;
            baseStonePrice = 0;

            if (eliPriceText != null) eliPriceText.text = ZeroText;
            if (itemCountText != null) itemCountText.text = ZeroText;
            if (stonePriceText != null) stonePriceText.text = ZeroText;

            if (purchaseSuccessNoti != null) purchaseSuccessNoti.SetActive(false);
            if (purchaseFailNoti != null) purchaseFailNoti.SetActive(false);
        }

        public SellableItemInfo GetCurrentSelectedItem() => currentSelectedItem;
        public (int eliPrice, int stonePrice) GetCurrentPrices() => (currentEliPrice, currentStonePrice);

        private void UpdateUserCacheUI()
        {
            if (eliUI != null) eliUI.UpdateText().Forget();
            if (stoneUI != null) stoneUI.UpdateText().Forget();
        }

        private void ShowPurchaseFailNoti()
        {
            if (purchaseFailNoti != null)
            {
                purchaseFailNoti.SetActive(true);
                CancelInvoke(nameof(HidePurchaseFailNoti));
                Invoke(nameof(HidePurchaseFailNoti), notiDisplayDuration);
            }
        }

        private void HidePurchaseFailNoti()
        {
            if (purchaseFailNoti != null)
            {
                purchaseFailNoti.SetActive(false);
            }
        }

        private void HidePurchaseSuccessNoti()
        {
            if (purchaseSuccessNoti != null)
            {
                purchaseSuccessNoti.SetActive(false);
            }
        }
    }
}
