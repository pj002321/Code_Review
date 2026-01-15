using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Hunt
{
    public class PoolManager : MonoBehaviourSingleton<PoolManager>
    {
        [SerializeField] private int maxPoolSize;

        private Dictionary<Type, object> pools = new Dictionary<Type, object>();
        
        public ObjectPool<T> GetPool<T>(T prefab, int poolSize=0) where T : Component
        {
            Type t = typeof(T);

            if (!pools.ContainsKey(t))
            {
                var pool = new ObjectPool<T>(
                    createFunc: () => CreatePooledItem(prefab),
                    actionOnGet: (obj) => obj.gameObject.SetActive(true),
                    actionOnRelease: (obj) => obj.gameObject.SetActive(false),
                    actionOnDestroy: (obj) => Destroy(obj.gameObject),
                    collectionCheck: true,
                    defaultCapacity: poolSize,
                    maxSize:maxPoolSize
                    );

                pools[t] = pool;

            }
            return pools[t] as ObjectPool<T>;
        }

        private T CreatePooledItem<T>(T prefab) where T : Component
        {
            T instance = Instantiate(prefab);
            instance.transform.SetParent(this.transform);
            instance.gameObject.SetActive(false);
            return instance;
        }
        
        public T Spawn<T>(T prefab, Vector3 position, Quaternion rotation) where T : Component
        {
            var pool = GetPool(prefab);
            T obj = pool.Get();
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            return obj;
        }

        public void Despawn<T>(T prefab, T obj) where T : Component
        {
            var pool = GetPool(prefab);
            pool.Release(obj);
        }

        protected override void Awake()
        {
            base.Awake();
        }
        
        protected override void OnDestroy()
        {
            pools.Clear();
            base.OnDestroy();
        }
    }
}