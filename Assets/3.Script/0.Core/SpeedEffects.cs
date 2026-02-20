using UnityEngine;

namespace Core
{
    public class SpeedEffects : MonoBehaviour
    {
        [Header("⚡ References")]
        [SerializeField] private Rigidbody targetRb;
        [SerializeField] private GhostTrail ghostTrail;
        [SerializeField] private ParticleSystem speedLines;

        [Header("⚙️ Settings")]
        [SerializeField] private float activationSpeed = 15f; // 이 속도 이상일 때 이펙트 발동
        [SerializeField] private float ghostInterval = 0.1f;  // 잔상 생성 간격
        
        private float _ghostTimer;
        private bool _isEffectActive;

        private void Start()
        {
            if (targetRb == null) targetRb = GetComponentInParent<Rigidbody>();
            if (ghostTrail == null) ghostTrail = GetComponentInChildren<GhostTrail>();
            
            // Speed Lines 파티클은 처음에 꺼둠
            if (speedLines != null)
            {
                var emission = speedLines.emission;
                emission.enabled = false;
            }
        }

        private void Update()
        {
            if (targetRb == null) return;

            float speed = targetRb.linearVelocity.magnitude;
            
            // 속도가 기준치를 넘으면 이펙트 활성화
            if (speed >= activationSpeed)
            {
                _isEffectActive = true;
            }
            else
            {
                // 히스테리시스: 속도가 조금 줄어들어도 바로 꺼지진 않게 (Optional)
                if (speed < activationSpeed * 0.8f) 
                {
                    _isEffectActive = false;
                }
            }

            // [New] 다이나믹 모션 블러 (속도에 비례해서 증가!)
            float maxSpeed = 40f; // 최대 속도 (대시 스피드 등 감안)
            // 현재 속도 비율 (0.0 ~ 1.0)
            float speedRatio = Mathf.Clamp01((speed - (activationSpeed * 0.5f)) / (maxSpeed - (activationSpeed * 0.5f))); 

            UpdateVisuals(speedRatio);

            if (Core.PostProcessManager.Instance != null)
            {
                // 모션 블러 강도 부드럽게 적용 (0 ~ 1.5 범위 내)
                Core.PostProcessManager.Instance.SetMotionBlur(speedRatio * 1.5f); // URP Motion Blur Intensity
            }

            // 카메라 시야각(FOV)도 속도에 비례해서 늘려주기! (최대 FOV + 20)
            if (Core.CameraEffectManager.Instance != null)
            {
                // 속도가 느릴 땐 0, 빠를 땐 20까지 FOV가 쭈~욱 늘어남!
                Core.CameraEffectManager.Instance.SetVelocityFOV(speedRatio * 20f);
            }
        }

        private void UpdateVisuals(float speedRatio)
        {
            // 1. Speed Lines (Particle) 다이내믹 튜닝 복구!
            if (speedLines != null)
            {
                var emission = speedLines.emission;
                
                if (_isEffectActive)
                {
                    emission.enabled = true;
                }
                else
                {
                    // 서서히 끄기 위해 Emission만 끔 (남아있는 파티클은 자연스럽게 사라짐)
                    emission.enabled = false;
                }
            }

            // 2. Ghost Trail 최적화 적용
            if (_isEffectActive && ghostTrail != null)
            {
                // [Fix] 속도가 빠를수록 잔상 간격을 비례해서 촘촘하게 남김 (대시할 때 훨씬 멋짐!)
                float currentInterval = Mathf.Lerp(ghostInterval, ghostInterval * 0.2f, speedRatio);
                
                _ghostTimer += Time.deltaTime;
                if (_ghostTimer >= currentInterval)
                {
                    ghostTrail.ShowGhost();
                    // [최적화 방어] 오버슈팅 방지를 위해 남은 시간 처리
                    _ghostTimer -= currentInterval; 
                    if (_ghostTimer > currentInterval) _ghostTimer = 0f;
                }
            }
        }
    }
}
