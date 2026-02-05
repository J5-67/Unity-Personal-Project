using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class HealthUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image heartImage;
        [SerializeField] private Sprite[] heartSprites;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Settings")]
        [SerializeField] private float visibleDuration = 3.0f;
        [SerializeField] private float fadeDuration = 0.5f;

        private Coroutine _fadeRoutine;

        private void Start()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }
        }

        public void UpdateHealth(int currentHealth)
        {
            int spriteIndex = Mathf.Clamp(currentHealth - 1, 0, heartSprites.Length - 1);
            
            if (heartImage != null && heartSprites.Length > 0)
            {
                heartImage.sprite = heartSprites[spriteIndex];
            }

            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
            _fadeRoutine = StartCoroutine(ShowAndHideRoutine());
        }

        private IEnumerator ShowAndHideRoutine()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f; 
            }

            yield return new WaitForSeconds(visibleDuration);

            float timer = 0f;
            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
                }
                yield return null;
            }

            if (canvasGroup != null) canvasGroup.alpha = 0f;
        }
    }
}
