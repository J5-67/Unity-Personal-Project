using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class HealthUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image heartImage; // 스프라이트를 변경할 이미지 (UI)
        [SerializeField] private Sprite[] heartSprites; // 0칸 ~ 4칸 스프라이트 배열 (인덱스 = 체력)
        [SerializeField] private CanvasGroup canvasGroup; // 페이드 효과를 위한 그룹

        [Header("Settings")]
        [SerializeField] private float visibleDuration = 3.0f; // 보여지는 시간
        [SerializeField] private float fadeDuration = 0.5f;    // 사라지는 시간

        private Coroutine _fadeRoutine;

        private void Start()
        {
            // 시작 시에는 숨김 (Alpha 0)
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }
        }

        // [유니] 외부(PlayerHealth)에서 체력이 바뀔 때 호출!
        public void UpdateHealth(int currentHealth)
        {
            // 1. 스프라이트 교체
            // [유니] 체력 1 = 인덱스 0 / 체력 4 = 인덱스 3
            // 체력이 0이거나 음수가 되면 인덱스 0(1칸)을 보여주는 걸로 처리 (어차피 죽으면 리셋되니까!)
            int spriteIndex = Mathf.Clamp(currentHealth - 1, 0, heartSprites.Length - 1);
            
            if (heartImage != null && heartSprites.Length > 0)
            {
                heartImage.sprite = heartSprites[spriteIndex];
            }

            // 2. UI 보여주기 (코루틴 시작)
            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
            _fadeRoutine = StartCoroutine(ShowAndHideRoutine());
        }

        private IEnumerator ShowAndHideRoutine()
        {
            // [유니] 즉시 등장! (페이드 인 없이 빰!)
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f; 
            }

            // [유니] 일정 시간 유지
            yield return new WaitForSeconds(visibleDuration);

            // [유니] 서서히 사라지기 (Fade Out)
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
