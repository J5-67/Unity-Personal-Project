using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

namespace Core
{
    public class CameraEffectManager : MonoBehaviour
    {
        public static CameraEffectManager Instance { get; private set; }

        private Camera _mainCam;
        private Unity.Cinemachine.CinemachineBrain _brain;
        private Unity.Cinemachine.CinemachineCamera _currentVirtualCamera;

        private float _defaultFOV;
        private Coroutine _punchRoutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            _mainCam = Camera.main;
            if (_mainCam != null)
            {
                _brain = _mainCam.GetComponent<Unity.Cinemachine.CinemachineBrain>();
            }
        }

        private void UpdateActiveCamera()
        {
            if (_brain != null && _brain.ActiveVirtualCamera != null)
            {
                var newVcam = _brain.ActiveVirtualCamera as Unity.Cinemachine.CinemachineCamera;
                if (newVcam != null && newVcam != _currentVirtualCamera)
                {
                    if (_currentVirtualCamera != null)
                    {
                        var lens = _currentVirtualCamera.Lens;
                        lens.FieldOfView = _defaultFOV;
                        _currentVirtualCamera.Lens = lens;
                    }

                    _currentVirtualCamera = newVcam;
                    _defaultFOV = _currentVirtualCamera.Lens.FieldOfView;
                }
            }
            else if (_mainCam != null && _currentVirtualCamera == null && _defaultFOV == 0f)
            {
                _defaultFOV = _mainCam.fieldOfView;
            }
        }

        private float _punchFOVOffset = 0f;
        private float _velocityFOVOffset = 0f;

        private void LateUpdate()
        {
            UpdateActiveCamera();

            if (_currentVirtualCamera != null || _mainCam != null)
            {
                SetFOV(_defaultFOV + _punchFOVOffset + _velocityFOVOffset);
            }
        }

        public void PunchFOV(float amount, float duration)
        {
            if (_punchRoutine != null) StopCoroutine(_punchRoutine);
            _punchRoutine = StartCoroutine(PunchRoutine(amount, duration));
        }

        private IEnumerator PunchRoutine(float amount, float duration)
        {
            float targetFOV = amount;
            float time = 0f;

            float expandDuration = duration * 0.2f;
            while (time < expandDuration)
            {
                time += Time.unscaledDeltaTime;
                float t = time / expandDuration;
                t = t * (2 - t);

                _punchFOVOffset = Mathf.Lerp(0f, targetFOV, t);
                yield return null;
            }

            time = 0f;
            float recoverDuration = duration * 0.8f;
            while (time < recoverDuration)
            {
                time += Time.unscaledDeltaTime;
                float t = time / recoverDuration;
                t = t * t;

                _punchFOVOffset = Mathf.Lerp(targetFOV, 0f, t);
                yield return null;
            }

            _punchFOVOffset = 0f;
            _punchRoutine = null;
        }

        private float GetCurrentFOV()
        {
            if (_currentVirtualCamera != null) return _currentVirtualCamera.Lens.FieldOfView;
            if (_mainCam != null) return _mainCam.fieldOfView;
            return 60f;
        }

        private void SetFOV(float fov)
        {
            if (_currentVirtualCamera != null)
            {
                var lens = _currentVirtualCamera.Lens;
                lens.FieldOfView = fov;
                _currentVirtualCamera.Lens = lens;
            }
            else if (_mainCam != null)
            {
                _mainCam.fieldOfView = fov;
            }
        }

        public void SetVelocityFOV(float targetOffset, float smoothTime = 5f)
        {
            _velocityFOVOffset = Mathf.Lerp(_velocityFOVOffset, targetOffset, Time.unscaledDeltaTime * smoothTime);
        }

        private float _shakeTrauma = 0f;

        private void OnEnable()
        {
            Application.onBeforeRender += ApplyUnscaledShake;
        }

        private void OnDisable()
        {
            Application.onBeforeRender -= ApplyUnscaledShake;
        }

        public void AddUnscaledShake(float intensity)
        {
            _shakeTrauma = Mathf.Clamp01(_shakeTrauma + intensity * 0.5f);
        }

        [Header("🔥 Camera Shake")]
        [SerializeField, Tooltip("최대 회전 각도 (도)")] private float shakeAngleMax = 3f;
        [SerializeField, Tooltip("최대 위치 이동 (m)")] private float shakeOffsetMax = 0.2f;
        [SerializeField, Tooltip("흔들림 속도 (주파수)")] private float shakeSpeed = 40f;
        [SerializeField, Tooltip("진동이 줄어드는 속도")] private float shakeDecay = 2.5f;

        private void ApplyUnscaledShake()
        {
            if (_mainCam == null) return;
            
            if (_shakeTrauma > 0f)
            {
                float shakePow = _shakeTrauma * _shakeTrauma;
                
                float seed = Time.unscaledTime * shakeSpeed;
                float rotX = (Mathf.PerlinNoise(seed, 0f) - 0.5f) * 2f * shakeAngleMax * shakePow;
                float rotY = (Mathf.PerlinNoise(0f, seed) - 0.5f) * 2f * shakeAngleMax * shakePow;
                float rotZ = (Mathf.PerlinNoise(seed, seed) - 0.5f) * 2f * shakeAngleMax * shakePow;

                float posX = (Mathf.PerlinNoise(seed + 10f, 0f) - 0.5f) * 2f * shakeOffsetMax * shakePow;
                float posY = (Mathf.PerlinNoise(0f, seed + 10f) - 0.5f) * 2f * shakeOffsetMax * shakePow;

                _mainCam.transform.localRotation *= Quaternion.Euler(rotX, rotY, rotZ);
                _mainCam.transform.localPosition += _mainCam.transform.right * posX + _mainCam.transform.up * posY;

                _shakeTrauma -= Time.unscaledDeltaTime * shakeDecay; 
                if (_shakeTrauma < 0f) _shakeTrauma = 0f;
            }
        }
    }
}
