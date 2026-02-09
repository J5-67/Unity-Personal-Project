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

            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            // 1. Speed Lines (Particle)
            if (speedLines != null)
            {
                var emission = speedLines.emission;
                emission.enabled = _isEffectActive;
            }

            // 2. Ghost Trail
            if (_isEffectActive && ghostTrail != null)
            {
                _ghostTimer += Time.deltaTime;
                if (_ghostTimer >= ghostInterval)
                {
                    ghostTrail.ShowGhost();
                    _ghostTimer = 0f;
                }
            }
        }
    }
}
