using UnityEngine;
using Core;

// [유니] 플레이어의 체력을 관리하고, 데드존/사망 처리를 담당하는 스크립트야! 💖
public class PlayerHealth : MonoBehaviour
{
    [Header("❤️ Health Settings")]
    [SerializeField] private int maxHealth = 4; // [유니] 체력 4칸으로 증가!
    [SerializeField] private int currentHealth;

    [Header("🎨 UI")]
    [SerializeField] private UI.HealthUI healthUI; // [유니] 체력 UI 연결!

    [Header("📍 Checkpoint")]
    [SerializeField] private Vector3 lastCheckpointPos; // 마지막 체크포인트 위치

    private void Start()
    {
        // 시작할 때 체력 꽉 채우기!
        currentHealth = maxHealth;
        // 체크포인트가 없으면 일단 시작 위치를 체크포인트로!
        lastCheckpointPos = transform.position;
        
        // [유니] 시작할 때 UI 싱크 맞추기 (근데 시작하자마자 뜨는 게 싫으면 빼도 됨)
        // 일단은 상태를 맞춰놔야 하니 업데이트는 해둘게!
        healthUI?.UpdateHealth(currentHealth);
    }

    [Header("🛡️ Invincibility")]
    [SerializeField] private float invincibilityDuration = 1.0f; // 무적 지속 시간
    [SerializeField] private float blinkInterval = 0.1f; // 깜빡이는 간격
    [SerializeField] private Renderer playerRenderer; // 깜빡일 렌더러 (MeshRenderer or SpriteRenderer)
    
    private bool _isInvincible = false;

    // [유니] 외부(적, 함정 등)에서 이 함수를 불러서 데미지를 줘!
    public void TakeDamage(int amount)
    {
        // [유니] 무적 상태라면 데미지 무시! 🛡️
        if (_isInvincible) return;

        currentHealth -= amount;

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            // [유니] 체력이 닳았으니 UI 보여주자!
            healthUI?.UpdateHealth(currentHealth);

            // [유니] 피격 시 타격감 추가! (히트 스탑 & 쉐이크)
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TriggerHitStop(0.1f); // 0.1초 멈춤!
                GameManager.Instance.TriggerCameraShake(1.5f); // 강하게 흔들기!
            }
            
            // [유니] 맞았으니 잠깐 무적! 깜빡깜빡 ✨
            StartCoroutine(InvincibilityRoutine());
        }
    }

    private System.Collections.IEnumerator InvincibilityRoutine()
    {
        _isInvincible = true;
        
        // 깜빡임 효과 시작
        float timer = 0f;
        while (timer < invincibilityDuration)
        {
            if (playerRenderer != null) playerRenderer.enabled = !playerRenderer.enabled; // 껐다 켰다
            yield return new WaitForSeconds(blinkInterval);
            timer += blinkInterval;
        }

        // 끝날 때 정리
        if (playerRenderer != null) playerRenderer.enabled = true; // 확실하게 켜두기!
        _isInvincible = false;
    }

    // [유니] 데드존(낙사, 가시 등)에 닿았을 때!
    public void OnDeadZoneEnter()
    {
        // 데드존은 무적 무시하고 죽어야 할까? 아니면 데드존도 무적 적용?
        // 보통 낙사는 무적이어도 죽어야 함! -> TakeDamage 대신 직접 처리하거나, 무적 체크를 여기서도 할지 결정 필요.
        // 일단 데드존은 '위치 리셋'이 목적이니 무적 무시하고 데미지 줌!
        
        currentHealth -= 1; // 강제 차감
        healthUI?.UpdateHealth(currentHealth); // UI도 갱신

        // 아직 살아있다면 마지막 체크포인트로 소환!
        if (currentHealth > 0)
        {
            Respawn(false); // false = 완전 사망은 아님 (적 부활 X)
            // 추락해서 돌아오면 잠깐 무적 주는 게 매너!
            StartCoroutine(InvincibilityRoutine());
        }
        else
        {
             Die();
        }
    }

    // [유니] 체력이 0이 되어 죽었을 때!
    private void Die()
    {
        // 죽으면 렌더러가 꺼져있을 수도 있으니 켜줘야 함
        if (playerRenderer != null) playerRenderer.enabled = true;
        StopAllCoroutines(); // 깜빡임 멈춰!
        _isInvincible = false;

        // TODO: 게임 오버 UI 띄우거나 연출 넣기? 
        // 지금은 바로 체크포인트에서 풀피로 부활시킬게!
        Respawn(true); // true = 완전 사망 후 부활 (적들도 리셋!)
    }
    
    // [유니] 부활 처리
    private void Respawn(bool isFullReset)
    {
        // 1. 위치 이동 (물리 속도 초기화 필수!)
        if (TryGetComponent(out Rigidbody rb))
        {
            rb.linearVelocity = Vector3.zero;
        }
        
        // [유니] 부활 시 훅이 연결되어 있으면 끊어줘야 해! (안 그러면 슝 날아감 🚀)
        if (TryGetComponent(out PlayerHook hook))
        {
            hook.StopHook();
        }

        transform.position = lastCheckpointPos;
        
        // [유니] 위치 이동 후에도 한 번 더 속도 0으로! (가끔 물리 잔상이 남을 수 있어서 확실하게!)
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero; // 회전력도 제거!
        }

        // 2. 완전 부활(사망 후 리스폰)이라면?
        if (isFullReset)
        {
            currentHealth = maxHealth; // 체력 풀회복
            healthUI?.UpdateHealth(currentHealth); // [유니] UI도 풀피로 갱신!
            
            // [중요] 죽였던 적들도 다시 살려내기!
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TriggerPlayerRespawn();
            }
        }
        
        // [유니] 부활했으니 깜빡임 무적 시간 같은 거 주면 좋아!
    }

    // [유니] 체크포인트에 닿으면 여기를 저장해!
    public void SetCheckpoint(Vector3 pos)
    {
        lastCheckpointPos = pos;
    }
    
    // [유니] 충돌 처리 (적에게 닿으면 데미지)
    private void OnCollisionEnter(Collision collision)
    {
        if (_isInvincible) return; // [유니] 무적이면 충돌도 무시? (물리 충돌은 일어나지만 데미지는 X)

        // [유니] Tag 체크는 만약 태그가 없으면 에러가 나니까, 안전하게 컴포넌트로 확인할게!
        // (Unity 6 스타일 + 안전성 확보!)
        if (collision.gameObject.TryGetComponent(out BaseEnemy enemy))
        {
            // 얼어있는 적은 안전한 발판! (데미지 X)
            if (enemy.IsFrozen) return;
            
            // 부딪히는 방향 체크해서 밟은 건지, 맞은 건지 구분할 수도 있어.
            // 일단은 닿으면 무조건 아야!
            TakeDamage(1);
            
            // 튕겨나가기 (넉백)
            if (TryGetComponent(out Rigidbody rb))
            {
                // 충돌 지점의 반대 방향으로 튕겨나가기
                Vector3 contactPoint = collision.GetContact(0).point;
                Vector3 dir = (transform.position - contactPoint).normalized;
                // 위쪽으로 살짝 더 띄워주기 (0.5f)
                dir += Vector3.up * 0.5f;
                rb.AddForce(dir.normalized * 10f, ForceMode.Impulse);
            }
        }
    }
}
