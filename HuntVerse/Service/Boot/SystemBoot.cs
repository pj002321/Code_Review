using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Hunt;
using Hunt.Net;
using UnityEngine;

public class SystemBoot : MonoBehaviourSingleton<SystemBoot>
{
    [Header("LogIn Window")]
    [SerializeField] private Canvas LogInCanvas;

    public bool isSystemContinue = false;
    private bool loginServerConnected;
    public bool LoginServerConnected => loginServerConnected;
    
    private bool isInit = false;
    private bool isInitializing = false;
    private static bool isFirstInitComplete = false;
    private CancellationTokenSource initCts;
    
    protected override bool DontDestroy => false;
    
    protected override void Awake()
    {
        base.Awake();
        loginServerConnected = false;
        LogInCanvas.gameObject.SetActive(false);
        
        Initialize().Forget();
    }

    private async UniTask Initialize()
    {
        if (isInitializing)
        {
            "[Boot] 이미 초기화 중입니다. 중복 호출 무시".DWarnning();
            return;
        }

        isInitializing = true;

        if (initCts != null)
        {
            try { initCts.Dispose(); } catch { }
            initCts = null;
        }

        initCts = new CancellationTokenSource();
        var token = initCts.Token;

        // isSystemContinue가 true면 서버 연결 스킵
        if (isSystemContinue)
        {
            "[Boot] SystemContinue 모드 - 서버 연결 스킵".DLog();

            await UniTask.WaitUntil(() => ContentsDownloader.Shared != null, cancellationToken: token);
            if (ContentsDownloader.Shared?.loadingCanvas != null)
            {
                ContentsDownloader.Shared.loadingCanvas.gameObject.SetActive(false);
            }

            isInit = true;
            isInitializing = false;

            SceneLoadHelper.Shared?.LoadSceneSingleMode(ResourceKeyConst.Ks_Mainmenu, false);
            return;
        }

        if (isFirstInitComplete)
        {
            "[Boot] 로그아웃 후 재진입 - 로그인 서버 재연결".DLog();

            await UniTask.Delay(100, cancellationToken: token);

            var loginScreen = FindAnyObjectByType<LoginScreen>(FindObjectsInactive.Include);
            if (loginScreen != null)
            {
                LogInCanvas = loginScreen.GetComponent<Canvas>();
            }
            else
            {
                "[Boot] LoginScreen을 찾을 수 없습니다!".DError();
            }

            await UniTask.WaitUntil(() => ContentsDownloader.Shared != null, cancellationToken: token);
            if (ContentsDownloader.Shared?.loadingCanvas != null)
            {
                ContentsDownloader.Shared.loadingCanvas.gameObject.SetActive(false);
            }

            await UniTask.WaitUntil(() => GameSession.Shared != null && GameSession.Shared.IsInitialized, cancellationToken: token);
            loginServerConnected = await GameSession.Shared.ConnectionToLoginServer();

            if (!loginServerConnected)
            {
                "[Boot] LoginServer Connection Fail".DError();
            }
            else
            {
                "[Boot] LoginServer Connection Success!".DLog();
            }

            if (LogInCanvas != null)
            {
                LogInCanvas.gameObject.SetActive(true);
            }
            else
            {
                "[Boot] LogInCanvas를 찾을 수 없습니다!".DError();
            }

            isInit = true;
            isInitializing = false;
            return;
        }

        try
        {
            "[Boot] Initializing...".DLog();

            await UniTask.WaitUntil(() => ContentsDownloader.Shared != null, cancellationToken: token);

            bool downloadSuccess = await ContentsDownloader.Shared.StartDownload();
            if (!downloadSuccess)
            {
                "[Boot] Resource Download Failed!".DError();
                return;
            }
            "[Boot] Resource Download Complete!".DLog();

            await UniTask.WaitUntil(() => NetworkManager.Shared != null, cancellationToken: token);
            await UniTask.WaitUntil(() => GameSession.Shared != null && GameSession.Shared.IsInitialized, cancellationToken: token);

            loginServerConnected = await GameSession.Shared.ConnectionToLoginServer();
            if (!loginServerConnected)
            {
                "[Boot] LoginServer Connection Fail".DError();
            }
            else
            {
                "[Boot] LoginServer Connection Success!".DLog();
            }

            await UniTask.WaitUntil(() => UserAuth.Shared != null, cancellationToken: token);

            int steamWaitSeconds = 0;
            while (!SteamManager.Initialized && !token.IsCancellationRequested)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(1), cancellationToken: token);
                steamWaitSeconds++;
                if (steamWaitSeconds % 5 == 0)
                {
                    $"[Boot] SteamManager not ready yet ({steamWaitSeconds}s)".DLog();
                }
            }

            if (token.IsCancellationRequested)
            {
                "[Boot] 초기화가 취소되었습니다".DLog();
                return;
            }

            UserAuth.Shared.Initialize();

            isInit = true;
            isFirstInitComplete = true;
            "[Boot] Initialize Success".DLog();

            if (isInit)
            {
                ContentsDownloader.Shared.loadingCanvas.gameObject.SetActive(false);
                LogInCanvas.gameObject.SetActive(true);
            }
        }
        catch (OperationCanceledException)
        {
            "[Boot] 초기화가 취소되었습니다".DLog();
        }
        catch (Exception e)
        {
            $"[Boot] 초기화 중 에러 발생: {e.Message}".DError();
        }
        finally
        {
            isInitializing = false;
        }
    }

    protected override void OnDestroy()
    {
        if (initCts != null && !initCts.IsCancellationRequested)
        {
            try
            {
                initCts.Cancel();
                initCts.Dispose();
            }
            catch { }
            finally
            {
                initCts = null;
            }
        }
        
        base.OnDestroy();
    }
}
