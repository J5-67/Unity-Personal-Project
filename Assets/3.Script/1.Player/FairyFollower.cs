using UnityEngine;

namespace Player
{
    public class FairyFollower : MonoBehaviour
    {
        [Header("Target & Positioning")]
        [SerializeField] private Transform playerTarget;
        [SerializeField] private Vector3 followOffset = new Vector3(-1f, 1.5f, 0f); // 플레이어 어깨 너머
        
        [Header("PID Tuning (P: 당기는힘 / I: 오차보정 / D: 흔들림방지)")]
        [SerializeField] private float kp = 15f; 
        [SerializeField] private float ki = 0f;  
        [SerializeField] private float kd = 2f;  
        
        [Header("Collision Avoidance")]
        [SerializeField] private LayerMask obstacleLayer; // 벽 등 장애물 레이어 (인스펙터에서 Wall/Ground 할당)
        [SerializeField] private float fairyRadius = 0.3f; // 요정의 충돌 크기
        
        [Header("Hovering Effect")]
        [SerializeField] private float hoverAmplitude = 0.2f;
        [SerializeField] private float hoverFrequency = 3f;

        [Header("Fairy Light (낭만 조명)")]
        [SerializeField] private Light fairyLight;
        [SerializeField] private Color normalColor = new Color(0.2f, 0.8f, 1f); // 요정색 (시안)
        [SerializeField] private Color warningColor = Color.red;
        [SerializeField] private float normalIntensity = 2f;
        [SerializeField] private float warningIntensity = 5f;
        [SerializeField] private float lightRange = 6f;

        private PID _pidX = new PID();
        private PID _pidY = new PID();
        private PID _pidZ = new PID();

        private Vector3 _velocity;
        private PlayerHealth _playerHealth;

        private void Start()
        {
            // 인스펙터에 타겟을 미리 넣어둔 경우 대응
            if (playerTarget != null && _playerHealth == null)
            {
                _playerHealth = playerTarget.GetComponent<PlayerHealth>();
            }

            // 요정 조명 자동 세팅
            if (fairyLight == null)
            {
                fairyLight = GetComponentInChildren<Light>();
                if (fairyLight == null)
                {
                    GameObject lightObj = new GameObject("FairyGlow");
                    lightObj.transform.SetParent(transform);
                    lightObj.transform.localPosition = Vector3.zero;
                    fairyLight = lightObj.AddComponent<Light>();
                }
            }
            
            fairyLight.type = LightType.Point;
            fairyLight.range = lightRange;
            fairyLight.renderMode = LightRenderMode.ForcePixel; 
            fairyLight.color = normalColor;
            fairyLight.intensity = normalIntensity;
        }

        public void SetTarget(Transform target)
        {
            playerTarget = target;
            if (playerTarget != null)
            {
                _playerHealth = playerTarget.GetComponent<PlayerHealth>();
            }
        }

        private void FixedUpdate()
        {
            if (playerTarget == null) return;

            float dt = Time.fixedDeltaTime;

            // 1. 목표 위치 계산 (오프셋 + 위아래 둥둥 효과)
            Vector3 targetPos = playerTarget.position + followOffset;
            targetPos.y += Mathf.Sin(Time.time * hoverFrequency) * hoverAmplitude;

            // [Fix] 1차 방어막: 플레이어 -> 요정 목표 지점 사이의 벽 검사!
            Vector3 dirToTarget = targetPos - playerTarget.position;
            float distToTarget = dirToTarget.magnitude;

            if (Physics.SphereCast(playerTarget.position, fairyRadius, dirToTarget.normalized, out RaycastHit hit, distToTarget, obstacleLayer))
            {
                // 타겟 위치 자체를 벽 안쪽으로 끄집어냄
                targetPos = hit.point + (hit.normal * fairyRadius * 1.5f);
                // [Fix] 여기도 똑같이 sqrMagnitude! (0.5 * 0.5 = 0.25)
                if ((playerTarget.position - targetPos).sqrMagnitude < 0.25f) targetPos += Vector3.up * 0.5f;
            }

            Vector3 currentPos = transform.position;

            // 2. 가속도 연산 (PID)
            float accX = _pidX.GetOutput(targetPos.x - currentPos.x, dt, kp, ki, kd);
            float accY = _pidY.GetOutput(targetPos.y - currentPos.y, dt, kp, ki, kd);
            float accZ = _pidZ.GetOutput(targetPos.z - currentPos.z, dt, kp, ki, kd);
            Vector3 acceleration = new Vector3(accX, accY, accZ);

            _velocity += acceleration * dt;
            Vector3 nextPos = transform.position + (_velocity * dt);

            // [Fix] 2차 절대 방어막! 요정이 벽으로 날아가는 '궤적' 자체를 막아버림 (강제 밀어내기)
            Vector3 currentToNext = nextPos - currentPos;
            if (Physics.SphereCast(currentPos, fairyRadius, currentToNext.normalized, out RaycastHit hit2, currentToNext.magnitude, obstacleLayer))
            {
                // 벽에 부딪히기 직전이면 튕겨냄 (속도 반전) 및 위치 고정
                nextPos = hit2.point + (hit2.normal * fairyRadius * 1.1f);
                _velocity = Vector3.Reflect(_velocity, hit2.normal) * 0.5f; // 너무 팅기지 않게 감쇠
            }

            transform.position = nextPos;

            // 4. 시선 처리 (2.5D 횡스크롤 문제 해결!)
            // 앞뒤(X축 깊이)로 이동할 때 요정이 화면 바깥을 쳐다봐서 종잇장처럼 얇아지는 현상 방지.
            // 플레이어처럼 무조건 좌우(Z축)만 쳐다보도록 고정!
            if (Mathf.Abs(_velocity.z) > 0.05f)
            {
                float zDir = _velocity.z > 0f ? 1f : -1f;
                Vector3 lookDir = new Vector3(0, 0, zDir);
                
                Quaternion targetRot = Quaternion.LookRotation(lookDir);
                
                // 뽀너스: 빠르게 날아갈 때 앞으로 살짝 몸을 기울이는(숙이는) 디테일!
                float speed = Mathf.Abs(_velocity.z) + Mathf.Abs(_velocity.y);
                float tiltAngle = Mathf.Clamp(speed * 1.5f, 0f, 40f); 
                targetRot *= Quaternion.Euler(tiltAngle, 0, 0);

                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, dt * 12f);
            }

            // 5. 조명 시스템 업데이트 (체력 감지)
            if (fairyLight != null && _playerHealth != null)
            {
                if (_playerHealth.CurrentHealth == 1)
                {
                    // 딸피(체력 1)일 때: 삐용삐용 경고등 효과 (PingPong)
                    float t = Mathf.PingPong(Time.time * 6f, 1f);
                    fairyLight.color = Color.Lerp(warningColor, new Color(warningColor.r * 0.2f, 0, 0), t);
                    fairyLight.intensity = Mathf.Lerp(warningIntensity, warningIntensity * 0.3f, t);
                }
                else
                {
                    // 평상시: 파란색으로 은은하게
                    fairyLight.color = Color.Lerp(fairyLight.color, normalColor, dt * 5f);
                    fairyLight.intensity = Mathf.Lerp(fairyLight.intensity, normalIntensity, dt * 5f);
                }
            }
        }
    }
}
