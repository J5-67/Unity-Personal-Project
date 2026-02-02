using UnityEngine;

// [유니] 플레이어가 떨어지거나 닿으면 데미지를 입고 복귀하는 위험 구역! ☠️
public class DeadZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (other.TryGetComponent(out PlayerHealth health))
            {
                // 데드존 진입 처리 (데미지 + 복귀)
                health.OnDeadZoneEnter();
            }
        }
    }
}
