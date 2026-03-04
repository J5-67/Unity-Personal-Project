using UnityEngine;

namespace Trap
{
    [RequireComponent(typeof(LineRenderer))]
    public class LaserHazard : MonoBehaviour
    {
        [Header("Laser Settings")]
        [SerializeField] private float maxDistance = 50f;
        [Tooltip("레이저의 실질적인 두께 (렌더러 및 충돌 판정)")]
        [SerializeField] private float laserThickness = 0.5f;

        [Tooltip("이 레이저를 통과하지 못하게 막을 지형 레이어(Wall 등)")]
        [SerializeField] private LayerMask obstacleLayer;
        [SerializeField] private LayerMask playerLayer;

        [Header("Damage & Knockback")]
        [SerializeField] private int damage = 1;
        [SerializeField] private float knockbackForce = 15f;

        private LineRenderer _lineRenderer;

        private void Awake()
        {
            _lineRenderer = GetComponent<LineRenderer>();
            _lineRenderer.positionCount = 2;
            _lineRenderer.useWorldSpace = true;

            _lineRenderer.startWidth = laserThickness;
            _lineRenderer.endWidth = laserThickness;
        }

        private void FixedUpdate()
        {
            float currentDistance = maxDistance;

            int mask = ~(playerLayer.value | (1 << 2));

            if (Physics.Raycast(transform.position, transform.forward, out RaycastHit wallHit, maxDistance, mask, QueryTriggerInteraction.Ignore))
            {
                currentDistance = wallHit.distance;
            }

            Vector3 startPos = transform.position;
            Vector3 endPos = startPos + transform.forward * currentDistance;

            _lineRenderer.SetPosition(0, startPos);
            _lineRenderer.SetPosition(1, endPos);

            Collider[] hits = Physics.OverlapCapsule(startPos, endPos, laserThickness / 2f, playerLayer);

            foreach (var col in hits)
            {

                if (col.TryGetComponent(out PlayerMovement pm) && pm.IsDashing)
                {

                    continue;
                }

                if (col.TryGetComponent(out PlayerHealth health))
                {

                    if (!health.IsInvincible)
                    {
                        health.TakeDamage(damage);
                    }

                    if (col.TryGetComponent(out PlayerMovement playerMove))
                    {
                        if (playerMove.CanMove)
                        {

                            Vector3 lineDir = transform.forward;
                            Vector3 toPlayer = col.transform.position - startPos;
                            float dot = Vector3.Dot(toPlayer, lineDir);
                            Vector3 closestPoint = startPos + lineDir * Mathf.Clamp(dot, 0, currentDistance);

                            Vector3 knockbackDir = (col.transform.position - closestPoint);

                            knockbackDir.x = 0f;

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
                    }
                    else if (col.TryGetComponent(out Rigidbody rb))
                    {
                        Vector3 fallbackDir = transform.forward + Vector3.up * 0.5f;
                        rb.linearVelocity = Vector3.zero;
                        rb.AddForce(fallbackDir.normalized * knockbackForce, ForceMode.Impulse);
                    }
                }
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Vector3 startPos = transform.position;
            Vector3 endPos = startPos + transform.forward * maxDistance;

            Gizmos.DrawLine(startPos, endPos);
            Gizmos.DrawWireSphere(startPos, laserThickness / 2f);
            Gizmos.DrawWireSphere(endPos, laserThickness / 2f);
        }
    }
}
