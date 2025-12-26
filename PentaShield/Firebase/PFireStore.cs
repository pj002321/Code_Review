using penta;
using System.IO;
using Cysharp.Threading.Tasks;
using Firebase.Firestore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace penta
{
    /// <summary>
    /// Firebase Firestore 래퍼 (주요 로직)
    /// - 문서 CRUD 작업
    /// - 랭킹 관리
    /// - 서버 타임스탬프
    /// </summary>
    public class PFireStore
    {
        private FirebaseFirestore fireStore = null;
        public const string UserCollection = "users";
        public const int RankingUpdateDay = 7;

        public bool IsInitialized { get; private set; } = false;

        public PFireStore(FirebaseFirestore _instance)
        {
            if (_instance == null)
            {
                return;
            }
            fireStore = _instance;
            IsInitialized = true;
        }

        /// <summary> 사용자 문서 참조 반환 </summary>
        public DocumentReference GetUserDocumentReference(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return null;
            }
            return fireStore.Collection(UserCollection).Document(userId);
        }

        /// <summary> 문서 저장 </summary>
        public async UniTask<bool> SetDocumentAsync<T>(string collectionPath, string documentId, T data)
        {
            if (!IsInitialized) return false;

            try
            {
                DocumentReference docRef = fireStore.Collection(collectionPath).Document(documentId);
                await UniTask.RunOnThreadPool(async () => await docRef.SetAsync(data));
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        /// <summary> 문서 읽기 </summary>
        public async UniTask<T> GetDocumentAsync<T>(string collectionPath, string documentId) where T : class
        {
            if (!IsInitialized) return default;

            try
            {
                DocumentReference docRef = fireStore.Collection(collectionPath).Document(documentId);
                DocumentSnapshot snapshot = null;

                await UniTask.RunOnThreadPool(async () =>
                {
                    snapshot = await docRef.GetSnapshotAsync();
                });

                if (snapshot != null && snapshot.Exists)
                {
                    return snapshot.ConvertTo<T>();
                }
                else
                {
                    return default;
                }
            }
            catch (Exception e)
            {
                return default;
            }
        }

        /// <summary> 문서 삭제 </summary>
        public async UniTask<bool> DeleteDocumentAsync(string collectionPath, string documentId)
        {
            if (!IsInitialized) return false;
            if (string.IsNullOrEmpty(collectionPath)) return false;
            if (string.IsNullOrEmpty(documentId)) return false;

            try
            {
                DocumentReference docRef = fireStore.Collection(collectionPath).Document(documentId);
                await UniTask.RunOnThreadPool(async () => await docRef.DeleteAsync());
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        private async UniTask<DocumentSnapshot> GetRawDocumentSnapshotAsync(string collection, string document)
        {
            DocumentReference docRef = fireStore.Collection(collection).Document(document);
            return await docRef.GetSnapshotAsync();
        }

        /// <summary> 스테이지 랭킹 가져오기 </summary>
        public async UniTask<CachedRankings> GetStageRankingsAsync(bool forceRefresh = false)
        {
            if (!forceRefresh)
            {
                CachedRankings cachedData = LoadRankingsFromLocal();
                if (cachedData != null && (DateTime.UtcNow - cachedData.LastUpdated).TotalDays < RankingUpdateDay)
                {
                    return cachedData;
                }
            }

            try
            {
                var firestoreData = await FetchRankingsFromFirestoreAsync();
                if (firestoreData != null)
                {
                    SaveRankingsToLocal(firestoreData);
                    return firestoreData;
                }
            }
            catch (Exception e)
            {
            }

            return LoadRankingsFromLocal();
        }

        private async UniTask<CachedRankings> FetchRankingsFromFirestoreAsync()
        {
            var rankingDocSnapshot = await GetRawDocumentSnapshotAsync("ranking", "stageRankings");

            if (rankingDocSnapshot == null || !rankingDocSnapshot.Exists)
            {
                return null;
            }

            return ParseRankingsFromSnapshot(rankingDocSnapshot);
        }

        /// <summary> 랭킹에서 유저 제거 </summary>
        public async UniTask<bool> RemoveUserFromRankingsAsync(string userId)
        {
            if (!IsInitialized) return false;
            if (string.IsNullOrEmpty(userId)) return false;

            try
            {
                var rankingDocSnapshot = await GetRawDocumentSnapshotAsync("ranking", "stageRankings");
                if (rankingDocSnapshot == null || !rankingDocSnapshot.Exists)
                {
                    return true;
                }

                Dictionary<string, object> rawData = rankingDocSnapshot.ToDictionary();
                Dictionary<string, object> updatedData = new Dictionary<string, object>();

                foreach (var pair in rawData)
                {
                    if (pair.Key == "lastUpdated")
                    {
                        updatedData[pair.Key] = pair.Value;
                        continue;
                    }

                    string stageName = pair.Key;
                    var rankerObjects = pair.Value as List<object>;
                    if (rankerObjects == null)
                    {
                        updatedData[pair.Key] = pair.Value;
                        continue;
                    }

                    var filteredRankerList = new List<object>();
                    foreach (var rankerObj in rankerObjects)
                    {
                        if (rankerObj is Dictionary<string, object> rankerData)
                        {
                            string rankUserId = rankerData.ContainsKey("Id") ? rankerData["Id"].ToString() : "";
                            if (rankUserId != userId)
                            {
                                filteredRankerList.Add(rankerObj);
                            }
                        }
                    }

                    updatedData[stageName] = filteredRankerList;
                }

                updatedData["lastUpdated"] = Timestamp.FromDateTime(DateTime.UtcNow);

                DocumentReference rankingDocRef = fireStore.Collection("ranking").Document("stageRankings");
                await UniTask.RunOnThreadPool(async () => await rankingDocRef.SetAsync(updatedData));
                if (File.Exists(PentaConst.SaveRankFilePath))
                {
                    try
                    {
                        File.Delete(PentaConst.SaveRankFilePath);
                    }
                    catch (Exception e)
                    {
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        private CachedRankings LoadRankingsFromLocal()
        {
            if (!File.Exists(PentaConst.SaveRankFilePath)) return null;

            try
            {
                byte[] encryptedBytes = File.ReadAllBytes(PentaConst.SaveRankFilePath);
                string json = EncryptionHelper.Decrypt(encryptedBytes);

                if (string.IsNullOrEmpty(json))
                {
                    return null;
                }
                return JsonConvert.DeserializeObject<CachedRankings>(json);
            }
            catch (Exception e)
            {
                return null;
            }
        }

        private void SaveRankingsToLocal(CachedRankings data)
        {
            try
            {
                string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                byte[] encryptedBytes = EncryptionHelper.Encrypt(json);
                File.WriteAllBytes(PentaConst.SaveRankFilePath, encryptedBytes);
            }
            catch (Exception e)
            {
            }
        }

        private CachedRankings ParseRankingsFromSnapshot(DocumentSnapshot snapshot)
        {
            var newCachedData = new CachedRankings();
            Dictionary<string, object> rawData = snapshot.ToDictionary();

            foreach (var pair in rawData)
            {
                if (pair.Key == "lastUpdated")
                {
                    newCachedData.LastUpdated = (pair.Value as Timestamp?)?.ToDateTime() ?? DateTime.MinValue;
                    continue;
                }

                string stageName = pair.Key;
                var rankerObjects = pair.Value as List<object>;
                if (rankerObjects == null) continue;

                var rankerList = new List<RankData>();
                foreach (var rankerObj in rankerObjects)
                {
                    if (rankerObj is Dictionary<string, object> rankerData)
                    {
                        string userId = rankerData.ContainsKey("Id") ? rankerData["Id"].ToString() : "";
                        string userName = rankerData.ContainsKey("Name") ? rankerData["Name"].ToString() : "";

                        if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(userId))
                        {
                            userName = "Unknown";
                        }

                        var info = new RankData
                        {
                            Id = userId,
                            Name = userName,
                            Region = rankerData.ContainsKey("Region") ? (RegionConst)Convert.ToInt64(rankerData["Region"]) : (RegionConst)0,
                            Level = rankerData.ContainsKey("Level") ? Convert.ToInt32(rankerData["Level"]) : 0,
                            StageName = rankerData.ContainsKey("StageName") ? rankerData["StageName"].ToString() : "",
                            Score = rankerData.ContainsKey("Score") ? Convert.ToSingle(rankerData["Score"]) : 0f,
                            Round = rankerData.ContainsKey("Round") ? Convert.ToInt32(rankerData["Round"]) : 0
                        };
                        rankerList.Add(info);
                    }
                }
                newCachedData.StageRankings[stageName] = rankerList;
            }
            return newCachedData;
        }

        /// <summary> 서버 타임스탬프 가져오기 </summary>
        public async UniTask<DateTime?> GetServerTimestampAsync()
        {
            if (!IsInitialized) return null;

            try
            {
                DocumentReference tempDocRef = fireStore.Collection("_temp").Document("serverTime");
                Dictionary<string, object> tempData = new Dictionary<string, object>
                {
                    { "timestamp", FieldValue.ServerTimestamp }
                };

                await UniTask.RunOnThreadPool(async () => await tempDocRef.SetAsync(tempData));

                DocumentSnapshot snapshot = await tempDocRef.GetSnapshotAsync();

                if (snapshot != null && snapshot.Exists)
                {
                    if (snapshot.TryGetValue("timestamp", out object timestampValue))
                    {
                        if (timestampValue is Timestamp timestamp)
                        {
                            DateTime serverTime = timestamp.ToDateTime();
                            await tempDocRef.DeleteAsync();
                            return serverTime;
                        }
                    }
                }

                try
                {
                    await tempDocRef.DeleteAsync();
                }
                catch { }

                return null;
            }
            catch (Exception e)
            {
                return null;
            }
        }
    }
}
