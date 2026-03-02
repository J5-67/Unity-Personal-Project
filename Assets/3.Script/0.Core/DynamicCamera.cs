using UnityEngine;
using Unity.Cinemachine;

namespace Core
{
    public class DynamicCamera : MonoBehaviour
    {
        [Header("🎯 Target")]
        [SerializeField] private Transform playerTransform;
        [SerializeField] private PlayerAim playerAim;
        [SerializeField] private CinemachineCamera virtualCamera;

        [Header("👀 Camera Shift (Aim Based)")]
        [SerializeField] private bool enableAimShift = false;
        [SerializeField] private float shiftAmount = 2.0f; // 기본값 상향 (확실한 효과)
        [SerializeField] private float shiftSpeed = 5f;
        [SerializeField] private float maxShiftDistance = 8f;

        private Vector3 _currentLookOffset;
        private Vector3 _initialOffset;
        
        // Cinemachine Component
        private CinemachineFollow _cinemachineFollow;

        private void Start()
        {
            if (playerTransform == null) 
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    playerTransform = player.transform;
                    playerAim = player.GetComponent<PlayerAim>();
                }
            }
            
            if (playerAim == null && playerTransform != null) playerAim = playerTransform.GetComponent<PlayerAim>();
            if (virtualCamera == null) virtualCamera = FindAnyObjectByType<CinemachineCamera>();

            // CinemachineFollow 컴포넌트 가져오기 (오프셋 직접 제어용)
            if (virtualCamera != null)
            {
                _cinemachineFollow = virtualCamera.GetComponent<CinemachineFollow>();
                if (_cinemachineFollow != null)
                {
                    _initialOffset = _cinemachineFollow.FollowOffset;
                }
                else
                {
                    Debug.LogWarning("[DynamicCamera] CinemachineFollow component not found on Virtual Camera!");
                }
            }
        }

        // Mouse/Input 기반이므로 Update/LateUpdate가 적절 (FixedUpdate는 물리 전용)
        private void LateUpdate()
        {
            if (playerTransform == null || _cinemachineFollow == null) return;

            HandleAimShift();
        }

        private void HandleAimShift()
        {
            Vector3 targetShift = Vector3.zero;

            if (enableAimShift && playerAim != null)
            {
                Vector3 aimPos = playerAim.GetAimWorldPosition();
                Vector3 playerPos = playerTransform.position;
                
                // 플레이어 -> 조준점 벡터
                Vector3 aimDir = aimPos - playerPos;

                // 마우스 커서 쪽으로 이동
                targetShift = aimDir * shiftAmount;
                targetShift = Vector3.ClampMagnitude(targetShift, maxShiftDistance);
            }

            // 부드럽게 오프셋 변경
            _currentLookOffset = Vector3.Lerp(_currentLookOffset, targetShift, Time.deltaTime * shiftSpeed);

            // 기존 오프셋(예: X=10)에 쉬프트값(Y, Z)을 더해서 적용
            _cinemachineFollow.FollowOffset = _initialOffset + _currentLookOffset;
        }

        private void OnDrawGizmos()
        {
            if (playerTransform != null && virtualCamera != null)
            {
                Gizmos.color = Color.yellow;
                Vector3 targetPos = playerTransform.position + _currentLookOffset;
                Gizmos.DrawWireSphere(targetPos, 0.5f);
                Gizmos.DrawLine(playerTransform.position, targetPos);
            }
        }
    }
}
