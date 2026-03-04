using UnityEngine;
using Unity.Cinemachine;

public class AutoCamTarget : MonoBehaviour
{
    private void OnEnable()
    {
        StartCoroutine(FindPlayerRoutine());
    }

    private System.Collections.IEnumerator FindPlayerRoutine()
    {
        yield return null;

        PlayerMovement playerScript = FindFirstObjectByType<PlayerMovement>();

        if (playerScript != null)
        {
            Transform playerTransform = playerScript.transform;

            var cam = GetComponent<CinemachineCamera>();

            if (cam != null)
            {
                cam.Follow = playerTransform;
            }
        }
    }
}
