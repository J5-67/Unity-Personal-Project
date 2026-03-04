using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BossHealthUI : MonoBehaviour
{
    [Header("🔗 Link")]
    [SerializeField] private BossHealth bossHealth;

    [Header("🎨 UI Components")]
    [SerializeField] private Slider hpSlider;
    [SerializeField] private Slider easeSlider;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("⚡ Effects")]
    [SerializeField] private float easeSpeed = 2f;
    [SerializeField] private float shakeAmount = 5f;
    [SerializeField] private float shakeDuration = 0.2f;
    [SerializeField] private float flashDuration = 0.1f;
    [SerializeField] private Color flashColor = Color.white;

    private Coroutine _easeCoroutine;
    private Coroutine _flashCoroutine;
    private RectTransform _rectTransform;
    private Vector2 _originalPos;
    private Image _fillImage;
    private Color _originalColor;

    private void Awake()
    {

    }

    private void Start()
    {

        if (canvasGroup == null) canvasGroup = GetComponentInChildren<CanvasGroup>();

        if (canvasGroup != null)
        {
            _rectTransform = canvasGroup.GetComponent<RectTransform>();
        }
        else
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        if (_rectTransform != null) _originalPos = _rectTransform.anchoredPosition;

        if (hpSlider != null)
        {

            var fills = hpSlider.GetComponentsInChildren<Image>();
            foreach (var img in fills)
            {

                if (img.gameObject != hpSlider.gameObject && img.transform.parent != hpSlider.transform)
                {
                    _fillImage = img;
                    _originalColor = _fillImage.color;
                    break;
                }
            }
        }

        if (bossHealth?.gameObject.activeInHierarchy == false) bossHealth = null;
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

        UpdateUI(bossHealth.CurrentHealth, bossHealth.MaxHealth);

        if (easeSlider != null && hpSlider != null)
             easeSlider.value = hpSlider.value;

        if (canvasGroup != null) canvasGroup.alpha = 1f;
    }

    private void UpdateUI(float current, float max)
    {
        if (max <= 0) return;

        float ratio = current / max;

        if (hpSlider != null) hpSlider.value = ratio;

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

        StopCoroutine(nameof(ShakeRoutine));
        StartCoroutine(nameof(ShakeRoutine));

        if (_fillImage != null)
        {
            if (_flashCoroutine != null) StopCoroutine(_flashCoroutine);
            _flashCoroutine = StartCoroutine(FlashRoutine());
        }
    }

    private IEnumerator FlashRoutine()
    {

        _fillImage.color = flashColor;

        yield return new WaitForSeconds(0.05f);

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
