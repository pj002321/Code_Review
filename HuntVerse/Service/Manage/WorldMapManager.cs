using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace Hunt
{
    /// <summary> 맵 Env 로드 및 전환 관리 </summary>
    public class WorldMapManager : MonoBehaviourSingleton<WorldMapManager>
    {
        private FieldTransitionInfo? currentTransition;
        private SceneInstance currentEnvScene;
        private GameObject currentMapNameUI;
        private bool isLoadingEnv;

        protected override bool DontDestroy => true;

        /// <summary> 맵 Env 로드 (Additive Scene). 맵 이름 UI는 InGameHud 하위에 생성·교체 </summary>
        public async UniTask LoadMapEnv(uint mapId, SceneType sceneType)
        {
            if (isLoadingEnv) return;
            isLoadingEnv = true;

            try
            {
                this.DLog($"MapId : {mapId} , SceneType : {sceneType}");

                if (currentMapNameUI != null)
                {
                    Destroy(currentMapNameUI);
                    currentMapNameUI = null;
                }
                await UniTask.Yield();

                if (currentEnvScene.Scene.IsValid())
                {
                    await SceneLoadHelper.Shared.UnloadSceneAdditive(currentEnvScene);
                }

                string envKey = GetEnvKey(mapId, sceneType);
                try
                {
                    currentEnvScene = await SceneLoadHelper.Shared.LoadSceneAdditiveMode(envKey);
                }
                catch (System.Exception e)
                {
                    this.DError($"Env 씬 로드 실패: {envKey}, {e.Message}");
                    return;
                }

                $"[WorldMapManager] 맵 Env 로드 완료: {mapId}".DLog();
                try
                {
                    var mapNameGo = await AbLoader.Shared.LoadInstantiateAsync(ResourceKeyConst.Kp_MapNameInfoUI);
                    if (mapNameGo == null) return;
                    var tmp = mapNameGo.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                    if (tmp != null) tmp.text = BindKeyConst.GetMapNameByMapId(mapId);
                    currentMapNameUI = mapNameGo;
                    InGameHud.Shared?.StagePanel?.UpdateStagePanel(mapId);
                }
                catch (System.Exception e)
                {
                    this.DError($"MapName UI 생성 실패: {e.Message}");
                }
            }
            finally
            {
                isLoadingEnv = false;
            }
        }

        /// <summary> mapId에 맞는 Env 씬 키 (ID값으로 씬 갈아끼움) </summary>
        private string GetEnvKey(uint mapId, SceneType sceneType)
        {
            return sceneType switch
            {
                SceneType.Village => $"village_{mapId}@scene",
                SceneType.FieldDungeon => $"fielddungeon_{mapId}@scene",
                SceneType.Town => $"town_{mapId}@scene",
                _ => $"map_{mapId}@scene"
            };
        }

        #region Field Transition

        /// <summary> 필드 전환 정보 저장 </summary>
        public void SetTransitionInfo(FieldTransitionInfo info)
        {
            currentTransition = info;
            this.DLog($"전환 정보 저장: {info.entryDirection} → {info.spawnDirection}");
        }

        /// <summary> 전환 정보 가져오고 초기화 </summary>
        public FieldTransitionInfo? GetAndClearTransitionInfo()
        {
            var info = currentTransition;
            currentTransition = null;
            return info;
        }

        #endregion
    }
}
