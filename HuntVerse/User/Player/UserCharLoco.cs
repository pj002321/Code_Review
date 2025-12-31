using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Hunt
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class UserCharLoco : MonoBehaviour, IPlayer
    {
        [Header("MOVE")]
        [SerializeField] private float moveSpeed = 5f;

        [Header("JUMP")]
        [SerializeField] private float jumpForce = 12f;
        [SerializeField] private float fallMultiplier = 2.5f;
        [SerializeField] private float lowJumpMultiplier = 2f;
        [SerializeField] private float coyoteTime = 0.15f;
        [SerializeField] private float jumpBufferTime = 0.2f;

        [Header("GROUND CHECK")]
        [SerializeField] private Transform groundCheckPoint;
        [SerializeField] private float groundCheckDistance = 0.1f;
        [SerializeField] private LayerMask groundLayer;
        #region Private Field

        private Rigidbody2D rb;
        private Animator animator;

        private bool canControl;
        private Vector2 moveInput;
        private float coyoteTimeCounter;
        private float jumpBufferCounter;
        private bool wasGrounded;
        private bool isGrounded;
        private SpriteRenderer spriteRenderer;

        private GameObject model;
        private InputManager inputKey;
        private IsAttackPointer hitpointer;

        private HashSet<IInteractable> nearbyInteractables = new HashSet<IInteractable>();
        private IInteractable currentInteractable;

        #endregion
        private void Awake()
        {
            UniTask.WaitUntil(() => !InputManager.Shared);
            inputKey = InputManager.Shared;
            inputKey.Player.Jump.performed += OnJumpPerformed;
            inputKey.Player.Attack.performed += OnAttackPerformed;
            inputKey.Player.Talk.performed += OnInteractPerformed;
        }
        private void Start()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            hitpointer = GetComponentInChildren<IsAttackPointer>();
            hitpointer.SetT(new Vector3(2.0f, 0.5f, 0f), new Vector2(1,1.25f)); // Custom
        }
        private void OnEnable()
        {
            inputKey?.Player.Enable();
        }

        private void OnDisable()
        {
            inputKey?.Player.Disable();
        }
        public void Initialize(GameObject characterModel)
        {
            model = characterModel;
            animator = model.GetComponent<Animator>();
            spriteRenderer = model.GetComponent<SpriteRenderer>();
            canControl = true;
        }
        private void Update()
        {
            if (!canControl) return;
            HandleInput();
            UpdateGroundCheck();
            UpdateTimers();
            UpdateAnimator();
            HandleMovement();
        }
        public void HandleInput()
        {
            if (isAttacking) return;
            moveInput = inputKey.Player.Move.ReadValue<Vector2>();
            if (moveInput.x > 0.1f)
            {
                transform.localScale = new Vector3(1, 1, 1);
            }
            else if (moveInput.x < -0.1f)
            {
                transform.localScale = new Vector3(-1, 1, 1);
            }

        }

        public bool isJumpping = true;
        private void OnJumpPerformed(InputAction.CallbackContext context)
        {
            if (!canControl) return;
            jumpBufferCounter = jumpBufferTime;
        }

        public bool isAttacking = false;
        private void OnAttackPerformed(InputAction.CallbackContext context)
        {
            if (!canControl) return;
            HandleAttack();

        }

        private void OnInteractPerformed(InputAction.CallbackContext context)
        {
            if (!canControl) return;
            HandleInteract();
        }

        // Sync NetWork
        public void HandleMovement()
        {
            if (isAttacking) return;
            float velx = moveInput.x * moveSpeed;
            rb.linearVelocity = new Vector2(velx, rb.linearVelocity.y);

        }

        // Sync NetWork
        public void HandleAttack()
        {
            if (!canControl || isAttacking) return;
            isAttacking = true;
            animator?.SetTrigger(AniKeyConst.K_tAttack);

            SpawnAttackVfx();
        }
        public void HandleInteract()
        {
            if (!canControl || isAttacking) return;

            var nearest = GetNearestInteractable();

            if (nearest != null && nearest.CanInteract())
            {
                nearest.Interact(transform);
                $"[UserCharLoco] {nearest.GetTransform().name}와 상호작용".DLog();
            }
            else
            {
                "[UserCharLoco] 상호작용 가능한 대상이 없습니다".DWarnning();
            }
        }
        public void HandleJump()
        {
            if (!canControl || isAttacking) return;

            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            coyoteTimeCounter = 0f;
            animator?.SetBool(AniKeyConst.k_bGround, true);
        }
        /// <summary>
        /// NPC가 "나랑 대화 가능해!" 알림
        /// </summary>
        public void RegisterInteractable(IInteractable interactable)
        {
            if (interactable == null) return;

            nearbyInteractables.Add(interactable);
            $"[UserCharLoco] {interactable.GetTransform().name} 등록 (총 {nearbyInteractables.Count}개)".DLog();
            UpdateInteractionUI();
        }

        /// <summary>
        /// NPC가 "나랑 대화 불가!" 알림
        /// </summary>
        public void UnregisterInteractable(IInteractable interactable)
        {
            if (interactable == null) return;

            nearbyInteractables.Remove(interactable);
            $"[UserCharLoco] {interactable.GetTransform().name} 해제 (남은 {nearbyInteractables.Count}개)".DLog();
            UpdateInteractionUI();
        }

        public void SetJumpEnabled(bool enabled) => isJumpping = enabled;

        private IInteractable GetNearestInteractable()
        {
            if (nearbyInteractables.Count == 0) return null;

            IInteractable nearest = null;
            float nearestDistance = float.MaxValue;

            foreach (var interactable in nearbyInteractables)
            {
                if (interactable == null || !interactable.CanInteract()) continue;

                float distance = Vector3.Distance(transform.position, interactable.GetTransform().position);
                if (distance < nearestDistance)
                {
                    nearest = interactable;
                    nearestDistance = distance;
                }
            }

            return nearest;
        }

        private void UpdateInteractionUI()
        {
            var nearest = GetNearestInteractable();

            if (nearest != null)
            {
                string text = nearest.GetInteractionText();
                $"[UserCharLoco] UI 표시: {text}".DLog();
                // TODO: InteractionUIManager.Shared.ShowPrompt(text);
            }
            else
            {
                $"[UserCharLoco] UI 숨김".DLog();
                // TODO: InteractionUIManager.Shared.HidePrompt();
            }
        }
        private async void SpawnAttackVfx()
        {
            $"⚔️ [PlayerAction] SpawnAttackVfx 시작".DLog();
           
            if (hitpointer == null)
            {
                $"⚔️ [PlayerAction] IsAttackPointer를 찾을 수 없음!".DError();
                return;
            }
                        
            if (VfxHelper.Shared == null)
            {
                $"⚔️ [PlayerAction] VfxHelper.Shared가 null!".DError();
                return;
            }
            
            var playerScale = transform.localScale;
            var vfxScale = new Vector3(playerScale.x, 1f, 1f);
            
            var vfxHandle = await VfxHelper.Shared.PlayOneShot(
                VfxKetConst.Kp_plain_hit_astera,
                hitpointer.GetT().position,
                hitpointer.GetT().rotation,
                this.transform,
                vfxScale
            );
            
            if (vfxHandle == null)
            {
                $"⚔️ [PlayerAction] VfxHandle이 null!".DError();
            }
            else
            {
                $"⚔️ [PlayerAction] VfxHandle 생성 성공!".DLog();
            }
        }


        #region Update
        private void UpdateAnimator()
        {
            if (animator == null) return;

            var speed = Mathf.Abs(moveInput.x);
            animator.SetBool(AniKeyConst.k_bMove, speed > 0.1f && isGrounded);
        }

        private void UpdateTimers()
        {
            
            coyoteTimeCounter -= Time.deltaTime;
            jumpBufferCounter -= Time.deltaTime;

            if (jumpBufferCounter > 0f && coyoteTimeCounter > 0f)
            {
                HandleJump();
                jumpBufferCounter = 0f;
            }
        }

        private void UpdateGroundCheck()
        {
            wasGrounded = isGrounded;

            RaycastHit2D hit = Physics2D.Raycast(
                groundCheckPoint.position,
                Vector2.down,
                groundCheckDistance,
                groundLayer
                );

            isGrounded = hit.collider != null;

            if (isGrounded)
            {
                coyoteTimeCounter = coyoteTime;
            }

            if (!wasGrounded && isGrounded)
            {
                OnLanded();
            }
        }

        private void OnLanded()
        {
            animator?.SetBool(AniKeyConst.k_bGround, false);
        }


        #endregion

        private void OnDrawGizmosSelected()
        {
            if (groundCheckPoint == null) return;

            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawLine(groundCheckPoint.position, groundCheckPoint.position + Vector3.down * groundCheckDistance);
            Gizmos.DrawWireSphere(groundCheckPoint.position + Vector3.down * groundCheckDistance, 0.05f);
        }

        private void OnDestroy()
        {
            if (inputKey != null)
            {
                inputKey.Player.Jump.performed -= OnJumpPerformed;
                inputKey.Player.Attack.performed -= OnAttackPerformed;
                inputKey.Player.Talk.performed -= OnInteractPerformed;
                inputKey.Action.Dispose();
            }
        }
    }
}
