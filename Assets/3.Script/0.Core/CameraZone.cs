using UnityEngine;
using Unity.Cinemachine;

namespace Core
{
    [RequireComponent(typeof(Collider))]
    public class CameraZone : MonoBehaviour
    {
        [Header("🌟 카메라 스무스 전환 (방마다 카메라 배치)")]
        [Tooltip("방에 들어오면 활성화시킬 이 구역 전용 가상 카메라")]
        [SerializeField] private CinemachineCamera roomCamera;

        private void Awake()
        {
            // Trigger 체크용 콜라이더 (플레이어 진입 감지용)
            Collider triggerCollider = GetComponent<Collider>();
            if (triggerCollider != null) triggerCollider.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                // 우선순위를 확 올려서! 시네머신이 알아서 스~무스하게 카메라를 넘기게 만듦
                if (roomCamera != null)
                {
                    roomCamera.Priority = 100;
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                // 방에서 탈출할 테면, 다음 카메라로 바통 터치할 수 있게 우선순위 0으로 초기화!
                if (roomCamera != null)
                {
                    roomCamera.Priority = 0;
                }
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            BoxCollider col = GetComponent<BoxCollider>();
            if (col != null)
            {
                // 콜라이더의 변형(스케일, 회전)을 기즈모 매트릭스에 적용
                Gizmos.matrix = transform.localToWorldMatrix;

                // 존에 들어왔을 때 활성화되는 거니까 초록색으로 칠하자! (투명도 30%)
                Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
                Gizmos.DrawCube(col.center, col.size);
                
                // 테두리는 찐한 초록색으로!
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(col.center, col.size);
            }
        }
#endif
    }
}
