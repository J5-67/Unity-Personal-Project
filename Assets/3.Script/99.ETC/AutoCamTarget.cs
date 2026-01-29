using UnityEngine;
using Unity.Cinemachine; // Cinemachine 3.x Namespace

public class AutoCamTarget : MonoBehaviour
{
    private void OnEnable()
    {
        // 1. 약간의 딜레이 후 실행 (초기화 순서 문제 방지)
        StartCoroutine(FindPlayerRoutine());
    }

    private System.Collections.IEnumerator FindPlayerRoutine()
    {
        yield return null; // 1프레임 대기

        // 2. PlayerMovement 컴포넌트 찾기
        PlayerMovement playerScript = FindFirstObjectByType<PlayerMovement>();

        if (playerScript != null)
        {
            Transform playerTransform = playerScript.transform;

            // 3. 시네머신 카메라 컴포넌트 가져오기
            var cam = GetComponent<CinemachineCamera>();
            
            if (cam != null)
            {
                // 4. Follow 타겟 설정
                cam.Follow = playerTransform;
                
                // [CM 3.x] 일부 버전에서는 LookAt도 같이 설정해야 할 수 있음 (필요 시)
                // cam.LookAt = playerTransform; 

            }
        }
    }
}
