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

        // LateUpdate 제거! (평소엔 건드리지 않음)

        public void PunchFOV(float amount, float duration)
        {
            if (_punchRoutine != null) StopCoroutine(_punchRoutine);
            _punchRoutine = StartCoroutine(PunchRoutine(amount, duration));
        }

        private IEnumerator PunchRoutine(float amount, float duration)
        {
            // 시작 시점의 FOV를 기준으로 잡음 (안전)
            float startFOV = GetCurrentFOV();
            float targetFOV = _defaultFOV + amount;

            float time = 0f;

            // 늘리기 (20%)
            float expandDuration = duration * 0.2f;
            while (time < expandDuration)
            {
                time += Time.deltaTime;
                float t = time / expandDuration;
                t = t * (2 - t); // EaseOut
                
                SetFOV(Mathf.Lerp(startFOV, targetFOV, t));
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

                SetFOV(Mathf.Lerp(targetFOV, _defaultFOV, t));
                yield return null;
            }

            SetFOV(_defaultFOV); // 확실하게 원복
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

        // Velocity 관련 메서드는 삭제 (안 쓰니까!)
        public void SetVelocityFOV(float targetOffset, float smoothTime = 5f) { }
    }
}
