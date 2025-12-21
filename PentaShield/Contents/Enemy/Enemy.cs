using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public enum EnemyTarget { Player, Guard }

namespace chaos
{
    /// <summary>
    /// 적 AI 베이스 클래스 (주요 로직)
    /// - 타겟 탐색 및 추적
    /// - 데미지 처리 및 사망
    /// - 상태 이상 (Petrify, Knockback, HitStun, Curse)
    /// - 보상 스폰 (경험치, 코인, 스코어)
    /// </summary>
    public class Enemy : MonoBehaviour, IDamageable
    {
        #region Fields
        [Header("SPEED")]
        [SerializeField] private float speed = 2.0f;

        [Header("REWARD")]
        [SerializeField] private GameObject experienceOrbPrefab;
        [SerializeField] private GameObject coinOrbPrefab;
        [SerializeField] private Vector2 expOrbCount = new Vector2(1, 3);
        [SerializeField] private Vector2 coinOrbCount = new Vector2(1, 3);

        [Header("UI")]
        [SerializeField] protected Slider healthSlider;

        public Transform targetTrans { get; set; }
        public float Damage { get; set; } = 5f;
        public float Health { get; set; } = 10f;
        public float MaxHealth { get; set; } = 100f;
        public int ExperienceValue { get; set; } = 5;
        public int ScoreValue { get; set; } = 10;
        public int CoinValue { get; set; } = 10;

        public bool IsCursed { get; set; }
        public bool IsPetrified { get; set; }
        public bool IsKnockedBack { get; set; }
        public bool IsGrounded { get; set; } = true;
        public bool IsTakingDotDamage { get; set; }
        public bool CanPerformActions => IsGrounded && !IsKnockedBack && !IsPetrified;

        private float originSpeed;
        private Vector3 originalScale;
        private Rigidbody enemyRigidbody;
        protected Animator animator;
        private Coroutine hitStunCoroutine;
        private Coroutine petrifyCoroutine;
        private Coroutine curseCoroutine;
        #endregion

        #region Unity Lifecycle
        protected virtual void Start()
        {
            animator = GetComponent<Animator>();
            enemyRigidbody = GetComponent<Rigidbody>();
            originSpeed = speed;
            originalScale = transform.localScale;
            targetTrans = FindTarget();
        }

        protected virtual void Update()
        {
            if (IsPetrified || IsKnockedBack) return;
            
            if (targetTrans == null || !targetTrans.gameObject.activeInHierarchy)
                targetTrans = FindTarget();

            if (targetTrans != null)
            {
                Look();
                Chase();
            }
        }
        #endregion

        #region Movement & Targeting
        /// <summary> 타겟 탐색: Player 또는 Guard 랜덤 선택 </summary>
        public Transform FindTarget()
        {
            var targetTypes = (EnemyTarget[])Enum.GetValues(typeof(EnemyTarget));
            var selectedTarget = targetTypes[UnityEngine.Random.Range(0, targetTypes.Length)];

            GameObject target = selectedTarget switch
            {
                EnemyTarget.Player => PlayerController.Shared?.gameObject,
                EnemyTarget.Guard => Guard.Shared?.gameObject,
                _ => null
            };

            return target?.transform;
        }

        /// <summary> 타겟 바라보기 </summary>
        protected void Look()
        {
            if (targetTrans == null) return;
            transform.LookAt(targetTrans);
        }

        /// <summary> 타겟 추적 이동 </summary>
        protected void Chase()
        {
            if (speed <= 0) return;
            Vector3 moveDirection = transform.forward * speed * Time.deltaTime;
            transform.position += moveDirection;
        }
        #endregion

        #region Combat
        /// <summary> 데미지 처리 </summary>
        public virtual void OnHit(float damage)
        {
            OnHit(damage, "Normal");
        }

        public virtual void OnHit(float damage, string damageType)
        {
            if (damage <= 0) return;

            HitFlashEffect.TriggerFlash(gameObject, 0.15f, Color.white);

            if (!IsTakingDotDamage && (damageType == "Normal" || damageType == "Thunder"))
                StartHitStun(0.5f);

            Health -= damage;
            if (healthSlider != null)
                healthSlider.value = Health;

            if (Health <= 0)
                OnDie();
        }

        /// <summary> 사망 처리 및 보상 스폰 </summary>
        public virtual void OnDie()
        {
            PlayDeathEffect().Forget();

            if (RoundSystem.Shared?.OngameOver != true)
            {
                SpawnExperienceOrbs();
                SpawnCoinOrbs();
                if (RewardUI.Shared != null)
                    RewardUI.Shared.SetScoreAmountToText(ScoreValue);
            }

            Destroy(gameObject);
        }

        private async UniTask PlayDeathEffect()
        {
            if (VFXManager.Shared != null)
                await VFXManager.Shared.SpawnVFX(PentaConst.KVfxEnemyDie, transform.position, Quaternion.identity);
        }
        #endregion

        #region Reward System
        /// <summary> 경험치 스폰 </summary>
        private void SpawnExperienceOrbs()
        {
            if (experienceOrbPrefab == null) return;

            int orbCount = UnityEngine.Random.Range((int)expOrbCount.x, (int)expOrbCount.y);
            int amount = ExperienceValue / orbCount;

            Guard.Shared?.GainExperience(amount);

            for (int i = 0; i < orbCount; i++)
            {
                Vector3 offset = new Vector3(
                    UnityEngine.Random.Range(-1.5f, 1.5f),
                    0.5f,
                    UnityEngine.Random.Range(-1.5f, 1.5f)
                );
                GameObject orb = Instantiate(experienceOrbPrefab, transform.position + offset, Quaternion.identity);
                orb.GetComponent<ExperienceOrb>()?.SetExperienceValue(amount);
            }
        }

        /// <summary> 코인 스폰 </summary>
        private void SpawnCoinOrbs()
        {
            if (coinOrbPrefab == null) return;

            int orbCount = UnityEngine.Random.Range((int)coinOrbCount.x, (int)coinOrbCount.y);
            int amount = CoinValue / orbCount;

            for (int i = 0; i < orbCount; i++)
            {
                Vector3 offset = new Vector3(
                    UnityEngine.Random.Range(-2.5f, 2.5f),
                    0f,
                    UnityEngine.Random.Range(-2.5f, 2.5f)
                );
                GameObject orb = Instantiate(coinOrbPrefab, transform.position + offset, Quaternion.identity);
                orb.GetComponent<CoinOrb>()?.SetCoinValue(amount);
            }
        }
        #endregion

        #region Status Effects
        /// <summary> HitStun: 피격 시 일시적 이동 정지 </summary>
        private void StartHitStun(float duration)
        {
            if (hitStunCoroutine != null) StopCoroutine(hitStunCoroutine);
            hitStunCoroutine = StartCoroutine(HitStunCoroutine(duration));
        }

        private IEnumerator HitStunCoroutine(float duration)
        {
            speed = 0f;
            yield return new WaitForSeconds(duration);
            speed = originSpeed;
            hitStunCoroutine = null;
        }

        /// <summary> Petrify: 석화 (이동/공격 불가, 크기 변화) </summary>
        public void StartPetrify(float duration, float scaleY)
        {
            if (petrifyCoroutine != null) StopCoroutine(petrifyCoroutine);
            petrifyCoroutine = StartCoroutine(PetrifyCoroutine(duration, scaleY));
        }

        private IEnumerator PetrifyCoroutine(float duration, float scaleY)
        {
            IsPetrified = true;
            speed = 0f;
            transform.localScale = new Vector3(originalScale.x, scaleY, originalScale.z);

            if (enemyRigidbody != null)
            {
                enemyRigidbody.velocity = Vector3.zero;
                enemyRigidbody.angularVelocity = Vector3.zero;
            }

            yield return new WaitForSeconds(duration);

            if (this != null && gameObject != null)
            {
                transform.localScale = originalScale;
                IsPetrified = false;
                speed = originSpeed;
            }

            petrifyCoroutine = null;
        }

        /// <summary> Knockback: 넉백 시작 (Rigidbody 물리 활성화) </summary>
        public void StartKnockback()
        {
            IsKnockedBack = true;
            IsGrounded = false;

            if (enemyRigidbody != null)
            {
                enemyRigidbody.isKinematic = false;
                enemyRigidbody.useGravity = true;
            }
        }

        /// <summary> Curse: 외부 코루틴 관리 </summary>
        public void StartCurseCoroutine(IEnumerator curseRoutine)
        {
            if (curseCoroutine != null) StopCoroutine(curseCoroutine);
            curseCoroutine = StartCoroutine(curseRoutine);
        }

        public void StopCurseCoroutine()
        {
            if (curseCoroutine != null)
            {
                StopCoroutine(curseCoroutine);
                curseCoroutine = null;
            }
        }
        #endregion

        #region Utility
        protected void InitHealthSlider(float maxHealth)
        {
            if (healthSlider == null) return;
            healthSlider.maxValue = maxHealth;
            healthSlider.value = maxHealth;
        }

        protected void UpdateSlider()
        {
            if (healthSlider != null)
                healthSlider.value = Health;
        }

        public void ToAni(int stateHash)
        {
            if (animator != null)
                animator.CrossFade(stateHash, 0.2f);
        }

        protected bool HasAnimationClip(int stateHash)
        {
            if (animator == null || animator.runtimeAnimatorController == null)
                return false;

            try
            {
                var currentState = animator.GetCurrentAnimatorStateInfo(0);
                animator.CrossFade(stateHash, 0.001f);
                animator.CrossFade(currentState.shortNameHash, 0.001f);
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }
}
