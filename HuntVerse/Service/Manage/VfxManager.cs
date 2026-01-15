using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Hunt
{
    public class VfxManager : MonoBehaviourSingleton<VfxManager>
    {
        [Header("VFX SETTINGS")]
        [SerializeField] private int maxVfxPoolCount = 500;

        // 키별 프리팹 캐시
        private readonly Dictionary<string, GameObject> prefabCache = new();
        private readonly Dictionary<string, VfxObject> vfxObjectPrefabs = new();
        // 키별 프리팹의 원본 scale 저장
        private readonly Dictionary<string, Vector3> prefabOriginalScales = new();
        
        // 키별 독립적인 풀 관리 (프리팹별로 구분)
        private readonly Dictionary<string, ObjectPool<VfxObject>> pools = new();
        
        protected override bool DontDestroy => true;

        #region 프리팹 로드

        private async UniTask<VfxObject> GetOrLoadVfxObject(string key)
        {
            if(vfxObjectPrefabs.TryGetValue(key, out var cachedPrefab))
            {
                return cachedPrefab;
            }

            var bundleKey = key.ToLower();
            var prefab = await AbLoader.Shared.LoadAssetAsync<GameObject>(bundleKey);

            if (prefab == null)
            {
                $"🎆 [VfxHelper] 프리팹 로드 실패: {key}".DError();
                return null;
            }

            prefabCache[key] = prefab;
            
            // 프리팹의 원본 scale 저장
            prefabOriginalScales[key] = prefab.transform.localScale;

            var vfxObj = prefab.GetComponent<VfxObject>();
            if (vfxObj == null)
            {
                vfxObj = prefab.AddComponent<VfxObject>();
            }

            vfxObjectPrefabs[key] = vfxObj;
            return vfxObj;
        }

        #endregion

        #region 풀 관리

        private ObjectPool<VfxObject> GetPool(string key, VfxObject prefab)
        {
            if (!pools.TryGetValue(key, out var pool))
            {
                pool = new ObjectPool<VfxObject>(
                    createFunc: () => CreatePooledItem(prefab, key),
                    actionOnGet: (obj) => 
                    {
                        // 원본 scale로 복원
                        if (prefabOriginalScales.TryGetValue(key, out var originalScale))
                        {
                            obj.transform.localScale = originalScale;
                        }
                        obj.gameObject.SetActive(true);
                    },
                    actionOnRelease: (obj) => 
                    {
                        // 모든 자식 객체 포함하여 비활성화
                        SetActiveRecursively(obj.gameObject, false);
                        obj.transform.SetParent(this.transform);
                        // 원본 scale로 복원
                        if (prefabOriginalScales.TryGetValue(key, out var originalScale))
                        {
                            obj.transform.localScale = originalScale;
                        }
                    },
                    actionOnDestroy: (obj) => Destroy(obj.gameObject),
                    collectionCheck: true,
                    defaultCapacity: 10,
                    maxSize: maxVfxPoolCount
                );
                pools[key] = pool;
            }
            
            return pool;
        }

        private VfxObject CreatePooledItem(VfxObject prefab, string key)
        {
            var instance = Instantiate(prefab);
            instance.transform.SetParent(this.transform);
            // 모든 자식 객체 포함하여 비활성화
            SetActiveRecursively(instance.gameObject, false);
            return instance;
        }

        #endregion

        #region Public API

        public async UniTask PreloadAsync(string key, int preloadCount = 0)
        {
            var vfxObj = await GetOrLoadVfxObject(key);
            if (vfxObj == null)
            {
                return;
            }
            
            var pool = GetPool(key, vfxObj);
            
            if (preloadCount > 0)
            {
                var preloadList = new List<VfxObject>();
                for(int i = 0; i < preloadCount; i++)
                {
                    var instance = pool.Get();
                    preloadList.Add(instance);
                }

                foreach(var instance in preloadList)
                {
                    pool.Release(instance);
                }

                $"🎆 [VfxHelper] Preload 완료: {key} ({preloadCount}ea)".DLog();
            }
            else
            {
                $"🎆 [VfxHelper] Preload 완료: {key} (프리팹만)".DLog();
            }
        }

        public async UniTask<VfxHandle> PlayOneShot(string key, Vector3 pos, Quaternion rot, Transform parent = null, Vector3? scale = null)
        {
            var vfxObj = await GetOrLoadVfxObject(key);
            if(vfxObj == null)
            {
                $"🎆 [VfxHelper] PlayOneShot 실패 - VfxObject 없음: {key}".DError();
                return null;
            }

            var pool = GetPool(key, vfxObj);
            var vfxInstance = pool.Get();
            
            if (vfxInstance == null)
            {
                $"🎆 [VfxHelper] PlayOneShot 실패 - 풀에서 인스턴스 가져오기 실패: {key}".DError();
                return null;
            }

            vfxInstance.transform.position = pos;
            vfxInstance.transform.rotation = rot;
            
            // 프리팹의 원본 scale을 가져와서 x만 변경
            if (prefabOriginalScales.TryGetValue(key, out var originalScale))
            {
                var finalScale = originalScale;
                if (scale.HasValue)
                {
                    finalScale.x = scale.Value.x; 
                }
                vfxInstance.transform.localScale = finalScale;
            }
            else
            {
                var finalScale = Vector3.one;
                if (scale.HasValue)
                {
                    finalScale.x = scale.Value.x;
                }
                vfxInstance.transform.localScale = finalScale;
            }

            if (parent != null)
            {
                vfxInstance.transform.SetParent(parent);
            }

            vfxInstance.Init(() =>
            {
                pool.Release(vfxInstance);
            });

            return new VfxHandle(vfxInstance);
        }

        public void Release(string key)
        {
            if(prefabCache.TryGetValue(key, out var prefab))
            {
                // 풀 정리
                if (pools.TryGetValue(key, out var pool))
                {
                    pools.Remove(key);
                }
                
                AbLoader.Shared.ReleaseAsset(key.ToLower());
                prefabCache.Remove(key);
                vfxObjectPrefabs.Remove(key);
                prefabOriginalScales.Remove(key);

                $"🎆 [VfxHelper] Released: {key}".DLog();
            }
        }

        public void ReleaseAll()
        {
            var keys = new List<string>(prefabCache.Keys);
            foreach (var key in keys)
            {
                Release(key);
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// 재귀적으로 GameObject와 모든 자식 객체의 활성화 상태를 설정
        /// </summary>
        private void SetActiveRecursively(GameObject obj, bool active)
        {
            if (obj == null) return;
            
            obj.SetActive(active);
            
            // 모든 자식 객체도 재귀적으로 처리
            for (int i = 0; i < obj.transform.childCount; i++)
            {
                SetActiveRecursively(obj.transform.GetChild(i).gameObject, active);
            }
        }

        #endregion
    }
}
