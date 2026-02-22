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
    
    [Header("🎬 Intro Settings")]
    [SerializeField] private float introDropHeight = 30f; // 하늘 높은 곳에서부터
    [SerializeField] private float introDuration = 3.0f;  // 떨어지는 데 걸리는 시간
    [SerializeField] private AudioClip landingSound;      // 착지할 때 나는 '쿵!' 소리

    public bool IsDead => currentHealth <= 0;
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;

    private Vector3 _introTargetPos;
    private bool _hasIntroTarget = false;

    private void Awake()
    {
        currentHealth = maxHealth;
        
        // [Fix] 최초 Awake 시점의 위치가 진짜 착지해야 할 바닥 위치!
        if (!_hasIntroTarget)
        {
            _introTargetPos = transform.position;
            _hasIntroTarget = true;
        }
    }

    private void OnEnable()
    {
        // [Fix] 코루틴 중복 실행 방지
        StopAllCoroutines();
        
        // 켜질 때 다시 꽉 찬 체력으로 리셋 (재도전 대비)
        currentHealth = maxHealth;
        UpdateUI();

        // 등장 연출 코루틴 재생!
        StartCoroutine(IntroSequenceRoutine());
    }

    private System.Collections.IEnumerator IntroSequenceRoutine()
    {
        // 1. 착지 지점 기억하고 하늘 위로 순간이동! (Awake에서 저장한 위치 사용)
        Vector3 finalPos = _introTargetPos;
        Vector3 startPos = finalPos + Vector3.up * introDropHeight;
        transform.position = startPos;

        // 2. 내려오는 동안 꼼수(미리 때리기) 방지 = 무적 & 통과 & 물리 중단
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
        
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) 
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
        }

        // 3. 천천히 떨어지기 (처음엔 확 내려오다가 끝에서 사뿐하게!)
        float elapsed = 0f;
        while (elapsed < introDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / introDuration;
            
            // 간지나는 감속 하강 математика (Ease Out Cubic)
            t = 1f - Mathf.Pow(1f - t, 3f); 
            
            transform.position = Vector3.Lerp(startPos, finalPos, t);
            yield return null;
        }

        // 4. 오차 없이 정확한 위치에 꽂기
        transform.position = finalPos;

        // 5. 땅에 닿는 순간의 충격파 연출! (쾅!!)
        if (Core.GameManager.Instance != null)
        {
            Core.GameManager.Instance.TriggerCameraShake(3.0f); // 화면 덜덜덜!
        }

        if (landingSound != null && Core.AudioManager.Instance != null)
        {
            Core.AudioManager.Instance.PlaySFX(landingSound); // 쿵! 소리 쾅!
        }

        if (Core.VFXManager.Instance != null)
        {
            // 발밑에서 먼지나는 효과가 있으면 좋지만 아쉬운대로 Spawn 이펙트 활용
            Core.VFXManager.Instance.PlaySpawnEffect(transform.position - Vector3.up * 1f); 
        }

        // 6. 등장 끝! 이제부터 보스를 때릴 수 있게 콜라이더 등등 원복!
        if (col != null) col.enabled = true;
        if (rb != null)
        {
            rb.isKinematic = false; 
            // 중력 등 기본 물리 다시 적용
        }
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
        
        // 사망 처리 시 바로 끄지 않고 연출 코루틴 시작!
        StartCoroutine(DeathSequenceRoutine());
    }

    private System.Collections.IEnumerator DeathSequenceRoutine()
    {
        // 1. 코어 파괴 (일단 콜라이더부터 꺼서 더 이상 맞거나 부딪히지 않게 함)
        if (TryGetComponent(out Collider col)) col.enabled = false;
        
        // 2. 화면 전체 진동 등 강력한 첫 타격 이펙트
        if (Core.VFXManager.Instance != null)
        {
            Core.VFXManager.Instance.PlayBossExplosion(transform.position);
        }

        // 3. 다발성 폭발 연출 (무작위 위치에서 펑펑펑)
        float duration = 2.0f; // 화려하게 터지는 시간
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            
            // 0.1 ~ 0.2초 간격으로 작은 폭발 (기존 HackExplosion 재활용)
            if (UnityEngine.Random.value < 0.2f && Core.VFXManager.Instance != null) 
            {
                // 보스 중심에서 주변으로 살짝 흩어진 위치 계산
                Vector3 randOffset = UnityEngine.Random.insideUnitSphere * 3f;
                randOffset.y = Mathf.Abs(randOffset.y); // 땅속으로 안 들어가게
                Core.VFXManager.Instance.PlayHackExplosion(transform.position + randOffset);
            }
            yield return null;
        }

        // 4. 마지막 대폭발과 함께 사망 처리
        if (Core.VFXManager.Instance != null)
        {
            Core.VFXManager.Instance.PlayBossExplosion(transform.position);
        }

        // 5. 완벽한 죽음 (이벤트 발송 후 비활성화)
        OnDeath?.Invoke();
        gameObject.SetActive(false);
    }
}
