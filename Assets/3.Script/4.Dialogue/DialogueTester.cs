using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem; 
using System.Linq; // [유니] 리스트 필터링을 위해 추가했어!

namespace UI
{
    [System.Serializable]
    public struct PortraitInfo
    {
        public string key; // CSV에 적을 키워드 (예: "Yuni_Smile")
        public Sprite sprite; // 실제 이미지
        public AudioClip typingSound; // [유니] 대사칠 때 나는 소리 (없으면 기본값)
    }

    public class DialogueTester : MonoBehaviour
    {
        // [유니] 어디서든 부를 수 있게 싱글톤 패턴 추가! 📢
        public static DialogueTester Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private DialogueUI dialogueUI;
        [SerializeField] private TextAsset csvFile; // CSV 파일 넣는 곳
        [SerializeField] private bool playTestOnStart = false; // [유니] 시작할 때 테스트할지 여부

        [Header("Data")]
        [SerializeField] private List<PortraitInfo> portraitDatabase; // 인스펙터 입력용
        
        // [유니] 검색 속도를 위해 딕셔너리로 변환! (리스트보다 훨씬 빨라!)
        private Dictionary<string, PortraitInfo> _portraitDic = new Dictionary<string, PortraitInfo>();

        private List<DialogueData> _allDialogueList; // [유니] 전체 대본 원본
        private List<DialogueData> _currentQueue;    // [유니] 현재 재생할 구간의 대본
        
        private int _currentIndex = -1;
        private GameInput _inputAction; 

        private void Awake()
        {
            // [유니] 싱글톤 초기화
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject); // 중복 방지
                return;
            }

            _inputAction = new GameInput(); 
            
            // [유니] 게임 시작할 때 초상화 목록을 딕셔너리로 옮겨 담기! (최적화)
            foreach (var info in portraitDatabase)
            {
                if (!_portraitDic.ContainsKey(info.key))
                {
                    _portraitDic.Add(info.key, info);
                }
            }
        }

        private void OnEnable()
        {
            _inputAction.UI.Enable(); 
            _inputAction.UI.NextDialogue.performed += OnNextDialogue; 
        }

        private void OnDisable()
        {
            _inputAction.UI.Disable();
            _inputAction.UI.NextDialogue.performed -= OnNextDialogue;
        }

        private void Start()
        {
            if (csvFile != null)
            {
                // 1. 전체 CSV 파싱 (한 번만 함)
                _allDialogueList = DialogueParser.Parse(csvFile.text);
                
                // 2. [테스트] 원하는 구간 실행! (켜져 있을 때만)
                if (playTestOnStart)
                {
                    PlayDialogueRange(1, 5);
                }
            }
            else
            {
                Debug.LogWarning("CSV 파일 연결 오류 인스펙터 확인");
            }
        }

        // [유니] 특정 구간(StartID ~ EndID)만 골라서 재생하는 함수야!
        public void PlayDialogueRange(int startId, int endId)
        {
            if (_allDialogueList == null) return;

            // ID 범위에 맞는 대사만 쏙쏙 뽑아오기 (LINQ 사용)
            _currentQueue = _allDialogueList
                .Where(d => d.id >= startId && d.id <= endId)
                .ToList();

            if (_currentQueue.Count > 0)
            {
                _currentIndex = -1;
                NextLine(); // 첫 대사 시작!
                //Debug.Log($"대화 시작 (ID: {startId} ~ {endId})");
            }
            else
            {
                Debug.LogWarning($"해당 범위의 대사가 없음 (ID: {startId} ~ {endId})");
            }
        }

        private void OnNextDialogue(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            if (dialogueUI.IsTyping)
            {
                dialogueUI.SkipTyping(); // 타자 스킵
            }
            else
            {
                NextLine(); // 다음 대화
            }
        }

        public void NextLine()
        {
            if (_currentQueue == null || _currentQueue.Count == 0) return;

            _currentIndex++;

            if (_currentIndex < _currentQueue.Count)
            {
                DialogueData data = _currentQueue[_currentIndex];

                // [유니] 딕셔너리에서 빠르게 찾아오기!
                PortraitInfo info = GetPortraitInfo(data.portraitKey);

                dialogueUI.Show(data.text, data.side, data.name, info.sprite, info.typingSound);
            }
            else
            {
                EndDialogue();
            }
        }

        private void EndDialogue()
        {
            dialogueUI.Hide();
            _currentIndex = -1; 
            _currentQueue = null; // 큐 비우기
            //Debug.Log("구간 대화 종료!");
        }

        // [유니] 딕셔너리(Dictionary)를 써서 검색 속도가 엄청 빨라졌어!
        // [유니] 딕셔너리(Dictionary)를 써서 검색 속도가 엄청 빨라졌어!
        private PortraitInfo GetPortraitInfo(string key)
        {
            if (string.IsNullOrEmpty(key)) return new PortraitInfo();

            if (_portraitDic.TryGetValue(key, out PortraitInfo info))
            {
                return info;
            }
            
            // 못 찾으면 빈 껍데기
            return new PortraitInfo(); 
        }
    }
}
