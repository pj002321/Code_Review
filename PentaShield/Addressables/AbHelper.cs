using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace penta
{
    public class AbHelper : MonoBehaviourSingleton<AbHelper>
    {
        private Dictionary<string, AsyncOperationHandle> _assetHandles = new Dictionary<string, AsyncOperationHandle>();
        private Dictionary<string, int> _assetReferenceCounts = new Dictionary<string, int>();
        private Dictionary<GameObject, string> _instantiatedObjects = new Dictionary<GameObject, string>();

        protected override bool DontDestroy => base.DontDestroy;

        protected override void Awake()
        {
            base.Awake();
        }

        protected override void OnDestroy()
        {
            foreach (var handle in _assetHandles.Values)
            {
                Addressables.Release(handle);
            }
            _assetHandles.Clear();
            _assetReferenceCounts.Clear();
            _instantiatedObjects.Clear();

            base.OnDestroy();
        }


        /// <summary>
        /// 주소(key)를 이용해 에셋을 비동기적으로 로드합니다.
        /// 이미 로드 중이거나 로드가 완료된 에셋은 기존 핸들을 반환합니다.
        /// </summary>
        /// <typeparam name="T">로드할 에셋의 타입</typeparam>
        /// <param name="key">어드레서블 주소 (키)</param>
        /// <returns>로드된 에셋을 담은 Task</returns>
        public async UniTask<T> LoadAssetAsync<T>(string _key) where T : UnityEngine.Object
        {
            string key = _key.ToLower();

            // 이미 로드 요청이 있었는지 확인
            if (_assetHandles.TryGetValue(key, out var existingHandle))
            {
                _assetReferenceCounts[key]++;
                // 아직 로드가 진행 중일 수 있으므로, 완료될 때까지 기다립니다.
                await existingHandle.Task;
                return (T)existingHandle.Result;
            }

            AsyncOperationHandle<T> newHandle = default;
            try
            {                
                 newHandle = Addressables.LoadAssetAsync<T>(key);
                _assetHandles[key] = newHandle;
                _assetReferenceCounts[key] = 1;

                // 핸들의 비동기 작업이 끝날 때까지 기다립니다.
                T result = await newHandle.Task;

                // 작업 성공 시 결과 반환
                if (newHandle.Status == AsyncOperationStatus.Succeeded)
                {
                    return result;
                }
                else
                {
                    // 로드 실패 시 예외가 발생했겠지만, 안전을 위해 추가 처리
                    throw new Exception($"[AbHelper] 에셋 로드 실패: {key}");
                }
            }
            catch (Exception ex)
            {
                $"[AbHelper] 에셋 로드 중 예외 발생: {key}, 에러: {ex.Message}".DError();
                _assetHandles.Remove(key);
                _assetReferenceCounts.Remove(key);
                Addressables.Release(newHandle); // 실패한 핸들도 릴리즈 해야 할 수 있습니다.
                return null;
            }
        }

        /// <summary>
        /// 주소(key)를 이용해 게임 오브젝트를 비동기적으로 생성(Instantiate)합니다.
        /// </summary>
        /// <returns>생성된 게임 오브젝트를 담은 Task</returns>
        public async UniTask<GameObject> InstantiateAsync(string key, Vector3 position = default, Quaternion rotation = default, Transform parent = null)
        {
            // 먼저 프리팹을 비동기적으로 로드합니다.
            GameObject prefab = await LoadAssetAsync<GameObject>(key);

            if (prefab == null)
            {
                $"[AbHelper] 프리팹 로드에 실패하여 인스턴스를 생성할 수 없습니다: {key}".DError();
                // LoadAssetAsync에서 이미 참조 카운트를 1 올렸으므로, 실패 시 다시 감소시켜야 합니다.
                ReleaseAsset(key);
                return null;
            }

            GameObject instance = Instantiate(prefab, position, rotation, parent);
            _instantiatedObjects[instance] = key; // 생성된 인스턴스와 키를 매핑

            return instance;
        }

        /// <summary>
        /// 로드했던 에셋의 참조를 해제합니다. 참조 카운트가 0이 되면 실제 메모리에서 해제됩니다.
        /// </summary>
        public void ReleaseAsset(string key)
        {
            if (!_assetHandles.ContainsKey(key))
            {
                $"[AbHelper] 헬퍼를 통해 로드되지 않았거나 이미 해제된 에셋의 해제를 시도했습니다: {key}".DWarning();
                return;
            }

            _assetReferenceCounts[key]--;
            if (_assetReferenceCounts[key] <= 0)
            {
                var handle = _assetHandles[key];
                Addressables.Release(handle);
                _assetHandles.Remove(key);
                _assetReferenceCounts.Remove(key);
                $"[AbHelper] 에셋이 메모리에서 해제되었습니다: {key}".Log();
            }
        }

        /// <summary>
        /// AbHelper를 통해 로드된 모든 에셋을 해제하고, 생성된 모든 게임 오브젝트를 파괴합니다.
        /// 주로 씬 전환 직전에 호출합니다.
        /// </summary>
        public void ReleaseAll()
        {
            "모든 어드레서블 리소스를 해제합니다.".Log();
            ClearAllTrackedAssets();
        }

        /// <summary>
        /// 추적 중인 모든 리소스를 정리하는 내부 함수입니다.
        /// </summary>
        private void ClearAllTrackedAssets()
        {
            foreach (var instance in _instantiatedObjects.Keys.ToList())
            {
                if (instance != null)
                {
                    Destroy(instance);
                }
            }

            foreach (var handle in _assetHandles.Values)
            {
                Addressables.Release(handle);
            }
            
            _assetHandles.Clear();
            _assetReferenceCounts.Clear();
            _instantiatedObjects.Clear();
        }

        /// <summary>
        /// 생성했던 게임 오브젝트 인스턴스를 파괴하고, 해당 에셋의 참조 카운트를 감소시킵니다.
        /// </summary>
        public void DestroyAndReleaseInstance(GameObject instanceToDestroy)
        {
            if (instanceToDestroy != null && _instantiatedObjects.TryGetValue(instanceToDestroy, out string key))
            {
                Destroy(instanceToDestroy);
                _instantiatedObjects.Remove(instanceToDestroy);
                ReleaseAsset(key);
            }
            else
            {
                $"[AbHelper] 이 헬퍼를 통해 생성되지 않은 오브젝트의 파괴를 시도했습니다.\n{instanceToDestroy}".DWarning();
                if (instanceToDestroy != null) Destroy(instanceToDestroy);
            }
        }

    }     
}
