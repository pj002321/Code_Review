using PentaShield;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace chaos
{
    public partial class MainMenuScreen
    {

        #region Serialized Fields
        [Header("BUTTON TRIGGER")]
        [SerializeField] private List<Button> startButton = new List<Button>();
        [SerializeField] private List<Button> optionsButton = new List<Button>();
        [SerializeField] private List<Button> exitButton = new List<Button>();
        [SerializeField] private Button rankButton;

        [Header("SOUND SETTING")]
        [SerializeField] private Slider bgmSlider;
        [SerializeField] private Slider effectSlider;

        [Header("SHOP")]
        public GameObject purchaseConfirmPanel;
        public List<Button> purchaseButton = new List<Button>();
        [SerializeField] private TextMeshProUGUI eliText;
        [SerializeField] private TextMeshProUGUI stoneText;

        [Header("PANNELS")]
        [SerializeField] private List<GameObject> pannels = new List<GameObject>();

        [Header("ENV")]
        [SerializeField] private GameObject Env;
        #endregion

        #region Private Fields
        private ShopItemConfirmUI shopConfirmUI;
        private SellableItemInfo selectedItemInfo;
        private Button selectedButton;
        #endregion

        #region Button Setup
        private void HandleButtonSfx()
        {
            SetupButtonList(startButton, AudioConst.BUTTON_CLICK, () => SetEnvActive(false));
            SetupButtonList(optionsButton, AudioConst.BUTTON_HOVER, () => SetEnvActive(false));
            SetupButtonList(exitButton, AudioConst.BUTTON_HOVER, () => SetEnvActive(true));
            SetupButtonList(purchaseButton, AudioConst.BUTTON_CLICK, null);
            SetupSingleButton(rankButton, AudioConst.BUTTON_CLICK);
        }

        private void SetupButtonList(List<Button> buttons, string audioConst, Action onClickAction)
        {
            foreach (var button in buttons)
            {
                if (button != null)
                {
                    SetupButton(button, audioConst, onClickAction);
                }
            }
        }

        private void SetupSingleButton(Button button, string audioConst)
        {
            if (button != null)
            {
                SetupButton(button, audioConst, null);
            }
        }

        private void SetupButton(Button button, string audioConst, Action onClickAction)
        {
            button.onClick.AddListener(() =>
            {
                PlayUISound(audioConst);
                onClickAction?.Invoke();
            });
        }

        private void PlayUISound(string audioConst)
        {
            try
            {
                AudioHelper.PlayUI(audioConst);
            }
            catch (Exception e)
            {
                $"[MainMenuScreen] Audio play error: {e.Message}".DError();
            }
        }

        private void SetEnvActive(bool active)
        {
            if (Env != null)
            {
                Env.SetActive(active);
            }
        }
        #endregion

        #region Volume Settings
        private void HandleVolumeSettings()
        {
            if (AudioManager.Shared == null)
            {
                return;
            }

            SetupBGMVolume();
            SetupSFXVolume();
        }

        private void SetupBGMVolume()
        {
            if (bgmSlider == null)
            {
                return;
            }

            bgmSlider.onValueChanged.RemoveAllListeners();
            bgmSlider.onValueChanged.AddListener(OnBGMVolumeChanged);

            float bgmValue = PlayerPrefs.GetFloat(BGM_PREF_KEY, DEFAULT_VOLUME);
            bgmSlider.value = bgmValue;
            AudioManager.Shared.SetCategoryVolume(AudioCategory.BGM, bgmValue);
        }

        private void SetupSFXVolume()
        {
            if (effectSlider == null)
            {
                return;
            }

            effectSlider.onValueChanged.RemoveAllListeners();
            effectSlider.onValueChanged.AddListener(OnSFXVolumeChanged);

            float sfxValue = PlayerPrefs.GetFloat(SFX_PREF_KEY, DEFAULT_VOLUME);
            effectSlider.value = sfxValue;
            AudioManager.Shared.SetCategoryVolume(AudioCategory.SFX, sfxValue);
        }

        private void OnBGMVolumeChanged(float value)
        {
            if (AudioManager.Shared != null)
            {
                AudioManager.Shared.SetCategoryVolume(AudioCategory.BGM, value);
            }
        }

        private void OnSFXVolumeChanged(float value)
        {
            if (AudioManager.Shared != null)
            {
                AudioManager.Shared.SetCategoryVolume(AudioCategory.SFX, value);
            }
        }
        #endregion

        #region Shop
        private void HandleSellBehaviour()
        {
            SetupPurchaseButtons();
            SetupPurchaseConfirmPanel();
        }

        private void SetupPurchaseButtons()
        {
            for (int i = 0; i < purchaseButton.Count; i++)
            {
                Button button = purchaseButton[i];
                if (button == null)
                {
                    continue;
                }

                SellableItemInfo itemInfo = button.GetComponent<SellableItemInfo>();
                if (itemInfo == null)
                {
                    $"[MainMenuScreen] purchaseButton[{i}]에 SellableItemInfo 컴포넌트가 없습니다!".DError();
                    continue;
                }

                Button currentButton = button;
                SellableItemInfo currentItemInfo = itemInfo;
                button.onClick.AddListener(() => OnItemButtonClicked(currentButton, currentItemInfo));
            }
        }

        private void SetupPurchaseConfirmPanel()
        {
            if (purchaseConfirmPanel == null)
            {
                return;
            }

            shopConfirmUI = purchaseConfirmPanel.GetComponent<ShopItemConfirmUI>();
            if (shopConfirmUI != null)
            {
                shopConfirmUI.onPurchaseSuccess += OnPurchaseConfirm;
            }

            purchaseConfirmPanel.SetActive(false);
        }

        private void OnItemButtonClicked(Button button, SellableItemInfo itemInfo)
        {
            if (!itemInfo.CanPurchase())
            {
                LogPurchaseError(itemInfo);
                return;
            }

            selectedItemInfo = itemInfo;
            selectedButton = button;

            LogItemSelected(itemInfo);
            ShowPurchaseConfirmPanel();
        }

        private void LogPurchaseError(SellableItemInfo itemInfo)
        {
            $"[MainMenuScreen] '{itemInfo.itemType}' 아이템을 구매할 수 없습니다. (엘리비용: {itemInfo.GetEliCost()}, 스톤비용: {itemInfo.GetStoneCost()})".DError();
        }

        private void LogItemSelected(SellableItemInfo itemInfo)
        {
            $"[MainMenuScreen] 아이템 선택됨: {itemInfo.itemType} (ID: {itemInfo.itemId}, 구매개수: {itemInfo.GetPurchaseCount()}개, 엘리비용: {itemInfo.GetEliCost()}, 스톤비용: {itemInfo.GetStoneCost()})".DWarning();
        }

        private void OnPurchaseConfirm()
        {
            if (selectedItemInfo == null)
            {
                return;
            }

            int purchaseCount = shopConfirmUI != null 
                ? shopConfirmUI.LastPurchaseCount 
                : selectedItemInfo.GetPurchaseCount();

            ProcessItemPurchase(selectedItemInfo, purchaseCount);
            ClosePurchaseConfirmPanel();
        }

        private void OnPurchaseCancel()
        {
            "[MainMenuScreen] 아이템 구매 취소".DLog();
            ClosePurchaseConfirmPanel();
        }

        private void ShowPurchaseConfirmPanel()
        {
            if (purchaseConfirmPanel == null || selectedItemInfo == null)
            {
                return;
            }

            shopConfirmUI ??= purchaseConfirmPanel.GetComponent<ShopItemConfirmUI>();
            shopConfirmUI?.SyncWithItem(selectedItemInfo);
            purchaseConfirmPanel.SetActive(true);
        }

        private void ClosePurchaseConfirmPanel()
        {
            if (purchaseConfirmPanel != null)
            {
                purchaseConfirmPanel.SetActive(false);
            }

            selectedItemInfo = null;
            selectedButton = null;
        }

        private void ProcessItemPurchase(SellableItemInfo itemInfo, int purchaseCount)
        {
            ItemData itemData = UserDataManager.Shared.ItemData;
            itemData.AddItem(itemInfo.itemType, purchaseCount);

            $"[MainMenuScreen] 아이템 '{itemInfo.itemType}' {purchaseCount}개 구매 완료 (엘리비용: {itemInfo.GetEliCost()}, 스톤비용: {itemInfo.GetStoneCost()})".DLog();
        }
        #endregion

        #region Currency UI
        private void SetEliText()
        {
            SetCurrencyText(eliText, GetEliValue());
        }

        private void SetStoneText()
        {
            SetCurrencyText(stoneText, GetStoneValue());
        }

        private void SetCurrencyText(TextMeshProUGUI textComponent, int value)
        {
            if (textComponent != null)
            {
                textComponent.text = value.ToString();
            }
        }

        private int GetEliValue()
        {
            if (UserDataManager.Shared?.Data != null)
            {
                return UserDataManager.Shared.Data.Eli;
            }

            "[MainMenuScreen] UserDataManager or UserData is null. Cannot get eli value.".DWarning();
            return 0;
        }

        private int GetStoneValue()
        {
            if (UserDataManager.Shared?.Data != null)
            {
                return UserDataManager.Shared.Data.Stone;
            }

            "[MainMenuScreen] UserDataManager or UserData is null. Cannot get stone value.".DWarning();
            return 0;
        }
        #endregion

        #region Panel Management
        private void ActiveStatePannels()
        {
            int activeIndex = FindActivePanelIndex();
            if (activeIndex == -1)
            {
                return;
            }

            DeactivateOtherPanels(activeIndex);
        }

        private int FindActivePanelIndex()
        {
            for (int i = 0; i < pannels.Count; i++)
            {
                if (pannels[i] != null && pannels[i].activeSelf)
                {
                    return i;
                }
            }
            return -1;
        }

        private void DeactivateOtherPanels(int activeIndex)
        {
            for (int i = 0; i < pannels.Count; i++)
            {
                if (i != activeIndex && pannels[i] != null && pannels[i].activeSelf)
                {
                    pannels[i].SetActive(false);
                }
            }
        }
        #endregion
    }
}


