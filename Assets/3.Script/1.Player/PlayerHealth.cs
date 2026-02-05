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

    private void Start()
    {
        currentHealth = maxHealth;
        lastCheckpointPos = transform.position;
        
        healthUI?.UpdateHealth(currentHealth);
    }

    public void TakeDamage(int amount)
    {
        if (_isInvincible) return;

        currentHealth -= amount;

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            healthUI?.UpdateHealth(currentHealth);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.TriggerHitStop(0.1f);
                GameManager.Instance.TriggerCameraShake(1.5f);
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
        currentHealth -= 1;
        healthUI?.UpdateHealth(currentHealth);

        if (currentHealth > 0)
        {
            Respawn(false);
            StartCoroutine(InvincibilityRoutine());
        }
        else
        {
             Die();
        }
    }

    private void Die()
    {
        if (playerRenderer != null) playerRenderer.enabled = true;
        StopAllCoroutines();
        _isInvincible = false;

        Respawn(true);
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
            
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TriggerPlayerRespawn();
            }
        }
    }

    public void SetCheckpoint(Vector3 pos)
    {
        lastCheckpointPos = pos;
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        if (_isInvincible) return;

        if (collision.gameObject.TryGetComponent(out BaseEnemy enemy))
        {
            if (enemy.IsFrozen) return;
            
            TakeDamage(1);
            
            if (TryGetComponent(out Rigidbody rb))
            {
                Vector3 contactPoint = collision.GetContact(0).point;
                Vector3 dir = (transform.position - contactPoint).normalized;
                dir += Vector3.up * 0.5f;
                rb.AddForce(dir.normalized * 10f, ForceMode.Impulse);
            }
        }
    }
}
