using UnityEngine;

namespace penta
{
    /// <summary>
    /// Water 엘리멘탈 공격 로직
    /// </summary>
    public partial class Water
    {
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
                projectileComponent.enemyLayer = enemyLayer;
                projectileComponent.Damage = damage;
                projectileComponent.ResetProjectile();
                projectileComponent.SetProjectileType(Projectile.ProjectileType.Normal);
                projectileComponent.SetOptimizedMode(true);
                projectileComponent.SetLifetime(projectileLifetime);
                projectileComponent.SetParent(this);
            }
        }
    }
}