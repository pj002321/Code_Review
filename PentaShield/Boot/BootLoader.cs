using penta;
using Cysharp.Threading.Tasks;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;

namespace penta
{
    /// <summary>
    /// 게임 부팅 및 초기화 프로세스 관리
    /// </summary>
    public class BootLoader : MonoBehaviour
    {
        private bool downloadComplete = false;

        [Header("BOOTWINDOW")]
        [SerializeField] private GameObject bootWindowPrefab;
        [SerializeField] private Canvas needUpdateNoti;
        [SerializeField] private Canvas maintenanceNoti;

        private Slider progressBar;
        private TextMeshProUGUI progressText;
        private TextMeshProUGUI progressGuideText;

        private BootingGuide bootingGuide;
        private GameObject bootWindowInstance;
        private float currentDisplayProgress = 0f;
        private float targetProgress = 0f;
        private bool isAnimating = false;

        private long currentDownloadedBytes = 0;
        private long currentTotalBytes = 0;

        private AddressableDownloadManager downloadManager;
        private AddressableSystemManager systemManager;
        
        private void Awake()
        {
            if (bootWindowPrefab == null)
            {
                "Boot window prefab is null".DError();
                return;
            }
            bootWindowInstance = Instantiate(bootWindowPrefab);
            bootingGuide = bootWindowInstance.GetComponent<BootingGuide>();

            if (bootingGuide == null)
            {
                "BootingGuide component is missing".DError();
                return;
            }

            var bootprogress = bootingGuide.GetComponentInChildren<BootProgress>();
            progressBar = bootingGuide.GetComponentInChildren<Slider>();
            progressText = bootprogress.loadingPercentText;
            progressGuideText = bootprogress.loadingGuideText;

            AddressableSystemManager.InitializeEarly();
        }

        private async void Start()
        {
            var config = FirebaseConfig.Load();
            string platform = GetPlatformName();
            
            downloadManager = new AddressableDownloadManager(config, platform);
            systemManager = new AddressableSystemManager(platform, downloadManager.AssetFolderPath);

            downloadManager.OnAllDownloadsComplete += OnDownloadComplete;
            downloadManager.OnProgressChanged += OnProgressChanged;
            downloadManager.OnDownloadStatsUpdated += OnDownloadStatsUpdated;

            await EntryGame();
        }

        private string GetPlatformName()
        {
#if UNITY_EDITOR
            return UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.iOS ? "iOS" : "Android";
#elif UNITY_IOS
            return "iOS";
#elif UNITY_ANDROID
            return "Android";
#endif
        }

        private void OnDownloadStatsUpdated(long downloadedBytes, long totalBytes)
        {
            currentDownloadedBytes = downloadedBytes;
            currentTotalBytes = totalBytes;
            UpdateProgressGuideText();
        }

        private void OnDownloadComplete()
        {
            downloadComplete = true;
            targetProgress = 1f;
            UpdateProgressGuideText();
        }

        private void OnProgressChanged(float progress)
        {
            targetProgress = progress;
            if (!isAnimating) StartCoroutine(AnimateProgress());

            if (downloadManager == null)
            {
                UpdateProgressGuideText();
                return;
            }
            
            var stats = downloadManager.DownloadStats;
            if (stats.totalBytes > 0 || stats.bytes > 0)
            {
                currentDownloadedBytes = stats.bytes;
                currentTotalBytes = stats.totalBytes;
            }

            UpdateProgressGuideText();
        }

        private IEnumerator AnimateProgress()
        {
            isAnimating = true;
            
            while (Mathf.Abs(currentDisplayProgress - targetProgress) > 0.01f)
            {
                currentDisplayProgress = Mathf.Lerp(currentDisplayProgress, targetProgress, Time.deltaTime * 3f);
                UpdateProgressDisplay();
                yield return null;
            }
            
            currentDisplayProgress = targetProgress;
            UpdateProgressDisplay();
            isAnimating = false;
        }

        private void UpdateProgressDisplay()
        {
            if (progressBar != null)
            {
                progressBar.value = currentDisplayProgress;
            }

            if (progressText != null)
            {
                progressText.text = $"{Mathf.RoundToInt(currentDisplayProgress * 100f)}%";
            }
        }

        private async UniTask EntryGame()
        {
            currentDisplayProgress = targetProgress = 0f;
            UpdateProgressDisplay();

#if !UNITY_EDITOR
            if (await CheckMaintenanceFlag())
            {
                maintenanceNoti?.gameObject.SetActive(true);
                return;
            }
#endif
            if (await CheckCatalogVersion())
            {
                needUpdateNoti?.gameObject.SetActive(true);
                return;
            }

            downloadManager.StartDownload().Forget();

            await UniTask.WaitUntil(() => downloadComplete);

#if !UNITY_EDITOR
            // 다운로드가 끝났지만, 다운로드된 Addressable 카탈로그 버전이
            // 현재 앱 번들 버전과 다르면 게임을 진행하지 않고 업데이트 노티를 띄운다.
            try
            {
                var appVersion = Application.version;
                var catalogVersion = downloadManager.GetDownloadedCatalogVersion();

                if (!string.IsNullOrEmpty(catalogVersion) &&
                    !string.IsNullOrEmpty(appVersion) &&
                    IsCatalogNewer(catalogVersion, appVersion))
                {
                    if (needUpdateNoti != null)
                    {
                        needUpdateNoti.gameObject.SetActive(true);
                    }
                    return;
                }
            }
            catch (System.Exception e)
            {
                $"카탈로그 버전 확인 실패: {e.Message}".DError();
            }
#endif
            await systemManager.RefreshAfterDownload(downloadManager.DownloadStats.downloaded);

            targetProgress = 1f;
            if (!isAnimating)
            {
                StartCoroutine(AnimateProgress());
            }

            await UniTask.Delay(2000);

            if (bootWindowInstance != null)
            {
                Destroy(bootWindowInstance);
                await UniTask.Yield();
            }

            await SceneSystem.Shared.LoadScene(PentaConst.kmainMenu_Scene);
        }

        private void UpdateProgressGuideText()
        {
            if (progressGuideText == null) return;

            if (downloadComplete)
            {
                progressGuideText.text = "Falling!";
                return;
            }

            progressGuideText.text = (currentDownloadedBytes > 0 || currentTotalBytes > 0)
                ? $"Downloading World ({currentDownloadedBytes / 1048576f:F1} MB / {currentTotalBytes / 1048576f:F1} MB)"
                : "Loading...";
        }

        private async UniTask<bool> CheckCatalogVersion()
        {
            try
            {
                var initHandle =Addressables.InitializeAsync();
                await initHandle.ToUniTask();
                Addressables.Release(initHandle);

                var checkHandle = Addressables.CheckForCatalogUpdates(false);
                var catalogsToUpdate = await checkHandle.ToUniTask();

                bool hasUpdate = catalogsToUpdate != null && catalogsToUpdate.Count > 0;

                Addressables.Release(checkHandle);
                return hasUpdate;
            }
            catch (System.Exception e)
            {
                $"[BootLoader] Catalog 버전 확인 실패: {e.Message}".DError();
                return false;
            }
        }

        /// <summary>
        /// Realtime DB에서 점검 플래그를 확인한다.
        /// </summary>
        private async UniTask<bool> CheckMaintenanceFlag()
        {
            try
            {
                for (int waited = 0; PentaFirebase.Shared == null && waited < 200; waited++)
                {
                    await UniTask.Delay(100);
                }
                if (PentaFirebase.Shared == null) return false;

                for (int waited = 0; !PentaFirebase.Shared.IsInitialized && waited < 200; waited++)
                {
                    await UniTask.Delay(100);
                }
                if (!PentaFirebase.Shared.IsInitialized) return false;

                var rtDb = PentaFirebase.Shared?.PRealTimeDb;
                if (rtDb == null) return false;

                for (int waited = 0; !rtDb.IsInitialized && waited < 100; waited++)
                {
                    await UniTask.Delay(100);
                }
                if (!rtDb.IsInitialized) return false;

                const string path = "maintenance/is_on";
                var value = await rtDb.GetValueAsync(path);
                bool result = false;
                
                if (value is bool b)
                {
                    result = b;
                }
                else if (value != null)
                {
                    bool.TryParse(value.ToString(), out result);
                }

                return result;
            }
            catch (System.Exception e)
            {
                $"Maintenance 플래그 확인 실패: {e.Message}".DError();
                return false;
            }
        }

        /// <summary>
        /// catalogVersion이 appVersion보다 높으면 true.
        /// </summary>
        private bool IsCatalogNewer(string catalogVersion, string appVersion)
        {
            var c = ParseVersion(catalogVersion);
            var a = ParseVersion(appVersion);

            if (c.major != a.major) return c.major > a.major;
            if (c.minor != a.minor) return c.minor > a.minor;
            return c.patch > a.patch;
        }

        private (int major, int minor, int patch) ParseVersion(string version)
        {
            if (string.IsNullOrEmpty(version)) return (0, 0, 0);

            var parts = version.Split('.');
            int major = 0, minor = 0, patch = 0;
            if (parts.Length > 0) int.TryParse(parts[0], out major);
            if (parts.Length > 1) int.TryParse(parts[1], out minor);
            if (parts.Length > 2) int.TryParse(parts[2], out patch);
            return (major, minor, patch);
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OnDestroy()
        {
            if (downloadManager != null)
            {
                downloadManager.OnAllDownloadsComplete -= OnDownloadComplete;
                downloadManager.OnProgressChanged -= OnProgressChanged;
                downloadManager.OnDownloadStatsUpdated -= OnDownloadStatsUpdated;
            }

            if (bootWindowInstance != null)
            {
                Destroy(bootWindowInstance);
            }
        }
    }
}