using UnityEngine;

namespace penta
{
    /// <summary>
    /// 스톤 글로벌 아이템 오브젝트 (주요 로직)
    /// - 가속 낙하 및 충돌 처리
    /// - 적 데미지 처리 (최대 체력의 30%) 및 석화 효과
    /// </summary>
    public class StoneGlobalItemObject : MonoBehaviour
    {
        [Header("STONE SETTINGS")]
        [SerializeField] private float damagePercent = 0.3f;
        [SerializeField] private float lifeTime = 5f;
        [SerializeField] private float initialFallSpeed = 2f;
        [SerializeField] private float finalFallSpeed = 20f;
        [SerializeField] private float accelerationTime = 1f;
        [SerializeField] private float petrifyDuration = 2f;
        [SerializeField] private float petrifiedScaleY = 0.1f;

        private Rigidbody rb;
        private bool hasHitGround = false;
        private bool hasHitEnemy = false;
        private float currentFallSpeed;
        private float fallTimer = 0f;

        private void Start()
        {
            InitializeStone();
        }

        /// <summary> 스톤 초기화 및 가속 낙하 설정 </summary>
        private void InitializeStone()
        {
            rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
            }
            rb.useGravity = false;
            rb.isKinematic = false;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            currentFallSpeed = initialFallSpeed;
            rb.velocity = Vector3.down * currentFallSpeed;

            Destroy(gameObject, lifeTime);
        }

        private void Update()
        {
            if (!hasHitGround && !hasHitEnemy)
            {
                fallTimer += Time.deltaTime;
                float t = Mathf.Clamp01(fallTimer / accelerationTime);
                currentFallSpeed = Mathf.Lerp(initialFallSpeed, finalFallSpeed, t);
                rb.velocity = Vector3.down * currentFallSpeed;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            Enemy enemy = other.GetComponent<Enemy>();
            if (enemy != null && !hasHitEnemy)
            {
                hasHitEnemy = true;
                DamageAndPetrifyEnemy(enemy);
                HitGround();
                return;
            }

            if (other.gameObject.layer == LayerMask.NameToLayer("Ground") && !hasHitGround)
            {
                HitGround();
            }
        }

        /// <summary> 적 데미지 처리 및 석화 효과 적용 </summary>
        private void DamageAndPetrifyEnemy(Enemy enemy)
        {
            float damage = enemy.MaxHealth * damagePercent;
            enemy.OnHit(damage);
            enemy.StartPetrify(petrifyDuration, petrifiedScaleY);
        }

        private void HitGround()
        {
            if (hasHitGround) return;
            hasHitGround = true;

            rb.velocity = Vector3.zero;
            rb.useGravity = false;
            Destroy(gameObject);
        }
    }
}
