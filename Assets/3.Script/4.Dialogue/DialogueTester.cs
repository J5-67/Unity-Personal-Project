using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem; 
using System.Linq;

namespace UI
{
    [System.Serializable]
    public struct PortraitInfo
    {
        public string key;
        public Sprite sprite;
        public AudioClip typingSound;
    }

    public class DialogueTester : MonoBehaviour
    {
        public static DialogueTester Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private DialogueUI dialogueUI;
        [SerializeField] private TextAsset csvFile;
        [SerializeField] private bool playTestOnStart = false;

        [Header("Data")]
        [SerializeField] private List<PortraitInfo> portraitDatabase;
        
        private Dictionary<string, PortraitInfo> _portraitDic = new Dictionary<string, PortraitInfo>();

        private List<DialogueData> _allDialogueList;
        private List<DialogueData> _currentQueue;
        
        private int _currentIndex = -1;
        private GameInput _inputAction; 

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            _inputAction = new GameInput(); 
            
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
            
            if (Core.GameManager.Instance != null)
                Core.GameManager.Instance.OnSkipDialogue += EndDialogue;
        }

        private void OnDisable()
        {
            _inputAction.UI.Disable();
            _inputAction.UI.NextDialogue.performed -= OnNextDialogue;
            
            if (Core.GameManager.Instance != null)
                Core.GameManager.Instance.OnSkipDialogue -= EndDialogue;
        }

        private void Start()
        {
            if (csvFile != null)
            {
                _allDialogueList = DialogueParser.Parse(csvFile.text);
                
                if (playTestOnStart)
                {
                    PlayDialogueRange(1, 5);
                }
            }
            else
            {
                Debug.LogWarning("CSV File Error");
            }
        }

        public void LoadDialogueData(TextAsset newCsv)
        {
            if (newCsv == null) return;
            if (csvFile == newCsv && _allDialogueList != null) return;

            csvFile = newCsv;
            _allDialogueList = DialogueParser.Parse(csvFile.text);
        }

        public void PlayDialogueRange(int startId, int endId)
        {
            if (_allDialogueList == null) return;

            _currentQueue = _allDialogueList
                .Where(d => d.id >= startId && d.id <= endId)
                .ToList();

            if (_currentQueue.Count > 0)
            {
                _currentIndex = -1;
                NextLine();
            }
            else
            {
                Debug.LogWarning($"Range Error (ID: {startId} ~ {endId})");
            }
        }

        private void OnNextDialogue(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            if (dialogueUI.IsTyping)
            {
                dialogueUI.SkipTyping();
            }
            else
            {
                NextLine();
            }
        }

        public void NextLine()
        {
            if (_currentQueue == null || _currentQueue.Count == 0) return;

            _currentIndex++;

            if (_currentIndex < _currentQueue.Count)
            {
                DialogueData data = _currentQueue[_currentIndex];

                PortraitInfo info = GetPortraitInfo(data.portraitKey);

                dialogueUI.Show(data.text, data.side, data.name, info.sprite, info.typingSound);
            }
            else
            {
                EndDialogue();
            }
        }

        public System.Action OnDialogueEnded;

        private void EndDialogue()
        {
            dialogueUI.Hide();
            _currentIndex = -1; 
            _currentQueue = null;
            OnDialogueEnded?.Invoke();
        }

        private PortraitInfo GetPortraitInfo(string key)
        {
            if (string.IsNullOrEmpty(key)) return new PortraitInfo();

            if (_portraitDic.TryGetValue(key, out PortraitInfo info))
            {
                return info;
            }
            
            return new PortraitInfo(); 
        }
    }
}
