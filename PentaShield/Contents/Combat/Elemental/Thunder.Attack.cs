using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace penta
{
    /// <summary>
    /// Thunder 엘리멘탈 공격 로직 - 체인 라이트닝
    /// </summary>
    public partial class Thunder
    {
        [Header("THUNDER SETTING")]
        [SerializeField] private float thunderWaveRange = 10f;
        [SerializeField] private GameObject lightnningeffectPrefab;
        private void OnUpgradedAttack()
        {
            if (!CanExecuteAttack()) return;

            OnAttackFromLevel(
                damage : GetCurrentDamage()
                );
        }

        private bool CanExecuteAttack()
        {
            return enemiesNearby && activeProjectiles.Count < maxActiveProjectiles;
        }

        private void OnAttackFromLevel(float damage)
        {
            if (activeProjectiles.Count >= maxActiveProjectiles) return;

            Transform nearestEnemy = FindNearestEnemy();
            if (nearestEnemy == null) return;

            Vector3 direction = (nearestEnemy.position - firePoint.position).normalized;

            GameObject projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.LookRotation(direction));
            activeProjectiles.Add(projectile);

            Projectile projectileComponent = projectile.GetComponent<Projectile>();
            if (projectileComponent != null)
            {
                projectileComponent.ResetProjectile();
                projectileComponent.enemyLayer = enemyLayer;
                projectileComponent.Damage = damage;
                projectileComponent.SetProjectileType(Projectile.ProjectileType.Thunder);
                projectileComponent.SetOptimizedMode(true);
                projectileComponent.SetLifetime(projectileLifetime);
                projectileComponent.SetParent(this);
                projectileComponent.SetThunderProperties(this, GetCurrentDamage());
            }
        }

        private Transform FindNearestEnemy()
        {
            Collider[] enemies = Physics.OverlapSphere(transform.position, thunderWaveRange, enemyLayer);

            Transform nearest = null;
            float shortestDistance = float.MaxValue;

            foreach (Collider enemy in enemies)
            {
                float distance = Vector3.Distance(transform.position, enemy.transform.position);
                if (distance < shortestDistance)
                {
                    shortestDistance = distance;
                    nearest = enemy.transform;
                }
            }

            return nearest;
        }

        /// <summary>
        /// 다음 체인 타겟 찾기
        /// </summary>
        public Transform FindNextBounceTarget(Vector3 currentPosition, HashSet<GameObject> hitTargets)
        {
            Collider[] enemies = Physics.OverlapSphere(currentPosition, thunderWaveRange, enemyLayer);

            Transform bestTarget = null;
            float shortestDistance = float.MaxValue;

            foreach (Collider enemy in enemies)
            {
                if (hitTargets.Contains(enemy.gameObject)) continue;

                float distance = Vector3.Distance(currentPosition, enemy.transform.position);
                if (distance < shortestDistance)
                {
                    shortestDistance = distance;
                    bestTarget = enemy.transform;
                }
            }

            return bestTarget;
        }

        /// <summary>
        /// 라이트닝 이펙트 생성
        /// </summary>
        public void CreateThunderBounceEffect(Vector3 position)
        {
            var e = Instantiate(lightnningeffectPrefab, position, Quaternion.identity);
            if (e != null)
            {
                var ps = e.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    float duration = ps.main.duration + ps.main.startLifetime.constantMax;
                    Destroy(e, duration + 0.5f);
                }
                else
                {
                    Destroy(e, 1f);
                }
            }
        }
    }
}