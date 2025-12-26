using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace penta
{
    #region Data Models

    [Serializable]
    public class FirebaseStorageItem
    {
        public string name;
        public string bucket;
        public string generation;
        public string metageneration;
        public string contentType;
        public string timeCreated;
        public string updated;
        public string storageClass;
        public string size;
        public string md5Hash;
        public string etag;
        public string downloadTokens;
    }

    [Serializable]
    public class FirebaseStorageList
    {
        public string kind;
        public List<FirebaseStorageItem> items;
        public string nextPageToken;
    }

    #endregion

    /// <summary>
    /// Firebase Storage API 클라이언트 (주요 로직)
    /// - 파일 목록 조회
    /// - 파일 다운로드
    /// </summary>
    public class FirebaseStorageClient
    {
        private const int REQUEST_DELAY_MS = 50;
        private readonly FirebaseConfig config;
        private readonly string downloadPath;

        public Action<string, float> OnFileDownloadProgress;

        public FirebaseStorageClient(FirebaseConfig config, string downloadPath)
        {
            this.config = config;
            this.downloadPath = downloadPath;
        }

        /// <summary> Firebase에서 파일 목록 가져오기 </summary>
        public async UniTask<FirebaseStorageList> FetchFileListAsync()
        {
            string listUrl = config.BuildFileListUrl(downloadPath);

            using (UnityWebRequest request = UnityWebRequest.Get(listUrl))
            {
                var operation = request.SendWebRequest();

                while (!operation.isDone)
                {
                    await UniTask.Delay(REQUEST_DELAY_MS);
                }

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        string responseText = request.downloadHandler.text;
                        return JsonConvert.DeserializeObject<FirebaseStorageList>(responseText);
                    }
                    catch (Exception e)
                    {
                        throw new Exception($"파일 목록 파싱 실패: {e.Message}");
                    }
                }
                else
                {
                    HandleRequestError(request);
                    return null;
                }
            }
        }

        /// <summary> 파일 다운로드 </summary>
        public async UniTask<byte[]> DownloadFileAsync(FirebaseStorageItem fileItem)
        {
            string downloadUrl = config.BuildDownloadUrl(fileItem.name);

            using (UnityWebRequest request = UnityWebRequest.Get(downloadUrl))
            {
                await DownloadWithProgress(request, fileItem.name);

                if (request.result == UnityWebRequest.Result.Success)
                {
                    return request.downloadHandler.data;
                }
                else
                {
                    throw new Exception($"다운로드 실패: {request.error}");
                }
            }
        }

        /// <summary> HEAD 요청으로 파일 크기 조회 </summary>
        public async UniTask<long> GetFileSizeAsync(string fileName)
        {
            string downloadUrl = config.BuildDownloadUrl(fileName);

            using (UnityWebRequest request = UnityWebRequest.Head(downloadUrl))
            {
                var operation = request.SendWebRequest();

                while (!operation.isDone)
                {
                    await UniTask.Delay(REQUEST_DELAY_MS);
                }

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string contentLength = request.GetResponseHeader("Content-Length");
                    if (long.TryParse(contentLength, out long fileSize))
                    {
                        return fileSize;
                    }
                }
            }

            return 0;
        }

        /// <summary> 진행률 추적하며 다운로드 </summary>
        private async UniTask DownloadWithProgress(UnityWebRequest request, string fileName)
        {
            var operation = request.SendWebRequest();
            float lastProgress = 0f;

            while (!operation.isDone)
            {
                float currentProgress = request.downloadProgress;
                if (currentProgress != lastProgress)
                {
                    OnFileDownloadProgress?.Invoke(fileName, currentProgress);
                    lastProgress = currentProgress;
                }

                await UniTask.Delay(REQUEST_DELAY_MS);
            }
        }

        /// <summary> Firebase 요청 오류 처리 </summary>
        private void HandleRequestError(UnityWebRequest request)
        {
            string errorMessage = request.responseCode switch
            {
                400 => "잘못된 요청: Firebase 프로젝트 ID 또는 API 키 확인 필요",
                401 => "인증 실패: Firebase API 키가 올바르지 않음",
                403 => "권한 거부: Firebase Storage 규칙 확인 필요",
                404 => "리소스 없음: Storage Bucket이 존재하지 않음",
                _ => $"알 수 없는 오류 (코드: {request.responseCode})"
            };

            throw new Exception($"Firebase 요청 실패: {errorMessage}");
        }
    }
}
