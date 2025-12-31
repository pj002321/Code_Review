using Cysharp.Threading.Tasks;
using System;
using System.Net;
using System.Reflection;
using System.Threading;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace Hunt
{
    public class SceneLoadHelper : MonoBehaviourSingleton<SceneLoadHelper>
    {
        // async job stop/cancel
        private CancellationTokenSource cts;
        private SceneInstance curScene;

        [Header("Loading Indicator")]
        [SerializeField] private Canvas loadingCanvas;
        [SerializeField] private CanvasGroup loadingCanvasGroup;
        [SerializeField] private float minLoadingDuration = 0.5f; 
        [SerializeField] private float fadeDuration = 0.7f; 

        protected override bool DontDestroy => base.DontDestroy;
        protected override void Awake()
        {
            base.Awake();

            if (loadingCanvas != null)
            {
                loadingCanvas.gameObject.SetActive(false);
                if (loadingCanvasGroup != null)
                {
                    loadingCanvasGroup.alpha = 0f;
                }
            }
        }

        private void Start()
        {
            cts = new CancellationTokenSource();
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
            
            if (cts != null)
            {
                try
                {
                    cts.Cancel();
                    cts.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SceneLoadHelper] OnDestroy에서 cts 정리 중 에러: {ex.Message}");
                }
                finally
                {
                    cts = null;
                }
            }
        }

        public async UniTask LoadSceneSingleMode(string key, bool isfadeactive = true)
        {
            CancelCurrentOps();

            float loadStartTime = Time.realtimeSinceStartup;

            try
            {
                // 1. 페이드 인: 로딩 화면 표시
                ShowLoadingIndicator(true);
                if (isfadeactive)
                {
                    await UIEffect.FadeIn(loadingCanvasGroup, cts.Token, fadeDuration);
                }

                // 2. 기존 씬 언로드
                if (curScene.Scene.IsValid())
                {
                    $"[SceneLoadHelper] 기존 씬 언로드 시작: {curScene.Scene.name}".DLog();
                    await Addressables.UnloadSceneAsync(curScene).ToUniTask(cancellationToken: cts.Token);
                    await UniTask.Yield(PlayerLoopTiming.PostLateUpdate, cts.Token); // 언로드 완료 대기
                    $"[SceneLoadHelper] 기존 씬 언로드 완료".DLog();
                }

                // 3. 새 씬 로드
                $"[SceneLoadHelper] 새 씬 로드 시작: {key}".DLog();
                var handle = Addressables.LoadSceneAsync(key, LoadSceneMode.Single);
                curScene = await handle.ToUniTask(cancellationToken: cts.Token);

                // 씬 활성화 대기
                await UniTask.WaitUntil(() => curScene.Scene.isLoaded, cancellationToken: cts.Token);
                $"[SceneLoadHelper] 새 씬 로드 완료: {curScene.Scene.name}".DLog();

                // 최소 로딩 시간 보장 (너무 빠른 전환으로 인한 깜빡임 방지)
                float elapsedTime = Time.realtimeSinceStartup - loadStartTime;
                if (elapsedTime < minLoadingDuration)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(minLoadingDuration - elapsedTime), cancellationToken: cts.Token);
                }

                if (isfadeactive)
                {
                    await UIEffect.FadeOut(loadingCanvasGroup,cts.Token,fadeDuration);
                }
                ShowLoadingIndicator(false);
            }
            catch (OperationCanceledException)
            {
                "[SceneLoadHelper] 씬 로드가 취소되었습니다.".DWarnning();
                ShowLoadingIndicator(false);
                throw;
            }
            catch (Exception ex)
            {
                $"[SceneLoadHelper] 씬 로드 중 오류 발생: {ex.Message}".DError();
                ShowLoadingIndicator(false);
                throw;
            }
        }

        /// <summary> Boot 씬으로 이동 (로그아웃 처리) </summary>
        public async UniTask LoadToLogOut()
        {
            if (cts == null)
            {
                cts = new CancellationTokenSource();
            }
            else
            {
                CancelCurrentOps();
            }

            float loadStartTime = Time.realtimeSinceStartup;

            try
            {
                ShowLoadingIndicator(true);
                if (loadingCanvasGroup != null)
                {
                    await UIEffect.FadeIn(loadingCanvasGroup, cts.Token, fadeDuration);
                }

                if (curScene.Scene.IsValid())
                {
                    try
                    {
                        await Addressables.UnloadSceneAsync(curScene).ToUniTask(cancellationToken: cts.Token);
                        await UniTask.Yield(PlayerLoopTiming.PostLateUpdate, cts.Token);
                    }
                    catch (Exception ex)
                    {
                        $"[SceneLoadHelper] 씬 언로드 중 에러: {ex.Message}".DLog();
                    }
                }

                for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
                {
                    try
                    {
                        var scene = SceneManager.GetSceneAt(i);
                        if (scene.isLoaded && scene.name != "DontDestroyOnLoad")
                        {
                            await SceneManager.UnloadSceneAsync(scene);
                            await UniTask.Yield(PlayerLoopTiming.PostLateUpdate, cts.Token);
                        }
                    }
                    catch (Exception ex)
                    {
                        $"[SceneLoadHelper] 씬 언로드 중 에러: {ex.Message}".DLog();
                    }
                }

                curScene = default;
                var loadOp = SceneManager.LoadSceneAsync(0, LoadSceneMode.Single);

                if (loadOp == null)
                {
                    "[SceneLoadHelper] Boot 씬 로드 실패".DError();
                    throw new Exception("Boot 씬 로드 실패");
                }

                while (!loadOp.isDone)
                {
                    if (cts == null || cts.Token.IsCancellationRequested)
                        break;
                    await UniTask.Yield(PlayerLoopTiming.PostLateUpdate, cts.Token);
                }

                await UniTask.Yield(PlayerLoopTiming.PostLateUpdate, cts.Token);

                if (cts != null && !cts.Token.IsCancellationRequested)
                {
                    float elapsedTime = Time.realtimeSinceStartup - loadStartTime;
                    if (elapsedTime < minLoadingDuration)
                    {
                        await UniTask.Delay(TimeSpan.FromSeconds(minLoadingDuration - elapsedTime), cancellationToken: cts.Token);
                    }

                    if (loadingCanvasGroup != null)
                    {
                        await UIEffect.FadeOut(loadingCanvasGroup, cts.Token, fadeDuration);
                    }
                }

                ShowLoadingIndicator(false);
            }
            catch (OperationCanceledException)
            {
                "[SceneLoadHelper] Boot 씬 로드가 취소되었습니다".DWarnning();
                ShowLoadingIndicator(false);
            }
            catch (Exception ex)
            {
                $"[SceneLoadHelper] Boot 씬 로드 중 오류: {ex.Message}".DError();
                ShowLoadingIndicator(false);

                try
                {
                    SceneManager.LoadScene(0);
                }
                catch (Exception fallbackEx)
                {
                    $"[SceneLoadHelper] 폴백 로드 실패: {fallbackEx.Message}".DError();
                }
            }
        }


        public async UniTask<SceneInstance> LoadSceneAdditiveMode(string key)
        {
            CancelCurrentOps();

            var handle = Addressables.LoadSceneAsync(key, LoadSceneMode.Additive);
            var scene = await handle.ToUniTask(cancellationToken: cts.Token);
            return scene;
        }
        public async UniTask UnloadSceneAdditive(SceneInstance scene)
        {
            if (!scene.Scene.IsValid())
                return;

            CancelCurrentOps();
            await Addressables.UnloadSceneAsync(scene);
        }

        private void CancelCurrentOps()
        {
            cts?.Cancel();
            cts?.Dispose();
            cts = new CancellationTokenSource();
        }


        private void ShowLoadingIndicator(bool show)
        {
            if (loadingCanvas != null)
            {
                loadingCanvas.gameObject.SetActive(show);
            }
        }

    }
}
