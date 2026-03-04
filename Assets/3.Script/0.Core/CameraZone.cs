using UnityEngine;
using Unity.Cinemachine;

namespace Core
{
    [RequireComponent(typeof(Collider))]
    public class CameraZone : MonoBehaviour
    {
        [Header("🌟 카메라 스무스 전환 (방마다 카메라 배치)")]
        [SerializeField] private CinemachineCamera roomCamera;

        private void Awake()
        {

            Collider triggerCollider = GetComponent<Collider>();
            if (triggerCollider != null) triggerCollider.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {

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

                Gizmos.matrix = transform.localToWorldMatrix;

                Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
                Gizmos.DrawCube(col.center, col.size);

                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(col.center, col.size);
            }
        }
#endif
    }
}
