using System.Collections;
using UnityEngine;
using TMPro; // TMP 기능 사용 필수!
using UnityEngine.Events;

namespace UI
{
    [RequireComponent(typeof(TMP_Text))]
    public class TypewriterEffect : MonoBehaviour
    {
        [Header("🖨️ Settings")]
        [SerializeField] private float typingSpeed = 0.05f; // 글자당 출력 시간
        [SerializeField] private bool playOnAwake = false;

        [Header("🔊 Events")]
        public UnityEvent onType;     // 글자가 찍힐 때 (타자 소리용)
        public UnityEvent onComplete; // 출력이 끝났을 때

        [Header("🔊 Audio Settings")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip typingSound;
        [Tooltip("몇 글자마다 소리를 낼지 설정 (1 = 매 글자마다)")]
        [Range(1, 10)] [SerializeField] private int soundFrequency = 2; 
        [Range(0.5f, 2f)] [SerializeField] private float minPitch = 0.9f;
        [Range(0.5f, 2f)] [SerializeField] private float maxPitch = 1.1f;

        private TMP_Text _tmp;
        private Coroutine _typeRoutine;
        private bool _isSkipping = false;
        private WaitForSeconds _cachedWait; // [유니] GC(가비지 컬렉션) 방지를 위해 대기 시간 캐싱!
        private AudioClip _defaultTypingSound; // [유니] 기본 소리 저장용

        public bool IsTyping => _typeRoutine != null; // 현재 타이핑 중인지 확인

        private void Awake()
        {
            // [유니] GetComponent는 무거운 연산이니까 Awake에서 한 번만!
            _tmp = GetComponent<TMP_Text>();

            if (audioSource == null)
            {
                TryGetComponent(out audioSource);
            }
            
            // [유니] 처음에 설정된 소리를 기본값으로 저장!
            if (typingSound != null)
            {
                _defaultTypingSound = typingSound;
            }

            // [유니] 안전장치! 텍스트 컴포넌트가 없으면 알려주기
            if (_tmp == null)
            {
                Debug.LogError($"[유니] 🚨 {gameObject.name}에 'TextMeshPro - Text (UI)' 컴포넌트가 없어! 타자 효과를 못 낸대! 😭");
            }
        }

        private void Start()
        {
            if (playOnAwake && _tmp != null)
            {
                Run(_tmp.text, typingSpeed);
            }
        }

        // [유니] 외부에서 타자 소리를 바꿀 수 있게! (null이면 기본 소리로 복구)
        public void SetTypingSound(AudioClip sound)
        {
            if (sound != null)
            {
                typingSound = sound;
            }
            else
            {
                typingSound = _defaultTypingSound; // 원래 소리로 복귀
            }
        }

        // 외부에서 텍스트를 넣고 타이핑 시작!
        public void Run(string textToType, float speedOverride = -1f)
        {

            if (_tmp == null)
            {
                _tmp = GetComponent<TMP_Text>();
                if (_tmp == null)
                {
                    Debug.LogError($"[유니] 🚨 {gameObject.name}에 'TextMeshPro - Text (UI)'가 없어! 텍스트를 출력할 수 없어 😭");
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

                // [유니] 공백이 아닐 때만 타자 소리 & 이벤트 발생!
                // IsVisibleCharacter가 가끔 이상할 때가 있어서, 공백 체크도 같이 함!
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
                Debug.LogError("[유니] 🚨 AudioSource가 null이야!");
                return;
            }
            if (typingSound == null) 
            {
                Debug.LogError("[유니] 🚨 AudioClip이 null이야!");
                return;
            }

            // [유니] 피치를 랜덤하게 바꿔서 기계적인 느낌을 줄이고 자연스럽게! 🎵
            audioSource.pitch = Random.Range(minPitch, maxPitch);
            audioSource.PlayOneShot(typingSound);
        }

        private void OnEnable()
        {
             // [유니] 오디오 리스너 체크 (오빠가 혹시 실수했을까봐!)
             if (FindObjectOfType<AudioListener>() == null)
             {
                 Debug.LogError("[유니] 🚨 씬에 'Audio Listener'가 없어! 소리를 들을 귀가 없는 상태야! Main Camera에 컴포넌트를 확인해줘!");
             }
        }
    }
}
