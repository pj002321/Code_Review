using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace PentaShield
{
    /// <summary> Firebase 설정 및 URL 관리 </summary>
    public class FirebaseConfig
    {
        private const string CONFIG_FILE = "firebase-config.json";
        private const string STORAGE_BASE_URL = "https://firebasestorage.googleapis.com/v0/b";

        public string ProjectId { get; private set; }
        public string ApiKey { get; private set; }
        public string StorageBucket { get; private set; }

        [Serializable]
        private class ConfigData
        {
            public string firebaseProjectId;
            public string firebaseApiKey;
            public string storageBucket;
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
                        Debug.Log($"[FirebaseConfig] ✅ 설정 로드 완료 (Android)");
                    }
                    else
                    {
                        Debug.LogWarning($"[FirebaseConfig] ⚠️ 설정 파일 없음: {configPath}");
                        return config;
                    }
                }
#else
                if (File.Exists(configPath))
                {
                    jsonContent = File.ReadAllText(configPath);
                    Debug.Log($"[FirebaseConfig] ✅ 설정 로드 완료");
                }
                else
                {
                    Debug.LogWarning($"[FirebaseConfig] ⚠️ 설정 파일 없음: {configPath}");
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
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseConfig] 설정 로드 실패: {e.Message}");
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

        /// <summary> 설정이 유효한지 확인 </summary>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(ProjectId) &&
                   !string.IsNullOrEmpty(ApiKey) &&
                   !string.IsNullOrEmpty(StorageBucket);
        }
    }
}

