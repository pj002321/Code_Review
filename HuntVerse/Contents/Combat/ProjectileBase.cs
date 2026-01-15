using Cysharp.Threading.Tasks;
using System;
using Unity.VisualScripting;
using UnityEngine;

namespace Hunt
{

    [RequireComponent(typeof(Rigidbody2D))]
    public abstract class ProjectileBase : MonoBehaviour, IProjectile
    {
        private Rigidbody2D rb;
        private float lifeTime = 5.0f;
        private float currentlifeTime = 0f;
        private float speed = 2.0f;
        
        private ProjectileBase prefabRef;

        protected virtual void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
        }

        public void Init(ProjectileBase prefab)
        {
            this.prefabRef = prefab;
        }

        public abstract void Launch(Vector2 direction, float speed, float duration);
        //{
        //    this.speed = speed;
        //    this.lifeTime = duration;
        //    currentlifeTime = 0f;

        //    gameObject.SetActive(true);
        //    rb.linearVelocity = direction.normalized * speed;
        //}

        protected virtual void Update()
        {
            currentlifeTime += Time.deltaTime;
            if (currentlifeTime >= lifeTime) 
            {
                Despawn();
            }
        }
        public void SetCollision(Vector2 size)
        {
            var c = GetComponent<CapsuleCollider2D>();
            c.size = size;
        }

        public async UniTask SetChildModel(string key)
        {
            if (AbLoader.Shared == null)
            {
                $"Asset Loader is NULL!".DError();
                return;
            }
            var go = await AbLoader.Shared.LoadInstantiateAsync(key);
            go.transform.SetParent(this.transform);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
        }

        protected virtual void OnTriggerEnter2D(Collider2D collision)
        {
            Despawn();
        }
        protected virtual void OnHit(Collider2D collision)
        {

        }
        protected virtual void Despawn()
        {
            rb.linearVelocity = Vector2.zero;
            PoolManager.Shared.Despawn(prefabRef, this);
        }


        protected virtual void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, rb.linearVelocity.normalized * 0.5f);
        }
    }
}