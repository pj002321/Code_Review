using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace penta
{
    /// <summary>
    /// 엘리멘탈 기본 클래스 - 공격, 궤도, 레벨 관리
    /// </summary>
    public class Elemental : MonoBehaviour, IElemental
    {
        private static Dictionary<string, Material> elementalMaterials = new Dictionary<string, Material>();
        public static Dictionary<string, GameObject> elementalProjs = new Dictionary<string, GameObject>();

        public const int MAX_ELEMENTAL_COUNT = 5;

        private Transform guardTarget;
        private float orbitAngle = 0f;
        private float currentOrbitRadius = 0f;

        [Header("ORBIT")]
        [SerializeField] private float orbitDistance = 3f;
        [SerializeField] private float orbitSpeed = 120f;
        [SerializeField] private float transitionSpeed = 2f;

        [Header("PROJECTILE")]
        [SerializeField] protected GameObject projectilePrefab;
        [SerializeField] protected Transform firePoint;
        [SerializeField] protected float fireRate = 0.5f;
        [SerializeField] protected int projectilePerShot = 1;
        [SerializeField] protected float spreadAngle = 10f;
        [SerializeField] protected int damage = 10;
        [SerializeField] protected float projectileLifetime = 5f;

        [Header("PERFORMANCE")]
        [SerializeField] protected float maxFireDistance = 30f;
        [SerializeField] protected bool onlyFireWhenEnemiesNearby = true; // Only fire when enemies are detected
        [SerializeField] protected float enemyCheckInterval = 0.5f; // How often to check for enemies
        [SerializeField] protected int maxActiveProjectiles = 2; // Limit maximum projectiles per elemental

        [Header("TARGETTING")]
        [SerializeField] protected LayerMask enemyLayer;
        [SerializeField] protected float detectionRadius = 20f;

        [Header("ENHANCE")]
        [SerializeField] protected int level = 1;
        public int Level
        {
            get { return level; }
            set
            {
                if (value != level)
                {
                    level = value;
                    _ = OnLevelUp();
                }
            }
        }
        [SerializeField] protected float damageEnhancement = 0;
        [SerializeField] protected float fireRateEnhancement = 0;
        [SerializeField] protected float curseTimeEnhancement = 0f;

        protected Dictionary<int, GameObject> projectilesByLevel = new Dictionary<int, GameObject>();

        private Coroutine fireCoroutine;
        private Coroutine enemyCheckCoroutine;
        public bool enemiesNearby = false;
        public List<GameObject> activeProjectiles = new List<GameObject>();
        private bool isDestroying = false;

        private Vector3 guardOffset = Vector3.zero;

        public int Stat { get; set; }
        public string Name { get; set; } = string.Empty;

        private void Awake()
        {
            enemyLayer = LayerMask.GetMask("Enemy");
            orbitAngle = Random.Range(0f, 360f);
        }

        protected virtual void Start()
        {
            guardTarget = FindAnyObjectByType<Guard>()?.transform;
            if (guardTarget != null)
            {
                guardOffset = new Vector3(0f, guardTarget.position.y + 1.5f, 0f);
            }

            if (firePoint == null)
            {
                firePoint = transform;
            }

            if (onlyFireWhenEnemiesNearby)
            {
                enemyCheckCoroutine = StartCoroutine(CO_CheckForEnemiesRoutine());
            }

            fireCoroutine = StartCoroutine(CO_ContinuousFireCoroutine());
        }

        protected virtual void Update()
        {
            if (guardTarget != null)
                AroundTarget(guardTarget, orbitDistance, orbitSpeed, transitionSpeed, ref orbitAngle, ref currentOrbitRadius);

            // Clean up inactive projectiles from list
            ManageActiveProjectiles();
        }


        // Remove inactive projectiles from tracking list
        private void ManageActiveProjectiles()
        {
            if (isDestroying) return;

            for (int i = activeProjectiles.Count - 1; i >= 0; i--)
            {
                if (activeProjectiles[i] == null || !activeProjectiles[i].activeInHierarchy)
                {
                    activeProjectiles.RemoveAt(i);
                }
            }
        }

        // Periodic enemy detection coroutine
        private IEnumerator CO_CheckForEnemiesRoutine()
        {
            WaitForSeconds wait = new WaitForSeconds(enemyCheckInterval);

            while (!isDestroying)
            {
                yield return wait;

                // Check for enemies at intervals instead of every frame
                Collider[] enemies = Physics.OverlapSphere(transform.position, detectionRadius, enemyLayer);
                enemiesNearby = enemies.Length > 0;
            }
        }

        /// <summary>
        /// 가장 가까운 적을 찾는 메서드
        /// </summary>
        private Transform FindClosestEnemy()
        {
            Collider[] enemies = Physics.OverlapSphere(transform.position, detectionRadius, enemyLayer);

            if (enemies.Length == 0)
                return null;

            Transform closestEnemy = null;
            float closestDistance = float.MaxValue;

            foreach (var enemyCollider in enemies)
            {
                if (enemyCollider == null || !enemyCollider.gameObject.activeInHierarchy)
                    continue;

                // 적의 HP 체크
                Enemy enemy = enemyCollider.GetComponent<Enemy>();
                if (enemy != null && enemy.Health <= 0)
                    continue;

                float distance = Vector3.Distance(transform.position, enemyCollider.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestEnemy = enemyCollider.transform;
                }
            }

            return closestEnemy;
        }

        public void AroundTarget(Transform guardTarget, float orbitDistance, float orbitSpeed, float transitionSpeed, ref float orbitAngle, ref float currentOrbitRadius)
        {
            if (guardTarget == null) return;

            currentOrbitRadius = Mathf.Lerp(currentOrbitRadius, orbitDistance, transitionSpeed * Time.deltaTime);
            orbitAngle += orbitSpeed * Time.deltaTime;

            Vector3 offset = new Vector3(
                Mathf.Sin(orbitAngle * Mathf.Deg2Rad),
                0f,
                Mathf.Cos(orbitAngle * Mathf.Deg2Rad)
            );

            transform.position = guardTarget.position + offset * currentOrbitRadius;

            Vector3 lookDirection = guardTarget.position - transform.position;
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, transitionSpeed * 2f * Time.deltaTime);

            transform.position += guardOffset;

        }

        private IEnumerator CO_ContinuousFireCoroutine()
        {
            WaitForSeconds minWaitTime = new WaitForSeconds(0.1f); // Minimum wait time

            while (!isDestroying)
            {
                float currentFireRate = GetCurrentFireRate();
                float waitTime = 1f / currentFireRate;

                yield return new WaitForSeconds(waitTime);

                // Only fire if conditions are met (enemies nearby and under projectile limit)
                if (enemiesNearby && activeProjectiles.Count < maxActiveProjectiles)
                {
                    OnAttack();
                }
                else
                {
                    // Minimum wait to avoid excessive checking
                    yield return minWaitTime;
                }
            }
        }

        protected virtual void OnAttack()
        {
            // Skip firing if conditions not met
            if (!enemiesNearby || activeProjectiles.Count >= maxActiveProjectiles)
                return;

            // 가장 가까운 적을 찾기
            Transform closestEnemy = FindClosestEnemy();
            if (closestEnemy == null)
                return;

            for (int i = 0; i < projectilePerShot; i++)
            {
                // Check projectile limit before each shot
                if (activeProjectiles.Count >= maxActiveProjectiles)
                    break;

                // Create projectile
                GameObject projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
                activeProjectiles.Add(projectile);

                // 적을 향하는 방향 계산
                Vector3 directionToEnemy = (closestEnemy.position - firePoint.position).normalized;
                Quaternion targetRotation = Quaternion.LookRotation(directionToEnemy);

                // Set projectile direction with spread
                float angleOffset = 0f;
                if (projectilePerShot > 1)
                {
                    angleOffset = (i - (projectilePerShot - 1) / 2f) * spreadAngle;
                }

                Quaternion spreadRotation = targetRotation * Quaternion.Euler(0f, angleOffset, 0f);
                projectile.transform.rotation = spreadRotation;

                // Configure projectile properties
                Projectile projectileComponent = projectile.GetComponent<Projectile>();
                if (projectileComponent != null)
                {
                    projectileComponent.enemyLayer = enemyLayer;
                    projectileComponent.Damage = GetCurrentDamage();

                    // Enable optimization mode for better performance
                    projectileComponent.SetOptimizedMode(true);
                    projectileComponent.SetLifetime(projectileLifetime);
                    projectileComponent.SetParent(this);
                }
            }
        }

        /// <summary>
        /// 레벨업 시 프로젝타일 프리팹 로드
        /// </summary>
        protected virtual async UniTask OnLevelUp()
        {
            string elementalName = GetType().Name;
            string resourceKey = $"{elementalName}proj@lv{Level}";
            GameObject projPrefab = await AbHelper.Shared.LoadAssetAsync<GameObject>(resourceKey);
            
            if (projPrefab != null)
            {
                projectilePrefab = projPrefab;
            }
        }

        /// <summary>
        /// 현재 레벨에 맞는 프로젝타일 프리팹 반환
        /// </summary>
        protected GameObject GetProjectileForCurrentLevel()
        {
            if (projectilesByLevel.ContainsKey(level))
            {
                return projectilesByLevel[level];
            }

            GameObject fallbackProjectile = null;
            int maxLevel = 0;

            foreach (var kvp in projectilesByLevel)
            {
                if (kvp.Key > maxLevel && kvp.Key <= level)
                {
                    maxLevel = kvp.Key;
                    fallbackProjectile = kvp.Value;
                }
            }

            return fallbackProjectile ?? projectilePrefab;
        }

        // Callback for projectiles to notify when destroyed
        public void NotifyProjectileDestroyed(GameObject projectile)
        {
            if (activeProjectiles.Contains(projectile))
            {
                activeProjectiles.Remove(projectile);
            }
        }

        protected virtual void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
        }

        protected virtual ElementalType GetElementalType()
        {
            return ElementalType.Unknown;
        }

        /// <summary>
        /// 데미지 강화
        /// </summary>
        public virtual void EnhanceDamage(int enhancementValue)
        {
            damageEnhancement = enhancementValue;
        }

        /// <summary>
        /// 공격 속도 강화
        /// </summary>
        public virtual async UniTask EnhanceFireRate(float rateValue)
        {
            fireRateEnhancement = rateValue;

            if (fireCoroutine != null)
            {
                StopCoroutine(fireCoroutine);
            }

            if (!gameObject.activeSelf)
            {
                await UniTask.WaitUntil(() => gameObject.activeSelf);
            }

            fireCoroutine = StartCoroutine(CO_ContinuousFireCoroutine());
        }

        /// <summary>
        /// Curse 지속시간 강화
        /// </summary>
        public virtual void EnhanceCurseTime(float cursetimeValue)
        {
            if (GetElementalType() == ElementalType.Curse)
            {
                curseTimeEnhancement = cursetimeValue;
            }
        }

        private void EnhanceAttackRate(float rateValue) { }
        private void EnhanceNuckBackValue(float nuckbackValue) { }
        private void EnhanceDotDamage(float dotValue) { }
        public virtual void Enhance(ElementalEnhancementType enhancementType, float value)
        {
            switch (enhancementType)
            {
                case ElementalEnhancementType.Damage:
                    EnhanceDamage((int)value);
                    break;
                case ElementalEnhancementType.AttackRate:
                    _ = EnhanceFireRate(value);
                    break;
                case ElementalEnhancementType.CurseTime:
                    EnhanceCurseTime(value);
                    break;
                case ElementalEnhancementType.NuckBack:
                    EnhanceNuckBackValue(value);
                    break;
                case ElementalEnhancementType.AreaDamage:
                    EnhanceAttackRate(value);
                    break;
                case ElementalEnhancementType.DotDamage:
                    EnhanceDotDamage(value);
                    break;
                case ElementalEnhancementType.Unkown:
                    break;

            }
        }

        protected virtual float GetCurrentDamage()
        {
            return damage + damageEnhancement;
        }

        protected virtual float GetCurrentFireRate()
        {
            return fireRate + fireRateEnhancement;
        }

        protected virtual float GetCurrentCurseTime()
        {
            return 2.0f + curseTimeEnhancement;
        }


        protected virtual void OnDestroy()
        {
            isDestroying = true;

            if (fireCoroutine != null)
            {
                StopCoroutine(fireCoroutine);
            }

            if (enemyCheckCoroutine != null)
            {
                StopCoroutine(enemyCheckCoroutine);
            }

            foreach (GameObject projectile in activeProjectiles)
            {
                if (projectile != null)
                {
                    Destroy(projectile);
                }
            }

            activeProjectiles.Clear();
        }



    }
}
