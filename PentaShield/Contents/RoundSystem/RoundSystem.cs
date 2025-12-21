using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using PentaShield;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using Sirenix.OdinInspector;
#endif

namespace chaos
{
    /// <summary>
    /// 라운드 기반 게임 시스템 관리
    /// - 라운드 진행 및 전환 제어
    /// - 게임 상태 관리 (일시정지/재개)
    /// - 게임 종료 처리 (게임오버/클리어)
    /// </summary>
    public partial class RoundSystem : MonoBehaviourSingleton<RoundSystem>
    {
        #region Constants
        private const int COUNTDOWN_DURATION = 3;
        private const float BANNER_LIFETIME = 3f;
        private const int MIN_ROUND = 1;
        #endregion

        #region Fields
#if UNITY_EDITOR
        [BoxGroup("Debug Tools")]
        [SerializeField, LabelText("점프할 라운드")] 
        private int debugJumpRound = 1;
#endif
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI roundText;
        [SerializeField] private GameObject countdownUI;
        [SerializeField] private TextMeshProUGUI gameOverText;
        [SerializeField] private GameObject gameResultWindow;
        [SerializeField] private GameObject stageBanner;

        private GameTimer gameTimer;
        private GameObject upgradeViewer;

        private TextMeshProUGUI countdownText;
        private Coroutine countdownCoroutine;
        private InGameSceneBase inGameScene;
        private int currentRound = MIN_ROUND;

        public int CurrentRound => currentRound;
        public bool OngameOver { get; private set; }
        public bool OngameClear { get; private set; }
        public bool IsRoundActive { get; private set; }
        protected override bool DontDestroy => true;
        #endregion

        #region Events
        public event Func<int, List<bool>> OnRoundStart;
        public event Func<int, List<bool>> OnRoundChange;
        public event Action<int> OnRoundEnd;
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();
        }

        private void Start()
        {
            _ = InitializeAsync();
        }

        protected override void OnDestroy()
        {
            Cleanup();
            base.OnDestroy();
        }
        #endregion

        #region Initialization
        /// <summary>
        /// 초기화 순서: UI/Timer/Scene → UpgradeTable 초기화 대기 → 원소 스폰 → 라운드 시작
        /// </summary>
        private async UniTask InitializeAsync()
        {
            InitializeUI();
            InitializeTimer();
            InitializeSceneReference();
            
            await WaitForUpgradeTable();
            await SpawnDefaultElemental();
            await UniTask.Yield();
            StartRound(currentRound);
        }

        private void InitializeUI()
        {
            countdownText = countdownUI?.GetComponentInChildren<TextMeshProUGUI>();
            countdownUI?.SetActive(false);
            UpdateRoundDisplay();
        }

        private void InitializeTimer()
        {
            gameTimer ??= GetComponent<GameTimer>();
            
            if (gameTimer == null)
            {
                "[RoundSystem] GameTimer를 찾을 수 없습니다. Inspector에서 할당하거나 같은 GameObject에 GameTimer 컴포넌트를 추가하세요.".DWarning();
                return;
            }
            
            gameTimer.onTimerComplete.AddListener(() => _ = CompleteRound());
        }

        private void InitializeSceneReference()
        {
            inGameScene = FindAnyObjectByType<InGameSceneBase>();
            if (inGameScene == null)
            {
                "[RoundSystem] InGameSceneBase를 찾을 수 없습니다.".DWarning();
            }
        }

        private async UniTask WaitForUpgradeTable()
        {
            await UniTask.WaitUntil(() => UpgradeTable.Shared?.Initalize == true);
        }

        private async UniTask SpawnDefaultElemental()
        {
            if (UpgradeTable.Shared == null)
            {
                "[RoundSystem] UpgradeTable.Shared is null, skipping default elemental spawn".DWarning();
                return;
            }
            
            await UpgradeTable.Shared.OnSpawnElemental("", isRandom: true);
        }

        private void Cleanup()
        {
            if (countdownCoroutine != null)
            {
                StopCoroutine(countdownCoroutine);
                countdownCoroutine = null;
            }
        }
        #endregion

        #region Round Flow
        /// <summary>라운드 시작</summary>
        private void StartRound(int round)
        {
            IsRoundActive = true;
            OnRoundStart?.Invoke(round);
            $"[RoundSystem] Round {round} started".DLog();
        }

        /// <summary>라운드 완료</summary>
        private async UniTask CompleteRound()
        {
            if (!IsRoundActive || OngameOver) return;

            $"[RoundSystem] Round {CurrentRound} completed".DLog();
            IsRoundActive = false;

            SetGamePaused(true);
            OnRoundEnd?.Invoke(currentRound);

            if (!ValidateNextRound())
            {
                await TriggerGameComplete();
                return;
            }

            ProgressToNextRound();
            ShowUpgradeScreen();
        }

        /// <summary>다음 라운드로 진행</summary>
        private void ProgressToNextRound()
        {
            currentRound++;
            UpdateRoundDisplay();
            $"[RoundSystem] Advanced to round {currentRound}".DLog();
        }

        /// <summary>라운드 재개</summary>
        private void ResumeRound()
        {
            if (!PrepareRoundSpawn())
            {
                $"[RoundSystem] Failed to prepare round {CurrentRound}".DError();
                return;
            }

            SetGamePaused(false);
            IsRoundActive = true;
            $"[RoundSystem] Round {CurrentRound} resumed".DLog();
        }

        /// <summary>라운드 강제 설정</summary>
        public bool SetCurrentRound(int targetRound)
        {
            if (targetRound < MIN_ROUND || CurrentRound >= targetRound)
            {
                $"[RoundSystem] Invalid round: {targetRound}".DWarning();
                return false;
            }

            currentRound = targetRound;
            List<bool> result = OnRoundChange?.Invoke(currentRound);
            return result?.Count > 0 && result[0];
        }
        #endregion

        #region Round Valid

        /// <summary>다음 라운드 존재 여부 확인</summary>
        private bool ValidateNextRound()
        {
            List<bool> result = OnRoundChange?.Invoke(currentRound + 1);
            bool hasNext = result?.Count > 0 && result[0];
            
            string status = hasNext ? "exists" : "not found";
            $"[RoundSystem] Next round ({currentRound + 1}): {status}".DLog();
            
            return hasNext;
        }

        /// <summary>라운드 스폰 준비</summary>
        private bool PrepareRoundSpawn()
        {
            List<bool> result = OnRoundChange?.Invoke(CurrentRound);
            bool success = result?.Count > 0 && result[0];
            
            if (!success)
            {
                $"[RoundSystem] Spawn preparation failed for round {CurrentRound}".DError();
            }
            
            return success;
        }

        /// <summary>스테이지 스폰 종료 콜백</summary>
        public void OnStageSpawnEnd()
        {
            $"[RoundSystem] Stage spawn completed".DLog();
        }
        #endregion

        #region Game State Control
        private void SetGamePaused(bool paused)
        {
            Time.timeScale = paused ? 0f : 1f;
            
            if (!paused)
            {
                gameTimer?.ResetTimer();
                gameTimer?.StartTimer();
            }
        }

        public void OnContinueButtonPressed()
        {
            if (IsUpgradeViewerActive())
            {
                AudioHelper.PlaySFX(AudioConst.UPGRADEFAIL, 0.5f);
                return;
            }

            HideUpgradeViewer();
            BeginRoundCountdown();
        }

        private void BeginRoundCountdown()
        {
            if (countdownUI == null || countdownText == null)
            {
                ResumeRound();
                return;
            }

            if (countdownCoroutine != null) 
                StopCoroutine(countdownCoroutine);
            
            countdownUI.SetActive(true);
            countdownCoroutine = StartCoroutine(CountdownSequence());
        }

        private IEnumerator CountdownSequence()
        {
            for (int count = COUNTDOWN_DURATION; count > 0; count--)
            {
                countdownText.text = count.ToString();
                yield return new WaitForSecondsRealtime(1f);
            }

            countdownUI.SetActive(false);
            DisplayRoundBanner();
            ResumeRound();
        }
        #endregion

        #region UI Management
        private void UpdateRoundDisplay()
        {
            roundText?.SetText(CurrentRound.ToString());
        }

        private void ShowUpgradeScreen()
        {
            UpgradeViewer.Shared?.OnUpgradeUI();
            // 레거시 fallback: UpgradeViewer.Shared가 없을 때만 사용
            if (UpgradeViewer.Shared == null)
            {
                upgradeViewer?.SetActive(true);
            }
        }

        private void HideUpgradeViewer()
        {
            UpgradeViewer.Shared?.DisplayActive(false);
        }

        private bool IsUpgradeViewerActive()
        {
            return UpgradeViewer.Shared?.IsActive() ?? false;
        }

        private void DisplayRoundBanner()
        {
            if (stageBanner == null) return;

            var banner = Instantiate(stageBanner, transform);
            banner.transform.localPosition = Vector3.zero;

            var text = banner.GetComponentInChildren<TextMeshProUGUI>();
            text?.SetText($"WAVE {CurrentRound}");

            banner.GetComponent<Animator>()?.SetTrigger(PentaConst.tMove);

            Destroy(banner, BANNER_LIFETIME);
        }

        private void HideAllUpgrades()
        {
            UpgradeViewer.Shared?.DisplayActive(false);
            // 레거시 fallback도 함께 비활성화
            upgradeViewer?.SetActive(false);
        }

        private void ShowGameResultScreen()
        {
            gameResultWindow?.SetActive(true);
        }

        private async UniTask DisplayGameOverSequence()
        {
            ShowGameResultScreen();

            if (gameOverText != null)
            {
                gameOverText.gameObject.SetActive(true);
                await TextEffect.FadeOutGameOverTextAsync(gameOverText);
            }
        }
        #endregion

        #region Game Over / Clear
        public async UniTask GameOver()
        {
            if (OngameOver) return;
            
            OngameOver = true;
            IsRoundActive = false;

            await SaveGameResult();
            CleanupGameSession();
            
            AudioHelper.PlaySFX(AudioConst.GAMEOVER, 1.5f);
            await DisplayGameOverSequence();
        }

        private async UniTask TriggerGameComplete()
        {
            OngameClear = true;
            IsRoundActive = false;
            
            $"[RoundSystem] Game completed at round {CurrentRound}".DLog();
            await SaveGameResult();
        }

        private void CleanupGameSession()
        {
            GlobalItem.Shared?.CleanupOnGameOver();
        }
        #endregion

        #region Data Persistence
        private async UniTask SaveGameResult()
        {
            ShowGameResultScreen();

            var stageData = BuildStageData();
            if (stageData == null || !ValidateUserDataAccess())
            {
                return;
            }

            if (TrySaveStageData(stageData))
            {
                UpdatePlayerRecords();
                await PersistUserData();
                BroadcastDataUpdate();
            }
        }

        private StageData BuildStageData()
        {
            string stageName = inGameScene?.Name ?? string.Empty;

            if (string.IsNullOrEmpty(stageName))
            {
                "[RoundSystem] Invalid stage name".DError();
                return null;
            }

            return new StageData
            {
                SaveTime = DateTime.UtcNow,
                Round = CurrentRound,
                StageName = stageName,
                Score = RewardUI.Shared?.Score ?? 0
            };
        }

        private bool ValidateUserDataAccess()
        {
            if (UserDataManager.Shared?.Data == null)
            {
                "[RoundSystem] User data unavailable".DError();
                return false;
            }
            return true;
        }

        private bool TrySaveStageData(StageData data)
        {
            if (!UserDataManager.Shared.Data.AddStageData(data))
            {
                "[RoundSystem] Failed to save stage data".DError();
                return false;
            }

            $"[RoundSystem] Stage data saved: Round {data.Round}".DLog();
            return true;
        }

        private void UpdatePlayerRecords()
        {
            var userData = UserDataManager.Shared?.Data;
            if (userData == null) return;

            if (CurrentRound > userData.HighestWave)
            {
                int previousRecord = userData.HighestWave;
                userData.HighestWave = CurrentRound;
                $"[RoundSystem] 🎉 New record! {previousRecord} → {CurrentRound}".DLog();
            }
        }

        private async UniTask PersistUserData()
        {
            var userManager = UserDataManager.Shared;
            if (userManager == null) return;

            bool isAnonymous = await userManager.IsAnonymouseUserAsync();

            if (isAnonymous)
            {
                await userManager.UpdateUserDataAsync();
            }
            else
            {
                await userManager.FirebaseSaveUserData();
            }
        }

        private void BroadcastDataUpdate()
        {
            UserDataManager.Shared?.NotifyDataUpdated();
            if (UserDataManager.Shared != null)
            {
                "[RoundSystem] ✅ Data persisted and broadcasted".DLog();
            }
        }
        #endregion

        #region Editor Tools
#if UNITY_EDITOR
        [BoxGroup("Debug Tools")]
        [Button("라운드로 점프", ButtonSizes.Large), GUIColor(0.4f, 0.8f, 1f)]
        private void EditorJumpToRound()
        {
            if (!ValidateEditorPlayMode()) return;
            if (!ValidateRoundNumber(debugJumpRound)) return;

            JumpToRound(debugJumpRound);
        }

        [BoxGroup("Debug Tools")]
        [Button("현재 라운드 건너뛰기", ButtonSizes.Medium), GUIColor(1f, 0.8f, 0.4f)]
        private void EditorSkipCurrentRound()
        {
            if (!ValidateEditorPlayMode()) return;
            CompleteRound();
        }

        private bool ValidateEditorPlayMode()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[RoundSystem] Play mode required");
                return false;
            }
            return true;
        }

        private bool ValidateRoundNumber(int round)
        {
            if (round < MIN_ROUND)
            {
                Debug.LogWarning($"[RoundSystem] Round must be >= {MIN_ROUND}");
                return false;
            }
            return true;
        }

        public void JumpToRound(int targetRound)
        {
            if (!ValidateRoundNumber(targetRound)) return;

            $"[RoundSystem] Jumping from round {CurrentRound} to {targetRound}".DLog();

            gameTimer?.ResetTimer();
            SetCurrentRound(targetRound);
            UpdateRoundDisplay();
            HideAllUpgrades();
            SetGamePaused(false);
            ResumeRound();
        }
#endif
        #endregion
    }
}
