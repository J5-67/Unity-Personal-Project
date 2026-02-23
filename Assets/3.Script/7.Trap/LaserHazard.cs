using UnityEngine;

namespace Trap
{
    [RequireComponent(typeof(LineRenderer))]
    public class LaserHazard : MonoBehaviour
    {
        [Header("Laser Settings")]
        [Tooltip("레이저의 최대 길이")]
        [SerializeField] private float maxDistance = 50f;
        [Tooltip("레이저의 실질적인 두께 (렌더러 및 충돌 판정)")]
        [SerializeField] private float laserThickness = 0.5f;
        
        [Tooltip("이 레이저를 통과하지 못하게 막을 지형 레이어(Wall 등)")]
        [SerializeField] private LayerMask obstacleLayer; 
        
        [Tooltip("피해를 입을 플레이어 레이어")]
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
            
            // 라인 렌더러의 시작/끝 두께를 변수에 맞춤
            _lineRenderer.startWidth = laserThickness;
            _lineRenderer.endWidth = laserThickness;
        }

        private void FixedUpdate()
        {
            float currentDistance = maxDistance;

            // [Fix] 인스펙터의 ObstacleLayer 설정과 무관하게, 
            // '플레이어 레이어'와 'IgnoreRaycast'를 제외한 "모든 솔리드(Solid) 벽/바닥"에 무조건 막히도록 마스크를 강제 할당!
            int mask = ~(playerLayer.value | (1 << 2));

            // 1. 벽에 막히는지 검사
            // (핵심 픽스: QueryTriggerInteraction.Ignore 를 통해 플랫폼 위에 깔린 '투명 탑승 판정 트리거'를 무시하고 진짜 바닥에만 맞게 수정!)
            if (Physics.Raycast(transform.position, transform.forward, out RaycastHit wallHit, maxDistance, mask, QueryTriggerInteraction.Ignore))
            {
                currentDistance = wallHit.distance;
            }

            Vector3 startPos = transform.position;
            Vector3 endPos = startPos + transform.forward * currentDistance;

            // 2. LineRenderer 시각적 업데이트 (에디터/플레이)
            _lineRenderer.SetPosition(0, startPos);
            _lineRenderer.SetPosition(1, endPos);

            // 3. 플레이어 충돌 검사 (대시 무시 기믹 포함)
            // OverlapCapsule을 사용하여 레이저 전체 범위(시작점부터 끝점까지 두께만큼)를 완벽하게 스캔
            Collider[] hits = Physics.OverlapCapsule(startPos, endPos, laserThickness / 2f, playerLayer);

            foreach (var col in hits)
            {
                // (오빠의 요청: 대시로 피해 없이 넘어갈 수 있어)
                if (col.TryGetComponent(out PlayerMovement pm) && pm.IsDashing)
                {
                    // 대시 중이면 무사 통과! 슝!
                    continue; 
                }

                if (col.TryGetComponent(out PlayerHealth health))
                {
                    // 1. 데미지는 플레이어가 무적 상태가 아닐 때만 입힘
                    if (!health.IsInvincible)
                    {
                        health.TakeDamage(damage);
                    }

                    // 2. 넉백은 무적 상태(IsInvincible)라도 조작 가능(CanMove)할 때 무조건 적용하여
                    // 억지로 데미지만 입고 걸어서 뚫고 지나가는 행위(Damage Boosting)를 원천 차단!
                    if (col.TryGetComponent(out PlayerMovement playerMove))
                    {
                        if (playerMove.CanMove)
                        {
                            // 레이저 중심선(선분)에서 플레이어 쪽으로 밀어내는 '방사형 넉백 벡터' 계산
                            Vector3 lineDir = transform.forward;
                            Vector3 toPlayer = col.transform.position - startPos;
                            float dot = Vector3.Dot(toPlayer, lineDir);
                            Vector3 closestPoint = startPos + lineDir * Mathf.Clamp(dot, 0, currentDistance);

                            Vector3 knockbackDir = (col.transform.position - closestPoint);
                            
                            // 우리 게임은 X축 이동이 얼어붙은 2.5D 게임이므로 X축 밀림은 원천 차단
                            knockbackDir.x = 0f;

                            // 만약 플레이어가 선과 완벽히 겹쳐있어 벡터가 0이라면 자신이 쳐다보던 반대 방향으로 튕김
                            if (knockbackDir.sqrMagnitude < 0.01f)
                            {
                                knockbackDir = -playerMove.transform.forward; 
                            }

                            // 오빠 요청: "위로 넉백은 안 돼" -> 이 계산식에 무조건 Y축 가중치를 주어 통통 튕기게 보정!
                            if (knockbackDir.y < 0.5f)
                            {
                                knockbackDir.y += 0.8f; 
                            }
                            
                            // 플랫폼 탑승을 풀고, 0.25초 조작 불능과 함께 뒤로 뻥! 튕겨냄
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
        
        // 에디터에서 레이저 발사 경로와 두께를 시각적으로 보여줌 (작업 편의성)
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
