using System.Collections;
using UnityEngine;

namespace chaos
{
    /// <summary>
    /// 썬더 글로벌 아이템 오브젝트 (주요 로직)
    /// - 적 머리 위에서 발사체 생성
    /// - 수직 발사체로 적 공격 (최대 체력의 30%)
    /// </summary>
    public class ThunderGlobalItemObject : MonoBehaviour
    {
        [Header("THUNDER SETTINGS")]
        [SerializeField] private float damagePercent = 0.3f;
        [SerializeField] private float lifeTime = 5f;
        [SerializeField] private float followHeight = 3f;
        [SerializeField] private float projectileSpeed = 15f;
        [SerializeField] private float projectileInterval = 0.3f;
        [SerializeField] private float projectileLifeTime = 2f;

        private Enemy targetEnemy;
        private bool isActive = true;
        private Coroutine shootingCoroutine;
        private Coroutine lifeTimeCoroutine;

        private void Start()
        {
            if (targetEnemy != null)
            {
                shootingCoroutine = StartCoroutine(Co_ShootProjectiles());
            }
            lifeTimeCoroutine = StartCoroutine(Co_LifeTime());
        }

        private void Update()
        {
            if (isActive && targetEnemy != null)
            {
                Vector3 targetPosition = targetEnemy.transform.position;
                targetPosition.y += followHeight;
                transform.position = targetPosition;
            }
        }

        public void SetTarget(Enemy enemy)
        {
            targetEnemy = enemy;
        }

        /// <summary> 발사체 생성 코루틴 </summary>
        private IEnumerator Co_ShootProjectiles()
        {
            while (isActive && targetEnemy != null)
            {
                ShootProjectile();
                yield return new WaitForSeconds(projectileInterval);
            }
        }

        /// <summary> 발사체 생성 및 발사 </summary>
        private void ShootProjectile()
        {
            GameObject projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectile.transform.position = transform.position;
            projectile.transform.localScale = Vector3.one * 0.3f;

            Rigidbody rb = projectile.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.velocity = Vector3.down * projectileSpeed;

            ThunderProjectile projectileScript = projectile.AddComponent<ThunderProjectile>();
            projectileScript.Initialize(damagePercent, projectileLifeTime);
        }

        private IEnumerator Co_LifeTime()
        {
            yield return new WaitForSeconds(lifeTime);
            isActive = false;

            if (shootingCoroutine != null)
            {
                StopCoroutine(shootingCoroutine);
            }

            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (shootingCoroutine != null)
            {
                StopCoroutine(shootingCoroutine);
            }

            if (lifeTimeCoroutine != null)
            {
                StopCoroutine(lifeTimeCoroutine);
            }
        }
    }

    /// <summary> 썬더 발사체 </summary>
    public class ThunderProjectile : MonoBehaviour
    {
        private float damagePercent;
        private bool hasHit = false;

        public void Initialize(float damage, float lifeTime)
        {
            damagePercent = damage;
            Destroy(gameObject, lifeTime);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (hasHit) return;

            Enemy enemy = other.GetComponent<Enemy>();
            if (enemy != null)
            {
                hasHit = true;
                float damage = enemy.MaxHealth * damagePercent;
                enemy.OnHit(damage);
                Destroy(gameObject);
            }
        }
    }
}
