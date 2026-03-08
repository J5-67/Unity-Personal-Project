using UnityEngine;

namespace Interaction
{
    public class HealingCheckpoint : MonoBehaviour, IInteractable
    {
        [Header("Settings")]
        [SerializeField] private bool activateOnce = true;
        [SerializeField] private bool isActivated = false;
        [SerializeField] private int healAmount = 99; // 기본값은 전체 회복급!

        [Header("Visuals")]
        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Sprite onSprite;
        [SerializeField] private Sprite offSprite;
        [SerializeField] private Color onColor = Color.cyan;
        [SerializeField] private Color offColor = Color.gray;

        [Header("Effects")]
        [SerializeField] private ParticleSystem activationParticle;

        private void Start()
        {
            UpdateVisual();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                OnInteract(other.gameObject);
            }
        }

        public void OnInteract(GameObject instigator)
        {
            if (activateOnce && isActivated) return;

            // 🎯 플레이어 체력 회복 및 체크포인트 설정!
            if (instigator.TryGetComponent(out PlayerHealth health))
            {
                health.RestoreHealth(healAmount);
                health.SetCheckpointAtCurrent();
                
                isActivated = true;
                UpdateVisual();

                if (activationParticle != null)
                {
                    activationParticle.Play();
                }

                // 🎯 GameManager를 통해 시각적인 알림을 주면 더 좋아 오빠! ✨
                if (Core.GameManager.Instance != null)
                {
                    Core.GameManager.Instance.TriggerCameraShake(0.5f);
                }
            }
        }

        private void UpdateVisual()
        {
            Color targetColor = isActivated ? onColor : offColor;

            if (meshRenderer != null)
            {
                meshRenderer.material.color = targetColor;
            }

            if (spriteRenderer != null)
            {
                if (onSprite != null && offSprite != null)
                {
                    spriteRenderer.sprite = isActivated ? onSprite : offSprite;
                }
                else
                {
                    spriteRenderer.color = targetColor;
                }
            }
        }
    }
}
