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

        private TMP_Text _tmp;
        private Coroutine _typeRoutine;
        private bool _isSkipping = false;
        private WaitForSeconds _cachedWait; // [유니] GC(가비지 컬렉션) 방지를 위해 대기 시간 캐싱!

        public bool IsTyping => _typeRoutine != null; // 현재 타이핑 중인지 확인

        private void Awake()
        {
            // [유니] GetComponent는 무거운 연산이니까 Awake에서 한 번만!
            _tmp = GetComponent<TMP_Text>();
        }

        private void Start()
        {
            if (playOnAwake)
            {
                Run(_tmp.text, typingSpeed);
            }
        }

        // 외부에서 텍스트를 넣고 타이핑 시작!
        public void Run(string textToType, float speedOverride = -1f)
        {
            if (_typeRoutine != null) StopCoroutine(_typeRoutine);
            
            _tmp.text = textToType;
            _tmp.maxVisibleCharacters = 0; // 일단 싹 가리기 (0개만 보임)

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
            
            // [유니] TMP는 내용이 바뀌면 ForceMeshUpdate를 해줘야 정확한 문자 정보(textInfo)를 가져올 수 있어!
            _tmp.ForceMeshUpdate(); 

            TMP_TextInfo textInfo = _tmp.textInfo;
            int totalVisibleCharacters = textInfo.characterCount; // 공백 포함 전체 글자 수
            
            // [유니] 최적화: 매번 new WaitForSeconds 하면 메모리 낭비니까 캐싱해서 쓰자!
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

            // 0부터 전체 글자 수까지 루프
            for (int i = 0; i < totalVisibleCharacters; i++)
            {
                 // [유니] 스킵 키를 눌렀다면? 바로 전체 출력하고 종료!
                if (_isSkipping)
                {
                    _tmp.maxVisibleCharacters = totalVisibleCharacters;
                    break; 
                }

                // 한 글자 더 보이게 설정
                _tmp.maxVisibleCharacters = i + 1;

                // [유니] 공백이 아닐 때만 타자 소리 이벤트 발생! (센스쟁이!)
                if (IsVisibleCharacter(i))
                {
                    onType?.Invoke();
                }

                yield return waitDelay;
            }

            // [완료] 루프가 끝나거나 스킵되면 확실하게 다 보여주기
            _tmp.maxVisibleCharacters = totalVisibleCharacters;
            _typeRoutine = null;
            _isSkipping = false;
            
            onComplete?.Invoke();
        }

        // [유니] 실제 눈에 보이는 글자인지 체크 (공백, 투명 문자 제외)
        private bool IsVisibleCharacter(int index)
        {
            if (_tmp.textInfo == null || index >= _tmp.textInfo.characterInfo.Length) return false;
            return _tmp.textInfo.characterInfo[index].isVisible;
        }
    }
}
