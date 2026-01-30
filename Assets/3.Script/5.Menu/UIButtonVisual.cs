using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening; // [유니] 도트윈(DOTween) 필수! ✨

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
            
            // [유니] 오디오 소스 찾거나 만들기
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.playOnAwake = false;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            // [유니] 마우스 올리면 커지게! 🎈
            transform.DOScale(_originalScale * hoverScale, duration)
                .SetEase(Ease.OutBack); // 띠요옹~ 하는 느낌

            if (hoverSound != null)
            {
                _audioSource.PlayOneShot(hoverSound);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            // [유니] 떼면 원래대로!
            transform.DOScale(_originalScale, duration)
                .SetEase(Ease.OutQuad);
        }

        public void OnPointerClick(PointerEventData eventData) // [유니] 이름 실수! Handler -> Click
        {
            // [유니] 클릭할 때 살짝 눌리는 느낌!
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
