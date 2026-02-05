using UnityEngine;

namespace Interaction
{
    public class Switch : MonoBehaviour, IInteractable
    {
        [Header("Connection")]
        [SerializeField] private Door targetDoor;
        
        [Header("Settings")]
        [SerializeField] private bool activateOnce = true;
        [SerializeField] private bool isActivated = false;

        [Header("Visuals (Optional)")]
        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Sprite onSprite;
        [SerializeField] private Sprite offSprite;
        [SerializeField] private Color onColor = Color.green;
        [SerializeField] private Color offColor = Color.red;

        private void Start()
        {
            UpdateVisual();
        }

        public void OnInteract(GameObject instigator)
        {
            if (activateOnce && isActivated) return;

            isActivated = !isActivated;
            
            if (activateOnce) isActivated = true;

            UpdateVisual();

            if (targetDoor != null)
            {
                if (activateOnce) targetDoor.Open();
                else targetDoor.Toggle();
            }

            Debug.Log($"[Switch] Click! State: {isActivated}");
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
