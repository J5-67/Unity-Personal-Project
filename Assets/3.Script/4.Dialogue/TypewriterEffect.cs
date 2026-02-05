using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.Events;

namespace UI
{
    [RequireComponent(typeof(TMP_Text))]
    public class TypewriterEffect : MonoBehaviour
    {
        [Header("🖨️ Settings")]
        [SerializeField] private float typingSpeed = 0.05f;
        [SerializeField] private bool playOnAwake = false;

        [Header("🔊 Events")]
        public UnityEvent onType;
        public UnityEvent onComplete;

        [Header("🔊 Audio Settings")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip typingSound;
        [Tooltip("Frequency of sound (1 = every chat)")]
        [Range(1, 10)] [SerializeField] private int soundFrequency = 2; 
        [Range(0.5f, 2f)] [SerializeField] private float minPitch = 0.9f;
        [Range(0.5f, 2f)] [SerializeField] private float maxPitch = 1.1f;

        private TMP_Text _tmp;
        private Coroutine _typeRoutine;
        private bool _isSkipping = false;
        private WaitForSeconds _cachedWait;
        private AudioClip _defaultTypingSound;

        public bool IsTyping => _typeRoutine != null;

        private void Awake()
        {
            _tmp = GetComponent<TMP_Text>();

            if (audioSource == null)
            {
                TryGetComponent(out audioSource);
            }
            
            if (typingSound != null)
            {
                _defaultTypingSound = typingSound;
            }

            if (_tmp == null)
            {
                Debug.LogError($"Error: No TMP_Text on {gameObject.name}");
            }
        }

        private void Start()
        {
            if (playOnAwake && _tmp != null)
            {
                Run(_tmp.text, typingSpeed);
            }
        }

        public void SetTypingSound(AudioClip sound)
        {
            if (sound != null)
            {
                typingSound = sound;
            }
            else
            {
                typingSound = _defaultTypingSound;
            }
        }

        public void Run(string textToType, float speedOverride = -1f)
        {

            if (_tmp == null)
            {
                _tmp = GetComponent<TMP_Text>();
                if (_tmp == null)
                {
                    Debug.LogError($"Error: No TMP_Text on {gameObject.name}");
                    return;
                }
            }

            gameObject.SetActive(true);

            if (_typeRoutine != null) StopCoroutine(_typeRoutine);
            
            _tmp.text = textToType;
            _tmp.maxVisibleCharacters = 0;

            float speed = (speedOverride > 0) ? speedOverride : typingSpeed;

            _typeRoutine = StartCoroutine(TypeRoutine(speed));
        }

        public void Skip()
        {
            if (IsTyping)
            {
                _isSkipping = true;
            }
        }

        private IEnumerator TypeRoutine(float speed)
        {
            _isSkipping = false;
            
            _tmp.ForceMeshUpdate(); 
            TMP_TextInfo textInfo = _tmp.textInfo;
            int totalVisibleCharacters = textInfo.characterCount;

            WaitForSeconds waitDelay = null;
            if (Mathf.Approximately(speed, typingSpeed))
            {
                if (_cachedWait == null) _cachedWait = new WaitForSeconds(speed);
                waitDelay = _cachedWait;
            }
            else
            {
                waitDelay = new WaitForSeconds(speed);
            }

            for (int i = 0; i < totalVisibleCharacters; i++)
            {
                if (_isSkipping)
                {
                    _tmp.maxVisibleCharacters = totalVisibleCharacters;
                    break; 
                }

                _tmp.maxVisibleCharacters = i + 1;

                if (IsVisibleCharacter(i) || !char.IsWhiteSpace(textInfo.characterInfo[i].character))
                {
                    if (i % soundFrequency == 0)
                    {
                         PlayTypingSound();
                    }
                    onType?.Invoke();
                }

                yield return waitDelay;
            }

            _tmp.maxVisibleCharacters = totalVisibleCharacters;
            _typeRoutine = null;
            _isSkipping = false;
            
            onComplete?.Invoke();
        }

        private bool IsVisibleCharacter(int index)
        {
            if (_tmp.textInfo == null || index >= _tmp.textInfo.characterInfo.Length) return false;
            return _tmp.textInfo.characterInfo[index].isVisible;
        }

        private void PlayTypingSound()
        {
            if (audioSource == null) 
            {
                return;
            }
            if (typingSound == null) 
            {
                return;
            }

            audioSource.pitch = Random.Range(minPitch, maxPitch);
            audioSource.PlayOneShot(typingSound);
        }

        private void OnEnable()
        {
             if (FindObjectOfType<AudioListener>() == null)
             {
                 Debug.LogError("Error: No AudioListener in scene.");
             }
        }
    }
}
