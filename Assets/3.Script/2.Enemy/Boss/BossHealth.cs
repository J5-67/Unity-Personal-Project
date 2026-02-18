using UnityEngine;
using UnityEngine.UI;

// 보스 체력 관리 및 피격 로직
// [ToDo] 나중에는 BaseEnemy나 IDamageable 인터페이스로 통합 가능
public class BossHealth : MonoBehaviour
{
    [Header("❤️ Health Settings")]
    [SerializeField] private float maxHealth = 1000f;
    [SerializeField] private float currentHealth;

    [Header("📊 UI Settings")]
    [SerializeField] private Slider hpSlider; // 보스 체력바 (World Space or Screen Overlay)

    public bool IsDead => currentHealth <= 0;

    private void Awake()
    {
        currentHealth = maxHealth;
        UpdateUI();
    }

    public void TakeDamage(float amount)
    {
        if (IsDead) return;

        currentHealth -= amount;
        
        // 체력 제한 (0 ~ Max)
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        // UI 갱신
        UpdateUI();

        // 사망 체크
        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            // [Effect] 피격 효과 (반짝거림 등) - 나중에 추가
            // Debug.Log($"Boss took {amount} damage. Current HP: {currentHealth}");
        }
    }

    private void UpdateUI()
    {
        if (hpSlider != null)
        {
            hpSlider.value = currentHealth / maxHealth;
        }
    }

    private void Die()
    {
        // [Effect] 대폭발, 슬로우 모션 등 연출
        Debug.Log("Boss Destroyed!");
        
        // 임시로 비활성화 (나중엔 파괴 연출 후 삭제)
        gameObject.SetActive(false);
    }
}
