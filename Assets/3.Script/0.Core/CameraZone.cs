using UnityEngine;
using Unity.Cinemachine;
using System.Collections;

namespace Core
{
    [RequireComponent(typeof(Collider))]
    public class CameraZone : MonoBehaviour
    {
        [Header("Cinemachine Settings")]
        [SerializeField] private CinemachineConfiner3D confiner;
        [Tooltip("이 구역에 들어왔을 때 카메라를 가둘 투명 상자 (Trigger 콜라이더와 같아도 되고 다를 수도 있음!)")]
        [SerializeField] private Collider boundingVolume;

        [Header("Interpolation Setting")]
        [SerializeField] private float transitionDuration = 1.5f; // 보간에 걸리는 시간

        private Coroutine _transitionCoroutine;

        private void Awake()
        {
            // Trigger 체크용 콜라이더 (플레이어 진입 감지용)
            Collider triggerCollider = GetComponent<Collider>();
            if (triggerCollider != null) triggerCollider.isTrigger = true;

            // 안 넣어줬다면 자기 자신을 boundingVolume으로 씀!
            if (boundingVolume == null) boundingVolume = triggerCollider;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                if (confiner == null)
                {
                    CinemachineCamera vcam = FindAnyObjectByType<CinemachineCamera>();
                    if (vcam != null)
                    {
                        confiner = vcam.GetComponent<CinemachineConfiner3D>();
                    }
                }

                if (confiner != null && boundingVolume != null)
                {
                    // Confiner의 Bounding Volume 업데이트
                    confiner.BoundingVolume = boundingVolume;

                    // // (옵션) 부드러운 전환을 적용하고 싶다면 Coroutine으로 처리 가능.
                    // if (_transitionCoroutine != null) StopCoroutine(_transitionCoroutine);
                    // _transitionCoroutine = StartCoroutine(SmoothTransition());
                }
            }
        }
    }
}
