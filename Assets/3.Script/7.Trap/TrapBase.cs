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

                    Collider trapCollider = GetComponentInChildren<Collider>();
                    Vector3 closestPoint = other.transform.position;
                    
                    if (trapCollider != null)
                    {
                        closestPoint = trapCollider.ClosestPoint(other.transform.position);
                    }
                    
                    Vector3 knockbackDir = (other.transform.position - closestPoint).normalized;

                    if (knockbackDir == Vector3.zero)
                    {
                        knockbackDir = (other.transform.position - transform.position).normalized;
                    }

                    if (other.TryGetComponent(out PlayerMovement playerMove))
                    {
                        knockbackDir.x = 0f; // Optional, maybe we want horizontal knockback?
                        if (knockbackDir.sqrMagnitude < 0.01f)
                        {
                            knockbackDir = -playerMove.transform.forward;
                        }
                        if (knockbackDir.y < 0.5f)
                        {
                            knockbackDir.y += 0.8f;
                        }
                        playerMove.ApplyKnockback(knockbackDir.normalized, knockbackForce, 0.25f);
                    }
                    else if (other.TryGetComponent(out Rigidbody rb))
                    {
                        rb.linearVelocity = Vector3.zero;
                        rb.AddForce(knockbackDir.normalized * knockbackForce, ForceMode.Impulse);
                    }

                    health.TakeDamage(damage, false);
                }
            }
        }
    }
}
