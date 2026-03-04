using UnityEngine;
using UnityEngine.UI;
using System;

public class BossHealth : MonoBehaviour
{
    [Header("❤️ Health Settings")]
    [SerializeField] private float maxHealth = 1000f;
    [SerializeField] private float currentHealth;

    [Header("📊 UI Settings (Legacy)")]
    [SerializeField] private Slider hpSlider;

    public event Action<float, float> OnHealthChanged;
    public event Action OnDamageTaken;
    public event Action OnDeath;

    [Header("🎬 Intro Settings")]
    [SerializeField] private float introDropHeight = 30f;
    [SerializeField] private float introDuration = 3.0f;
    [SerializeField] private AudioClip landingSound;

    public bool IsDead => currentHealth <= 0;
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;

    private Vector3 _introTargetPos;
    private bool _hasIntroTarget = false;

    private void Awake()
    {
        currentHealth = maxHealth;

        if (!_hasIntroTarget)
        {
            _introTargetPos = transform.position;
            _hasIntroTarget = true;
        }
    }

    private void OnEnable()
    {

        StopAllCoroutines();

        currentHealth = maxHealth;
        UpdateUI();

        StartCoroutine(IntroSequenceRoutine());
    }

    private System.Collections.IEnumerator IntroSequenceRoutine()
    {

        Vector3 finalPos = _introTargetPos;
        Vector3 startPos = finalPos + Vector3.up * introDropHeight;
        transform.position = startPos;

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
        }

        float elapsed = 0f;
        while (elapsed < introDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / introDuration;

            t = 1f - Mathf.Pow(1f - t, 3f);

            transform.position = Vector3.Lerp(startPos, finalPos, t);
            yield return null;
        }

        transform.position = finalPos;

        if (Core.GameManager.Instance != null)
        {
            Core.GameManager.Instance.TriggerCameraShake(3.0f);
        }

        if (landingSound != null && Core.AudioManager.Instance != null)
        {
            Core.AudioManager.Instance.PlaySFX(landingSound);
        }

        if (Core.VFXManager.Instance != null)
        {

            Core.VFXManager.Instance.PlaySpawnEffect(transform.position - Vector3.up * 1f);
        }

        if (col != null) col.enabled = true;
        if (rb != null)
        {
            rb.isKinematic = false;

        }
    }

    private void Start()
    {

        UpdateUI();
    }

    public void TakeDamage(float amount)
    {
        if (IsDead) return;

        currentHealth -= amount;

        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        UpdateUI();
        OnDamageTaken?.Invoke();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void UpdateUI()
    {

        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (hpSlider != null)
        {
            hpSlider.value = currentHealth / maxHealth;
        }
    }

    private void Die()
    {

        StartCoroutine(DeathSequenceRoutine());
    }

    private System.Collections.IEnumerator DeathSequenceRoutine()
    {

        if (TryGetComponent(out Collider col)) col.enabled = false;

        if (Core.VFXManager.Instance != null)
        {
            Core.VFXManager.Instance.PlayBossExplosion(transform.position);
        }

        float duration = 2.0f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            if (UnityEngine.Random.value < 0.2f && Core.VFXManager.Instance != null)
            {

                Vector3 randOffset = UnityEngine.Random.insideUnitSphere * 3f;
                randOffset.y = Mathf.Abs(randOffset.y);
                Core.VFXManager.Instance.PlayHackExplosion(transform.position + randOffset);
            }
            yield return null;
        }

        if (Core.VFXManager.Instance != null)
        {
            Core.VFXManager.Instance.PlayBossExplosion(transform.position);
        }

        OnDeath?.Invoke();
        gameObject.SetActive(false);
    }
}
