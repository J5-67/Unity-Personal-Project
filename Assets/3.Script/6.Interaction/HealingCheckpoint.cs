using UnityEngine;
namespace Interaction
{
    public class HealingCheckpoint : MonoBehaviour, IInteractable
    {
        [Header("Settings")]
        [SerializeField] private bool activateOnce = true;
        [SerializeField] private bool isActivated = false;
        [SerializeField] private int healAmount = 99;
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
