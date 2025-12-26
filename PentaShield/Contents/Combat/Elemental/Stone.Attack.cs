using System.Collections;
using UnityEngine;

namespace penta
{
    /// <summary>
    /// Stone 엘리멘탈 공격 로직 - 넉백 및 스턴 효과
    /// </summary>
    public partial class Stone
    {
        [Header("STONE SETTING")]
        [SerializeField] private float knockbackForce = 10f;
        [SerializeField] private float knockbackDuration = 0.5f;
        private void OnUpgradedAttack()
        {
            if (!CanExecuteAttack()) return;

            OnAttackFromLevel(
                count: 1,
                angleStep: 360f / (3 + level),
                damage: GetCurrentDamage(), 0f);
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
                    -0.5f,
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
                projectileComponent.SetProjectileType(Projectile.ProjectileType.Stone);
                projectileComponent.SetOptimizedMode(true);
                projectileComponent.SetLifetime(projectileLifetime);
                projectileComponent.SetParent(this);
                projectileComponent.SetStoneProperties(this, knockbackForce, knockbackDuration);
            }
        }
        /// <summary>
        /// 넉백 효과 적용
        /// </summary>
        public IEnumerator CO_ApplyKnockback(GameObject target, Vector3 direction, float force, float duration = 0.5f)
        {
            if (target == null) yield break;

            Rigidbody targetRb = target.GetComponent<Rigidbody>();
            Enemy enemyComponent = target.GetComponent<Enemy>();

            if (targetRb == null) yield break;

            if (enemyComponent != null && enemyComponent.MaxHealth >= 100f)
            {
                yield return CO_ApplyStun(target, duration);
                yield break;
            }

            if (enemyComponent != null)
            {
                enemyComponent.StartKnockback();
            }

            bool originalGravity = targetRb.useGravity;
            float originalDrag = targetRb.drag;
            bool originalKinematic = targetRb.isKinematic;

            targetRb.isKinematic = false;
            targetRb.useGravity = true;
            targetRb.drag = 0.1f;

            targetRb.velocity = Vector3.zero;
            targetRb.angularVelocity = Vector3.zero;

            Vector3 knockbackDir = direction.normalized;
            const float launchAngle = 45f;
            
            Vector3 launchDirection = new Vector3(
                knockbackDir.x * Mathf.Cos(launchAngle * Mathf.Deg2Rad),
                Mathf.Sin(launchAngle * Mathf.Deg2Rad),
                knockbackDir.z * Mathf.Cos(launchAngle * Mathf.Deg2Rad)
            );

            float adjustedForce = force * 2f;
            targetRb.AddForce(launchDirection * adjustedForce, ForceMode.Impulse);

            yield return new WaitForSeconds(0.1f);

            float elapsedTime = 0f;
            bool hasLanded = false;
            const float maxWaitTime = 2.5f;
            
            while (elapsedTime < maxWaitTime && !hasLanded)
            {
                elapsedTime += Time.deltaTime;
                
                if (enemyComponent != null)
                {
                    hasLanded = enemyComponent.IsGrounded && !enemyComponent.IsKnockedBack;
                }
                else
                {
                    hasLanded = Mathf.Abs(targetRb.velocity.y) < 0.5f && targetRb.transform.position.y <= 2.0f;
                }
                
                yield return null;
            }

            if (targetRb != null)
            {
                if (hasLanded)
                {
                    targetRb.velocity = Vector3.zero;
                    targetRb.angularVelocity = Vector3.zero;
                }
                
                if (enemyComponent == null)
                {
                    targetRb.useGravity = originalGravity;
                    targetRb.drag = originalDrag;
                    targetRb.isKinematic = originalKinematic;
                }
            }
        }

        /// <summary>
        /// 스턴 효과 적용 (큰 적에게)
        /// </summary>
        private IEnumerator CO_ApplyStun(GameObject target, float duration)
        {
            if (target == null) yield break;

            Enemy enemyComponent = target.GetComponent<Enemy>();
            if (enemyComponent == null) yield break;

            enemyComponent.SetBehaviourStop();

            bool wasStunned = enemyComponent.IsPetrified;
            enemyComponent.IsPetrified = true;

            HitFlashEffect.TriggerFlash(target, duration, new Color(0.7f, 0.7f, 0.7f));

            yield return new WaitForSeconds(duration);

            if (enemyComponent != null)
            {
                enemyComponent.IsPetrified = wasStunned;
                if (!wasStunned)
                {
                    enemyComponent.ResumBehaviour();
                }
            }
        }
    }
}



