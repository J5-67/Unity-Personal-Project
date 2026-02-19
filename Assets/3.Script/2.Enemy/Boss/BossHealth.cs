using UnityEngine;
using UnityEngine.UI;
using System;

// 보스 체력 관리 및 피격 로직
// [Update] BossHealthUI와 연동하기 위해 이벤트 시스템 추가 (2026-02-19)
public class BossHealth : MonoBehaviour
{
    [Header("❤️ Health Settings")]
    [SerializeField] private float maxHealth = 1000f;
    [SerializeField] private float currentHealth;

    [Header("📊 UI Settings (Legacy)")]
    [SerializeField] private Slider hpSlider; // [Deprecated] 이제 BossHealthUI를 사용하세요!

    // [New] UI 업데이트를 위한 이벤트
    public event Action<float, float> OnHealthChanged; // current, max
    public event Action OnDamageTaken; // 피격 효과용 (Shake 등)
    public event Action OnDeath; // 사망 시

    public bool IsDead => currentHealth <= 0;
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    private void Start()
    {
        // 초기화 시 이벤트 발생 (UI 동기화)
        UpdateUI();
    }

    public void TakeDamage(float amount)
    {
        if (IsDead) return;

        currentHealth -= amount;
        
        // 체력 제한 (0 ~ Max)
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        // UI 갱신 및 효과
        UpdateUI();
        OnDamageTaken?.Invoke();

        // 사망 체크
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void UpdateUI()
    {
        // 1. 이벤트 발송 (New System)
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        // 2. 구형 슬라이더 지원 (Old System)
        if (hpSlider != null)
        {
            hpSlider.value = currentHealth / maxHealth;
        }
    }

    private void Die()
    {
        Debug.Log("Boss Destroyed!");
        OnDeath?.Invoke();
        
        // [Effect] 대폭발, 슬로우 모션 등 연출을 위해 바로 끄지 않고 코루틴 활용 가능
        // 지금은 일단 비활성화
        gameObject.SetActive(false);
    }
}
