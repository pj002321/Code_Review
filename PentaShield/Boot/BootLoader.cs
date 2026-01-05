using penta;
using Cysharp.Threading.Tasks;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace penta
{
    /// <summary>
    /// 게임 부팅 및 초기화 프로세스 관리 (주요 로직)
    /// - Addressable 다운로드 관리
    /// - 점검/업데이트 체크
    /// - 진행률 표시
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
            bootWindowInstance = Instantiate(bootWindowPrefab);
            bootingGuide = bootWindowInstance.GetComponent<BootingGuide>();

            var bootprogress = bootWindowInstance.GetComponentInChildren<BootProgress>();
            if (bootprogress != null)
            {
                progressText = bootprogress.loadingPercentText;
                progressGuideText = bootprogress.loadingGuideText;
            }

            progressBar = bootWindowInstance.GetComponentInChildren<Slider>();

            AddressableSystemManager.InitializeEarly();
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

        /// <summary> 게임 부팅 프로세스 시작 </summary>
        public async UniTask InitializeAsync()
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
            switch (UnityEditor.EditorUserBuildSettings.activeBuildTarget)
            {
                case UnityEditor.BuildTarget.iOS:
                    return "iOS";
                case UnityEditor.BuildTarget.Android:
                default:
                    return "Android";
            }
#else
#if UNITY_IOS
            return "iOS";
#else
            return "Android";
#endif
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
            if (!isAnimating)
            {
                StartCoroutine(AnimateProgress());
            }
        }

        /// <summary> 진행률 애니메이션 </summary>
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
                int percentage = Mathf.RoundToInt(currentDisplayProgress * 100f);
                progressText.text = $"{percentage}%";
            }
        }

        /// <summary> 게임 진입 프로세스 </summary>
        private async UniTask EntryGame()
        {
            currentDisplayProgress = 0f;
            targetProgress = 0f;
            UpdateProgressDisplay();

            bool isMaintenance = await CheckMaintenanceFlag();
            if (isMaintenance)
            {
                if (maintenanceNoti != null)
                {
                    maintenanceNoti.gameObject.SetActive(true);
                }
                return;
            }

            bool needsUpdate = await CheckCatalogVersion();
            if (needsUpdate)
            {
                needUpdateNoti.gameObject.SetActive(true);
                return;
            }

            downloadManager.StartDownload().Forget();

            await UniTask.WaitUntil(() => downloadComplete);

#if !UNITY_EDITOR
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

        /// <summary> 진행 가이드 텍스트 업데이트 </summary>
        private void UpdateProgressGuideText()
        {
            if (progressGuideText == null) return;

            if (downloadComplete)
            {
                progressGuideText.text = "Loading...";
                return;
            }

            if (currentDownloadedBytes > 0 || currentTotalBytes > 0)
            {
                float downloadedMB = currentDownloadedBytes / 1048576f;
                float totalMB = currentTotalBytes / 1048576f;
                progressGuideText.text = $"Downloading World ({downloadedMB:F1} MB / {totalMB:F1} MB)";
            }
            else
            {
                progressGuideText.text = "Loading...";
            }
        }

        /// <summary> 카탈로그 버전 체크 </summary>
        private async UniTask<bool> CheckCatalogVersion()
        {
            try
            {
                var initHandle = UnityEngine.AddressableAssets.Addressables.InitializeAsync();
                await initHandle.ToUniTask();
                UnityEngine.AddressableAssets.Addressables.Release(initHandle);

                var checkHandle = UnityEngine.AddressableAssets.Addressables.CheckForCatalogUpdates(false);
                var catalogsToUpdate = await checkHandle.ToUniTask();

                bool hasUpdate = catalogsToUpdate != null && catalogsToUpdate.Count > 0;

                UnityEngine.AddressableAssets.Addressables.Release(checkHandle);
                return hasUpdate;
            }
            catch (System.Exception e)
            {
                return false;
            }
        }

        /// <summary> 점검 플래그 확인 </summary>
        private async UniTask<bool> CheckMaintenanceFlag()
        {
            try
            {
                int maxWait = 200;
                int waited = 0;

                while (PentaFirebase.Shared == null && waited < maxWait)
                {
                    await UniTask.Delay(100);
                    waited++;
                }

                if (PentaFirebase.Shared == null)
                {
                    return false;
                }

                maxWait = 200;
                waited = 0;

                while (!PentaFirebase.Shared.IsInitialized && waited < maxWait)
                {
                    await UniTask.Delay(100);
                    waited++;
                }

                if (!PentaFirebase.Shared.IsInitialized)
                {
                    return false;
                }

                var rtDb = PentaFirebase.Shared?.PRealTimeDb;
                if (rtDb == null)
                {
                    return false;
                }

                maxWait = 100;
                waited = 0;

                while (!rtDb.IsInitialized && waited < maxWait)
                {
                    await UniTask.Delay(100);
                    waited++;
                }

                if (!rtDb.IsInitialized)
                {
                    return false;
                }

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
                return false;
            }
        }

        /// <summary> 카탈로그 버전이 앱 버전보다 높은지 확인 </summary>
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

        /// <summary> 게임 종료 </summary>
        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
