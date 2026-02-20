using System.Collections;
using UnityEngine;
using Unity.Cinemachine; 

namespace Core
{
    public class CameraEffectManager : MonoBehaviour
    {
        public static CameraEffectManager Instance { get; private set; }

        [Header("References")]
        [SerializeField] private CinemachineCamera virtualCamera;
        private Camera _mainCam;
        
        private float _defaultFOV;
        private Coroutine _punchRoutine;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            if (virtualCamera == null)
            {
                virtualCamera = FindAnyObjectByType<CinemachineCamera>();
            }

            _mainCam = Camera.main;

            if (virtualCamera != null)
            {
                _defaultFOV = virtualCamera.Lens.FieldOfView;
            }
            else if (_mainCam != null)
            {
                _defaultFOV = _mainCam.fieldOfView;
            }
        }

        private float _punchFOVOffset = 0f;
        private float _velocityFOVOffset = 0f;

        private void LateUpdate()
        {
            if (virtualCamera != null || _mainCam != null)
            {
                // [Fix] 대시할 때의 순간적인 FOV(Punch)와 속도에 비례하는 FOV(Velocity)를 합산해서 부드럽게 적용!
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

            // 늘리기 (20%)
            float expandDuration = duration * 0.2f;
            while (time < expandDuration)
            {
                time += Time.deltaTime;
                float t = time / expandDuration;
                t = t * (2 - t); // EaseOut
                
                _punchFOVOffset = Mathf.Lerp(0f, targetFOV, t);
                yield return null;
            }

            // 복구하기 (80%)
            time = 0f;
            float recoverDuration = duration * 0.8f;
            while (time < recoverDuration)
            {
                time += Time.deltaTime;
                float t = time / recoverDuration;
                t = t * t; // EaseIn

                _punchFOVOffset = Mathf.Lerp(targetFOV, 0f, t);
                yield return null;
            }

            _punchFOVOffset = 0f; // 확실하게 원복
            _punchRoutine = null;
        }

        private float GetCurrentFOV()
        {
            if (virtualCamera != null) return virtualCamera.Lens.FieldOfView;
            if (_mainCam != null) return _mainCam.fieldOfView;
            return 60f;
        }

        private void SetFOV(float fov)
        {
            if (virtualCamera != null)
            {
                var lens = virtualCamera.Lens;
                lens.FieldOfView = fov;
                virtualCamera.Lens = lens;
            }
            else if (_mainCam != null)
            {
                _mainCam.fieldOfView = fov;
            }
        }

        // [New] 속도에 비례해서 시야각(FOV)을 부드럽게 늘렸다 줄였다 하는 기능!
        public void SetVelocityFOV(float targetOffset, float smoothTime = 5f) 
        { 
            _velocityFOVOffset = Mathf.Lerp(_velocityFOVOffset, targetOffset, Time.deltaTime * smoothTime);
        }
    }
}
