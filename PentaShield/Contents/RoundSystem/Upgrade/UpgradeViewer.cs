using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace penta
{
    /// <summary>
    /// 업그레이드 UI 뷰어 관리 (주요 로직)
    /// - 업그레이드 정보 표시
    /// - 업그레이드 실행 처리
    /// </summary>
    public class UpgradeViewer : MonoBehaviourSingleton<UpgradeViewer>
    {
        [SerializeField] private GameObject upgradeCanvas;
        [SerializeField] public Button playerUpgradeButton;
        [SerializeField] public Button elementalUpgradeButton;
        [SerializeField] public Button guardUpgradeButton;
        [SerializeField] private Button skipButton;

        [SerializeField] private TextMeshProUGUI playerUpgradeText;
        [SerializeField] private TextMeshProUGUI playerUpgradeCostText;
        [SerializeField] private TextMeshProUGUI playerUpgradeLevelText;
        [SerializeField] private TextMeshProUGUI elementalUpgradeText;
        [SerializeField] private TextMeshProUGUI elementalUpgradeCostText;
        [SerializeField] private TextMeshProUGUI elementalUpgradeLevelText;
        [SerializeField] private TextMeshProUGUI guardUpgradeText;
        [SerializeField] private TextMeshProUGUI guardUpgradeCostText;
        [SerializeField] private TextMeshProUGUI guardUpgradeLevelText;
        [SerializeField] private Image playerUpgradeImage;
        [SerializeField] private Image elementalUpgradeImage;
        [SerializeField] private Image guardUpgradeImage;

        protected override void Awake()
        {
            base.Awake();
            SetupButtonEvents();
        }

        /// <summary> UI 컴포넌트 초기화 </summary>
        public void InitializeUI()
        {
            if (playerUpgradeButton == null || elementalUpgradeButton == null || guardUpgradeButton == null)
            {
                return;
            }

            if (playerUpgradeText == null)
                playerUpgradeText = playerUpgradeButton.GetComponentInChildren<TextMeshProUGUI>();
            if (guardUpgradeText == null)
                guardUpgradeText = guardUpgradeButton.GetComponentInChildren<TextMeshProUGUI>();
            if (elementalUpgradeText == null)
                elementalUpgradeText = elementalUpgradeButton.GetComponentInChildren<TextMeshProUGUI>();

            if (playerUpgradeImage == null)
                playerUpgradeImage = GetUpgradeImage(playerUpgradeButton);
            if (elementalUpgradeImage == null)
                elementalUpgradeImage = GetUpgradeImage(elementalUpgradeButton);
            if (guardUpgradeImage == null)
                guardUpgradeImage = GetUpgradeImage(guardUpgradeButton);

            if (playerUpgradeCostText == null)
                playerUpgradeCostText = GetCostText(playerUpgradeButton);
            if (elementalUpgradeCostText == null)
                elementalUpgradeCostText = GetCostText(elementalUpgradeButton);
            if (guardUpgradeCostText == null)
                guardUpgradeCostText = GetCostText(guardUpgradeButton);

            if (playerUpgradeLevelText == null)
                playerUpgradeLevelText = GetLevelText(playerUpgradeButton);
            if (elementalUpgradeLevelText == null)
                elementalUpgradeLevelText = GetLevelText(elementalUpgradeButton);
            if (guardUpgradeLevelText == null)
                guardUpgradeLevelText = GetLevelText(guardUpgradeButton);

            DisplayActive(false);
        }

        private void SetupButtonEvents()
        {
            if (playerUpgradeButton != null)
                playerUpgradeButton.onClick.AddListener(OnPlayerUpgradeEvent);
            if (elementalUpgradeButton != null)
                elementalUpgradeButton.onClick.AddListener(OnElementalUpgradeEvent);
            if (guardUpgradeButton != null)
                guardUpgradeButton.onClick.AddListener(OnGuardUpgradeEvent);
            if (skipButton != null)
                skipButton.onClick.AddListener(OnSkipEvent);
        }

        private Image GetUpgradeImage(Button button)
        {
            if (button.transform.childCount > 2 && button.transform.GetChild(2).childCount > 0)
            {
                return button.transform.GetChild(2).GetChild(0).GetComponent<Image>();
            }
            return null;
        }

        private TextMeshProUGUI GetCostText(Button button)
        {
            Transform costView = button.transform.Find("CostView");
            return costView?.GetComponentInChildren<TextMeshProUGUI>();
        }

        private TextMeshProUGUI GetLevelText(Button button)
        {
            var levelComp = button.GetComponentInChildren<IsLevelText>(true);
            return levelComp?.GetComponent<TextMeshProUGUI>();
        }

        public void DisplayActive(bool enable)
        {
            upgradeCanvas?.SetActive(enable);
        }

        public bool IsActive()
        {
            return upgradeCanvas?.activeInHierarchy ?? false;
        }

        /// <summary> 업그레이드 UI 표시 </summary>
        public async void OnUpgradeUI()
        {
            try
            {
                if (UpgradeTable.Shared == null || !UpgradeTable.Shared.Initalize)
                {
                    return;
                }

                DisplayActive(true);

                if (playerUpgradeButton != null) playerUpgradeButton.interactable = true;
                if (elementalUpgradeButton != null) elementalUpgradeButton.interactable = true;
                if (guardUpgradeButton != null) guardUpgradeButton.interactable = true;

                UpgradeTable.Shared.ElementalUpgrade.UpgradeSetting();
                UpgradeTable.Shared.PlayerUpgrade.UpgradeSetting();
                UpgradeTable.Shared.GuardUpgrade.UpgradeSetting();

                await UniTask.WhenAll(
                    OutputElementalUpgrade(),
                    OutputPlayerUpgrade(),
                    OutputGuardUpgrade()
                );
            }
            catch (Exception ex)
            {
                DisplayActive(true);
            }
        }

        /// <summary> Elemental 업그레이드 정보 표시 </summary>
        private async UniTask OutputElementalUpgrade()
        {
            var cachedata = UpgradeTable.Shared.ElementalUpgrade.UpgradeData;

            if (cachedata == null || cachedata.Cost == -1)
            {
                if (elementalUpgradeButton != null) elementalUpgradeButton.interactable = false;
                return;
            }

            elementalUpgradeText?.SetText("");
            elementalUpgradeCostText?.SetText($"{cachedata.Cost}");

            if (elementalUpgradeLevelText != null)
            {
                string[] nameParts = cachedata.Name.Split('_');
                string levelText = nameParts.Length > 1 ? $"Lv {nameParts[1]}" : "Lv 1";
                elementalUpgradeLevelText.text = levelText;
            }

            try
            {
                await UniTask.WaitUntil(() => UpgradeTable.Shared.ElementalUpgrade.UpgradeData.IsSpriteLoad)
                    .Timeout(TimeSpan.FromSeconds(5));

                var sprite = UpgradeTable.Shared.ElementalUpgrade.UpgradeData.UpgradeSprite;
                if (elementalUpgradeImage != null && sprite != null)
                {
                    elementalUpgradeImage.sprite = sprite;
                }
            }
            catch (TimeoutException)
            {
            }

            if (elementalUpgradeButton != null) elementalUpgradeButton.interactable = true;
        }

        /// <summary> Player 업그레이드 정보 표시 </summary>
        private async UniTask OutputPlayerUpgrade()
        {
            var cacheData = UpgradeTable.Shared.PlayerUpgrade.UpgradeData;

            if (cacheData == null || cacheData.Cost == -1)
            {
                if (playerUpgradeButton != null) playerUpgradeButton.interactable = false;
                return;
            }

            var playerBehaviour = PlayerController.Shared?.GetComponentInChildren<PlayerBehaviour>();
            if (playerBehaviour?.playerTable == null)
            {
                if (playerUpgradeButton != null) playerUpgradeButton.interactable = false;
                return;
            }

            playerUpgradeText?.SetText("");
            playerUpgradeCostText?.SetText($"{cacheData.Cost}");
            playerUpgradeLevelText?.SetText($"Lv {cacheData.UnlockLevel}");

            await UniTask.WaitUntil(() => UpgradeTable.Shared.PlayerUpgrade.UpgradeData.IsSpriteLoad);

            var playerSprite = UpgradeTable.Shared.PlayerUpgrade.UpgradeData.UpgradeSprite;
            if (playerUpgradeImage != null && playerSprite != null)
                playerUpgradeImage.sprite = playerSprite;

            if (playerUpgradeButton != null) playerUpgradeButton.interactable = true;
        }

        /// <summary> Guard 업그레이드 정보 표시 </summary>
        private async UniTask OutputGuardUpgrade()
        {
            var cacheData = UpgradeTable.Shared.GuardUpgrade.UpgradeData;

            if (cacheData == null || cacheData.Cost == -1)
            {
                if (guardUpgradeButton != null) guardUpgradeButton.interactable = false;
                return;
            }

            guardUpgradeText?.SetText("");
            guardUpgradeCostText?.SetText($"{cacheData.Cost}");
            guardUpgradeLevelText?.SetText($"Lv {cacheData.UnlockLevel}");

            await UniTask.WaitUntil(() => UpgradeTable.Shared.GuardUpgrade.UpgradeData.IsSpriteLoad);

            var guardSprite = UpgradeTable.Shared.GuardUpgrade.UpgradeData.UpgradeSprite;
            if (guardUpgradeImage != null && guardSprite != null)
                guardUpgradeImage.sprite = guardSprite;

            if (guardUpgradeButton != null) guardUpgradeButton.interactable = true;
        }

        /// <summary> Player 업그레이드 버튼 이벤트 </summary>
        public async void OnPlayerUpgradeEvent()
        {
            if (!await UpgradePlayer())
            {
                await SpawnEffect("upgrade_disable@fx", playerUpgradeButton.transform, 500);
                return;
            }

            StartCoroutine(AnimateButtonAndResume(playerUpgradeButton));
        }

        /// <summary> 이펙트 생성 </summary>
        private async UniTask SpawnEffect(string effectName, Transform parent, int delay)
        {
            if (VFXManager.Shared == null || parent == null)
            {
                return;
            }

            var go = await VFXManager.Shared.SpawnVFX(effectName, parent.position, parent.rotation, parent);
            if (go == null)
            {
                return;
            }

            go.transform.localPosition = Vector3.zero;
            go.transform.localScale = Vector3.one;

            StartCoroutine(DestroyAfterDelay(go, delay));
        }

        private IEnumerator DestroyAfterDelay(GameObject target, int delayMs)
        {
            yield return new WaitForSecondsRealtime(delayMs / 1000f);
            Destroy(target);
        }

        /// <summary> 버튼 애니메이션 및 게임 재개 </summary>
        private IEnumerator AnimateButtonAndResume(Button targetButton)
        {
            ...
            DisplayActive(false);
            RoundSystem.Shared?.OnContinueButtonPressed();
        }

        /// <summary> Elemental 업그레이드 버튼 이벤트 </summary>
        public async void OnElementalUpgradeEvent()
        {
            if (!await UpgradeElemental())
            {
                await SpawnEffect(PentaConst.KVfxUpgradeDisable, elementalUpgradeButton.transform, 500);
                return;
            }

            StartCoroutine(AnimateButtonAndResume(elementalUpgradeButton));
        }

        /// <summary> Guard 업그레이드 버튼 이벤트 </summary>
        public async void OnGuardUpgradeEvent()
        {
            if (!await UpgradeGuard())
            {
                await SpawnEffect("upgrade_disable@fx", guardUpgradeButton.transform, 500);
                return;
            }

            StartCoroutine(AnimateButtonAndResume(guardUpgradeButton));
        }

        private void OnSkipEvent()
        {
            UpgradeTable.Shared?.UpgradeSettingAllClear();
            DisplayActive(false);
            RoundSystem.Shared?.OnContinueButtonPressed();
        }

        private UniTask<bool> UpgradePlayer()
        {
            return UpgradeTable.Shared.PlayerUpgrade.ExcuteLevelupUpgrade();
        }

        private UniTask<bool> UpgradeElemental()
        {
            return UpgradeTable.Shared.ElementalUpgrade.ExcuteLevelupUpgrade();
        }

        private UniTask<bool> UpgradeGuard()
        {
            return UpgradeTable.Shared.GuardUpgrade.ExcuteLevelupUpgrade();
        }

        public void OpenUpgradeMenu()
        {
            DisplayActive(true);
        }

        public void SetElementalUpgradeFlag(bool isActive)
        {
            var flagObject = elementalUpgradeButton?.GetComponentInChildren<IsUpgradeFlag>(true);
            flagObject?.gameObject.SetActive(isActive);
        }
    }
}
