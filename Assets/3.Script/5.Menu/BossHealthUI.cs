using UnityEngine;
using UnityEngine.UI;
using System.Collections;

// 보스 체력바 전용 UI (Smooth Sliding & Shake Effect)
public class BossHealthUI : MonoBehaviour
{
    [Header("🔗 Link")]
    [SerializeField] private BossHealth bossHealth; // 인스펙터에서 할당하거나 자동 검색

    [Header("🎨 UI Components")]
    [SerializeField] private Slider hpSlider;    // 실제 체력 (앞쪽, 빨강 등)
    [SerializeField] private Slider easeSlider;  // 감소 효과 (뒤쪽, 흰색)
    [SerializeField] private CanvasGroup canvasGroup; 
    
    [Header("⚡ Effects")]
    [SerializeField] private float easeSpeed = 2f;
    [SerializeField] private float shakeAmount = 5f; // 흔들림 강도
    [SerializeField] private float shakeDuration = 0.2f;
    [SerializeField] private float flashDuration = 0.1f; // [New] 깜빡임 지속 시간
    [SerializeField] private Color flashColor = Color.white; // [New] 피격 시 잠깐 변할 색상

    private Coroutine _easeCoroutine;
    private Coroutine _flashCoroutine;
    private RectTransform _rectTransform;
    private Vector2 _originalPos;
    private Image _fillImage; // [New] 실제 색상을 바꿀 이미지
    private Color _originalColor; // 원래 색상 저장용

    private void Awake()
    {
        // Awake에서는 아무것도 하지 않음 (Start에서 결정)
    }

    private void Start()
    {
        // 1. Canvas Group 연결 (없으면 자동 검색 시도)
        if (canvasGroup == null) canvasGroup = GetComponentInChildren<CanvasGroup>();

        // 2. Shake 대상 설정: 무조건 CanvasGroup이 붙은 놈(Panel)을 흔든다!
        if (canvasGroup != null)
        {
            _rectTransform = canvasGroup.GetComponent<RectTransform>();
        }
        else
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        if (_rectTransform != null) _originalPos = _rectTransform.anchoredPosition;

        // [New] Fill Image 찾기 (HP Slider의 자식 중 Fill Area/Fill)
        if (hpSlider != null)
        {
            // 보통 Slider 구조: Slider -> Fill Area -> Fill (Image)
            var fills = hpSlider.GetComponentsInChildren<Image>();
            foreach (var img in fills)
            {
                // Background가 아닌 놈을 찾음 (보통 Fill은 이름이 Fill이거나 두번째 놈)
                if (img.gameObject != hpSlider.gameObject && img.transform.parent != hpSlider.transform)
                {
                    _fillImage = img;
                    _originalColor = _fillImage.color;
                    break;
                }
            }
        }

        // 보스가 할당되지 않았다면 자동 검색
        if (bossHealth?.gameObject.activeInHierarchy == false) bossHealth = null; // 죽은 놈 제외
        if (bossHealth == null)
        {
            bossHealth = FindAnyObjectByType<BossHealth>();
        }

        if (bossHealth != null)
        {
            ConnectBoss(bossHealth);
        }
        else
        {
            // 보스가 없으면 일단 숨김
            if(canvasGroup != null) canvasGroup.alpha = 0f;
        }
    }

    private void OnDestroy()
    {
        if (bossHealth != null)
        {
            bossHealth.OnHealthChanged -= UpdateUI;
            bossHealth.OnDamageTaken -= ShakeUI;
            bossHealth.OnDeath -= OnBossDead;
        }
    }

    public void ConnectBoss(BossHealth newBoss)
    {
        bossHealth = newBoss;
        bossHealth.OnHealthChanged += UpdateUI;
        bossHealth.OnDamageTaken += ShakeUI;
        bossHealth.OnDeath += OnBossDead;

        // UI 초기화
        UpdateUI(bossHealth.CurrentHealth, bossHealth.MaxHealth);
        
        // 초기화 시 이즈 슬라이더도 즉시 맞춤
        if (easeSlider != null && hpSlider != null) 
             easeSlider.value = hpSlider.value;
             
        if (canvasGroup != null) canvasGroup.alpha = 1f;
    }

    private void UpdateUI(float current, float max)
    {
        if (max <= 0) return; // Divide by zero 방지

        float ratio = current / max;

        // 1. 실제 체력바는 즉시 갱신
        if (hpSlider != null) hpSlider.value = ratio;

        // 2. 이즈(잔상) 체력바는 천천히 따라감
        if (easeSlider != null)
        {
            if (_easeCoroutine != null) StopCoroutine(_easeCoroutine);
            _easeCoroutine = StartCoroutine(EaseRoutine(ratio));
        }
    }

    private IEnumerator EaseRoutine(float targetValue)
    {
        if (easeSlider == null) yield break;

        while (Mathf.Abs(easeSlider.value - targetValue) > 0.001f)
        {
            easeSlider.value = Mathf.Lerp(easeSlider.value, targetValue, Time.deltaTime * easeSpeed);
            yield return null;
        }
        easeSlider.value = targetValue;
    }

    private void ShakeUI()
    {
        // 1. 흔들기
        StopCoroutine(nameof(ShakeRoutine));
        StartCoroutine(nameof(ShakeRoutine));

        // 2. 깜빡이기 [New]
        if (_fillImage != null)
        {
            if (_flashCoroutine != null) StopCoroutine(_flashCoroutine);
            _flashCoroutine = StartCoroutine(FlashRoutine());
        }
    }

    private IEnumerator FlashRoutine()
    {
        // 1. 흰색으로 번쩍!
        _fillImage.color = flashColor;
        
        // 2. 잠시 유지
        yield return new WaitForSeconds(0.05f);

        // 3. 원래 색으로 돌아오기 (Lerp로 부드럽게)
        float timer = 0f;
        while (timer < flashDuration)
        {
            _fillImage.color = Color.Lerp(flashColor, _originalColor, timer / flashDuration);
            timer += Time.deltaTime;
            yield return null;
        }
        _fillImage.color = _originalColor;
    }

    private IEnumerator ShakeRoutine()
    {
        if (_rectTransform == null) yield break;

        float timer = 0f;
        while (timer < shakeDuration)
        {
            Vector2 randomOffset = Random.insideUnitCircle * shakeAmount;
            _rectTransform.anchoredPosition = _originalPos + randomOffset;
            
            timer += Time.deltaTime;
            yield return null;
        }
        _rectTransform.anchoredPosition = _originalPos;
    }

    private void OnBossDead()
    {
        // 보스 사망 시 서서히 사라짐
        if (canvasGroup != null) StartCoroutine(FadeOut());
    }

    private IEnumerator FadeOut()
    {
        float timer = 0f;
        while (timer < 1f)
        {
            canvasGroup.alpha = 1f - timer;
            timer += Time.deltaTime;
            yield return null;
        }
        canvasGroup.alpha = 0f;
    }
}
