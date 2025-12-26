using System.Collections;
using UnityEngine;

namespace penta
{
    /// <summary>
    /// Curse 엘리멘탈 공격 로직
    /// </summary>
    public partial class Curse
    {
        [Header("CURSE SETTING")]
        [SerializeField] private float curseDuration = 5f;
        private void OnUpgradedAttack()
        {
            if (!CanExecuteAttack()) return;
            OnAttackFromLevel(
                 count: 1,
                 angleStep: 360f / (3 + level),
                 damage: GetCurrentDamage()
             );
        }

        private bool CanExecuteAttack()
        {
            return enemiesNearby && activeProjectiles.Count < maxActiveProjectiles;
        }

        private void OnAttackFromLevel(int count, float angleStep, float damage, float offsetAngle = 0f)
        {
            for (int i = 0; i < count; i++)
            {
                if (activeProjectiles.Count >= maxActiveProjectiles)
                    break;

                float angle = (i * angleStep) + offsetAngle;
                Vector3 direction = new Vector3(
                    Mathf.Sin(angle * Mathf.Deg2Rad),
                    0,
                    Mathf.Cos(angle * Mathf.Deg2Rad)
                );

                CreateProjectileFromLevel(direction, damage);
            }
        }

        private void CreateProjectileFromLevel(Vector3 direction, float damage)
        {
            GameObject levelProjectile = GetProjectileForCurrentLevel();
            GameObject projectile = Instantiate(levelProjectile, firePoint.position, Quaternion.LookRotation(direction));
            activeProjectiles.Add(projectile);

            Projectile projectileComponent = projectile.GetComponent<Projectile>();
            if (projectileComponent != null)
            {
                projectileComponent.ResetProjectile();
                projectileComponent.enemyLayer = enemyLayer;
                projectileComponent.Damage = damage;
                projectileComponent.SetProjectileType(Projectile.ProjectileType.Curse);
                projectileComponent.SetOptimizedMode(true);
                projectileComponent.SetLifetime(projectileLifetime);
                projectileComponent.SetParent(this);
                projectileComponent.SetCurseProperties(this, curseDuration);
            }
        }

        /// <summary>
        /// Curse 효과 적용 - 적의 타겟을 다른 적으로 변경
        /// </summary>
        public IEnumerator CO_ApplyCurse(GameObject target, float duration)
        {
            var e = target.GetComponent<Enemy>();
            if (e == null) yield break;

            bool wasAlreadyCursed = e.IsCursed;
            Transform originalTarget = wasAlreadyCursed ? e.FindTarget() : e.targetTrans;
            var nearestEnemy = FindNearestEnemyTarget(e.gameObject);
            
            if (nearestEnemy == null) 
            {
                if (wasAlreadyCursed)
                {
                    e.IsCursed = false;
                    e.targetTrans = originalTarget != null && originalTarget.gameObject.activeInHierarchy 
                        ? originalTarget 
                        : e.FindTarget();
                }
                yield break;
            }

            e.IsCursed = true;
            e.targetTrans = nearestEnemy;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (e == null || e.gameObject == null || e.Health <= 0)
                {
                    yield break;
                }

                if (e.targetTrans == null || !e.targetTrans.gameObject.activeInHierarchy)
                {
                    var newTarget = FindNearestEnemyTarget(e.gameObject);
                    if (newTarget != null)
                    {
                        e.targetTrans = newTarget;
                    }
                    else
                    {
                        e.IsCursed = false;
                        e.targetTrans = originalTarget != null && originalTarget.gameObject.activeInHierarchy 
                            ? originalTarget 
                            : e.FindTarget();
                        yield break;
                    }
                }

                yield return new WaitForSeconds(0.1f);
                elapsed += 0.1f;
            }

            if (e != null && e.gameObject != null && e.Health > 0)
            {
                e.IsCursed = false;
                e.targetTrans = originalTarget != null && originalTarget.gameObject.activeInHierarchy 
                    ? originalTarget 
                    : e.FindTarget();
            }
        }

        /// <summary>
        /// Curse된 적 주변에서 가장 가까운 적 찾기
        /// </summary>
        private Transform FindNearestEnemyTarget(GameObject cursedEnemy)
        {
            const float searchRadius = 50f;
            Collider[] enemyColliders = Physics.OverlapSphere(cursedEnemy.transform.position, searchRadius, enemyLayer);

            Transform nearestEnemy = null;
            float nearestDistance = float.MaxValue;

            foreach (Collider enemy in enemyColliders)
            {
                if (enemy.gameObject == cursedEnemy) continue;

                var e = enemy.gameObject.GetComponent<Enemy>();
                if (e == null || e.Health <= 0) continue;

                float distance = Vector3.Distance(cursedEnemy.transform.position, e.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestEnemy = e.transform;
                }
            }

            return nearestEnemy;
        }
    }
}

