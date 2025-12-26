using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace penta
{
    /// <summary> Addressable 에셋 다운로드 매니저 </summary>       
    public class AddressableDownloadManager
    {
        #region Fields
        private readonly FirebaseConfig config;
        private readonly FirebaseStorageClient storageClient;
        private readonly string platform;
        private readonly string downloadPath;
        private readonly string persistentDataPath;
        private readonly string versionFilePath;
        
        private bool isDownloading = false;
        private float totalProgress = 0f;
        private int totalFiles = 0;
        private int downloadedFiles = 0;
        private long totalBytes = 0;
        private long downloadedBytes = 0;

        private List<FirebaseStorageItem> filesToDownload = new List<FirebaseStorageItem>();
        private ConcurrentQueue<FirebaseStorageItem> downloadQueue = new ConcurrentQueue<FirebaseStorageItem>();
        private HashSet<string> verifiedFiles = new HashSet<string>();
        private int currentDownloads = 0;
        private int maxConcurrentDownloads = 3;
        private bool verifyFileIntegrity = true;
        private bool overwriteExisting = false;

        public Action<float> OnProgressChanged;
        public Action<string, float> OnFileDownloadProgress;
        public Action<string> OnFileDownloaded;
        public Action OnAllDownloadsComplete;
        public Action<string> OnDownloadError;
        public Action<string> OnStatusUpdate;
        public Action<long, long> OnDownloadStatsUpdated;

        #endregion

        #region Properties

        public bool IsDownloading => isDownloading;
        public float Progress => totalProgress;
        public (int downloaded, int total, long bytes, long totalBytes) DownloadStats =>
            (downloadedFiles, totalFiles, downloadedBytes, totalBytes);
        public string AssetFolderPath => persistentDataPath;

        #endregion

        public AddressableDownloadManager(FirebaseConfig config, string platform)
        {
            this.config = config;
            this.platform = platform;
            this.downloadPath = $"ab/{platform}/";
            this.persistentDataPath = Path.Combine(Application.persistentDataPath, "ab", platform);
            this.versionFilePath = Path.Combine(persistentDataPath, "version.txt");

            if (!Directory.Exists(persistentDataPath))
            {
                Directory.CreateDirectory(persistentDataPath);
            }

            storageClient = new FirebaseStorageClient(config, downloadPath);
            storageClient.OnFileDownloadProgress += (fileName, progress) => OnFileDownloadProgress?.Invoke(fileName, progress);
        }
        /// <summary> 다운로드 시작 </summary>
        public async UniTask StartDownload()
        {
            if (isDownloading)
            {
                LogStatus("이미 다운로드 중입니다.");
                return;
            }

            if (!config.IsValid())
            {
                LogError("Firebase 설정이 올바르지 않습니다.");
                return;
            }

            isDownloading = true;
            ResetProgress();

            LogStatus("🔥 Firebase에서 파일 목록 가져오는 중...");

            try
            {
                // 1. Firebase에서 파일 목록 가져오기
                var response = await storageClient.FetchFileListAsync();
                if (response?.items == null)
                {
                    LogStatus("다운로드할 파일이 없습니다.");
                    CompleteDownload();
                    return;
                }

                // 2. 버전 확인 및 캐시 정리
                string currentVersion = GenerateVersionHash(response.items);
                string lastVersion = LoadLastKnownVersion();

                if (currentVersion != lastVersion)
                {
                    LogStatus($"🔄 버전 변경 감지: {lastVersion} -> {currentVersion}");
                    LogStatus("🗑️ 기존 캐시 정리 중...");
                    ClearLocalCache();
                }
                else
                {
                    LogStatus($"✅ 버전 동일: {currentVersion} (캐시 유지)");
                }

                // 3. 다운로드 대상 파일 결정
                filesToDownload = ProcessFileList(response.items);
                totalFiles = filesToDownload.Count;

                if (filesToDownload.Count == 0)
                {
                    LogStatus("다운로드할 파일이 없습니다.");
                    await SaveCurrentVersion(currentVersion);
                    CompleteDownload();
                    return;
                }

                // 4. 파일 크기 확인
                await GetFileSizesAsync();

                LogStatus($"📥 {filesToDownload.Count}개 파일 다운로드 시작...");

                // 5. 다운로드 큐 초기화
                downloadQueue = new ConcurrentQueue<FirebaseStorageItem>();
                foreach (var file in filesToDownload)
                {
                    downloadQueue.Enqueue(file);
                }

                // 6. 병렬 다운로드 시작
                int queueCount = downloadQueue.Count;
                for (int i = 0; i < Mathf.Min(maxConcurrentDownloads, queueCount); i++)
                {
                    await ProcessDownloadQueue();
                }
            }
            catch (Exception e)
            {
                LogError($"다운로드 시작 실패: {e.Message}");
                isDownloading = false;
            }
        }

        /// <summary> 다운로드 중단 </summary>
        public void StopDownload()
        {
            if (!isDownloading) return;

            isDownloading = false;
            downloadQueue = new ConcurrentQueue<FirebaseStorageItem>();
            currentDownloads = 0;

            LogStatus("❌ 다운로드가 중단되었습니다.");
        }

        /// <summary> 로컬 캐시 삭제 </summary>
        public void ClearLocalCache()
        {
            try
            {
                if (Directory.Exists(persistentDataPath))
                {
                    Directory.Delete(persistentDataPath, true);
                    Directory.CreateDirectory(persistentDataPath);
                    verifiedFiles.Clear();
                    DeleteVersionFile();
                    LogStatus("🗑️ 로컬 캐시가 삭제되었습니다.");
                }
            }
            catch (Exception e)
            {
                LogError($"캐시 삭제 실패: {e.Message}");
            }
        }

        /// <summary> 로컬 캐시 정보 가져오기 </summary>
        public (int fileCount, long totalSize, string version) GetCacheInfo()
        {
            int fileCount = 0;
            long totalSize = 0;

            try
            {
                if (Directory.Exists(persistentDataPath))
                {
                    var files = Directory.GetFiles(persistentDataPath, "*", SearchOption.AllDirectories)
                        .Where(f => !f.EndsWith("version.txt"))
                        .ToArray();

                    fileCount = files.Length;
                    totalSize = files.Sum(f => new FileInfo(f).Length);
                }
            }
            catch (Exception e)
            {
                LogError($"캐시 정보 확인 실패: {e.Message}");
            }

            return (fileCount, totalSize, LoadLastKnownVersion());
        }

        /// <summary> 다운로드된 카탈로그 버전 가져오기 </summary>
        public string GetDownloadedCatalogVersion()
        {
            try
            {
                var platformDir = Path.Combine(Application.persistentDataPath, "ab", platform);
                if (!Directory.Exists(platformDir)) return null;

                var catalogs = Directory.GetFiles(platformDir, "catalog_*.json", SearchOption.TopDirectoryOnly);
                if (catalogs == null || catalogs.Length == 0) return null;

                Array.Sort(catalogs, StringComparer.OrdinalIgnoreCase);
                var latestCatalog = catalogs[catalogs.Length - 1];
                var fileName = Path.GetFileNameWithoutExtension(latestCatalog);
                
                if (string.IsNullOrEmpty(fileName)) return null;

                var parts = fileName.Split('_');
                return parts.Length >= 2 ? parts[parts.Length - 1] : null;
            }
            catch (Exception e)
            {
                LogError($"카탈로그 버전 추출 실패: {e.Message}");
                return null;
            }
        }

        #region Download

        /// <summary> 다운로드 큐 처리 </summary>
        private async UniTask ProcessDownloadQueue()
        {
            if (!isDownloading || downloadQueue.IsEmpty) return;

            if (!downloadQueue.TryDequeue(out FirebaseStorageItem fileToDownload))
            {
                return;
            }

            currentDownloads++;

            try
            {
                // 다운로드
                byte[] fileData = await storageClient.DownloadFileAsync(fileToDownload);
                
                // 저장
                string localPath = GetLocalFilePath(fileToDownload.name);
                EnsureDirectoryExists(localPath);
                await File.WriteAllBytesAsync(localPath, fileData);

                // 무결성 검증
                if (verifyFileIntegrity && !string.IsNullOrEmpty(fileToDownload.md5Hash))
                {
                    ValidateFileIntegrity(fileToDownload, localPath);
                }

                OnFileDownloadComplete(fileToDownload.name);
            }
            catch (Exception e)
            {
                LogError($"파일 다운로드 실패 ({fileToDownload.name}): {e.Message}");
            }
            finally
            {
                currentDownloads--;
                TryContinueOrCompleteDownload();
            }
        }

        /// <summary> 파일 다운로드 완료 </summary>
        private void OnFileDownloadComplete(string fileName)
        {
            downloadedFiles++;
            OnFileDownloaded?.Invoke(fileName);
            UpdateProgress();
        }

        /// <summary> 다운로드 계속 또는 완료 </summary>
        private void TryContinueOrCompleteDownload()
        {
            if (!downloadQueue.IsEmpty && isDownloading)
            {
                ProcessDownloadQueue().Forget();
            }
            else if (currentDownloads == 0 && downloadQueue.IsEmpty)
            {
                CompleteDownload();
            }
        }

        /// <summary> 다운로드 완료 </summary>
        private async void CompleteDownload()
        {
            isDownloading = false;
            currentDownloads = 0;

            LogStatus($"🎉 모든 다운로드 완료! (총 {downloadedFiles}개 파일, {FormatFileSize(downloadedBytes)})");

            try
            {
                // 버전 저장
                var response = await storageClient.FetchFileListAsync();
                if (response?.items != null)
                {
                    string currentVersion = GenerateVersionHash(response.items);
                    await SaveCurrentVersion(currentVersion);
                }

                OnAllDownloadsComplete?.Invoke();
            }
            catch (Exception e)
            {
                LogError($"다운로드 완료 처리 실패: {e.Message}");
                OnAllDownloadsComplete?.Invoke();
            }
        }

        /// <summary> 파일 크기 확인 </summary>
        private async UniTask GetFileSizesAsync()
        {
            LogStatus("📏 파일 크기 확인 중...");

            foreach (var file in filesToDownload)
            {
                try
                {
                    long fileSize = await storageClient.GetFileSizeAsync(file.name);
                    if (fileSize > 0)
                    {
                        totalBytes += fileSize;
                    }
                }
                catch (Exception e)
                {
                    LogError($"파일 크기 확인 실패 ({file.name}): {e.Message}");
                }
            }

            LogStatus($"📊 전체 다운로드 크기: {FormatFileSize(totalBytes)}");
            UpdateProgress();
        }

        #endregion

        #region File Processing

        /// <summary> 파일 목록 처리 </summary>
        private List<FirebaseStorageItem> ProcessFileList(List<FirebaseStorageItem> items)
        {
            var filesToDownload = new List<FirebaseStorageItem>();

            foreach (var item in items)
            {
                if (item.name.EndsWith("/")) continue; 

                string localPath = GetLocalFilePath(item.name);
                bool shouldDownload = ShouldDownloadFile(item, localPath, out long cachedFileSize);

                if (shouldDownload)
                {
                    filesToDownload.Add(item);
                }
                else
                {
                    // 캐시된 파일도 이미 다운로드된 것으로 간주
                    totalBytes += cachedFileSize;
                    downloadedBytes += cachedFileSize;
                }
            }

            return filesToDownload;
        }

        /// <summary> 파일 다운로드 여부 확인 </summary>
        private bool ShouldDownloadFile(FirebaseStorageItem item, string localPath, out long cachedFileSize)
        {
            cachedFileSize = 0;

            if (!File.Exists(localPath)) return true;

            cachedFileSize = new FileInfo(localPath).Length;

            if (overwriteExisting) return true;

            if (verifyFileIntegrity && !string.IsNullOrEmpty(item.md5Hash))
            {
                if (verifiedFiles.Contains(item.name)) return false;

                // MD5 검증
                string localHash = CalculateMD5(localPath);
                if (localHash == item.md5Hash)
                {
                    verifiedFiles.Add(item.name);
                    return false;
                }
                else
                {
                    verifiedFiles.Remove(item.name);
                    return true;
                }
            }

            return false; // 파일 존재하고 검증 비활성화면 스킵
        }

        private void ValidateFileIntegrity(FirebaseStorageItem fileItem, string localPath)
        {
            string downloadedHash = CalculateMD5(localPath);
            if (downloadedHash != fileItem.md5Hash)
            {
                File.Delete(localPath);
                throw new Exception($"파일 무결성 검증 실패: {fileItem.name}");
            }

            verifiedFiles.Add(fileItem.name);
        }

        private string CalculateMD5(string filePath)
        {
            try
            {
                using (var md5 = MD5.Create())
                using (var stream = File.OpenRead(filePath))
                {
                    byte[] hash = md5.ComputeHash(stream);
                    return Convert.ToBase64String(hash);
                }
            }
            catch
            {
                return string.Empty;
            }
        }

        #endregion

        #region Version Management

        private string LoadLastKnownVersion()
        {
            try
            {
                if (File.Exists(versionFilePath))
                {
                    return File.ReadAllText(versionFilePath).Trim();
                }
            }
            catch (Exception e)
            {
                $"[VersionManager] 버전 파일 읽기 실패: {e.Message}".DError();
            }

            return "";
        }

        private async Task SaveCurrentVersion(string version)
        {
            try
            {
                await File.WriteAllTextAsync(versionFilePath, version);
            }
            catch (Exception e)
            {
                $"[VersionManager] 버전 저장 실패: {e.Message}".DError();
            }
        }

        private void DeleteVersionFile()
        {
            if (File.Exists(versionFilePath))
            {
                File.Delete(versionFilePath);
            }
        }

        private string GenerateVersionHash(List<FirebaseStorageItem> items)
        {
            var fileNames = items
                .Where(item => !item.name.EndsWith("/"))
                .Select(item => item.name)
                .OrderBy(name => name)
                .ToList();

            string combinedNames = string.Join("|", fileNames);
            using (var md5 = MD5.Create())
            {
                byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(combinedNames));
                return Convert.ToBase64String(hash);
            }
        }

        #endregion

        #region Utility

        private string GetLocalFilePath(string firebasePath)
        {
            string relativePath = firebasePath;
            if (relativePath.StartsWith(downloadPath))
            {
                relativePath = relativePath.Substring(downloadPath.Length);
            }

            return Path.Combine(persistentDataPath, relativePath);
        }

        private void EnsureDirectoryExists(string filePath)
        {
            string localDir = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(localDir))
            {
                Directory.CreateDirectory(localDir);
            }
        }

        private void UpdateProgress()
        {
            if (totalFiles > 0)
            {
                totalProgress = (float)downloadedFiles / totalFiles;
                OnProgressChanged?.Invoke(totalProgress);
            }
            OnDownloadStatsUpdated?.Invoke(downloadedBytes, totalBytes);
        }

        private void ResetProgress()
        {
            totalProgress = 0f;
            downloadedFiles = 0;
            downloadedBytes = 0;
            UpdateProgress();
        }

        private string FormatFileSize(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB" };
            int counter = 0;
            decimal number = bytes;

            while (Math.Round(number / 1024) >= 1)
            {
                number /= 1024;
                counter++;
            }

            return $"{number:n1} {suffixes[counter]}";
        }

        private void LogStatus(string message)
        {
            $"[AddressableDownloadManager] {message}".DLog();
            OnStatusUpdate?.Invoke(message);
        }

        private void LogError(string message)
        {
            $"[AddressableDownloadManager] {message}".DError();
            OnDownloadError?.Invoke(message);
        }

        #endregion
    }
}

