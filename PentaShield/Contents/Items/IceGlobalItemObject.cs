using System.Collections;
using UnityEngine;

namespace chaos
{
    /// <summary>
    /// 아이스 메테오 글로벌 아이템 오브젝트 (주요 로직)
    /// - 사선 낙하 및 충돌 처리
    /// - 적 데미지 처리 (최대 체력의 30%)
    /// </summary>
    public class IceGlobalItemObject : MonoBehaviour
    {
        [Header("METEO SETTINGS")]
        [SerializeField] private float fallSpeed = 10f;
        [SerializeField] private float damagePercent = 0.3f;
        [SerializeField] private float explosionRadius = 2f;
        [SerializeField] private float lifeTime = 10f;
        [SerializeField] private float diagonalAngle = 15f;

        private Rigidbody rb;
        private bool hasExploded = false;
        private bool hasHitGround = false;
        private Vector3 targetPosition;
        private Vector3 diagonalDirection;

        private void Start()
        {
            InitializeMeteo();
        }

        /// <summary> 메테오 초기화 및 사선 낙하 설정 </summary>
        private void InitializeMeteo()
        {
            rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
            }

            rb.useGravity = true;
            
            if (targetPosition != Vector3.zero)
            {
                Vector3 baseDirection = (targetPosition - transform.position).normalized;
                float randomAngle = Random.Range(-diagonalAngle, diagonalAngle);
                diagonalDirection = Quaternion.AngleAxis(randomAngle, Vector3.up) * baseDirection;
                transform.rotation = Quaternion.LookRotation(diagonalDirection);
                rb.velocity = diagonalDirection * fallSpeed;
            }
            else
            {
                float randomAngle = Random.Range(-diagonalAngle, diagonalAngle);
                diagonalDirection = Quaternion.AngleAxis(randomAngle, Vector3.up) * Vector3.down;
                transform.rotation = Quaternion.LookRotation(diagonalDirection);
                rb.velocity = diagonalDirection * fallSpeed;
            }

            Destroy(gameObject, lifeTime);
        }

        public void SetTargetPosition(Vector3 target)
        {
            targetPosition = target;
        }

        private void OnTriggerEnter(Collider other)
        {
            Enemy enemy = other.GetComponent<Enemy>();
            if (enemy != null)
            {
                DamageEnemy(enemy);
                ExplodeMeteo();
                return;
            }

            if (other.gameObject.layer == LayerMask.NameToLayer("Ground") && !hasExploded && !hasHitGround)
            {
                OnGroundHit();
            }
        }

        /// <summary> 적 데미지 처리 </summary>
        private void DamageEnemy(Enemy enemy)
        {
            if (enemy == null) return;
            float damage = enemy.MaxHealth * damagePercent;
            enemy.OnHit(damage);
        }

        private void OnGroundHit()
        {
            if (hasHitGround) return;
            hasHitGround = true;

            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.isKinematic = true;
            }

            Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionRadius);
            foreach (var hitCollider in hitColliders)
            {
                Enemy enemy = hitCollider.GetComponent<Enemy>();
                if (enemy != null)
                {
                    DamageEnemy(enemy);
                }
            }

            Destroy(gameObject);
        }

        private void ExplodeMeteo()
        {
            if (hasExploded) return;
            hasExploded = true;

            Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionRadius);

            foreach (var hitCollider in hitColliders)
            {
                Enemy enemy = hitCollider.GetComponent<Enemy>();
                if (enemy != null)
                {
                    DamageEnemy(enemy);
                }
            }

            Destroy(gameObject);
        }
    }
}
