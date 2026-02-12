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
}
