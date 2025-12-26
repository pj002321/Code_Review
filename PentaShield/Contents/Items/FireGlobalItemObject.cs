using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace penta
{
    /// <summary>
    /// 파이어 메테오 글로벌 아이템 오브젝트 (주요 로직)
    /// - 사선 낙하 및 충돌 처리
    /// - 적 데미지 처리 (즉시 30% + 도트 2%)
    /// - 그라운드 충돌 시 도트 데미지 영역 생성
    /// </summary>
    public class FireGlobalItemObject : MonoBehaviour
    {
        [Header("FIREBALL SETTINGS")]
        [SerializeField] private float fallSpeed = 10f;
        [SerializeField] private float damagePercent = 0.3f;
        [SerializeField] private float explosionRadius = 2f;
        [SerializeField] private float lifeTime = 10f;
        [SerializeField] private float diagonalAngle = 15f;
        
        [Header("DOT DAMAGE SETTINGS")]
        [SerializeField] private float dotDamagePercent = 0.02f;
        [SerializeField] private float dotDamageInterval = 0.5f;
        [SerializeField] private float dotDamageDuration = 3f;

        private Rigidbody rb;
        private bool hasExploded = false;
        private bool hasHitGround = false;
        private Vector3 targetPosition;
        private bool isDotDamageActive = false;
        private Coroutine dotDamageCoroutine;
        private List<Enemy> affectedEnemies = new List<Enemy>();
        private Vector3 diagonalDirection;

        private void Start()
        {
            InitializeFireBall();
        }

        /// <summary> 파이어볼 초기화 및 사선 낙하 설정 </summary>
        private void InitializeFireBall()
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
                ExplodeFireBall();
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

            StartDotDamage();
            Destroy(gameObject, dotDamageDuration);
        }

        private void ExplodeFireBall()
        {
            if (hasExploded) return;
            hasExploded = true;
            
            StopDotDamage();

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

        /// <summary> 도트 데미지 시작 </summary>
        private void StartDotDamage()
        {
            if (isDotDamageActive) return;
            isDotDamageActive = true;
            dotDamageCoroutine = StartCoroutine(DotDamageCoroutine());
        }

        private void StopDotDamage()
        {
            if (!isDotDamageActive) return;
            isDotDamageActive = false;
            if (dotDamageCoroutine != null)
            {
                StopCoroutine(dotDamageCoroutine);
                dotDamageCoroutine = null;
            }
            
            foreach (var enemy in affectedEnemies)
            {
                if (enemy != null)
                {
                    enemy.IsTakingDotDamage = false;
                }
            }
            affectedEnemies.Clear();
        }

        /// <summary> 도트 데미지 코루틴 - 주기적으로 범위 내 적에게 데미지 적용 </summary>
        private IEnumerator DotDamageCoroutine()
        {
            float elapsedTime = 0f;
            
            while (elapsedTime < dotDamageDuration && isDotDamageActive)
            {
                Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionRadius);
                
                foreach (var hitCollider in hitColliders)
                {
                    Enemy enemy = hitCollider.GetComponent<Enemy>();
                    if (enemy != null && !affectedEnemies.Contains(enemy))
                    {
                        affectedEnemies.Add(enemy);
                    }
                }
                
                for (int i = affectedEnemies.Count - 1; i >= 0; i--)
                {
                    if (affectedEnemies[i] == null)
                    {
                        affectedEnemies.RemoveAt(i);
                        continue;
                    }
                    
                    float distance = Vector3.Distance(transform.position, affectedEnemies[i].transform.position);
                    if (distance <= explosionRadius)
                    {
                        ApplyDotDamage(affectedEnemies[i]);
                    }
                    else
                    {
                        affectedEnemies.RemoveAt(i);
                    }
                }
                
                elapsedTime += dotDamageInterval;
                yield return new WaitForSeconds(dotDamageInterval);
            }
            
            StopDotDamage();
        }

        /// <summary> 도트 데미지 적용 </summary>
        private void ApplyDotDamage(Enemy enemy)
        {
            if (enemy == null) return;
            enemy.IsTakingDotDamage = true;
            float dotDamage = enemy.MaxHealth * dotDamagePercent;
            enemy.OnHit(dotDamage);
        }
    }
}
