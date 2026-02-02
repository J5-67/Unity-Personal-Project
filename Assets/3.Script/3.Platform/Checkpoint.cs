using UnityEngine;

// [유니] 플레이어가 닿으면 체크포인트가 저장되는 구역이야! 🚩
public class Checkpoint : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (other.TryGetComponent(out PlayerHealth health))
            {
                // 현재 위치(자신의 위치)를 체크포인트로 저장!
                health.SetCheckpoint(transform.position);
            }
        }
    }
}
