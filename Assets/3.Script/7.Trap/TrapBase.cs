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
                    Vector3 knockbackDir = (other.transform.position - transform.position).normalized;
                    
                    health.TakeDamage(damage); 
                   
                    if (other.TryGetComponent(out Rigidbody rb))
                    {
                        rb.linearVelocity = Vector3.zero;
                        rb.AddForce(knockbackDir * knockbackForce, ForceMode.Impulse);
                    }
                    
                    Debug.Log($"[Trap] Ouch! Hit by {gameObject.name}");
                }
            }
        }
    }
}
