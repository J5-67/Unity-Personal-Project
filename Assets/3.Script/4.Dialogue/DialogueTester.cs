using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem; 

namespace UI
{
    [System.Serializable]
    public struct PortraitInfo
    {
        public string key; // CSV에 적을 키워드 (예: "Yuni_Smile")
        public Sprite sprite; // 실제 이미지
    }

    public class DialogueTester : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private DialogueUI dialogueUI;
        [SerializeField] private TextAsset csvFile; // CSV 파일 넣는 곳

        [Header("Data")]
        [SerializeField] private List<PortraitInfo> portraitDatabase; // 초상화 목록

        private List<DialogueData> _dialogueQueue;
        private int _currentIndex = -1;
        private GameInput _inputAction; 

        private void Awake()
        {
            _inputAction = new GameInput(); 
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
                // [유니] CSV 파싱해서 대기열에 넣기!
                _dialogueQueue = DialogueParser.Parse(csvFile.text);
                _currentIndex = -1;
                NextLine();
            }
            else
            {
                Debug.LogWarning("[유니] 오빠, CSV 파일이 연결 안 됐어! 인스펙터 확인해줘.");
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
            if (_dialogueQueue == null || _dialogueQueue.Count == 0) return;

            _currentIndex++;

            if (_currentIndex < _dialogueQueue.Count)
            {
                DialogueData data = _dialogueQueue[_currentIndex];

                // [유니] 키 값을 이용해서 스프라이트 찾기
                Sprite portrait = GetPortrait(data.portraitKey);

                dialogueUI.Show(data.text, data.side, data.name, portrait);
            }
            else
            {
                dialogueUI.Hide();
                _currentIndex = -1; 
                Debug.Log("[유니] 대화 종료 (CSV 끝)");
            }
        }

        // [유니] 키(Key)로 초상화 이미지(Sprite)를 찾는 함수
        private Sprite GetPortrait(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;

            foreach (var info in portraitDatabase)
            {
                if (info.key == key) return info.sprite;
            }
            return null; // 못 찾으면 null
        }
    }
}
