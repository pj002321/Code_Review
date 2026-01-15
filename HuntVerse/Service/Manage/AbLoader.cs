using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System;

namespace Hunt
{
    public class AbLoader : MonoBehaviourSingleton<AbLoader>
    {
        private Dictionary<string, AsyncOperationHandle> assetHandlers = new Dictionary<string, AsyncOperationHandle>();
        private Dictionary<string, int> assetReferenceCounts = new Dictionary<string, int>();
        private Dictionary<GameObject, string> instantiatedObjects = new Dictionary<GameObject, string>();
        protected override bool DontDestroy => true;
        protected override void Awake()
        {
            base.Awake();
        }

        protected override void OnDestroy()
        {
            foreach (var handler in assetHandlers.Values)
            {
                Addressables.Release(handler);
            }
            assetHandlers.Clear();
            assetReferenceCounts.Clear();
            instantiatedObjects.Clear();
            base.OnDestroy();
        }

        public async UniTask<T> LoadAssetAsync<T>(string key) where T : UnityEngine.Object
        {
            var bundleKey = key.ToLower();
            if (assetHandlers.TryGetValue(bundleKey, out var existingHandle))
            {
                assetReferenceCounts[bundleKey]++;
                await existingHandle.Task;
                return (T)existingHandle.Result;
            }

            AsyncOperationHandle<T> newHandle = default;
            try
            {
                newHandle = Addressables.LoadAssetAsync<T>(bundleKey);
                assetHandlers[bundleKey] = newHandle;
                assetReferenceCounts[bundleKey] = 1;
                T result = await newHandle.Task;

                if (newHandle.Status == AsyncOperationStatus.Succeeded)
                {
                    return result;
                }
                else
                {
                    throw new Exception($"📥 [AbLoader] Asset Load Fail: {bundleKey}");

                }
            }
            catch (Exception ex)
            {
                $"📥 [AbLoader] Asset Load Fail : {ex.Message}".DError();
                assetHandlers.Remove(bundleKey);
                assetReferenceCounts.Remove(bundleKey);
                Addressables.Release(newHandle);
                return null;
            }

        }
        public async UniTask<GameObject> LoadInstantiateAsync(string key, Vector3 position = default, Quaternion rotation = default)
        {
            var prefab = await LoadAssetAsync<GameObject>(key);

            if(prefab == null)
            {
                $"📥 [AbLoader] Prefab Load Fail, Don't Create {key} instance".DError();
                ReleaseAsset(key);
                return null;
            }

            GameObject instance = Instantiate(prefab, position, rotation);
            instantiatedObjects[instance] = key;
            return instance;
        }

        public void ReleaseAsset(string key)
        {
            if(!assetHandlers.ContainsKey(key))
            {
                return;
            }

            assetReferenceCounts[key]--;
            if (assetReferenceCounts[key] <= 0)
            {
                var handle = assetHandlers[key];
                Addressables.Release(handle);
                assetHandlers.Remove(key);
                assetReferenceCounts.Remove(key);
                $"📥 [AbLoader] Assets were freed from memory {key}".DLog();
            }
        }
    }
}
