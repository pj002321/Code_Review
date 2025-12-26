using penta;
using Cysharp.Threading.Tasks;
using UnityEngine;


namespace penta
{

    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerReward))]
    /// <summary>
    /// 플레이어 컨트롤러 - 이동, 애니메이션, 체력 관리
    /// - 조이스틱 입력 처리 및 이동
    /// - 캐릭터 애니메이션 제어
    /// - 체력 및 데미지 처리
    /// - 공격 상태 관리
    /// </summary>
    public class PlayerController : MonoBehaviourSingleton<PlayerController>, IPlayer, IDamageable, IObject
    {
        
        [SerializeField] private Transform characterModel;

        #region Constants
        private const float DEFAULT_MOVE_SPEED = 5f;
        private const float DEFAULT_ROTATION_SPEED = 10f;
        private const float GRAVITY = 9.8f;
        private const float LEAN_ANGLE = 15f;
        private const float LEAN_X_MULTIPLIER = 0.6f;
        private const float MIN_INPUT_MAGNITUDE = 0.1f;
        private const float FIXED_Y_POSITION = 1.5f;
        private const float GROUNDED_VELOCITY = -0.5f;
        private const float ROTATION_TO_DIRECTION_SPEED = 1.3f;
        private const float ANIMATION_MID_POINT = 0.5f;
        private const float ANIMATION_END_POINT = 0.95f;
        private const float INVINCIBLE_FLASH_DURATION = 0.15f;
        private const int INITIAL_HEALTH = 100;
        #endregion

        #region Fields
        [Header("MOVEMENT")]
        [SerializeField] private FixedJoystick joystick;
        [SerializeField] public float moveSpeed = DEFAULT_MOVE_SPEED;
        [SerializeField] private float rotationSpeed = DEFAULT_ROTATION_SPEED;
        [SerializeField] private float leanAngle = LEAN_ANGLE; 

        private Animator anim;
        private readonly int attackLayerIndex = 1;
        
        private bool hasTriggeredMidPoint;
        private bool isAttacking;
        private bool debuff;

        public SliderValue healthSlider { get; private set; }
        private CharacterController controller;
        private float verticalVelocity;
        private PlayerReward playerExperience;
        #endregion

        public bool IsInvincible { get; set; }
        public bool Debuff
        {
            get => debuff;
            set => debuff = value;
        }
        #region IObject 
        // 2025 05 23 this interface is used to Buff, Debuff cur use speed
        public string IObjGUID { get; set; } = string.Empty;
        public int BaseHeath { get; set; } = default;
        public int BaseMaxHeath { get; set; } = default;
        public int BaseDamage { get; set; } = default;
        public float BaseSpeed { get; set; } = 5f;
        public float BaseAttackSpeed { get; set; } = default;
        public float BaseAttackRange { get; set; } = default;
        public float BaseAttackDistance { get; set; } = default;
        public float BaseAttackTime { get; set; } = default;
        public float BaseAttackTimeMax { get; set; } = default;
        public float BaseAttackTimeMin { get; set; } = default;

        public int CurHeath { get; set; } = default;
        public int CurMaxHeath { get; set; } = default;
        public int CurDamage { get; set; } = default;
        public float CurSpeed { get; set; } = 5f;
        public float CurAttackSpeed { get; set; } = default;
        public float CurAttackRange { get; set; } = default;
        public float CurAttackDistance { get; set; } = default;
        public float CurAttackTime { get; set; } = default;

        public float Health
        {
            get => CurHeath;
            set
            {
                CurHeath = (int)value;
                healthSlider?.SetCurrentHealth(CurHeath);
            }
        }

        public float MaxHealth
        {
            get => CurMaxHeath;
            set
            {
                CurMaxHeath = (int)value;
                healthSlider?.SetMaxHealth(CurMaxHeath);
            }
        }

        private protected override bool DontDestroy => false;

        #endregion

        #region Life
        protected override void Awake()
        {
            base.Awake();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }
        private void Start()
        {
            InitializeComponents();
            InitializeHealth();
            InitializeReferences();
        }

        private void InitializeComponents()
        {
            controller = GetComponent<CharacterController>();
            playerExperience = GetComponent<PlayerReward>();
            anim = characterModel?.GetComponent<Animator>();
            healthSlider = GetComponent<SliderValue>();
        }

        private void InitializeHealth()
        {
            CurHeath = INITIAL_HEALTH;
            CurMaxHeath = INITIAL_HEALTH;
            healthSlider?.Initialize(CurMaxHeath, CurHeath);
        }

        private void InitializeReferences()
        {
            if (playerExperience == null)
                gameObject.AddComponent<PlayerReward>();
            
            joystick ??= FindObjectOfType<FixedJoystick>();
            characterModel ??= transform;
            IObjGUID = gameObject.GetInstanceID().ToString();
        }

        private void Update()
        {
            HandleMove();
            CheckAttackAnimationTiming();
        }


        #endregion

        private void HandleMove()
        {
            if (GameFreezeManager.IsGameFrozen)
            {
                StopMovement();
                return;
            }

            if (joystick == null || controller == null) return;

            Vector3 inputDirection = GetInputDirection();
            float currentMoveSpeed = GetCurrentMoveSpeed();

            if (!isAttacking)
            {
                HandleRotation(inputDirection);
            }
            else
            {
                StopRotation();
            }

            ApplyGravity();
            ApplyMovement(inputDirection, currentMoveSpeed);
            ClampYPosition();
        }

        private void StopMovement()
        {
            UpdateAnimation(0f);
            ResetLeanRotation();
        }

        private Vector3 GetInputDirection()
        {
            float horizontal = joystick.Horizontal;
            float vertical = joystick.Vertical;
            return new Vector3(horizontal, 0f, vertical).normalized;
        }

        private float GetCurrentMoveSpeed()
        {
            const float DEBUFF_SPEED_MULTIPLIER = 0.5f;
            return debuff ? moveSpeed * DEBUFF_SPEED_MULTIPLIER : moveSpeed;
        }

        private void HandleRotation(Vector3 inputDirection)
        {
            if (inputDirection.magnitude > MIN_INPUT_MAGNITUDE)
            {
                RotateToInputDirection(inputDirection);
                ApplyLeanRotation(inputDirection);
                UpdateAnimation(inputDirection.magnitude);
            }
            else
            {
                ResetLeanRotation();
                UpdateAnimation(0f);
            }
        }

        private void RotateToInputDirection(Vector3 inputDirection)
        {
            if (characterModel == null) return;

            Quaternion targetRotation = Quaternion.LookRotation(inputDirection);
            characterModel.rotation = Quaternion.Slerp(
                characterModel.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }

        private void StopRotation()
        {
            ResetLeanRotation();
            UpdateAnimation(0f);
        }

        private void ApplyGravity()
        {
            if (controller.isGrounded)
            {
                verticalVelocity = GROUNDED_VELOCITY;
            }
            else
            {
                verticalVelocity -= GRAVITY * Time.deltaTime;
            }
        }

        private void ApplyMovement(Vector3 inputDirection, float currentMoveSpeed)
        {
            Vector3 movement = isAttacking ? Vector3.zero : inputDirection * currentMoveSpeed;
            movement.y = verticalVelocity;
            controller.Move(movement * Time.deltaTime);
        }

        private void ClampYPosition()
        {
            if (Mathf.Approximately(transform.position.y, FIXED_Y_POSITION)) return;
            transform.position = new Vector3(transform.position.x, FIXED_Y_POSITION, transform.position.z);
        }
        private void UpdateAnimation(float mag)
        {
            anim?.SetBool(PentaConst.tWalk, mag > 0.1f);
        }

        private void ApplyLeanRotation(Vector3 inputDirection)
        {
            if (characterModel == null) return;

            float leanZ = -inputDirection.x * leanAngle;
            float leanX = inputDirection.z * leanAngle * LEAN_X_MULTIPLIER;
            
            Quaternion leanRotation = Quaternion.Euler(leanX, 0f, leanZ);
            characterModel.rotation = Quaternion.Slerp(
                characterModel.rotation,
                characterModel.rotation * leanRotation,
                rotationSpeed * Time.deltaTime
            );
        }

        private void ResetLeanRotation()
        {
            if (characterModel == null) return;

            characterModel.rotation = Quaternion.Slerp(
                characterModel.rotation,
                Quaternion.LookRotation(characterModel.forward),
                rotationSpeed * Time.deltaTime
            );
        }
        public void SetAttacking(bool attacking)
        {
            isAttacking = attacking;
        }

        public void RotateToDirection(Vector3 direction)
        {
            if (characterModel == null || direction.magnitude <= MIN_INPUT_MAGNITUDE) return;

            direction.y = 0f;
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            characterModel.rotation = Quaternion.Slerp(characterModel.rotation, targetRotation, ROTATION_TO_DIRECTION_SPEED);
        }

        /// <summary>
        /// 특정 애니메이션 해시로 공격 애니메이션을 트리거합니다.
        /// </summary>
        /// <param name="animationHash">트리거할 애니메이션의 해시</param>
        public void TriggerAttackAnimation(int animationHash)
        {
            if (anim == null) return;

            anim.SetLayerWeight(attackLayerIndex, 1f);
            anim.SetTrigger(animationHash);
            hasTriggeredMidPoint = false;
        }

        /// <summary>
        /// 현재 재생 중인 Attack 애니메이션의 중간 지점 체크
        /// </summary>
        private void CheckAttackAnimationTiming()
        {
            if (anim == null) return;

            AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(attackLayerIndex);
            bool isAttackingState = stateInfo.IsName(PentaConst.tLeftAttack.ToString()) 
                || stateInfo.IsName(PentaConst.tRightAttack.ToString()) 
                || stateInfo.IsName(PentaConst.tAttack.ToString());

            if (isAttackingState)
            {
                float normalizedTime = stateInfo.normalizedTime % 1.0f;
                
                if (normalizedTime >= ANIMATION_MID_POINT && !hasTriggeredMidPoint)
                {
                    hasTriggeredMidPoint = true;
                    PlayerBehaviour.Shared?.OnAttackAnimationEvent();
                }
                
                if (normalizedTime >= ANIMATION_END_POINT && isAttacking)
                {
                    ResetAttackState();
                }
            }
            else if (isAttacking)
            {
                ResetAttackState();
            }
        }

        private void ResetAttackState()
        {
            isAttacking = false;
            hasTriggeredMidPoint = false;
        }

        public void OnHit(float damage)
        {
            if (IsInvincible)
            {
                Debug.Log("Player is invincible! Damage blocked.");
                return;
            }

            ApplyDamage((int)damage);
            PlayHitEffects();

            if (CurHeath <= 0)
            {
                OnDie();
            }
        }

        private void ApplyDamage(int damageAmount)
        {
            CurHeath = Mathf.Max(0, CurHeath - damageAmount);
            healthSlider?.SetCurrentHealth(CurHeath);
            healthSlider?.TakeDamage(0);
        }

        private void PlayHitEffects()
        {
            HitFlashEffect.TriggerFlash(gameObject, INVINCIBLE_FLASH_DURATION, Color.white);
            AudioHelper.PlaySFX(AudioConst.PLAYER_HIT_IMAPCT);
        }

        public void OnDie()
        {
            RoundSystem.Shared?.GameOver().Forget();
        }

     

    }
}