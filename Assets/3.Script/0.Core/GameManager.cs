using UnityEngine;
using UnityEngine.InputSystem;
using UI; // PauseUI를 위해

namespace Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private bool isPaused = false;

        [Header("References")]
        [SerializeField] private PauseUI pauseUI;

        public bool IsDialogueActive { get; private set; } // [유니] 대화 중인지 확인하는 변수!

        private GameInput _gameInput;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject); // 씬 이동해도 유지!
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            // 인풋 시스템 초기화
            _gameInput = new GameInput();
            
            // ESC 키 이벤트 연결 (Time.timeScale 등 처리는 여기서!)
            _gameInput.UI.Pause.performed += OnPausePerformed;
        }

        private void OnEnable()
        {
            // [유니] Awake에서 초기화되기 전에 OnEnable이 불릴 수도 있고,
            // 중복된 매니저가 파괴될 때 불릴 수도 있어서 체크 필수!
            if (_gameInput != null)
            {
                _gameInput.Enable();
            }
        }

        private void OnDisable()
        {
            if (_gameInput != null)
            {
                _gameInput.Disable();
            }
        }

        private void OnPausePerformed(InputAction.CallbackContext context)
        {
            // [유니] 일시정지 토글!
            TogglePause();
        }

        public void TogglePause()
        {
            isPaused = !isPaused;

            if (isPaused)
            {
                // 멈춤!
                Time.timeScale = 0f;
                if (pauseUI != null) pauseUI.Show();
                
                // [유니] 일시정지 때도 UI 조작(ESC 등)만 가능하게 맵 전환!
                FindAnyObjectByType<PlayerInput>()?.SwitchCurrentActionMap("UI");
            }
            else
            {
                // 진행!
                Time.timeScale = 1f;
                if (pauseUI != null) pauseUI.Hide();
                
                // [유니] 일시정지 해제되면 다시 플레이어 조작 가능!
                FindAnyObjectByType<PlayerInput>()?.SwitchCurrentActionMap("Player");
            }
        }
        
        // [유니] 외부에서 PauseUI를 연결할 수 있게!
        public void SetPauseUI(PauseUI ui)
        {
            pauseUI = ui;
        }

        // [유니] 대화 상태 설정 함수 (DialogueUI에서 부를 거야!)
        public void SetDialogueState(bool isActive)
        {
            IsDialogueActive = isActive;

            // [유니] PlayerInput 컴포넌트를 찾아서 Action Map을 전환!
            // 인게임 동작(Player)과 UI 조작(UI)을 확실하게 분리할 수 있어!
            PlayerInput playerInput = FindAnyObjectByType<PlayerInput>();
            if (playerInput != null)
            {
                if (isActive)
                {
                    // 대화 중일 때는 'UI' 맵으로 전환 (이동, 훅, 점프 등 Player 맵의 입력 차단)
                    playerInput.SwitchCurrentActionMap("UI");
                }
                else
                {
                    // 대화가 끝나면 다시 'Player' 맵으로 복귀!
                    playerInput.SwitchCurrentActionMap("Player");
                }
            }
        }
    }
}
