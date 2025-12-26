using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace penta
{
    /// <summary>
    /// Firebase 설정 및 URL 관리 (주요 로직)
    /// - 설정 파일 로드
    /// - Storage/Database URL 생성
    /// </summary>
    public class FirebaseConfig
    {
        private const string CONFIG_FILE = "firebase-config.json";
        private const string STORAGE_BASE_URL = "https://firebasestorage.googleapis.com/v0/b";

        public string ProjectId { get; private set; }
        public string ApiKey { get; private set; }
        public string StorageBucket { get; private set; }
        public string DatabaseUrl { get; private set; }
        public string GoogleWebClientId { get; private set; }

        [Serializable]
        private class ConfigData
        {
            public string firebaseProjectId;
            public string firebaseApiKey;
            public string storageBucket;
            public string databaseUrl;
            public string googleWebClientId;
        }

        /// <summary> StreamingAssets에서 설정 로드 </summary>
        public static FirebaseConfig Load()
        {
            var config = new FirebaseConfig();

            try
            {
                string configPath = Path.Combine(Application.streamingAssetsPath, CONFIG_FILE);
                string jsonContent = null;

#if UNITY_ANDROID && !UNITY_EDITOR
                using (UnityWebRequest request = UnityWebRequest.Get(configPath))
                {
                    request.SendWebRequest();
                    while (!request.isDone) { }

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        jsonContent = request.downloadHandler.text;
                    }
                    else
                    {
                        return config;
                    }
                }
#else
                if (File.Exists(configPath))
                {
                    jsonContent = File.ReadAllText(configPath);
                }
                else
                {
                    return config;
                }
#endif

                if (!string.IsNullOrEmpty(jsonContent))
                {
                    var data = JsonConvert.DeserializeObject<ConfigData>(jsonContent);
                    if (data != null)
                    {
                        config.ProjectId = data.firebaseProjectId ?? "";
                        config.ApiKey = data.firebaseApiKey ?? "";
                        config.StorageBucket = data.storageBucket ?? "";
                        config.DatabaseUrl = data.databaseUrl ?? "";
                        config.GoogleWebClientId = data.googleWebClientId ?? "";
                    }
                }
            }
            catch (Exception e)
            {
            }

            return config;
        }

        /// <summary> 파일 목록 조회 URL 생성 </summary>
        public string BuildFileListUrl(string prefix)
        {
            string url = $"{STORAGE_BASE_URL}/{StorageBucket}/o?prefix={Uri.EscapeDataString(prefix)}";
            if (!string.IsNullOrEmpty(ApiKey))
            {
                url += $"&key={ApiKey}";
            }
            return url;
        }

        /// <summary> 파일 다운로드 URL 생성 </summary>
        public string BuildDownloadUrl(string fileName)
        {
            string url = $"{STORAGE_BASE_URL}/{StorageBucket}/o/{Uri.EscapeDataString(fileName)}?alt=media";
            if (!string.IsNullOrEmpty(ApiKey))
            {
                url += $"&key={ApiKey}";
            }
            return url;
        }

        /// <summary> Database URL 정규화 </summary>
        public string GetNormalizedDatabaseUrl()
        {
            if (string.IsNullOrEmpty(DatabaseUrl))
            {
                return string.Empty;
            }

            string normalized = DatabaseUrl.Trim();
            if (normalized.EndsWith("/"))
            {
                normalized = normalized.Substring(0, normalized.Length - 1);
            }

            return normalized;
        }

        /// <summary> Database URL 유효성 검사 </summary>
        public bool IsDatabaseUrlValid()
        {
            string url = GetNormalizedDatabaseUrl();
            if (string.IsNullOrEmpty(url))
            {
                return false;
            }

            return url.StartsWith("https://") &&
                   (url.Contains(".firebaseio.com") || url.Contains(".firebasedatabase.app"));
        }

        /// <summary> 설정 유효성 확인 </summary>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(ProjectId) &&
                   !string.IsNullOrEmpty(ApiKey) &&
                   !string.IsNullOrEmpty(StorageBucket);
        }
    }
}
