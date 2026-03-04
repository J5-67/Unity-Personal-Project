using UnityEngine;

namespace Player
{
    public class FairyFollower : MonoBehaviour
    {
        [Header("Target & Positioning")]
        [SerializeField] private Transform playerTarget;
        [SerializeField] private Vector3 followOffset = new Vector3(-1f, 1.5f, 0f);

        [Header("PID Tuning (P: 당기는힘 / I: 오차보정 / D: 흔들림방지)")]
        [SerializeField] private float kp = 15f;
        [SerializeField] private float ki = 0f;
        [SerializeField] private float kd = 2f;

        [Header("Collision Avoidance")]
        [SerializeField] private LayerMask obstacleLayer;
        [SerializeField] private float fairyRadius = 0.3f;

        [Header("Hovering Effect")]
        [SerializeField] private float hoverAmplitude = 0.2f;
        [SerializeField] private float hoverFrequency = 3f;

        [Header("Fairy Light (낭만 조명)")]
        [SerializeField] private Light fairyLight;
        [SerializeField] private Color normalColor = new Color(0.2f, 0.8f, 1f);
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

            if (playerTarget != null && _playerHealth == null)
            {
                _playerHealth = playerTarget.GetComponent<PlayerHealth>();
            }

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

            Vector3 targetPos = playerTarget.position + followOffset;
            targetPos.y += Mathf.Sin(Time.time * hoverFrequency) * hoverAmplitude;

            Vector3 dirToTarget = targetPos - playerTarget.position;
            float distToTarget = dirToTarget.magnitude;

            if (Physics.SphereCast(playerTarget.position, fairyRadius, dirToTarget.normalized, out RaycastHit hit, distToTarget, obstacleLayer))
            {

                targetPos = hit.point + (hit.normal * fairyRadius * 1.5f);

                if ((playerTarget.position - targetPos).sqrMagnitude < 0.25f) targetPos += Vector3.up * 0.5f;
            }

            Vector3 currentPos = transform.position;

            float accX = _pidX.GetOutput(targetPos.x - currentPos.x, dt, kp, ki, kd);
            float accY = _pidY.GetOutput(targetPos.y - currentPos.y, dt, kp, ki, kd);
            float accZ = _pidZ.GetOutput(targetPos.z - currentPos.z, dt, kp, ki, kd);
            Vector3 acceleration = new Vector3(accX, accY, accZ);

            _velocity += acceleration * dt;
            Vector3 nextPos = transform.position + (_velocity * dt);

            Vector3 currentToNext = nextPos - currentPos;
            if (Physics.SphereCast(currentPos, fairyRadius, currentToNext.normalized, out RaycastHit hit2, currentToNext.magnitude, obstacleLayer))
            {

                nextPos = hit2.point + (hit2.normal * fairyRadius * 1.1f);
                _velocity = Vector3.Reflect(_velocity, hit2.normal) * 0.5f;
            }

            transform.position = nextPos;

            if (Mathf.Abs(_velocity.z) > 0.05f)
            {
                float zDir = _velocity.z > 0f ? 1f : -1f;
                Vector3 lookDir = new Vector3(0, 0, zDir);

                Quaternion targetRot = Quaternion.LookRotation(lookDir);

                float speed = Mathf.Abs(_velocity.z) + Mathf.Abs(_velocity.y);
                float tiltAngle = Mathf.Clamp(speed * 1.5f, 0f, 40f);
                targetRot *= Quaternion.Euler(tiltAngle, 0, 0);

                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, dt * 12f);
            }

            if (fairyLight != null && _playerHealth != null)
            {
                if (_playerHealth.CurrentHealth == 1)
                {

                    float t = Mathf.PingPong(Time.time * 6f, 1f);
                    fairyLight.color = Color.Lerp(warningColor, new Color(warningColor.r * 0.2f, 0, 0), t);
                    fairyLight.intensity = Mathf.Lerp(warningIntensity, warningIntensity * 0.3f, t);
                }
                else
                {

                    fairyLight.color = Color.Lerp(fairyLight.color, normalColor, dt * 5f);
                    fairyLight.intensity = Mathf.Lerp(fairyLight.intensity, normalIntensity, dt * 5f);
                }
            }
        }
    }
}
