using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace penta
{
    /// <summary>
    /// Flame 엘리멘탈 공격 로직 및 DoT 효과
    /// </summary>
    public partial class Flame
    {
        [Header("FLAME SETTING")]
        [SerializeField] private float dotDamage = 5f;
        [SerializeField] private float dotDuration = 3f;
        [SerializeField] private float dotInterval = 0.5f;
        [SerializeField] private GameObject dotEffectPrefab;

        private Dictionary<GameObject, Coroutine> activeDoTs = new Dictionary<GameObject, Coroutine>();

        private void OnUpgradedAttack()
        {
            if (!CanExecuteAttack()) return;

            OnAttackFlame(angleStep: 360f / (3 + level),
                          count: 3 + level,
                          damage: GetCurrentDamage()
                          );
        }
        private bool CanExecuteAttack()
        {
            return enemiesNearby && activeProjectiles.Count < maxActiveProjectiles;
        }

        private void OnAttackFlame(int count, float angleStep, float damage, float offsetAngle = 0f)
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
                projectileComponent.SetProjectileType(Projectile.ProjectileType.Flame);
                projectileComponent.SetOptimizedMode(true);
                projectileComponent.SetLifetime(projectileLifetime);
                projectileComponent.SetParent(this);
                projectileComponent.SetFlameProperties(this, level >= 1);
            }
        }
        /// <summary>
        /// DoT 데미지 적용
        /// </summary>
        public void ApplyDotDamage(GameObject target, bool enhanced = false)
        {
            if (target == null) return;

            if (activeDoTs.ContainsKey(target))
            {
                if (activeDoTs[target] != null)
                {
                    StopCoroutine(activeDoTs[target]);
                }
                activeDoTs.Remove(target);
            }

            Coroutine dotCoroutine = StartCoroutine(CO_DotDamageCoroutine(target, enhanced));
            activeDoTs.Add(target, dotCoroutine);
        }

        /// <summary>
        /// DoT 데미지 코루틴
        /// </summary>
        private IEnumerator CO_DotDamageCoroutine(GameObject target, bool enhanced = false)
        {
            if (target == null)
            {
                yield break;
            }

            IDamageable targetHealth = target.GetComponent<IDamageable>();
            if (targetHealth == null)
            {
                yield break;
            }

            // Enemy 컴포넌트가 있으면 도트 데미지 상태 설정
            Enemy enemy = target.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.IsTakingDotDamage = true;
            }

            float currentDotDamage = enhanced ? dotDamage * 1.5f : dotDamage;
            float currentDotDuration = enhanced ? dotDuration * 1.3f : dotDuration;
            float currentDotInterval = enhanced ? dotInterval * 0.8f : dotInterval;

            float elapsedTime = 0f;
            int tickCount = 0;

            while (elapsedTime < currentDotDuration && target != null && targetHealth != null)
            {
                targetHealth.OnHit(currentDotDamage);
                tickCount++;

                CreateDotEffect(target.transform.position, enhanced);

                yield return new WaitForSeconds(currentDotInterval);
                elapsedTime += currentDotInterval;
            }

            // 도트 데미지 종료 시 상태 해제
            if (enemy != null)
            {
                enemy.IsTakingDotDamage = false;
            }

            if (activeDoTs.ContainsKey(target))
            {
                activeDoTs.Remove(target);
            }
        }

        /// <summary>
        /// DoT 이펙트 생성
        /// </summary>
        private void CreateDotEffect(Vector3 position, bool enhanced)
        {
            // SerializeField로 할당된 프리팹은 Addressable 키가 필요
            // 임시로 기존 방식 유지 (프리팹 직접 참조)
            var effect = Instantiate(dotEffectPrefab, position, Quaternion.identity);
            if (effect != null)
            {
                var ps = effect.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    float duration = ps.main.duration + ps.main.startLifetime.constantMax;
                    Destroy(effect, duration + 0.5f);
                }
                else
                {
                    Destroy(effect, 3f);
                }
            }
        }
        /// <summary>
        /// 특정 타겟의 DoT 제거
        /// </summary>
        public void RemoveDotFromTarget(GameObject target)
        {
            if (activeDoTs.ContainsKey(target))
            {
                if (activeDoTs[target] != null)
                {
                    StopCoroutine(activeDoTs[target]);
                }
                activeDoTs.Remove(target);
            }
        }

        protected override void OnDestroy()
        {
            foreach (var dotPair in activeDoTs)
            {
                if (dotPair.Value != null)
                {
                    StopCoroutine(dotPair.Value);
                }
            }
            activeDoTs.Clear();

            base.OnDestroy();
        }

        public int GetActiveDotCount()
        {
            return activeDoTs.Count;
        }
    }
}