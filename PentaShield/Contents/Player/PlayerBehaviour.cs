using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace penta
{
    /// <summary>
    /// 플레이어 무기 및 공격 시스템 관리
    /// - 발사체 풀링 및 발사 로직
    /// - 무기 테이블 관리 및 강화
    /// - 유도탄 시스템 관리
    /// - 자동 타겟팅
    /// </summary>
    public class PlayerBehaviour : MonoBehaviourSingleton<PlayerBehaviour>
    {
        #region Constants
        private const float DEFAULT_ATTACK_RATE = 0.5f;
        private const int DEFAULT_DAMAGE = 10;
        private const int DEFAULT_PER_SHOT = 1;
        private const float DEFAULT_SPREAD_ANGLE = 10f;
        private const float MUZZLE_FLASH_ROTATION_X = 180f;
        #endregion

        #region Fields
        [Header("PROJECTILE")]
        [SerializeField] private GameObject projectilePrefab;
        private GameObject projParent;
        [SerializeField] private Transform leftFirePoint;
        [SerializeField] private Transform rightFirePoint;
        [SerializeField] private float attackRate = DEFAULT_ATTACK_RATE;
        [SerializeField] private int damage = DEFAULT_DAMAGE;

        [Header("ATTACK PATTERN")]
        [SerializeField] private int perShot = DEFAULT_PER_SHOT;
        [SerializeField] private float spreadAngle = DEFAULT_SPREAD_ANGLE;

        [Header("POOLING")]
        [SerializeField] private int poolSize = 20;
        [SerializeField] private bool expandPool = true;

        [Header("EFFECTS")]
        [SerializeField] private GameObject muzzleFlashEffect;

        [Header("HOMING MISSILE")]
        private int homingMissileCount = 0;

        [Header("AUTO TARGETING")]
        [SerializeField] private float targetSearchRadius = 15f; 

        private List<GameObject> missilePool;
        private bool useLeftFirePoint = true;
        #endregion

        public PlayerWeaponTable playerTable;
        public List<Elemental> ElementalList { get; private set; } = null;
        private protected override bool DontDestroy => false;
        protected override void Awake()
        {
            base.Awake();
            ElementalList = new List<Elemental>();
        }

        private void Start()
        {
            perShot = DEFAULT_PER_SHOT;
            leftFirePoint ??= transform;
            rightFirePoint ??= transform;

            LoadWeaponTable();
            InitializeProjectilePool();
        }

        private void LoadWeaponTable()
        {
            var sheetData = SheetManager.GetSheetObject();
            playerTable = sheetData?.PlayerWeaponTableList?.FirstOrDefault();
            if (playerTable == null)
            {
                "[PlayerBehaviour] PlayerWeaponTable을 찾을 수 없습니다.".DWarning();
            }
        }

        protected override void OnDestroy()
        {
            Destroy(projParent);
            base.OnDestroy();
        }

        #region Pool
        private void InitializeProjectilePool()
        {
            projParent = new GameObject("ProjParent");

            missilePool = new List<GameObject>();
            for (int i = 0; i < poolSize; i++)
                CreateProjectileForPool();
        }

        private GameObject CreateProjectileForPool()
        {
            GameObject missile = Instantiate(projectilePrefab, projParent.transform);
            missile.SetActive(false);
            missilePool.Add(missile);
            return missile;
        }

        private GameObject GetProjectileFromPool()
        {
            if (missilePool == null) return null;

            foreach (var missile in missilePool)
            {
                if (missile != null && !missile.activeInHierarchy)
                    return missile;
            }

            return expandPool ? CreateProjectileForPool() : null;
        }

        #endregion

        /// <summary> 버튼 클릭 시 호출되는 공격 메서드 </summary>
        public void Attack()
        {
            AudioHelper.PlaySFX(AudioConst.PLAYER_ATTACK, 0.5f);
            
            var playerController = PlayerController.Shared;
            if (playerController == null) return;

            GameObject nearestEnemy = FindNearestEnemy();
            if (nearestEnemy != null)
            {
                RotateTowardsEnemy(nearestEnemy);
            }
            
            playerController.SetAttacking(true);
            int attackHash = useLeftFirePoint ? PentaConst.tLeftAttack : PentaConst.tRightAttack;
            playerController.TriggerAttackAnimation(attackHash);
        }

      
        private GameObject FindNearestEnemy()
        {
            int enemyLayer = LayerMask.GetMask("Enemy");
            Collider[] enemyColliders = Physics.OverlapSphere(transform.position, targetSearchRadius, enemyLayer);
            
            if (enemyColliders == null || enemyColliders.Length == 0)
                return null;
            
            GameObject nearestEnemy = null;
            float nearestDistance = float.MaxValue;

            foreach (var col in enemyColliders)
            {
                if (col?.gameObject.activeInHierarchy != true)
                    continue;
                
                float distance = Vector3.Distance(transform.position, col.transform.position);
                
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestEnemy = col.gameObject;
                }
            }
            
            return nearestEnemy;
        }

        private void RotateTowardsEnemy(GameObject enemy)
        {
            if (enemy == null) return;
        
            Vector3 directionToEnemy = enemy.transform.position - transform.position;
            PlayerController.Shared?.RotateToDirection(directionToEnemy);
        }


        public void OnAttackAnimationEvent()
        {
            Transform firePoint = GetCurrentFirePoint();
            if (firePoint == null) return;

            FireProjectiles(firePoint);
            SpawnMuzzleFlash(firePoint);
            ToggleFirePoint();
        }

        private Transform GetCurrentFirePoint() => useLeftFirePoint ? leftFirePoint : rightFirePoint;

        private void FireProjectiles(Transform firePoint)
        {
            float centerIndex = (perShot - 1) / 2f;

            for (int i = 0; i < perShot; i++)
            {
                GameObject projectile = GetProjectileFromPool();
                if (projectile == null) return;

                SetupProjectile(projectile, firePoint, i, centerIndex);
            }
        }

        private void SetupProjectile(GameObject projectile, Transform firePoint, int index, float centerIndex)
        {
            projectile.transform.position = firePoint.position;

            float angleOffset = perShot > 1 ? (index - centerIndex) * spreadAngle : 0f;
            Quaternion spreadRotation = firePoint.rotation * Quaternion.Euler(0f, angleOffset, 0f);
            projectile.transform.rotation = spreadRotation;

            var projectileComponent = projectile.GetComponent<Projectile>();
            if (projectileComponent == null) return;

            ConfigureProjectile(projectileComponent, index);
            SetProjectileDirection(projectileComponent);
            projectile.SetActive(true);
        }

        private void ConfigureProjectile(Projectile projectile, int index)
        {
            projectile.SetShooter(gameObject);
            projectile.Damage = damage;
            projectile.SetProjectileType(Projectile.ProjectileType.Normal);

            bool isHoming = IsHomingMissileIndex(index, perShot, homingMissileCount);
            projectile.SetMovementType(isHoming ? Projectile.MovementType.Guided : Projectile.MovementType.Straight);
        }

        private void SetProjectileDirection(Projectile projectile)
        {
            var playerController = GetComponentInParent<PlayerController>();
            if (playerController?.transform.parent != null)
            {
                projectile.SetDirection(playerController.transform.parent.forward);
            }
        }

        private void SpawnMuzzleFlash(Transform firePoint)
        {
            if (muzzleFlashEffect == null || firePoint == null) return;

            GameObject effect = Instantiate(muzzleFlashEffect, firePoint.position, firePoint.rotation);
            effect.transform.SetParent(firePoint);
            effect.transform.localPosition = Vector3.zero;
            effect.transform.localRotation = Quaternion.Euler(MUZZLE_FLASH_ROTATION_X, 0f, 0f);
        }

        private void ToggleFirePoint() => useLeftFirePoint = !useLeftFirePoint;

        public void Enhance(PlayerEnhancementType enhancementType, float value, int coinValue)
        {
            switch (enhancementType)
            {
                case PlayerEnhancementType.Damage:
                    SetDamageValue((int)value);
                    break;
                case PlayerEnhancementType.ProjCount:
                    SetProjCountValue((int)value);
                    break;
                case PlayerEnhancementType.CoolTime:
                    SetAttackRateValue(value);
                    break;
                case PlayerEnhancementType.Intersection:
                    SetIntersection(value);
                    break;
                case PlayerEnhancementType.Heal:
                    DoHealPlayer(value > 0 ? (int)value : 0);
                    break;
                default:
                    Debug.LogWarning($"Unknown enhancement type: {enhancementType}");
                    break;
            }
        }
        public void SetDamageValue(int damageValue)
        {
            damage = damageValue;
        }

        public void SetProjCountValue(int projCount)
        {
            perShot = projCount;
        }

        public void SetAttackRateValue(float attackRate)
        {
            this.attackRate = attackRate;
        }
        public void SetIntersection(float area)
        {
            var projectile = projectilePrefab?.GetComponent<Projectile>();
            if (projectile != null)
            {
                projectile.detectionRadius = area;
            }
        }

        /// <summary> 유도탄 개수 설정 </summary>
        public void SetHomingMissileCount(int count)
        {
            int maxHoming = Mathf.Max(0, perShot - 1);
            homingMissileCount = Mathf.Clamp(count, 0, maxHoming);
            $"Homing missile count set to {homingMissileCount} (max: {maxHoming}, total projectiles: {perShot})".DLog();
        }

        /// <summary> 유도탄 1개 추가 </summary>
        public bool AddHomingMissile()
        {
            int maxHoming = perShot - 1;
            if (homingMissileCount < maxHoming)
            {
                homingMissileCount++;
                $"Homing missile added: {homingMissileCount}/{maxHoming}".DLog();
                return true;
            }
            else
            {
                $"Cannot add more homing missiles: already at max ({maxHoming})".DWarning();
                return false;
            }
        }

        /// <summary> 현재 유도탄 개수 반환 </summary>
        public int GetHomingMissileCount()
        {
            return homingMissileCount;
        }

        /// <summary> i번째 발사체가 유도탄인지 확인 중앙에 가까운 homingCount개를 유도탄으로 할당 </summary>
        private bool IsHomingMissileIndex(int index, int totalCount, int homingCount)
        {
            if (homingCount <= 0 || totalCount <= 1) return false;

            int startIndex = Mathf.FloorToInt((totalCount - homingCount) / 2f);
            int endIndex = startIndex + homingCount - 1;

            return index >= startIndex && index <= endIndex;
        }

        /// <summary> 플레이어 체력 회복 </summary>
        public void DoHealPlayer(int amount)
        {
            var playerController = PlayerController.Shared;
            if (playerController == null) return;

            playerController.CurHeath += amount;
            playerController.CurHeath = Mathf.Min(playerController.CurMaxHeath, playerController.CurHeath);
            playerController.healthSlider?.SetCurrentHealth(playerController.CurHeath);
        }
    }
}