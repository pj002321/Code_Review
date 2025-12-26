using System;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace penta
{
    /// <summary> Addressable 시스템 초기화 및 갱신 관리 </summary>
    public class AddressableSystemManager
    {
        private const string CATALOG_FILE_PATTERN = "catalog_*.json";
        private const int ADDRESSABLE_DELAY_FRAMES = 1;
        
        private readonly string platform;
        private readonly string persistentDataPath;

        public Action<string> OnStatusUpdate;
        public Action<string> OnError;

        public AddressableSystemManager(string platform, string persistentDataPath)
        {
            this.platform = platform;
            this.persistentDataPath = persistentDataPath;
        }

        /// <summary> Addressable 시스템 초기화 (게임 시작 시) </summary>
        public static void InitializeEarly()
        {
            string persistentRoot = Application.persistentDataPath;
            EnsureInternalIdTransform(persistentRoot);
            
            $"[AddressableSystemManager] Hook 초기화 완료. PersistentDataPath={persistentRoot}".DLog();
        }

        /// <summary> Addressable 시스템 갱신 (다운로드 완료 후) </summary>
        public async UniTask RefreshAfterDownload(int downloadedFileCount)
        {
            try
            {
                LogStatus("🔄 Addressable 시스템 갱신 중...");
                EnsureInternalIdTransform(Application.persistentDataPath);

                LogExistingResourceLocators();

                if (downloadedFileCount > 0)
                {
                    await UpdateCatalogsIfNeeded();
                }

                await UniTask.Yield();
                await LoadLocalCatalog();

                LogStatus("✅ Addressable 시스템 갱신 완료!");
            }
            catch (Exception e)
            {
                LogError($"Addressable 시스템 갱신 실패: {e.Message}");
                await TryFallbackInitialization();
            }
        }

        #region Private Methods

        /// <summary> InternalIdTransform 설정 (ab/ 경로를 PersistentDataPath로 변환) </summary>
        private static void EnsureInternalIdTransform(string persistentRoot)
        {
            Func<IResourceLocation, string> transform = (loc) =>
            {
                var id = loc.InternalId;
                if (string.IsNullOrEmpty(id)) return id;
                if (id.StartsWith("file://", StringComparison.OrdinalIgnoreCase)) return id;
                
                if (id.StartsWith("ab/", StringComparison.OrdinalIgnoreCase))
                {
                    string fullPath = Path.Combine(persistentRoot, id);
                    return ToFileUri(fullPath);
                }
                
                return id;
            };

            Addressables.InternalIdTransformFunc = transform;
            Addressables.ResourceManager.InternalIdTransformFunc = transform;
        }

        /// <summary> 파일 경로를 file:// URI로 변환 </summary>
        private static string ToFileUri(string path)
        {
            string unityPath = path.Replace("\\", "/");

            if (unityPath.StartsWith("/"))
            {
                return "file://" + unityPath; // Android/Unix
            }
            else
            {
                return "file:///" + unityPath; // Windows
            }
        }

        /// <summary> 기존 리소스 로케이터 로그 출력 </summary>
        private void LogExistingResourceLocators()
        {
            foreach (var locator in Addressables.ResourceLocators)
            {
                LogStatus($"🗂️ 기존 리소스 로케이터: {locator}");
            }
        }

        /// <summary> 카탈로그 업데이트 확인 및 적용 </summary>
        private async UniTask UpdateCatalogsIfNeeded()
        {
            var checkHandle = Addressables.CheckForCatalogUpdates(false);
            var catalogsToUpdate = await checkHandle.ToUniTask();

            if (catalogsToUpdate != null && catalogsToUpdate.Count > 0)
            {
                LogStatus($"📋 {catalogsToUpdate.Count}개 카탈로그 업데이트 발견");
                var updateHandle = Addressables.UpdateCatalogs(catalogsToUpdate, false);
                await updateHandle.ToUniTask();
                Addressables.Release(updateHandle);
            }

            Addressables.Release(checkHandle);
        }

        /// <summary> 로컬 카탈로그 로드 </summary>
        private async UniTask LoadLocalCatalog()
        {
            string catalogJsonPath = FindLocalCatalogPath();

            if (string.IsNullOrEmpty(catalogJsonPath) || !File.Exists(catalogJsonPath))
            {
                LogError("⚠️ 로컬 카탈로그(catalog_*.json)를 찾지 못했습니다.");
                return;
            }

            LogStatus($"📖 로컬 카탈로그 로드: {catalogJsonPath}");
            var catalogUri = ToFileUri(catalogJsonPath);
            var loadCatalogHandle = Addressables.LoadContentCatalogAsync(catalogUri, false);
            var locator = await loadCatalogHandle.ToUniTask();

            if (locator != null)
            {
                LogStatus("✅ 로컬 카탈로그 로드 완료");
            }
            else
            {
                LogError("❌ 로컬 카탈로그 로드 실패 (locator null)");
            }

            Addressables.Release(loadCatalogHandle);
        }

        /// <summary> 로컬 카탈로그 검색 </summary>
        private string FindLocalCatalogPath()
        {
            try
            {
                var platformDir = Path.Combine(Application.persistentDataPath, "ab", platform);
                if (!Directory.Exists(platformDir)) return null;

                var catalogs = Directory.GetFiles(platformDir, CATALOG_FILE_PATTERN, SearchOption.TopDirectoryOnly);
                if (catalogs == null || catalogs.Length == 0) return null;

                Array.Sort(catalogs, StringComparer.OrdinalIgnoreCase);
                return catalogs[catalogs.Length - 1]; 
            }
            catch (Exception e)
            {
                LogError($"로컬 카탈로그 검색 실패: {e.Message}");
                return null;
            }
        }

        /// <summary> 기본 Addressable 초기화 시도 </summary>   
        private async UniTask TryFallbackInitialization()
        {
            try
            {
                var fallbackInitHandle = Addressables.InitializeAsync(false);
                await fallbackInitHandle.ToUniTask();
                Addressables.Release(fallbackInitHandle);
                LogStatus("🔄 기본 Addressable 초기화 완료");
            }
            catch (Exception e)
            {
                LogError($"기본 초기화도 실패: {e.Message}");
            }
        }

        private void LogStatus(string message)
        {
            Debug.Log($"[AddressableSystemManager] {message}");
            OnStatusUpdate?.Invoke(message);
        }

        private void LogError(string message)
        {
            Debug.LogError($"[AddressableSystemManager] {message}");
            OnError?.Invoke(message);
        }

        #endregion
    }
}

