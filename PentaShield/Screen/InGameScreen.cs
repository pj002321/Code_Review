using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PentaShield;
namespace chaos
{
    public enum Stage
    {
        ICE,FIRE,STONE
    }

    public class InGameScreen : MonoBehaviour
    {
        [SerializeField] private Stage _stageType;

        [Header("UI BUTTON")]
        [SerializeField] private List<Button> optionButtons = new List<Button>();
        [SerializeField] private List<Button> exitButtons = new List<Button>();
        [SerializeField] private List<Button> upgradeButtons = new List<Button>();
        [SerializeField] private Button soundOnButton;
        [SerializeField] private Button soundOffButton;
        [SerializeField] private Button stopButton;
        [SerializeField] private Button resumButton;
        [Header("GLOBAL ITEM")]
        [SerializeField] private Button radomboxbutton;
        [SerializeField] private Button healbutton;
        [SerializeField] private Button hastebutton;
        [SerializeField] private Button godbutton;
        [SerializeField] private Button feverbutton;

        private TextMeshProUGUI randomboxItemCount;
        private TextMeshProUGUI healItemCount;
        private TextMeshProUGUI hasteItemCount;
        private TextMeshProUGUI godItemCount;
        private TextMeshProUGUI feverItemCount;

        private Image randomboxCooldownImage;
        private Image healCooldownImage;
        private Image hasteCooldownImage;
        private Image godCooldownImage;
        private Image feverCooldownImage;

        // 점수 자동 증가 관련
        private Coroutine scoreIncreaseCoroutine;
        private const float SCORE_INCREASE_INTERVAL = 3f; 
        private const int SCORE_INCREASE_AMOUNT = 9; 

        public Stage stageType => _stageType;
        void Start()
        {
            
            if (radomboxbutton != null)
            {
                randomboxItemCount = radomboxbutton.GetComponentInChildren<TextMeshProUGUI>();
                var images = radomboxbutton.GetComponentsInChildren<Image>();
                randomboxCooldownImage = images.Length > 1 ? images[1] : null;
                if (randomboxCooldownImage != null) randomboxCooldownImage.fillAmount = 0;
            }
            if (healbutton != null)
            {
                healItemCount = healbutton.GetComponentInChildren<TextMeshProUGUI>();
                var images = healbutton.GetComponentsInChildren<Image>();
                healCooldownImage = images.Length > 1 ? images[1] : null;
                if (healCooldownImage != null) healCooldownImage.fillAmount = 0;
            }
            if (hastebutton != null)
            {
                hasteItemCount = hastebutton.GetComponentInChildren<TextMeshProUGUI>();
                var images = hastebutton.GetComponentsInChildren<Image>();
                hasteCooldownImage = images.Length > 1 ? images[1] : null;
                if (hasteCooldownImage != null) hasteCooldownImage.fillAmount = 0;
            }
            if (godbutton != null)
            {
                godItemCount = godbutton.GetComponentInChildren<TextMeshProUGUI>();
                var images = godbutton.GetComponentsInChildren<Image>();
                godCooldownImage = images.Length > 1 ? images[1] : null;
                if (godCooldownImage != null) godCooldownImage.fillAmount = 0;
            }
            if (feverbutton != null)
            {
                feverItemCount = feverbutton.GetComponentInChildren<TextMeshProUGUI>();
                var images = feverbutton.GetComponentsInChildren<Image>();
                feverCooldownImage = images.Length > 1 ? images[1] : null;
                if (feverCooldownImage != null) feverCooldownImage.fillAmount = 0;
            }

            StartCoroutine(PlayBGMAfterDelay());
            HandleButtonSfx();
            HandleButtonsBehaviour();
            StartCoroutine(UpdateCooldowns());

            // Defer initial count binding until user data ready, then subscribe for changes
            InitializeItemCountsAsync().Forget();
            
            // 점수 자동 증가 시작
            StartScoreAutoIncrease();
        }


        private void OnDestroy()
        {
            // 점수 자동 증가 중지
            StopScoreAutoIncrease();
            
            if (UserDataManager.Shared?.ItemData != null)
            {
                UserDataManager.Shared.ItemData.OnItemCountChanged -= HandleItemCountChanged;
            }
            foreach (var button in optionButtons)
            {
                button?.onClick.RemoveAllListeners();
            }
            foreach (var button in exitButtons)
            {
                button?.onClick.RemoveAllListeners();
            }
            foreach (var button in upgradeButtons)
            {
                button?.onClick.RemoveAllListeners();
            }
           
            radomboxbutton?.onClick.RemoveAllListeners();
            healbutton?.onClick.RemoveAllListeners();
            hastebutton?.onClick.RemoveAllListeners();
            godbutton?.onClick.RemoveAllListeners();
            feverbutton?.onClick.RemoveAllListeners();
            soundOnButton?.onClick.RemoveAllListeners();
            soundOffButton?.onClick.RemoveAllListeners();
            stopButton?.onClick.RemoveAllListeners();
            resumButton?.onClick.RemoveAllListeners();
        }


        private IEnumerator PlayBGMAfterDelay()
        {
            yield return new WaitForSeconds(0.1f);
            switch(_stageType)
            {
                case Stage.ICE:
                    AudioHelper.PlayBGM(AudioConst.STAGE1_BGM, 1f);
                    break;
                case Stage.FIRE:
                    AudioHelper.PlayBGM(AudioConst.STAGE2_BGM, 1f);
                    break;
                case Stage.STONE:
                    AudioHelper.PlayBGM(AudioConst.STAGE3_BGM, 1f);
                    break;
            }
        }
        private bool isLoadingScene = false;

        public async UniTask LoadStage(string sceneKey)
        {
            // 이미 씬 로딩 중이면 무시
            if (isLoadingScene)
            {
                "[InGameScreen] Scene is already loading, ignoring duplicate request".DWarning();
                return;
            }

            // SceneSystem의 입력 차단 체크
            if (SceneSystem.IsUIInputBlocked)
            {
                "[InGameScreen] UI input is blocked, ignoring scene load request".DWarning();
                return;
            }

            isLoadingScene = true;
            try
            {
                await SceneSystem.Shared.LoadScene(sceneKey);
            }
            finally
            {
                isLoadingScene = false;
            }
        }
        public async UniTask UnloadStage()
        {
            AudioHelper.RestorePreviousBGM(1f);
            await SceneSystem.Shared.UnloadTopScene();
        }

        private void HandleButtonSfx()
        {
            foreach (var button in optionButtons)
            {
                button.onClick.AddListener(() => AudioHelper.PlayUI(AudioConst.BUTTON_CLICK));
            }
            foreach (var button in exitButtons)
            {
                button.onClick.AddListener(() => AudioHelper.PlayUI(AudioConst.BUTTON_HOVER));
            }
            //foreach (var button in upgradeButtons)
            //{
            //    button.onClick.AddListener(() => AudioHelper.PlayUI(AudioClips.UPGRADE, 0.5f));
            //}
            healbutton?.onClick.AddListener(() => AudioHelper.PlayUI(AudioConst.BUTTON_CLICK));
            hastebutton?.onClick.AddListener(() => AudioHelper.PlayUI(AudioConst.BUTTON_CLICK));
            godbutton?.onClick.AddListener(() => AudioHelper.PlayUI(AudioConst.BUTTON_CLICK));
        }
        public void HandleButtonsBehaviour()
        {
            ExitButtonBehaviour();
            ItemButtonBehaviour();
            SoundButtonBehaviour();
            PauseButtonBehaviour();
        }

        private void SoundButtonBehaviour()
        {
            // 기존 리스너 제거 후 추가하여 중복 방지
            soundOnButton?.onClick.RemoveAllListeners();
            soundOnButton?.onClick.AddListener(() =>
            {
                AudioManager.Shared?.ToggleAudioListener();
                soundOnButton?.gameObject.SetActive(false);
                soundOffButton?.gameObject.SetActive(true);
            });

            soundOffButton?.onClick.RemoveAllListeners();
            soundOffButton?.onClick.AddListener(() =>
            {
                AudioManager.Shared?.ToggleAudioListener();
                soundOffButton?.gameObject.SetActive(false);
                soundOnButton?.gameObject.SetActive(true);
            });
        }

        private void PauseButtonBehaviour()
        {
            // Stop 버튼 - 게임 일시정지
            stopButton?.onClick.RemoveAllListeners();
            stopButton?.onClick.AddListener(() =>
            {
                AudioHelper.PlayUI(AudioConst.BUTTON_CLICK);
                PauseGame();
            });

            // Resume 버튼 - 게임 재개
            resumButton?.onClick.RemoveAllListeners();
            resumButton?.onClick.AddListener(() =>
            {
                AudioHelper.PlayUI(AudioConst.BUTTON_CLICK);
                ResumeGame();
            });
        }

        private void PauseGame()
        {
            Time.timeScale = 0f;
            stopButton?.gameObject.SetActive(false);
            resumButton?.gameObject.SetActive(true);
            "[InGameScreen] Game paused".Log();
        }

        private void ResumeGame()
        {
            Time.timeScale = 1f;
            resumButton?.gameObject.SetActive(false);
            stopButton?.gameObject.SetActive(true);
            "[InGameScreen] Game resumed".Log();
        }

        private void ExitButtonBehaviour()
        {
            foreach (var button in exitButtons)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => HandleExitButton());
            }
        }

        private void HandleExitButton()
        {
            // 나가기 전에 게임 시간 복구
            if (Time.timeScale == 0f)
            {
                Time.timeScale = 1f;
            }
            
            if (GlobalItem.Shared != null)
            {
                GlobalItem.Shared.CleanupOnGameOver();
            }
            UnloadStage().Forget();
        }
        private void ItemButtonBehaviour()
        {
            // 기존 리스너 제거 후 추가하여 중복 방지
            healbutton?.onClick.RemoveAllListeners();
            healbutton?.onClick.AddListener(() => PlayerHealItem());

            hastebutton?.onClick.RemoveAllListeners();
            hastebutton?.onClick.AddListener(() => PlayerHasteItem());

            godbutton?.onClick.RemoveAllListeners();
            godbutton?.onClick.AddListener(() => PlayerGodItem());

            feverbutton?.onClick.RemoveAllListeners();
            feverbutton?.onClick.AddListener(() => PlayerFeverItem());

            radomboxbutton?.onClick.RemoveAllListeners();
            radomboxbutton?.onClick.AddListener(() => RandomBoxItem());
            // Initial values will be set in InitializeItemCountsAsync()
        }

        private async Cysharp.Threading.Tasks.UniTaskVoid InitializeItemCountsAsync()
        {
            await Cysharp.Threading.Tasks.UniTask.WaitUntil(() => UserDataManager.Shared != null && UserDataManager.Shared.IsInitialized);

            var item = UserDataManager.Shared.ItemData;
            if (item != null)
            {
                // Set all counts once
                UpdateItemCountUI(ItemType.RandomBox, item.randomboxes);
                UpdateItemCountUI(ItemType.Potion, item.potions);
                UpdateItemCountUI(ItemType.Haste, item.hastes);
                UpdateItemCountUI(ItemType.God, item.gods);
                UpdateItemCountUI(ItemType.Fiver, item.fever);

                // Subscribe for subsequent changes
                item.OnItemCountChanged += HandleItemCountChanged;
            }
        }

        private void HandleItemCountChanged(ItemType type, int count)
        {
            UpdateItemCountUI(type, count);
        }

        private void UpdateItemCountUI(ItemType type, int count)
        {
            switch (type)
            {
                case ItemType.RandomBox:
                    if (randomboxItemCount != null) randomboxItemCount.text = count.ToString();
                    break;
                case ItemType.Potion:
                    if (healItemCount != null) healItemCount.text = count.ToString();
                    break;
                case ItemType.Haste:
                    if (hasteItemCount != null) hasteItemCount.text = count.ToString();
                    break;
                case ItemType.God:
                    if (godItemCount != null) godItemCount.text = count.ToString();
                    break;
                case ItemType.Fiver:
                    if (feverItemCount != null) feverItemCount.text = count.ToString();
                    break;
            }
        }

        private void PlayerHealItem()
        {
            if (GlobalItem.Shared != null && GlobalItem.Shared.IsHealOnCooldown)
            {
                Debug.Log("Heal is on cooldown!");
                return;
            }

            if (GlobalItem.Shared != null && !GlobalItem.Shared.UseAbleItem(ItemType.Potion))
            {
                Debug.Log("No heal items available!");
                return;
            }

            StartCoroutine(GlobalItem.Shared?.Co_PlayerHeal());
            if (healItemCount != null)
                healItemCount.text = $"{GlobalItem.Shared.UserItem?.potions}";
        }
        private void PlayerHasteItem()
        {
            if (GlobalItem.Shared != null && GlobalItem.Shared.IsHasteOnCooldown)
            {
                Debug.Log("Haste is on cooldown!");
                return;
            }

            if (GlobalItem.Shared != null && !GlobalItem.Shared.UseAbleItem(ItemType.Haste))
            {
                Debug.Log("No haste items available!");
                return;
            }

            StartCoroutine(GlobalItem.Shared?.Co_PlayerHaste());
            if (hasteItemCount != null)
                hasteItemCount.text = $"{GlobalItem.Shared.UserItem?.hastes}";
        }
        private void PlayerGodItem()
        {
            if (GlobalItem.Shared != null && GlobalItem.Shared.IsGodOnCooldown)
            {
                Debug.Log("God mode is on cooldown!");
                return;
            }

            if (GlobalItem.Shared != null && !GlobalItem.Shared.UseAbleItem(ItemType.God))
            {
                Debug.Log("No god items available!");
                return;
            }

            StartCoroutine(GlobalItem.Shared?.Co_PlayerGod());
            if (godItemCount != null)
                godItemCount.text = $"{GlobalItem.Shared.UserItem?.gods}";
        }
        private void PlayerFeverItem()
        {
            if(GlobalItem.Shared != null && GlobalItem.Shared.IsFeverOnCooldown)
            {
                Debug.Log("Fiver mode is on cooldown!");
                return;
            }

            if (GlobalItem.Shared != null && !GlobalItem.Shared.UseAbleItem(ItemType.Fiver))
            {
                Debug.Log("No fever items available!");
                return;
            }

            StartCoroutine(GlobalItem.Shared?.Co_PlayerFever());
            if (feverItemCount != null)
                feverItemCount.text = $"{GlobalItem.Shared.UserItem?.fever}";

        }
        private void RandomBoxItem()
        {
            if (GlobalItem.Shared != null && GlobalItem.Shared.IsRandomBoxOnCooldown)
            {
                Debug.Log("Random box is on cooldown!");
                return;
            }

            if (GlobalItem.Shared != null && !GlobalItem.Shared.UseAbleItem(ItemType.RandomBox))
            {
                Debug.Log("No random box items available!");
                return;
            }

            GlobalItem.Shared?.ExecuteRandomBoxItem();
            if (randomboxItemCount != null)
                randomboxItemCount.text = $"{GlobalItem.Shared.UserItem?.randomboxes}";
        }

        private IEnumerator UpdateCooldowns()
        {
            while (true)
            {
                if (GlobalItem.Shared != null)
                {
                    // Heal Cooldown
                    if (GlobalItem.Shared.IsHealOnCooldown)
                    {
                        if (healCooldownImage != null)
                        {
                            float fillAmount = GlobalItem.Shared.HealCooldownRemaining / GlobalItem.Shared.HealCooldownMax;
                            healCooldownImage.fillAmount = fillAmount;
                        }
                        if (healbutton != null)
                            healbutton.interactable = false;
                    }
                    else
                    {
                        if (healCooldownImage != null)
                            healCooldownImage.fillAmount = 0;
                        if (healbutton != null)
                            healbutton.interactable = true;
                    }

                    // Haste Cooldown
                    if (GlobalItem.Shared.IsHasteOnCooldown)
                    {
                        if (hasteCooldownImage != null)
                        {
                            float fillAmount = GlobalItem.Shared.HasteCooldownRemaining / GlobalItem.Shared.HasteCooldownMax;
                            hasteCooldownImage.fillAmount = fillAmount;
                        }
                        if (hastebutton != null)
                            hastebutton.interactable = false;
                    }
                    else
                    {
                        if (hasteCooldownImage != null)
                            hasteCooldownImage.fillAmount = 0;
                        if (hastebutton != null)
                            hastebutton.interactable = true;
                    }

                    // God Cooldown
                    if (GlobalItem.Shared.IsGodOnCooldown)
                    {
                        if (godCooldownImage != null)
                        {
                            float fillAmount = GlobalItem.Shared.GodCooldownRemaining / GlobalItem.Shared.GodCooldownMax;
                            godCooldownImage.fillAmount = fillAmount;
                        }
                        if (godbutton != null)
                            godbutton.interactable = false;
                    }
                    else
                    {
                        if (godCooldownImage != null)
                            godCooldownImage.fillAmount = 0;
                        if (godbutton != null)
                            godbutton.interactable = true;
                    }

                    // RandomBox Cooldown
                    if (GlobalItem.Shared.IsRandomBoxOnCooldown)
                    {
                        if (randomboxCooldownImage != null)
                        {
                            float fillAmount = GlobalItem.Shared.RandomBoxCooldownRemaining / GlobalItem.Shared.RandomBoxCooldownMax;
                            randomboxCooldownImage.fillAmount = fillAmount;
                        }
                        if (radomboxbutton != null)
                            radomboxbutton.interactable = false;
                    }
                    else
                    {
                        if (randomboxCooldownImage != null)
                            randomboxCooldownImage.fillAmount = 0;
                        if (radomboxbutton != null)
                            radomboxbutton.interactable = true;
                    }

                    // Fiver Cooldown
                    if (GlobalItem.Shared.IsFeverOnCooldown)
                    {
                        if (feverCooldownImage != null)
                        {
                            float fillAmount = GlobalItem.Shared.FeverCooldonwRemaining/ GlobalItem.Shared.FeverCooldownMax;
                            feverCooldownImage.fillAmount = fillAmount;
                        }
                        if (feverbutton != null)
                            feverbutton.interactable = false;
                    }
                    else
                    {
                        if (feverCooldownImage!= null)
                            feverCooldownImage.fillAmount = 0;
                        if (feverbutton != null)
                            feverbutton.interactable = true;
                    }
                }

                yield return null;
            }
        }

        /// <summary>
        /// 점수 자동 증가를 시작합니다
        /// </summary>
        private void StartScoreAutoIncrease()
        {
            if (scoreIncreaseCoroutine == null)
            {
                scoreIncreaseCoroutine = StartCoroutine(ScoreAutoIncreaseCoroutine());
                "[InGameScreen] Score auto increase started".Log();
            }
        }

        /// <summary>
        /// 점수 자동 증가를 중지합니다
        /// </summary>
        private void StopScoreAutoIncrease()
        {
            if (scoreIncreaseCoroutine != null)
            {
                StopCoroutine(scoreIncreaseCoroutine);
                scoreIncreaseCoroutine = null;
                "[InGameScreen] Score auto increase stopped".Log();
            }
        }

        /// <summary>
        /// 2초마다 10점씩 점수를 증가시키는 코루틴
        /// Time.timeScale이 0일 때는 일시정지됩니다
        /// GameOver 상태일 때는 중지됩니다
        /// </summary>
        private IEnumerator ScoreAutoIncreaseCoroutine()
        {
            while (true)
            {
                // Time.timeScale을 고려한 대기 (일시정지 시 자동으로 멈춤)
                yield return new WaitForSeconds(SCORE_INCREASE_INTERVAL);

                // GameOver 상태 확인 - GameOver일 경우 코루틴 종료
                if (RoundSystem.Shared != null && RoundSystem.Shared.OngameOver)
                {
                    "[InGameScreen] Score auto increase stopped due to GameOver".Log();
                    scoreIncreaseCoroutine = null;
                    yield break;
                }

                // RewardUI가 존재하는지 확인 후 점수 증가
                if (RewardUI.Shared != null)
                {
                    RewardUI.Shared.SetScoreAmountToText(SCORE_INCREASE_AMOUNT);
                }
            }
        }

    }
}
