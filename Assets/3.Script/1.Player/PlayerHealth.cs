using UnityEngine;
using Core;

public class PlayerHealth : MonoBehaviour
{
    [Header("❤️ Health Settings")]
    [SerializeField] private int maxHealth = 4;
    [SerializeField] private int currentHealth;

    [Header("🎨 UI")]
    [SerializeField] private UI.HealthUI healthUI;

    [Header("📍 Checkpoint")]
    [SerializeField] private Vector3 lastCheckpointPos;

    [Header("🛡️ Invincibility")]
    [SerializeField] private float invincibilityDuration = 1.0f;
    [SerializeField] private float blinkInterval = 0.1f;
    [SerializeField] private Renderer playerRenderer;

    private bool _isInvincible = false;
    private bool _isDashInvincible = false;
    private bool _isDead = false;
    public bool IsInvincible => _isInvincible || _isDashInvincible;
    public bool IsDead => _isDead;
    public int CurrentHealth => currentHealth;

    public event System.Action OnTakeDamageEvent;
    public event System.Action OnDieEvent;
    public event System.Action OnRespawnEvent;


    private void Start()
    {
        currentHealth = maxHealth;
        
        // 🎯 오빠! 저장된 데이터가 있으면 그 위치를 체크포인트로 쓸게! 🥰
        if (Core.Data.DataManager.Instance != null && Core.Data.DataManager.Instance.CurrentData.hasSavedPosition)
        {
            lastCheckpointPos = Core.Data.DataManager.Instance.CurrentData.lastCheckpointPosition;
            transform.position = lastCheckpointPos;
        }
        else
        {
            lastCheckpointPos = transform.position;
        }

        healthUI?.UpdateHealth(currentHealth);
        UpdateLowHealthEffect();
    }

    private void UpdateLowHealthEffect()
    {
        if (PostProcessManager.Instance != null)
        {
            PostProcessManager.Instance.SetLowHealthEffect(currentHealth == 1);
        }
    }

    public void SetDashInvincible(bool state)
    {
        _isDashInvincible = state;
    }

    public void TakeDamage(int amount, bool applyGenericKnockback = true)
    {
        if (_isDead || _isInvincible || _isDashInvincible) return;

        if (TryGetComponent(out PlayerHook hook) && hook.IsHooking)
        {
            hook.StopHook();
            
            if (applyGenericKnockback && TryGetComponent(out Rigidbody rb))
            {
                rb.linearVelocity = Vector3.zero;
                rb.AddForce((Vector3.up * 7f) + (-transform.forward * 5f), ForceMode.Impulse);
            }
        }

        currentHealth -= amount;

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            OnTakeDamageEvent?.Invoke();

            healthUI?.UpdateHealth(currentHealth);
            UpdateLowHealthEffect();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.TriggerHitStop(0.1f);
                GameManager.Instance.TriggerCameraShake(1.5f);
                GameManager.Instance.TriggerBulletTime(0.5f, 0.1f, false);
            }
            if (PostProcessManager.Instance != null)
            {
                PostProcessManager.Instance.TriggerChromaticAberration(1.0f, 0.5f);
            }

            StartCoroutine(InvincibilityRoutine());
        }
    }

    private System.Collections.IEnumerator InvincibilityRoutine()
    {
        _isInvincible = true;

        float timer = 0f;
        while (timer < invincibilityDuration)
        {
            if (playerRenderer != null) playerRenderer.enabled = !playerRenderer.enabled;
            yield return new WaitForSeconds(blinkInterval);
            timer += blinkInterval;
        }

        if (playerRenderer != null) playerRenderer.enabled = true;
        _isInvincible = false;
    }

    public void OnDeadZoneEnter()
    {
        if (_isDead) return;
        
        currentHealth -= 1;
        healthUI?.UpdateHealth(currentHealth);
        UpdateLowHealthEffect();

        if (currentHealth > 0)
        {
            OnTakeDamageEvent?.Invoke();
            // 🎯 오빠! 기다리지 말고 바로 체크포인트로 보내줄게! 슝~ ✈️
            StartCoroutine(QuickRespawnRoutine());
            StartCoroutine(InvincibilityRoutine());
        }
        else
        {
             Die();
        }
    }

    private System.Collections.IEnumerator QuickRespawnRoutine()
    {
        _isDead = true; 
        
        if (TryGetComponent(out PlayerMovement pm)) pm.SetDeadState(true);

        // 🎯 앗! 여기서 1초나 기다리고 있었어 오빠! 바로 리스폰하게 고쳤어! 🥰
        Respawn(false);
        
        // 아주 잠깐만(0.1초) 멈췄다가 다시 움직이게 해줄게! (화면 전환 느낌)
        yield return new WaitForSeconds(0.1f);

        _isDead = false;
        if (TryGetComponent(out PlayerMovement pm2)) pm2.SetDeadState(false);
    }

    private System.Collections.IEnumerator RespawnDelayRoutine(bool isFullReset)
    {
        _isDead = true; 
        
        if (TryGetComponent(out PlayerMovement pm))
        {
            pm.SetDeadState(true);
        }

        // Wait for death/fall animation time
        yield return new WaitForSeconds(1.0f);

        Respawn(isFullReset);
        _isDead = false;
        
        if (TryGetComponent(out PlayerMovement pm2))
        {
            pm2.SetDeadState(false);
        }
    }

    private void Die()
    {
        if (_isDead) return;
        _isDead = true;
        
        OnDieEvent?.Invoke();

        if (playerRenderer != null) playerRenderer.enabled = true;
        StopAllCoroutines();
        // Keep them invincible during death animation
        _isInvincible = true; 

        StartCoroutine(DeathRoutine());
    }

    private System.Collections.IEnumerator DeathRoutine()
    {
        if (TryGetComponent(out PlayerMovement pm))
        {
            pm.SetDeadState(true);
        }

        // Add hitstop effect on death for impact
        if (GameManager.Instance != null)
        {
            GameManager.Instance.TriggerHitStop(0.2f);
            GameManager.Instance.TriggerCameraShake(2.0f);
        }

        // Wait to show player flying off from the trap's huge knockback!
        yield return new WaitForSeconds(1.5f);

        _isInvincible = false;
        Respawn(true);
        _isDead = false;
        
        if (TryGetComponent(out PlayerMovement pm2))
        {
            pm2.SetDeadState(false);
        }
    }

    private void Respawn(bool isFullReset)
    {
        if (TryGetComponent(out Rigidbody rb))
        {
            rb.linearVelocity = Vector3.zero;
        }

        if (TryGetComponent(out PlayerHook hook))
        {
            hook.StopHook();
        }

        transform.position = lastCheckpointPos;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (isFullReset)
        {
            currentHealth = maxHealth;
            healthUI?.UpdateHealth(currentHealth);
            UpdateLowHealthEffect();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.TriggerPlayerRespawn();
            }
        }
        
        OnRespawnEvent?.Invoke();
    }

    public void SetCheckpoint(Vector3 pos)
    {
        lastCheckpointPos = pos;
        
        // 🎯 오빠! 체크포인트 밟을 때마다 자동으로 저장해줄게! 걱정 마! 💾✨
        if (Core.Data.DataManager.Instance != null)
        {
            Core.Data.DataManager.Instance.SaveProgress(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, pos);
        }
    }

    public void SetCheckpointAtCurrent()
    {
        lastCheckpointPos = transform.position;
    }

    public void RestoreHealth(int amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        healthUI?.UpdateHealth(currentHealth);
        UpdateLowHealthEffect();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_isInvincible) return;

        if (collision.gameObject.TryGetComponent(out BaseEnemy enemy))
        {
            if (enemy.IsFrozen) return;

            if (TryGetComponent(out Rigidbody rb))
            {
                Vector3 contactPoint = collision.GetContact(0).point;
                Vector3 dir = (transform.position - contactPoint).normalized;
                dir += Vector3.up * 0.5f;
                rb.linearVelocity = Vector3.zero; // Clear velocity before bouncing
                rb.AddForce(dir.normalized * 10f, ForceMode.Impulse);
            }

            TakeDamage(1, false);
        }
    }
}
