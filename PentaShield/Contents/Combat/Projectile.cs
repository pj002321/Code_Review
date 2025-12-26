using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using penta;

/// <summary>
/// 발사체 시스템 (주요 로직)
/// - 이동 패턴 (유도/직선)
/// - 속도 운동 (등속/가속)
/// - 타입별 특수 효과 (Thunder 튕김, Flame DoT, Stone 넉백, Curse)
/// - 충돌 및 데미지 처리
/// </summary>
public class Projectile : MonoBehaviour, IDamager
{
    #region Enums
    public enum ProjectileType { Normal, Thunder, Flame, Stone, Curse }
    public enum MovementType { Guided, Straight }
    public enum SpeedMotionType { Constant, Accelerated }
    #endregion

    #region Fields
    [Header("TYPE")]
    [SerializeField] public ProjectileType projectileType = ProjectileType.Normal;
    [SerializeField] public MovementType movementType = MovementType.Guided;
    [SerializeField] private SpeedMotionType speedMotionType = SpeedMotionType.Accelerated;

    [Header("PROPERTIES")]
    [SerializeField] public LayerMask enemyLayer;
    [SerializeField] public LayerMask excludedLayers;
    [SerializeField] private float moveSpeed = 15f;
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private float detectionRadius = 25f;
    [SerializeField] private float rotationSpeed = 20f;

    [Header("ACCELERATION")]
    [SerializeField] private float initialSpeed = 1f;
    [SerializeField] private float acceleration = 35f;
    [SerializeField] private float maxSpeed = 50f;

    [Header("DAMAGE")]
    [SerializeField] private float explosionRadius = 0f;

    public float Damage { get; set; } = 10f;

    // Type-specific properties
    private Flame flameParent;
    private bool enhancedDoT = false;
    
    private Thunder thunderParent;
    private float originalDamage = 0f;
    private int bounceCount = 0;
    private HashSet<GameObject> bouncedTargets = new HashSet<GameObject>();
    
    private Stone stoneParent;
    private float knockBackForce = 0f;
    private float knockBackDuration = 0f;
    
    private Curse curseParent;
    private float curseDuration = 3f;

    private GameObject shooter;
    private Transform target;
    private Rigidbody rb;
    private Vector3 initialDirection;
    private float currentSpeed;
    private bool hasDealtDamage = false;
    private HashSet<GameObject> damagedObjects = new HashSet<GameObject>();
    private Coroutine lifetimeCoroutine;
    #endregion

    #region Unity Lifecycle
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = false;
        }
    }

    void OnEnable()
    {
        initialDirection = transform.forward;
        currentSpeed = speedMotionType == SpeedMotionType.Constant ? moveSpeed : initialSpeed;
        target = null;
        hasDealtDamage = false;
        damagedObjects.Clear();

        if (rb != null)
            rb.velocity = initialDirection * currentSpeed;

        if (lifetimeCoroutine != null) StopCoroutine(lifetimeCoroutine);
        lifetimeCoroutine = StartCoroutine(DeactivateAfterLifetime());
    }

    void FixedUpdate()
    {
        UpdateSpeed();
        UpdateMovement();
    }
    #endregion

    #region Configuration
    public void SetShooter(GameObject shooterObject) => shooter = shooterObject;
    public void SetDirection(Vector3 direction) => initialDirection = direction;
    public ProjectileType GetProjType() => projectileType;
    public void SetProjectileType(ProjectileType type) => projectileType = type;

    public void SetFlameProperties(Flame flame, bool enhanced = false)
    {
        flameParent = flame;
        enhancedDoT = enhanced;
    }

    public void SetThunderProperties(Thunder thunder, float initialDamage)
    {
        thunderParent = thunder;
        originalDamage = initialDamage;
        bounceCount = 0;
        bouncedTargets.Clear();
    }

    public void SetStoneProperties(Stone stone, float knockbackForce, float knockbackDuration)
    {
        stoneParent = stone;
        knockBackForce = knockbackForce;
        knockBackDuration = knockbackDuration;
    }

    public void SetCurseProperties(Curse curse, float duration)
    {
        curseParent = curse;
        curseDuration = duration;
    }

    public void ResetProjectile()
    {
        hasDealtDamage = false;
        damagedObjects.Clear();
        shooter = null;
        target = null;
        bounceCount = 0;
        bouncedTargets.Clear();
    }
    #endregion

    #region Movement
    /// <summary> 속도 업데이트 (가속 모드) </summary>
    private void UpdateSpeed()
    {
        if (speedMotionType == SpeedMotionType.Accelerated)
        {
            currentSpeed = Mathf.Min(currentSpeed + acceleration * Time.fixedDeltaTime, maxSpeed);
        }
    }

    /// <summary> 이동 로직 (유도/직선) </summary>
    private void UpdateMovement()
    {
        if (movementType == MovementType.Guided)
            UpdateGuidedMovement();
        else
            UpdateStraightMovement();
    }

    /// <summary> 유도 미사일 이동: 타겟 자동 탐색 및 추적 </summary>
    private void UpdateGuidedMovement()
    {
        if (target == null || !target.gameObject.activeInHierarchy)
            FindNearestTarget();

        if (target != null)
        {
            Vector3 direction = (target.position - transform.position).normalized;
            rb.velocity = direction * currentSpeed;
            
            if (direction != Vector3.zero)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), rotationSpeed * Time.fixedDeltaTime);
        }
        else
        {
            rb.velocity = initialDirection * currentSpeed;
        }
    }

    /// <summary> 직선 이동 </summary>
    private void UpdateStraightMovement()
    {
        rb.velocity = initialDirection * currentSpeed;
    }

    /// <summary> 가장 가까운 적 탐색 </summary>
    private void FindNearestTarget()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius, enemyLayer);
        float closestDistance = Mathf.Infinity;
        Transform closestTarget = null;

        foreach (var hit in hits)
        {
            if (hit.gameObject == shooter) continue;
            
            float distance = Vector3.Distance(transform.position, hit.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestTarget = hit.transform;
            }
        }

        target = closestTarget;
    }
    #endregion

    #region Collision
    void OnTriggerEnter(Collider other)
    {
        ProcessCollision(other.gameObject);
    }

    private void ProcessCollision(GameObject hitObject)
    {
        if (hasDealtDamage && projectileType != ProjectileType.Thunder) return;
        if (shooter != null && hitObject == shooter) return;

        int hitLayer = 1 << hitObject.layer;
        if ((hitLayer & excludedLayers.value) != 0) return;

        bool isEnemyHit = (hitLayer & enemyLayer.value) != 0;
        if (isEnemyHit && !damagedObjects.Contains(hitObject))
        {
            HandleEnemyHit(hitObject);
            if (projectileType == ProjectileType.Thunder) return;
        }

        if (projectileType != ProjectileType.Thunder)
            ExplodeAndDeactivate();
    }

    private void HandleEnemyHit(GameObject hitObject)
    {
        IDamageable damageable = hitObject.GetComponent<IDamageable>();
        if (damageable != null)
        {
            TakeDamage(damageable);
            damagedObjects.Add(hitObject);

            // Lifesteal 처리
            if (shooter != null)
            {
                var shooterEnemy = shooter.GetComponent<Enemy>();
                if (shooterEnemy?.IsLifesteal == true)
                    shooterEnemy.ProcessLifesteal(Damage);
            }
        }
    }

    private void ExplodeAndDeactivate()
    {
        if (hasDealtDamage && projectileType != ProjectileType.Normal) return;

        // 폭발 범위 데미지
        if (explosionRadius > 0f)
        {
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionRadius, enemyLayer);
            foreach (var collider in hitColliders)
            {
                if (!damagedObjects.Contains(collider.gameObject))
                {
                    IDamageable damageable = collider.GetComponent<IDamageable>();
                    if (damageable != null)
                        TakeDamage(damageable);
                }
            }
        }

        hasDealtDamage = true;
        gameObject.SetActive(false);
    }
    #endregion

    #region Type-Specific Effects
    /// <summary> 데미지 처리 및 타입별 효과 적용 </summary>
    public void TakeDamage(IDamageable target)
    {
        if (target == null) return;

        GameObject targetObject = ((MonoBehaviour)target).gameObject;
        HitFlashEffect.TriggerFlash(targetObject, 0.15f, Color.white);

        if (target is Enemy enemy)
            enemy.OnHit(Damage, projectileType.ToString());
        else
            target.OnHit(Damage);

        ApplyTypeSpecificEffects(targetObject);
    }

    private void ApplyTypeSpecificEffects(GameObject targetObject)
    {
        switch (projectileType)
        {
            case ProjectileType.Thunder:
                HandleThunderHit(targetObject);
                break;
            case ProjectileType.Flame:
                flameParent?.ApplyDotDamage(targetObject, enhancedDoT);
                break;
            case ProjectileType.Stone:
                HandleStoneHit(targetObject);
                break;
            case ProjectileType.Curse:
                HandleCurseHit(targetObject);
                break;
        }
    }

    /// <summary> Thunder: 적 간 튕김 (데미지 감소) </summary>
    private void HandleThunderHit(GameObject targetObj)
    {
        if (thunderParent == null) return;

        bouncedTargets.Add(targetObj);
        thunderParent.CreateThunderBounceEffect(transform.position);
        StartCoroutine(BounceToNextTarget());
    }

    private IEnumerator BounceToNextTarget()
    {
        yield return new WaitForSeconds(0.1f);

        if (thunderParent == null) yield break;

        Transform nextTarget = thunderParent.FindNextBounceTarget(transform.position, bouncedTargets);

        if (nextTarget != null)
        {
            bounceCount++;
            Damage = originalDamage * Mathf.Pow(0.7f, bounceCount); // 30% 감소
            target = nextTarget;
            hasDealtDamage = false;

            Vector3 direction = (nextTarget.position - transform.position).normalized;
            if (rb != null)
                rb.velocity = direction * currentSpeed;

            transform.rotation = Quaternion.LookRotation(direction);
        }
        else
        {
            ExplodeAndDeactivate();
        }
    }

    /// <summary> Stone: 넉백 적용 </summary>
    private void HandleStoneHit(GameObject targetObj)
    {
        var damageable = targetObj.GetComponent<IDamageable>();
        damageable?.OnHit(Damage);

        if (stoneParent != null)
        {
            Vector3 knockbackDirection = (targetObj.transform.position - transform.position).normalized;
            knockbackDirection.y = 0;
            StartCoroutine(stoneParent.CO_ApplyKnockback(targetObj, knockbackDirection, knockBackForce, knockBackDuration));
        }
    }

    /// <summary> Curse: 적이 아군 공격 </summary>
    private void HandleCurseHit(GameObject targetObj)
    {
        if (curseParent == null) return;

        var enemy = targetObj.GetComponent<Enemy>();
        if (enemy != null)
            enemy.StartCurseCoroutine(curseParent.CO_ApplyCurse(targetObj, curseDuration));
    }
    #endregion

    private IEnumerator DeactivateAfterLifetime()
    {
        yield return new WaitForSeconds(lifetime);
        ExplodeAndDeactivate();
    }
}
