using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

namespace UI
{
    public class UIButtonVisual : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("Settings")]
        [SerializeField] private float hoverScale = 1.1f;
        [SerializeField] private float duration = 0.2f;
        [SerializeField] private AudioClip hoverSound;
        [SerializeField] private AudioClip clickSound;

        private Vector3 _originalScale;
        private AudioSource _audioSource;

        private void Awake()
        {
            _originalScale = transform.localScale;

            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.playOnAwake = false;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            transform.DOScale(_originalScale * hoverScale, duration)
                .SetEase(Ease.OutBack);

            if (hoverSound != null)
            {
                _audioSource.PlayOneShot(hoverSound);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            transform.DOScale(_originalScale, duration)
                .SetEase(Ease.OutQuad);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            transform.DOScale(_originalScale * 0.9f, 0.1f)
                .SetEase(Ease.OutQuad)
                .OnComplete(() => transform.DOScale(_originalScale, 0.1f));

            if (clickSound != null)
            {
                _audioSource.PlayOneShot(clickSound);
            }
        }
    }
}
