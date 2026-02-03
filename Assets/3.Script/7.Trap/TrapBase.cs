using UnityEngine;

namespace Trap
{
    // [유니] 모든 함정의 조상님! 
    // 플레이어와 부딪히면 데미지를 주는 역할만 충실히 수행함.
    public class TrapBase : MonoBehaviour
    {
        [Header("Trap Settings")]
        [SerializeField] private int damage = 1;         // 데미지 양
        [SerializeField] private float knockbackForce = 10f; // 넉백 힘

        protected virtual void OnTriggerEnter(Collider other)
        {
            // 플레이어인지 확인! (Player 태그 또는 컴포넌트 체크)
            if (other.CompareTag("Player"))
            {
                // PlayerHealth 컴포넌트가 있는지 확인
                if (other.TryGetComponent(out PlayerHealth health))
                {
                    // 데미지 입히기!
                    // [옵션] 넉백 방향 계산 (함정 중심 -> 플레이어)
                    Vector3 knockbackDir = (other.transform.position - transform.position).normalized;
                    
                    // 데미지 함수 호출 (넉백 파라미터가 있다면 같이 전달)
                    health.TakeDamage(damage); 
                   
                    // [유니] 만약 PlayerHealth에 넉백 기능이 따로 없다면, Rigidbody를 직접 밀어주자!
                    if (other.TryGetComponent(out Rigidbody rb))
                    {
                        rb.linearVelocity = Vector3.zero; // 기존 속도 초기화 (확실하게 밀려나도록)
                        rb.AddForce(knockbackDir * knockbackForce, ForceMode.Impulse);
                    }
                    
                    Debug.Log($"[Trap] 으악! {gameObject.name}에 찔렸다! 🩸");
                }
            }
        }
    }
}
