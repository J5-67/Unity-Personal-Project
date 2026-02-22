using UnityEngine;

public class EnemyShield : MonoBehaviour
{
    [SerializeField] private float bounceForce = 20f;
    [SerializeField] private float blockDuration = 0.5f;

    public float BounceForce => bounceForce;
    
    // 이 메서드는 방패에 대시가 부딪혔을 때 호출될 예정
    public void OnBlock(Vector3 hitPoint)
    {
        // 튕겨나가는 효과음이나 VFX 추가 가능
        Debug.Log("🛡️ SHIELD BLOCKED DASH! 🛡️");
    }

    // [New] 해킹된 미사일 등에 맞았을 때 쉴드가 팩 깨지는 기능
    public void BreakShield()
    {
        // 쉴드 깨지는 이펙트 추가 (VFXManager에 넣거나 여기서 직접 처리)
        if (Core.VFXManager.Instance != null)
        {
            Core.VFXManager.Instance.PlayKamikazeExplosion(transform.position); // 임시로 폭발 이펙트 (또는 쉴드 파괴 전용 이펙트)
        }

        // 오브젝트 자체를 비활성화 (보통 리스폰을 위해 Destroy 대신 비활성화)
        gameObject.SetActive(false);
    }
}
