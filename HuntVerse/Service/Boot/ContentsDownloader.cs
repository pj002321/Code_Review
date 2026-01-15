using Cysharp.Threading.Tasks;
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.Initialization;   
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Hunt
{
    public class ContentsDownloader : MonoBehaviourSingleton<ContentsDownloader>
    {
        public Canvas loadingCanvas;
        private LoadingIndicator loadingIndicator;
        private string envConfigFileName = "env_contents.json";

        public float DownloadProgress { get; private set; }

        private CcdEnvConfig cachedConfig;
        private bool envConfigLoadAttempted;

        protected override bool DontDestroy => base.DontDestroy;

        protected override void Awake()
        {
            if (loadingCanvas != null)
            {
                loadingIndicator = loadingCanvas.GetComponent<LoadingIndicator>();
                UpdateLoadingUI(0f);
            }
            base.Awake();
        }

        public void ResetDownloadState()
        {
            cachedConfig = null;
            envConfigLoadAttempted = false;
            DownloadProgress = 0f;
        }

        public async UniTask<bool> StartDownload()
        {
            try
            {
                "📦 [Downloader] Start!!".DLog();

                var config = LoadEnvConfig();
                if (config == null)
                {
                    "📦 [Downloader] Env config Load Fail".DError();
                    return false;
                }
                UpdateLoadingUI(0f);

                if (string.IsNullOrWhiteSpace(config.remoteCatalogUrl))
                {
                    "📦 [Downloader] remoteCatalogUrl missing (env_contents.json)".DError();
                    return false;
                }

                if (string.IsNullOrWhiteSpace(config.downloadLabel))
                {
                    "📦 [Downloader] downloadLabel missing (env_contents.json)".DError();
                    return false;
                }

                ApplyCcdRuntimeProperties(config);
                UpdateLoadingUI(0.1f);

                if (!await LoadRemoteCatalog(config.remoteCatalogUrl))
                    return false;
                UpdateLoadingUI(0.2f);

                if (!await UpdateCatalog())
                    return false;
                UpdateLoadingUI(0.3f);

                if (!await DownloadAddressablesByLabel(config.downloadLabel))
                    return false;
                UpdateLoadingUI(1f);

                "📦 [Downloader] All Complete!".DLog();
                return true;
            }
            catch (Exception e)
            {
                $"📦 [Downloader] ERROR: {e}".DError();
                return false;
            }
        }

        #region Catalog

        private async UniTask<bool> LoadRemoteCatalog(string catalogUrl)
        {
            if (string.IsNullOrWhiteSpace(catalogUrl))
            {
                "📦 [Downloader] remoteCatalogUrl missing".DError();
                return false;
            }

            var catalogHandle = Addressables.LoadContentCatalogAsync(catalogUrl, true);
            await catalogHandle.Task;

            if (!catalogHandle.IsValid() || catalogHandle.Status != AsyncOperationStatus.Succeeded)
            {
                string errorMsg = catalogHandle.IsValid() ? catalogHandle.OperationException?.ToString() : "Invalid operation handle";
                $"📦 [Downloader] Failed to load catalog - {errorMsg}".DError();
                if (catalogHandle.IsValid())
                {
                    Addressables.Release(catalogHandle);
                }
                return false;
            }
            Addressables.Release(catalogHandle);
            return true;
        }

        private async UniTask<bool> UpdateCatalog()
        {
            var checkHandle = Addressables.CheckForCatalogUpdates(false);
            await checkHandle.Task;

            if (checkHandle.Status != AsyncOperationStatus.Succeeded)
            {
                $"📦 [Downloader] Catalog check failed: {checkHandle.OperationException}".DError();
                Addressables.Release(checkHandle);
                return false;
            }

            var catalogs = checkHandle.Result;
            Addressables.Release(checkHandle);

            if (catalogs == null)
            {
                "📦 [Downloader] Catalog list is null".DError();
                return false;
            }

            if (catalogs.Count == 0)
            {
                return true;
            }

            var updateHandle = Addressables.UpdateCatalogs(catalogs, false);
            await updateHandle.Task;

            if (updateHandle.Status != AsyncOperationStatus.Succeeded)
            {
                $"📦 [Downloader] Catalog update failed: {updateHandle.OperationException}".DError();
                Addressables.Release(updateHandle);
                return false;
            }

            Addressables.Release(updateHandle);
            return true;
        }

        #endregion

        #region Download

        private async UniTask<bool> DownloadAddressablesByLabel(string label)
        {
            if (string.IsNullOrWhiteSpace(label))
            {
                "📦 [Downloader] downloadLabel 비어있음".DError();
                return false;
            }

            var sizeHandle = Addressables.GetDownloadSizeAsync(label);
            await sizeHandle.Task;

            if (sizeHandle.Status != AsyncOperationStatus.Succeeded)
            {
                $"📦 [Downloader] GetDownloadSize failed: {sizeHandle.OperationException}".DError();
                Addressables.Release(sizeHandle);
                return false;
            }

            long size = sizeHandle.Result;
            Addressables.Release(sizeHandle);

            if (size <= 0)
            {
                return true;
            }

            $"📦 [Downloader] Download size: {size / (1024f * 1024f):F2} MB".DLog();

            var downloadHandle = Addressables.DownloadDependenciesAsync(label, true);

            while (!downloadHandle.IsDone)
            {
                DownloadProgress = downloadHandle.PercentComplete;
                UpdateLoadingUI(DownloadProgress);
                await UniTask.Yield();
            }

            if (downloadHandle.Status != AsyncOperationStatus.Succeeded)
            {
                $"📦 [Downloader] Download failed: {downloadHandle.OperationException}".DError();
                Addressables.Release(downloadHandle);
                return false;
            }

            Addressables.Release(downloadHandle);
            UpdateLoadingUI(1f);
            return true;
        }

        #endregion

        #region CCD Runtime Properties

        private void ApplyCcdRuntimeProperties(CcdEnvConfig config = null)
        {
            config ??= LoadEnvConfig();
            if (config == null)
            {
                Debug.LogError("📦 [Downloader] Env config not found or invalid. Unable to set CCD runtime properties.");
                return;
            }

            AddressablesRuntimeProperties.SetPropertyValue("CcdManager.EnvironmentId", config.environmentId);
            AddressablesRuntimeProperties.SetPropertyValue("CcdManager.EnvironmentName", config.environmentName);
            AddressablesRuntimeProperties.SetPropertyValue("CcdManager.BucketId", config.bucketId);
            AddressablesRuntimeProperties.SetPropertyValue("CcdManager.BucketName", config.bucketName);
            AddressablesRuntimeProperties.SetPropertyValue("CcdManager.Badge", config.badge);
        }

        private CcdEnvConfig LoadEnvConfig()
        {
            if (cachedConfig != null || envConfigLoadAttempted)
                return cachedConfig;

            envConfigLoadAttempted = true;

            if (string.IsNullOrWhiteSpace(envConfigFileName))
            {
                "📦 [Downloader] Env config filename is empty".DError();
                return null;
            }

            string configPath = Path.Combine(Application.streamingAssetsPath, "aa", envConfigFileName);

            if (!File.Exists(configPath))
            {
                $"📦 [Downloader] Env config not found: {configPath}".DError();
                return null;
            }

            try
            {
                string json = File.ReadAllText(configPath);
                cachedConfig = JsonUtility.FromJson<CcdEnvConfig>(json);
                if (cachedConfig == null)
                {
                    "📦 [Downloader] Failed to parse env config".DError();
                }
            }
            catch (Exception e)
            {
                $"📦 [Downloader] Failed to read env config: {e.Message}".DError();
            }

            return cachedConfig;
        }

        private void UpdateLoadingUI(float normalizedValue)
        {
            loadingIndicator?.UpdateProgress(Mathf.Clamp01(normalizedValue));
        }

        [Serializable]
        private class CcdEnvConfig
        {
            public string environmentId;
            public string environmentName;
            public string bucketId;
            public string bucketName;
            public string badge;
            public string remoteCatalogUrl;
            public string downloadLabel;
        }

        #endregion
    }
}
