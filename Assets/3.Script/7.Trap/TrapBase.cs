using UnityEngine;

namespace Trap
{
    public class TrapBase : MonoBehaviour
    {
        [Header("Trap Settings")]
        [SerializeField] private int damage = 1;         
        [SerializeField] private float knockbackForce = 10f; 

        protected virtual void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                if (other.TryGetComponent(out PlayerHealth health))
                {
                    // 1. 충돌 지점 기반으로 밀어내는 방향 계산 (ClosestPoint 사용)
                    Collider trapCollider = GetComponent<Collider>();
                    Vector3 closestPoint = trapCollider.ClosestPoint(other.transform.position);
                    Vector3 knockbackDir = (other.transform.position - closestPoint).normalized;

                    // 만약 플레이어가 완전히 안쪽에 있어서 벡터가 0이면, 기존 방식(중심점 기준)으로 Fallback
                    if (knockbackDir == Vector3.zero)
                    {
                        knockbackDir = (other.transform.position - transform.position).normalized;
                    }

                    health.TakeDamage(damage); 
                   
                    if (other.TryGetComponent(out Rigidbody rb))
                    {
                        rb.linearVelocity = Vector3.zero;
                        rb.AddForce(knockbackDir * knockbackForce, ForceMode.Impulse);
                    }
                    
                    Debug.Log($"[Trap] Ouch! Hit by {gameObject.name} / Dir: {knockbackDir}");
                }
            }
        }
    }
}
