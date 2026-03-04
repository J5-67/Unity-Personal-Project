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
        [SerializeField] private float activationSpeed = 15f;
        [SerializeField] private float ghostInterval = 0.1f;

        private float _ghostTimer;
        private bool _isEffectActive;

        private void Start()
        {
            if (targetRb == null) targetRb = GetComponentInParent<Rigidbody>();
            if (ghostTrail == null) ghostTrail = GetComponentInChildren<GhostTrail>();

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

            if (speed >= activationSpeed)
            {
                _isEffectActive = true;
            }
            else
            {

                if (speed < activationSpeed * 0.8f)
                {
                    _isEffectActive = false;
                }
            }

            float maxSpeed = 40f;

            float speedRatio = Mathf.Clamp01((speed - (activationSpeed * 0.5f)) / (maxSpeed - (activationSpeed * 0.5f)));

            UpdateVisuals(speedRatio);

            if (Core.PostProcessManager.Instance != null)
            {

                Core.PostProcessManager.Instance.SetMotionBlur(speedRatio * 1.5f);
            }

            if (Core.CameraEffectManager.Instance != null)
            {

                Core.CameraEffectManager.Instance.SetVelocityFOV(speedRatio * 20f);
            }
        }

        private void UpdateVisuals(float speedRatio)
        {

            if (speedLines != null)
            {
                var emission = speedLines.emission;

                if (_isEffectActive)
                {
                    emission.enabled = true;
                }
                else
                {

                    emission.enabled = false;
                }
            }

            if (_isEffectActive && ghostTrail != null)
            {

                float currentInterval = Mathf.Lerp(ghostInterval, ghostInterval * 0.2f, speedRatio);

                _ghostTimer += Time.deltaTime;
                if (_ghostTimer >= currentInterval)
                {
                    ghostTrail.ShowGhost();

                    _ghostTimer -= currentInterval;
                    if (_ghostTimer > currentInterval) _ghostTimer = 0f;
                }
            }
        }
    }
}
