using Cysharp.Threading.Tasks;
using Firebase.Database;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace penta
{
    /// <summary>
    /// Firebase Realtime Database 래퍼 (주요 로직)
    /// - CRUD 작업
    /// - 실시간 리스너
    /// </summary>
    public class PRealTimeDb : IDisposable
    {
        private DatabaseReference _databaseRef = null;
        private Dictionary<string, EventHandler<ValueChangedEventArgs>> _activeListeners = new Dictionary<string, EventHandler<ValueChangedEventArgs>>();

        public bool IsInitialized { get; private set; } = false;
        public DatabaseReference RootReference => _databaseRef;

        public PRealTimeDb(DatabaseReference instance)
        {
            if (instance == null)
            {
                return;
            }

            _databaseRef = instance;
            IsInitialized = true;
        }

        /// <summary> 데이터 저장 </summary>
        public async UniTask<bool> SetDataAsync<T>(string path, T data)
        {
            if (!IsInitialized) return false;
            if (string.IsNullOrEmpty(path)) return false;

            try
            {
                DatabaseReference reference = _databaseRef.Child(path);
                string json = JsonConvert.SerializeObject(data);
                await reference.SetRawJsonValueAsync(json);
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        /// <summary> 단순 값 저장 </summary>
        public async UniTask<bool> SetValueAsync(string path, object value)
        {
            if (!IsInitialized) return false;
            if (string.IsNullOrEmpty(path)) return false;

            try
            {
                DatabaseReference reference = _databaseRef.Child(path);
                await reference.SetValueAsync(value);
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        /// <summary> 데이터 읽기 </summary>
        public async UniTask<T> GetDataAsync<T>(string path) where T : class
        {
            if (!IsInitialized) return default;
            if (string.IsNullOrEmpty(path)) return default;

            try
            {
                DatabaseReference reference = _databaseRef.Child(path);
                DataSnapshot snapshot = await reference.GetValueAsync();

                if (snapshot != null && snapshot.Exists)
                {
                    string json = snapshot.GetRawJsonValue();
                    if (string.IsNullOrEmpty(json))
                    {
                        return default;
                    }

                    T result = JsonConvert.DeserializeObject<T>(json);
                    return result;
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

        /// <summary> 단순 값 읽기 </summary>
        public async UniTask<object> GetValueAsync(string path)
        {
            if (!IsInitialized) return null;
            if (string.IsNullOrEmpty(path)) return null;

            try
            {
                DatabaseReference reference = _databaseRef.Child(path);
                DataSnapshot snapshot = await reference.GetValueAsync();

                if (snapshot != null && snapshot.Exists)
                {
                    return snapshot.Value;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception e)
            {
                return null;
            }
        }

        /// <summary> 데이터 부분 업데이트 </summary>
        public async UniTask<bool> UpdateDataAsync(string path, Dictionary<string, object> updates)
        {
            if (!IsInitialized) return false;
            if (string.IsNullOrEmpty(path)) return false;
            if (updates == null || updates.Count == 0) return false;

            try
            {
                DatabaseReference reference = _databaseRef.Child(path);
                await reference.UpdateChildrenAsync(updates);
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        /// <summary> 데이터 삭제 </summary>
        public async UniTask<bool> DeleteDataAsync(string path)
        {
            if (!IsInitialized) return false;
            if (string.IsNullOrEmpty(path)) return false;

            try
            {
                DatabaseReference reference = _databaseRef.Child(path);
                await reference.RemoveValueAsync();
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        /// <summary> 실시간 리스너 등록 </summary>
        public bool ListenToValueChanged(string path, Action<DataSnapshot> onValueChanged)
        {
            if (!IsInitialized) return false;
            if (string.IsNullOrEmpty(path)) return false;
            if (onValueChanged == null) return false;

            try
            {
                if (_activeListeners.ContainsKey(path))
                {
                    RemoveListener(path);
                }

                DatabaseReference reference = _databaseRef.Child(path);

                EventHandler<ValueChangedEventArgs> eventHandler = (sender, args) =>
                {
                    if (args.DatabaseError != null)
                    {
                        return;
                    }

                    try
                    {
                        onValueChanged?.Invoke(args.Snapshot);
                    }
                    catch (Exception e)
                    {
                    }
                };

                reference.ValueChanged += eventHandler;
                _activeListeners[path] = eventHandler;

                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        /// <summary> 리스너 제거 </summary>
        public bool RemoveListener(string path)
        {
            if (!_activeListeners.ContainsKey(path))
            {
                return false;
            }

            try
            {
                DatabaseReference reference = _databaseRef.Child(path);
                EventHandler<ValueChangedEventArgs> eventHandler = _activeListeners[path];

                reference.ValueChanged -= eventHandler;
                _activeListeners.Remove(path);

                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        /// <summary> 모든 리스너 제거 </summary>
        public void RemoveAllListeners()
        {
            var paths = new List<string>(_activeListeners.Keys);
            foreach (var path in paths)
            {
                RemoveListener(path);
            }
        }

        /// <summary> DatabaseReference 반환 </summary>
        public DatabaseReference GetReference(string path)
        {
            if (!IsInitialized) return null;

            if (string.IsNullOrEmpty(path))
            {
                return _databaseRef;
            }

            return _databaseRef.Child(path);
        }

        /// <summary> 사용자별 데이터 경로 생성 </summary>
        public string GetUserPath(string userId, string subPath = "")
        {
            if (string.IsNullOrEmpty(userId))
            {
                return string.Empty;
            }

            string basePath = $"users/{userId}";
            return string.IsNullOrEmpty(subPath) ? basePath : $"{basePath}/{subPath}";
        }

        public void Dispose()
        {
            RemoveAllListeners();
            _databaseRef = null;
            IsInitialized = false;
        }
    }
}
