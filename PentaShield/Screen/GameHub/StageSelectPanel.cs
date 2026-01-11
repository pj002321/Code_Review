using penta;
using Cysharp.Threading.Tasks;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace penta
{
    /// <summary>
    /// 스테이지 선택 UI 관리
    /// </summary>
    public class StageSelectPanel : MonoBehaviour, ISceneChangedUpdate
    {
        [Header("Stage Type")]
        [SerializeField] private Stage stageType;

        [Header("UI References")]
        private StarList starList;
        private ScoreTextBase scoreTextBase;
        private StageWaveText stageWaveText;
        private Button startButton;
        [SerializeField] Image lockOverlay;

        public Stage StageType { get; private set; }
        public string StageName { get; private set; }
        public string LoadSceneKey { get; private set; }
        public bool IsInitalized { get; private set; } = false;

        public E_SceneChangeUpdateTime E_SceneUpdateTime { get; set; } = E_SceneChangeUpdateTime.Excute;

        private void Awake()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            if (starList == null)
            {
                starList = GetComponentInChildren<StarList>();
            }
            if (scoreTextBase == null)
            {
                scoreTextBase = GetComponentInChildren<ScoreTextBase>();
            }
            if (stageWaveText == null)
            {
                stageWaveText = GetComponentInChildren<StageWaveText>();
            }
            if (startButton == null)
            {
                startButton = GetComponentInChildren<Button>();
                $"startButton : {startButton.name}".DLog();
            }

            StageType = stageType;
            StageName = GetStageNameByType(stageType);
            LoadSceneKey = GetSceneKeyByType(stageType);

            if (scoreTextBase != null)
            {
                scoreTextBase.TargetStageName = StageName;
            }

            if (starList == null || scoreTextBase == null || startButton == null || lockOverlay == null || stageWaveText == null)
            {
                $"[{this.name}] Initialize Error! Null or Empty Object!".DError();
            }

            ChainOnLickEvent();
            IsInitalized = true;
        }

        private string GetStageNameByType(Stage type)
        {
            return type switch
            {
                Stage.ICE => PentaConst.StageSnowAgeName,
                Stage.FIRE => PentaConst.StageFireWorldName,
                Stage.STONE => PentaConst.StageStoneAgeName,
                _ => string.Empty
            };
        }

        private string GetSceneKeyByType(Stage type)
        {
            return type switch
            {
                Stage.ICE => PentaConst.KsnowAge_Scene,
                Stage.FIRE => PentaConst.KfireWorld_Scene,
                Stage.STONE => PentaConst.KstoneAge_Scene,
                _ => string.Empty
            };
        }

        protected async virtual void OnEnable()
        {
            await UniTask.WaitUntil(() => IsInitalized == true);

            if (UserDataManager.Shared != null)
            {
                await UniTask.WaitUntil(() => UserDataManager.Shared.IsInitialized == true);
            }

            if (StageType == Stage.ICE)
            {
                Unlock();
            }

            StageData targetData = null;
            if (UserDataManager.Shared == null || UserDataManager.Shared.Data == null)
            {
                $"[{this.name}] UserDataManager is not initialized, skipping unlock check".DWarning();
                UpdateWaveText(targetData);
                return;
            }

            targetData = GetStageClearData();
            ScoreTextUpdate(targetData);
            UpdateWaveText(targetData);
            if (UnlockCondition(targetData)) { Unlock(); }
        }

        protected virtual bool UnlockCondition(StageData stageData)
        {
            if (StageType == Stage.ICE) return true;

            const int UNLOCK_ROUND_THRESHOLD = 30;
            string prerequisiteStageName = GetPrerequisiteStageName();

            if (string.IsNullOrEmpty(prerequisiteStageName)) return false;

            var prerequisiteData = GetStageDataByName(prerequisiteStageName);
            if (prerequisiteData == null)
            {
                $"[{this.name}] {prerequisiteStageName} Stage Is Can't Find Clear Data".DWarning();
                return false;
            }

            return prerequisiteData.Round >= UNLOCK_ROUND_THRESHOLD;
        }

        private string GetPrerequisiteStageName()
        {
            return StageType switch
            {
                Stage.FIRE => PentaConst.StageSnowAgeName,
                Stage.STONE => PentaConst.StageFireWorldName,
                _ => string.Empty
            };
        }

        public void Unlock()
        {
            if (lockOverlay == null)
            {
                $"[{this.name}] lockOverlay Component Is Missing!".DError();
                return;
            }
            lockOverlay.gameObject.SetActive(false);
        }

        private void ChainOnLickEvent()
        {
            if (startButton == null)
            {
                $"[{this.name}] Start Button Is Missing!".DError();
                return;
            }
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(async () =>
            {
                if (SceneSystem.IsUIInputBlocked)
                {
                    $"[{this.name}] Click ignored due to input block window".DWarning();
                    return;
                }
                await LoadStage(LoadSceneKey);
            });
        }

        private async UniTask LoadStage(string sceneKey)
        {
            $"[{this.name}] 씬 로딩 시작: {sceneKey}".DLog();

            if (SceneSystem.Shared == null)
            {
                $"[{this.name}] SceneSystem.Shared가 null입니다!".DError();
                return;
            }

            bool success = await SceneSystem.Shared.LoadScene(sceneKey);
            if (success)
            {
                $"[{this.name}] 씬 로딩 성공: {sceneKey}".DLog();
            }
            else
            {
                $"[{this.name}] 씬 로딩 실패: {sceneKey}".DError();
            }
        }

        protected virtual void ScoreTextUpdate(StageData data)
        {
            if (scoreTextBase == null) return;

            if (data == null)
            {
                if (scoreTextBase.ScoreText != null)
                {
                    scoreTextBase.ScoreText.text = "-";
                }
                return;
            }

            if (scoreTextBase.ScoreText != null)
            {
                scoreTextBase.ScoreText.text = data.Score.ToString("F0");
            }

            starList?.ActiveStarList((int)data.Score).Forget();
        }

        protected virtual void UpdateWaveText(StageData stageData)
        {
            if (stageWaveText == null) return;
            int wave = stageData?.Round ?? 0;
            stageWaveText.SetWaveText(wave);
        }

        protected StageData GetStageClearData()
        {
            if (StageName.IsNullOrEmpty()) return null;
            return UserDataManager.Shared.Data.StageDatas.FirstOrDefault(datas => datas.StageName == StageName);
        }

        protected StageData GetStageDataByName(string stageName)
        {
            if (string.IsNullOrEmpty(stageName)) return null;
            if (UserDataManager.Shared == null || UserDataManager.Shared.Data == null) return null;
            return UserDataManager.Shared.Data.StageDatas.FirstOrDefault(datas => datas.StageName == stageName);
        }

        public void Excute()
        {
            StageData stageData = GetStageClearData();
            ScoreTextUpdate(stageData);
            UpdateWaveText(stageData);
        }
    }
}