using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using penta;

namespace penta
{
    public enum Stage
    {
        ICE, FIRE, STONE
    }

    /// <summary>
    /// 인게임 화면 관리
    /// - 글로벌 아이템 사용
    /// - 아이템 쿨다운 관리
    /// - 게임 일시정지/재개
    /// - 점수 자동 증가
    /// - 씬 로드/언로드
    /// </summary>
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
        [SerializeField] private TextMeshProUGUI randomboxItemCount;
        [SerializeField] private TextMeshProUGUI healItemCount;
        [SerializeField] private TextMeshProUGUI hasteItemCount;
        [SerializeField] private TextMeshProUGUI godItemCount;
        [SerializeField] private TextMeshProUGUI feverItemCount;
        [SerializeField] private Image randomboxCooldownImage;
        [SerializeField] private Image healCooldownImage;
        [SerializeField] private Image hasteCooldownImage;
        [SerializeField] private Image godCooldownImage;
        [SerializeField] private Image feverCooldownImage;

        private Coroutine scoreIncreaseCoroutine;
        private const float SCORE_INCREASE_INTERVAL = 3f;
        private const int SCORE_INCREASE_AMOUNT = 9;

        private bool isLoadingScene = false;

        public Stage stageType => _stageType;

        private void Awake()
        {
            HandleButtonsBehaviour();
            StartCoroutine(UpdateCooldowns());
            InitializeItemCountsAsync().Forget();
            StartScoreAutoIncrease();
        }

        private void OnDestroy()
        {
            StopScoreAutoIncrease();

            if (UserDataManager.Shared?.ItemData != null)
            {
                UserDataManager.Shared.ItemData.OnItemCountChanged -= HandleItemCountChanged;
            }
        }

        /// <summary> 씬 로드 </summary>
        public async UniTask LoadStage(string sceneKey)
        {
            if (isLoadingScene) return;
            if (SceneSystem.IsUIInputBlocked) return;

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

        /// <summary> 씬 언로드 </summary>
        public async UniTask UnloadStage()
        {
            await SceneSystem.Shared.UnloadTopScene();
        }

        /// <summary> 버튼 동작 설정 </summary>
        public void HandleButtonsBehaviour()
        {
            ExitButtonBehaviour();
            ItemButtonBehaviour();
            SoundButtonBehaviour();
            PauseButtonBehaviour();
        }

        private void SoundButtonBehaviour()
        {
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
            stopButton?.onClick.RemoveAllListeners();
            stopButton?.onClick.AddListener(() => PauseGame());

            resumButton?.onClick.RemoveAllListeners();
            resumButton?.onClick.AddListener(() => ResumeGame());
        }

        /// <summary> 게임 일시정지 </summary>
        private void PauseGame()
        {
            Time.timeScale = 0f;
            stopButton?.gameObject.SetActive(false);
            resumButton?.gameObject.SetActive(true);
        }

        /// <summary> 게임 재개 </summary>
        private void ResumeGame()
        {
            Time.timeScale = 1f;
            resumButton?.gameObject.SetActive(false);
            stopButton?.gameObject.SetActive(true);
        }

        private void ExitButtonBehaviour()
        {
            foreach (var button in exitButtons)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => HandleExitButton());
            }
        }

        /// <summary> 나가기 버튼 처리 </summary>
        private void HandleExitButton()
        {
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
        }

        /// <summary> 아이템 개수 초기화 </summary>
        private async UniTaskVoid InitializeItemCountsAsync()
        {
            await UniTask.WaitUntil(() => UserDataManager.Shared != null && UserDataManager.Shared.IsInitialized);

            var item = UserDataManager.Shared.ItemData;
            if (item != null)
            {
                UpdateItemCountUI(ItemType.RandomBox, item.randomboxes);
                UpdateItemCountUI(ItemType.Potion, item.potions);
                UpdateItemCountUI(ItemType.Haste, item.hastes);
                UpdateItemCountUI(ItemType.God, item.gods);
                UpdateItemCountUI(ItemType.Fiver, item.fever);

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

        /// <summary> 힐 아이템 사용 </summary>
        private void PlayerHealItem()
        {
            if (GlobalItem.Shared != null && GlobalItem.Shared.IsHealOnCooldown) return;
            if (GlobalItem.Shared != null && !GlobalItem.Shared.UseAbleItem(ItemType.Potion)) return;

            StartCoroutine(GlobalItem.Shared?.Co_PlayerHeal());
            if (healItemCount != null)
                healItemCount.text = $"{GlobalItem.Shared.UserItem?.potions}";
        }

        /// <summary> 헤이스트 아이템 사용 </summary>
        private void PlayerHasteItem()
        {
            if (GlobalItem.Shared != null && GlobalItem.Shared.IsHasteOnCooldown) return;
            if (GlobalItem.Shared != null && !GlobalItem.Shared.UseAbleItem(ItemType.Haste)) return;

            StartCoroutine(GlobalItem.Shared?.Co_PlayerHaste());
            if (hasteItemCount != null)
                hasteItemCount.text = $"{GlobalItem.Shared.UserItem?.hastes}";
        }

        /// <summary> 갓 아이템 사용 </summary>
        private void PlayerGodItem()
        {
            if (GlobalItem.Shared != null && GlobalItem.Shared.IsGodOnCooldown) return;
            if (GlobalItem.Shared != null && !GlobalItem.Shared.UseAbleItem(ItemType.God)) return;

            StartCoroutine(GlobalItem.Shared?.Co_PlayerGod());
            if (godItemCount != null)
                godItemCount.text = $"{GlobalItem.Shared.UserItem?.gods}";
        }

        /// <summary> 피버 아이템 사용 </summary>
        private void PlayerFeverItem()
        {
            if (GlobalItem.Shared != null && GlobalItem.Shared.IsFeverOnCooldown) return;
            if (GlobalItem.Shared != null && !GlobalItem.Shared.UseAbleItem(ItemType.Fiver)) return;

            StartCoroutine(GlobalItem.Shared?.Co_PlayerFever());
            if (feverItemCount != null)
                feverItemCount.text = $"{GlobalItem.Shared.UserItem?.fever}";
        }

        /// <summary> 랜덤박스 아이템 사용 </summary>
        private void RandomBoxItem()
        {
            if (GlobalItem.Shared != null && GlobalItem.Shared.IsRandomBoxOnCooldown) return;
            if (GlobalItem.Shared != null && !GlobalItem.Shared.UseAbleItem(ItemType.RandomBox)) return;

            GlobalItem.Shared?.ExecuteRandomBoxItem();
            if (randomboxItemCount != null)
                randomboxItemCount.text = $"{GlobalItem.Shared.UserItem?.randomboxes}";
        }

        /// <summary> 아이템 쿨다운 업데이트 </summary>
        private IEnumerator UpdateCooldowns()
        {
            while (true)
            {
                if (GlobalItem.Shared != null)
                {
                    UpdateHealCooldown();
                    UpdateHasteCooldown();
                    UpdateGodCooldown();
                    UpdateRandomBoxCooldown();
                    UpdateFeverCooldown();
                }

                yield return null;
            }
        }

        private void UpdateHealCooldown()
        {
            if (GlobalItem.Shared.IsHealOnCooldown)
            {
                if (healCooldownImage != null)
                {
                    float fillAmount = GlobalItem.Shared.HealCooldownRemaining / GlobalItem.Shared.HealCooldownMax;
                    healCooldownImage.fillAmount = fillAmount;
                }
                if (healbutton != null) healbutton.interactable = false;
            }
            else
            {
                if (healCooldownImage != null) healCooldownImage.fillAmount = 0;
                if (healbutton != null) healbutton.interactable = true;
            }
        }

        private void UpdateHasteCooldown()
        {
            if (GlobalItem.Shared.IsHasteOnCooldown)
            {
                if (hasteCooldownImage != null)
                {
                    float fillAmount = GlobalItem.Shared.HasteCooldownRemaining / GlobalItem.Shared.HasteCooldownMax;
                    hasteCooldownImage.fillAmount = fillAmount;
                }
                if (hastebutton != null) hastebutton.interactable = false;
            }
            else
            {
                if (hasteCooldownImage != null) hasteCooldownImage.fillAmount = 0;
                if (hastebutton != null) hastebutton.interactable = true;
            }
        }

        private void UpdateGodCooldown()
        {
            if (GlobalItem.Shared.IsGodOnCooldown)
            {
                if (godCooldownImage != null)
                {
                    float fillAmount = GlobalItem.Shared.GodCooldownRemaining / GlobalItem.Shared.GodCooldownMax;
                    godCooldownImage.fillAmount = fillAmount;
                }
                if (godbutton != null) godbutton.interactable = false;
            }
            else
            {
                if (godCooldownImage != null) godCooldownImage.fillAmount = 0;
                if (godbutton != null) godbutton.interactable = true;
            }
        }

        private void UpdateRandomBoxCooldown()
        {
            if (GlobalItem.Shared.IsRandomBoxOnCooldown)
            {
                if (randomboxCooldownImage != null)
                {
                    float fillAmount = GlobalItem.Shared.RandomBoxCooldownRemaining / GlobalItem.Shared.RandomBoxCooldownMax;
                    randomboxCooldownImage.fillAmount = fillAmount;
                }
                if (radomboxbutton != null) radomboxbutton.interactable = false;
            }
            else
            {
                if (randomboxCooldownImage != null) randomboxCooldownImage.fillAmount = 0;
                if (radomboxbutton != null) radomboxbutton.interactable = true;
            }
        }

        private void UpdateFeverCooldown()
        {
            if (GlobalItem.Shared.IsFeverOnCooldown)
            {
                if (feverCooldownImage != null)
                {
                    float fillAmount = GlobalItem.Shared.FeverCooldonwRemaining / GlobalItem.Shared.FeverCooldownMax;
                    feverCooldownImage.fillAmount = fillAmount;
                }
                if (feverbutton != null) feverbutton.interactable = false;
            }
            else
            {
                if (feverCooldownImage != null) feverCooldownImage.fillAmount = 0;
                if (feverbutton != null) feverbutton.interactable = true;
            }
        }

        /// <summary> 점수 자동 증가 시작 </summary>
        private void StartScoreAutoIncrease()
        {
            if (scoreIncreaseCoroutine == null)
            {
                scoreIncreaseCoroutine = StartCoroutine(ScoreAutoIncreaseCoroutine());
            }
        }

        /// <summary> 점수 자동 증가 중지 </summary>
        private void StopScoreAutoIncrease()
        {
            if (scoreIncreaseCoroutine != null)
            {
                StopCoroutine(scoreIncreaseCoroutine);
                scoreIncreaseCoroutine = null;
            }
        }

        /// <summary> 점수 자동 증가 코루틴 </summary>
        private IEnumerator ScoreAutoIncreaseCoroutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(SCORE_INCREASE_INTERVAL);

                if (RoundSystem.Shared != null && RoundSystem.Shared.OngameOver)
                {
                    scoreIncreaseCoroutine = null;
                    yield break;
                }

                if (RewardUI.Shared != null)
                {
                    RewardUI.Shared.SetScoreAmountToText(SCORE_INCREASE_AMOUNT);
                }
            }
        }
    }
}
