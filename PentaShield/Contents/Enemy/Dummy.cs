using System.Collections.Generic;
using System.Collections;
using UnityEngine;

namespace chaos
{
    /// <summary>
    /// 원거리 공격 적 (Projectile 사용)
    /// - 거리 기반 행동 전환 (추적 ↔ 공격)
    /// - 프로젝타일 풀링 시스템
    /// - Curse 상태 시 아군 공격
    /// </summary>
    public class Dummy : Enemy
    {
        #region Fields
        [Header("ATTACK")]
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private float attackRange = 10f;
        [SerializeField] private float attackCooldown = 2f;
        [SerializeField] private Transform shootPoint;
        [SerializeField] private float damage = 5f;

        [Header("REWARD")]
        [SerializeField] private float maxHealth = 10f;
        [SerializeField] private int coinValue = 10;
        [SerializeField] private int scoreValue = 10;
        [SerializeField] private int expValue = 10;

        private const int POOL_SIZE = 5;
        private GameObject projParent;
        private List<GameObject> projectilePool;

        private float cooldownTimer = 0f;
        private bool canAttack = true;
        private bool isInAttackRange = false;
        #endregion

        protected override void Start()
        {
            base.Start();
            MaxHealth = maxHealth;
            Health = maxHealth;
            ExperienceValue = expValue;
            CoinValue = coinValue;
            ScoreValue = scoreValue;
            Damage = damage;
            InitHealthSlider(MaxHealth);

            InitializeProjectilePool();
        }

        #region Projectile Pool
        /// <summary> 프로젝타일 풀링 초기화 </summary>
        private void InitializeProjectilePool()
        {
            projectilePool = new List<GameObject>();
            projParent = new GameObject("ProjParent");

            for (int i = 0; i < POOL_SIZE; i++)
            {
                if (projectilePrefab == null) continue;
                GameObject projectile = Instantiate(projectilePrefab, projParent.transform);
                projectile.SetActive(false);
                projectilePool.Add(projectile);
            }
        }

        /// <summary> 풀에서 비활성 프로젝타일 가져오기 </summary>
        private GameObject GetProjectileFromPool()
        {
            foreach (var projectile in projectilePool)
            {
                if (projectile != null && !projectile.activeInHierarchy)
                    return projectile;
            }

            // 풀이 부족하면 새로 생성
            if (projectilePrefab != null)
            {
                GameObject newProj = Instantiate(projectilePrefab, projParent.transform);
                newProj.SetActive(false);
                projectilePool.Add(newProj);
                return newProj;
            }

            return null;
        }
        #endregion

        #region Combat Logic
        protected override void Update()
        {
            // 쿨다운 타이머 감소
            if (!canAttack)
            {
                cooldownTimer -= Time.deltaTime;
                if (cooldownTimer <= 0) canAttack = true;
            }

            if (IsPetrified || IsKnockedBack) return;

            // 타겟 유효성 체크
            if (targetTrans == null || !targetTrans.gameObject.activeInHierarchy)
                targetTrans = FindTarget();

            if (targetTrans == null)
            {
                isInAttackRange = false;
                return;
            }

            // 거리 기반 행동 결정
            float distance = Vector3.Distance(transform.position, targetTrans.position);
            isInAttackRange = distance <= attackRange;

            if (!isInAttackRange)
            {
                // 범위 밖: 추적
                Look();
                Chase();
            }
            else
            {
                // 범위 안: 멈추고 공격
                Look();
                if (canAttack && CanPerformActions)
                    OnAttack();
            }
        }

        /// <summary> 공격 시작 </summary>
        private void OnAttack()
        {
            if (projectilePrefab == null || targetTrans == null) return;

            if (animator != null && HasAnimationClip(PentaConst.tAttack))
            {
                animator.SetTrigger(PentaConst.tAttack);
            }
            else
            {
                OnCreateProj();
            }

            canAttack = false;
            cooldownTimer = attackCooldown;
        }

        /// <summary> 
        /// 프로젝타일 생성 및 발사 (Animation Event에서 호출)
        /// Curse 상태 시 아군(Player/Guard)을 타겟으로 전환
        /// </summary>
        public void OnCreateProj()
        {
            GameObject projectile = GetProjectileFromPool();
            if (projectile == null || targetTrans == null)
            {
                $"[Dummy] {gameObject.name} OnCreateProj - projectile 또는 targetTrans가 null입니다!".DError();
                return;
            }

            SetupProjectileTransform(projectile);
            
            Projectile projectileComponent = projectile.GetComponent<Projectile>();
            if (projectileComponent != null)
            {
                ConfigureProjectile(projectileComponent);
                projectile.SetActive(true);
            }
        }
        private void SetupProjectileTransform(GameObject projectile)
        {
            projectile.transform.position = shootPoint.position;
            Vector3 direction = (targetTrans.position - shootPoint.position).normalized;
            projectile.transform.rotation = Quaternion.LookRotation(direction);
        }
        private void SetProjectileLayers(Projectile projectileComponent)
        {
            if (IsCursed)
                SetCursedProjectileLayers(projectileComponent);
            else
                SetNormalProjectileLayers(projectileComponent);
        }
        public override void OnHit(float damage)
        {
            if (damage <= 0) return;
            base.OnHit(damage);

            if (!IsTakingDotDamage)
                ToAni(PentaConst.tHit);

            UpdateSlider();
        }
        #endregion

        protected override void OnDestroy()
        {
            if (projParent != null) Destroy(projParent);
            base.OnDestroy();
        }
    }
}
