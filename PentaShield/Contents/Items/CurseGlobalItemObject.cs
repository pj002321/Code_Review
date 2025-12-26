using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace chaos
{
    /// <summary>
    /// 커스 글로벌 아이템 오브젝트 (주요 로직)
    /// - 범위 내 적에게 저주 적용
    /// - 저주받은 적들이 서로 공격
    /// - 주기적 저주 데미지 적용 (최대 체력의 20%)
    /// </summary>
    public class CurseGlobalItemObject : MonoBehaviour
    {
        [Header("CURSE SETTINGS")]
        [SerializeField] private float damagePercent = 0.2f;
        [SerializeField] private float explosionRadius = 5f;
        [SerializeField] private float lifeTime = 10f;

        private List<Enemy> cursedEnemies = new List<Enemy>();
        private bool isDestroyed = false;
        private Coroutine multiCurseCoroutine;

        private void Start()
        {
            ApplyCurseToNearbyEnemies();
            multiCurseCoroutine = StartCoroutine(Co_ManageCurseEffect());
            StartCoroutine(DestroyAfterLifetime());
        }

        /// <summary> 범위 내 적에게 저주 적용 </summary>
        private void ApplyCurseToNearbyEnemies()
        {
            Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, explosionRadius);

            foreach (var collider in nearbyColliders)
            {
                Enemy enemy = collider.GetComponent<Enemy>();
                if (enemy != null)
                {
                    enemy.IsCursed = true;
                    cursedEnemies.Add(enemy);
                    SetEnemyToTargetOtherEnemies(enemy);
                }
            }
        }

        /// <summary> 저주받은 적의 타겟을 다른 적으로 변경 </summary>
        private void SetEnemyToTargetOtherEnemies(Enemy cursedEnemy)
        {
            Collider[] allEnemyColliders = Physics.OverlapSphere(transform.position, explosionRadius * 2f);
            List<Enemy> potentialTargets = new List<Enemy>();
            
            foreach (var collider in allEnemyColliders)
            {
                Enemy otherEnemy = collider.GetComponent<Enemy>();
                if (otherEnemy != null && otherEnemy != cursedEnemy)
                {
                    potentialTargets.Add(otherEnemy);
                }
            }
            
            if (potentialTargets.Count > 0)
            {
                Enemy randomTarget = potentialTargets[Random.Range(0, potentialTargets.Count)];
                cursedEnemy.targetTrans = randomTarget.transform;
            }
        }

        /// <summary> 저주 효과 관리 - 주기적으로 적 상태 체크 및 데미지 적용 </summary>
        private IEnumerator Co_ManageCurseEffect()
        {
            float curseTickInterval = 1f;
            float curseDamageInterval = 2f;
            float lastDamageTime = 0f;

            while (!isDestroyed)
            {
                yield return new WaitForSeconds(curseTickInterval);

                CleanupDeadEnemies();
                ApplyCurseToNewEnemies();
                ReassignTargetsForCursedEnemies();

                if (Time.time - lastDamageTime >= curseDamageInterval)
                {
                    ApplyCurseDamage();
                    lastDamageTime = Time.time;
                }
            }
        }

        /// <summary> 범위 내 새로운 적들에게 저주 적용 </summary>
        private void ApplyCurseToNewEnemies()
        {
            Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, explosionRadius);

            foreach (var collider in nearbyColliders)
            {
                Enemy enemy = collider.GetComponent<Enemy>();
                if (enemy != null && !cursedEnemies.Contains(enemy))
                {
                    enemy.IsCursed = true;
                    cursedEnemies.Add(enemy);
                    SetEnemyToTargetOtherEnemies(enemy);
                }
            }
        }

        /// <summary> 저주받은 적들의 타겟 재설정 </summary>
        private void ReassignTargetsForCursedEnemies()
        {
            foreach (Enemy cursedEnemy in cursedEnemies)
            {
                if (cursedEnemy != null)
                {
                    if (cursedEnemy.targetTrans == null || !cursedEnemy.targetTrans.gameObject.activeInHierarchy)
                    {
                        SetEnemyToTargetOtherEnemies(cursedEnemy);
                    }
                }
            }
        }

        /// <summary> 저주받은 적들에게 주기적으로 데미지 적용 </summary>
        private void ApplyCurseDamage()
        {
            foreach (Enemy cursedEnemy in cursedEnemies)
            {
                if (cursedEnemy != null)
                {
                    float damage = cursedEnemy.MaxHealth * damagePercent;
                    cursedEnemy.OnHit(damage, "Curse");
                }
            }
        }

        /// <summary> 죽은 적들 정리 </summary>
        private void CleanupDeadEnemies()
        {
            for (int i = cursedEnemies.Count - 1; i >= 0; i--)
            {
                if (cursedEnemies[i] == null)
                {
                    cursedEnemies.RemoveAt(i);
                }
            }
        }

        private IEnumerator DestroyAfterLifetime()
        {
            yield return new WaitForSeconds(lifeTime);
            
            if (!isDestroyed)
            {
                isDestroyed = true;
                if (multiCurseCoroutine != null)
                {
                    StopCoroutine(multiCurseCoroutine);
                }
                RestoreEnemyTargets();
                Destroy(gameObject);
            }
        }

        private void RestoreEnemyTargets()
        {
            foreach (Enemy enemy in cursedEnemies)
            {
                if (enemy != null)
                {
                    enemy.IsCursed = false;
                    enemy.targetTrans = enemy.FindTarget();
                }
            }
            cursedEnemies.Clear();
        }

        private void OnDestroy()
        {
            if (multiCurseCoroutine != null)
            {
                StopCoroutine(multiCurseCoroutine);
            }
            
            if (!isDestroyed)
            {
                RestoreEnemyTargets();
            }
        }
    }
}
