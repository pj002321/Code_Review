using penta;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace penta
{
    public partial class MainMenuScreen
    {
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

        private ShopItemConfirmUI shopConfirmUI;
        private SellableItemInfo selectedItemInfo;
        private Button selectedButton;

        /// <summary> 버튼 사운드 설정 </summary>
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
                AudioHelper.PlayUI(audioConst);
                onClickAction?.Invoke();
            });
        }

        private void SetEnvActive(bool active)
        {
            if (Env != null)
            {
                Env.SetActive(active);
            }
        }

        /// <summary> 볼륨 설정 처리 </summary>
        private void HandleVolumeSettings()
        {
            if (AudioManager.Shared == null) return;

            SetupBGMVolume();
            SetupSFXVolume();
        }

        private void SetupBGMVolume()
        {
            if (bgmSlider == null) return;

            bgmSlider.onValueChanged.RemoveAllListeners();
            bgmSlider.onValueChanged.AddListener(OnBGMVolumeChanged);

            float bgmValue = PlayerPrefs.GetFloat(BGM_PREF_KEY, DEFAULT_VOLUME);
            bgmSlider.value = bgmValue;
            AudioManager.Shared.SetCategoryVolume(AudioCategory.BGM, bgmValue);
        }

        private void SetupSFXVolume()
        {
            if (effectSlider == null) return;

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

        /// <summary> 상점 구매 동작 설정 </summary>
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
                if (button == null) continue;

                SellableItemInfo itemInfo = button.GetComponent<SellableItemInfo>();
                if (itemInfo == null) continue;

                Button currentButton = button;
                SellableItemInfo currentItemInfo = itemInfo;
                button.onClick.AddListener(() => OnItemButtonClicked(currentButton, currentItemInfo));
            }
        }

        private void SetupPurchaseConfirmPanel()
        {
            if (purchaseConfirmPanel == null) return;

            shopConfirmUI = purchaseConfirmPanel.GetComponent<ShopItemConfirmUI>();
            if (shopConfirmUI != null)
            {
                shopConfirmUI.onPurchaseSuccess += OnPurchaseConfirm;
            }

            purchaseConfirmPanel.SetActive(false);
        }

        /// <summary> 아이템 버튼 클릭 처리 </summary>
        private void OnItemButtonClicked(Button button, SellableItemInfo itemInfo)
        {
            if (!itemInfo.CanPurchase()) return;

            selectedItemInfo = itemInfo;
            selectedButton = button;
            ShowPurchaseConfirmPanel();
        }

        /// <summary> 구매 확인 처리 </summary>
        private void OnPurchaseConfirm()
        {
            if (selectedItemInfo == null) return;

            int purchaseCount = shopConfirmUI != null
                ? shopConfirmUI.LastPurchaseCount
                : selectedItemInfo.GetPurchaseCount();

            ProcessItemPurchase(selectedItemInfo, purchaseCount);
            ClosePurchaseConfirmPanel();
        }

        private void ShowPurchaseConfirmPanel()
        {
            if (purchaseConfirmPanel == null || selectedItemInfo == null) return;

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

        /// <summary> 아이템 구매 처리 </summary>
        private void ProcessItemPurchase(SellableItemInfo itemInfo, int purchaseCount)
        {
            ItemData itemData = UserDataManager.Shared.ItemData;
            itemData.AddItem(itemInfo.itemType, purchaseCount);
        }

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
            return 0;
        }

        private int GetStoneValue()
        {
            if (UserDataManager.Shared?.Data != null)
            {
                return UserDataManager.Shared.Data.Stone;
            }
            return 0;
        }

        /// <summary> 패널 상태 관리 </summary>
        private void ActiveStatePannels()
        {
            int activeIndex = FindActivePanelIndex();
            if (activeIndex == -1) return;

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
    }
}
