using Cysharp.Threading.Tasks;
using Hunt.Common;
using UnityEngine;

namespace Hunt
{
    /// <summary> core@scene 단일 로드 후, village_xxxx / fielddungeon_xxxx Env를 Additive로 전환. 카메라는 각 Env 씬에서 처리 </summary>
    public class InGameCoreScreen : MonoBehaviour
    {
        private void Awake()
        {
            InGameService.OnMapChangeResponse += OnMapChangeResponse;
        }

        private void OnDestroy()
        {
            InGameService.OnMapChangeResponse -= OnMapChangeResponse;
        }

        private async void Start()
        {
            await UniTask.WaitUntil(() => AudioManager.Shared);

            uint currentMapId = GameSession.Shared?.CurrentMapId ?? 0;
            if (currentMapId == 0 || (currentMapId < 24000 || currentMapId >= 25000) && (currentMapId < 27000 || currentMapId >= 28000))
            {
                currentMapId = 24000;
            }

            PlayBgmBySceneType(GameSession.Shared.GetSceneTypeByMapId(currentMapId));

            var player = await GameSession.Shared.SpawnLocalPlayer(Vector3.zero);
            if (player == null)
            {
                this.DError("플레이어 스폰 실패.");
                return;
            }

            await WorldMapManager.Shared.LoadMapEnv(currentMapId, GameSession.Shared.GetSceneTypeByMapId(currentMapId));

            // core 씬 + Env까지 모두 로드된 뒤에 페이드아웃
            await SceneLoadHelper.Shared.CompleteDeferredFadeOut();

            GameSession.Shared?.SetCurrentMap(currentMapId);
            RefreshHUD();
            PositionPlayerAtPortal();
        }

        /// <summary> 서버 맵 변경 응답 — Core 유지, Env만 교체 </summary>
        private void OnMapChangeResponse(ErrorType errorType, uint newMapId)
        {
            if (errorType != ErrorType.ErrNon)
            {
                $"[CoreScreen] 맵 변경 실패: {errorType}".DError();
                return;
            }
            $"[CoreScreen] 맵 변경 승인: {newMapId}".DLog();
            ReplaceEnvByMapId(newMapId).Forget();
        }

        private void PlayBgmBySceneType(SceneType sceneType)
        {
            var key = sceneType == SceneType.Village
                ? AudioKeyConst.GetSfxKey(AudioType.BGM_VILLAGE)
                : AudioKeyConst.GetSfxKey(AudioType.BGM_FIELD);
            AudioManager.Shared.PlayBgm(key);
        }

        private void RefreshHUD()
        {
            if (InGameHud.Shared == null) return;
            var panels = new MonoBehaviour[]
            {
                InGameHud.Shared.SettingPanel,
                InGameHud.Shared.CharStatPanel,
                InGameHud.Shared.CharInventoryPanel
            };
            foreach (var panel in panels)
            {
                if (panel != null && panel.gameObject.activeSelf)
                {
                    panel.gameObject.SetActive(false);
                    panel.gameObject.SetActive(true);
                }
            }
        }

        private void PositionPlayerAtPortal()
        {
            var transitionInfo = WorldMapManager.Shared?.GetAndClearTransitionInfo();
            if (transitionInfo.HasValue)
            {
                GameSession.Shared?.MovePlayerToPortal(transitionInfo.Value.spawnDirection);
            }
        }

        /// <summary> Core는 유지하고, mapId에 맞는 Env만 Additive로 교체 </summary>
        private async UniTaskVoid ReplaceEnvByMapId(uint mapId)
        {
            var sceneType = GameSession.Shared.GetSceneTypeByMapId(mapId);
            await WorldMapManager.Shared.LoadMapEnv(mapId, sceneType);
            GameSession.Shared?.SetCurrentMap(mapId);
            PlayBgmBySceneType(sceneType);
            RefreshHUD();
            PositionPlayerAtPortal();
        }
    }
}
