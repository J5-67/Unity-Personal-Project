using UnityEngine;
using UnityEngine.InputSystem;
using UI;

namespace Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private bool isPaused = false;

        [Header("References")]
        [SerializeField] private PauseUI pauseUI;
        [SerializeField] private Unity.Cinemachine.CinemachineImpulseSource impulseSource;

        public event System.Action OnPlayerRespawn;
        public event System.Action OnSkipDialogue;

        public bool IsDialogueActive { get; private set; }
        public bool IsPaused => isPaused;

        private GameInput _gameInput;

        private void Awake()
        {
            Debug.LogError("!!! [GameManager] Awake Called !!!");

            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Debug.LogWarning("[GameManager] Duplicate Instance Destroyed.");
                Destroy(gameObject);
                return;
            }

            _gameInput = new GameInput();
        }

        private void OnEnable()
        {
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

        private float _lastPauseTime = 0f;
        private const float PauseCooldown = 0.3f;

        private void Update()
        {
            // 🎯 수사 1단계: F-키들을 통한 비상 조작
            if (Input.GetKeyDown(KeyCode.F1)) { ForceUnlockCursor(); }
            if (Input.GetKeyDown(KeyCode.F2)) TogglePause();
            if (Input.GetKeyDown(KeyCode.F3)) { isPaused = false; ApplyPauseState(); } // 강제 해제
            if (Input.GetKeyDown(KeyCode.F4)) { SetDialogueState(false); } // 대화 강제 종료

            // 🎯 2. 메인 입력 감지
            bool pauseTriggered = false;

            // ESC 체크
            if (Input.GetKeyDown(KeyCode.Escape) || (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame))
            {
                Debug.LogError("!!! [Input] ESC DETECTED !!!");
                pauseTriggered = true;
            }

            if (pauseTriggered)
            {
                HandlePauseToggle();
            }
        }

        private void ForceUnlockCursor()
        {
            Debug.LogError("!!! [Action] Force Unlock Cursor !!!");
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        private void OnGUI()
        {
            #if DEVELOPMENT_BUILD || UNITY_EDITOR
            // 🎯 디버그 모드에서는 마우스 커서를 항상 보이게 강제할게! (오빠가 클릭해야 하니까)
            // Cursor.visible = true; // 주석 해제하면 게임 내내 커서가 보임
            
            Rect debugWindow = new Rect(10, 10, 480, 240);
            
            // 박스 근처에 마우스가 오면 커서 풀기
            Vector2 mousePos = Event.current.mousePosition;
            if (debugWindow.Contains(mousePos))
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }

            GUI.backgroundColor = new Color(0, 0, 0, 0.8f);
            GUI.Box(debugWindow, "🛠️ YUNI'S SUPER DEBUG PANEL");

            GUILayout.BeginArea(new Rect(20, 40, 460, 200));
            
            GUILayout.BeginHorizontal();
            GUI.color = isPaused ? Color.red : Color.green;
            GUILayout.Label($"PAUSE: {isPaused}", GUILayout.Width(120));
            GUI.color = IsDialogueActive ? Color.yellow : Color.white;
            GUILayout.Label($"DIALOGUE: {IsDialogueActive}", GUILayout.Width(150));
            GUI.color = Color.white;
            GUILayout.Label($"TIME: {Time.timeScale:F2}");
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.Label($"Current Scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
            GUILayout.Label($"PauseUI Linked: {(pauseUI != null ? "YES" : "NO (Missing!)")}");
            
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("FORCE UNPAUSE", GUILayout.Height(40))) { isPaused = false; ApplyPauseState(); }
            if (GUILayout.Button("FORCE PAUSE", GUILayout.Height(40))) { isPaused = true; ApplyPauseState(); }
            GUILayout.EndHorizontal();

            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("UNLOCK MOUSE", GUILayout.Height(40))) ForceUnlockCursor();
            if (GUILayout.Button("RESET DIALOGUE", GUILayout.Height(40))) SetDialogueState(false);
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
            #endif
        }

        private void HandlePauseToggle()
        {
            float currentTime = Time.unscaledTime;
            Debug.LogError($"[Pause Flow] HandlePauseToggle Called. Time: {currentTime}");

            if (currentTime < _lastPauseTime + PauseCooldown) 
            {
                Debug.LogWarning("[Pause Flow] Blocked by Cooldown.");
                return;
            }
            _lastPauseTime = currentTime;

            if (IsDialogueActive)
            {
                Debug.LogError("[Pause Flow] Dialogue Active! Triggering Skip.");
                OnSkipDialogue?.Invoke();
                return; 
            }

            TogglePause();
        }

        public void TogglePause()
        {
            isPaused = !isPaused;
            Debug.LogError($"[Pause Flow] TogglePause - New State: {isPaused}");
            ApplyPauseState();
        }

        private void ApplyPauseState()
        {
            if (isPaused)
            {
                Time.timeScale = 0f;
                if (pauseUI != null) pauseUI.Show();

                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;

                FindAnyObjectByType<PlayerInput>()?.SwitchCurrentActionMap("UI");
            }
            else
            {
                if (_hitStopRoutine != null)
                {
                    Time.timeScale = 0f;
                }
                else if (_isBulletTimeActive)
                {
                    Time.timeScale = _currentBulletTimeScale;
                }
                else
                {
                    Time.timeScale = 1f;
                }

                if (pauseUI != null) pauseUI.Hide();

                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;

                FindAnyObjectByType<PlayerInput>()?.SwitchCurrentActionMap("Player");
            }
        }

        public void SetPauseUI(PauseUI ui)
        {
            pauseUI = ui;
        }

        public void SetDialogueState(bool isActive)
        {
            IsDialogueActive = isActive;

            PlayerInput playerInput = FindAnyObjectByType<PlayerInput>();
            if (playerInput != null)
            {
                if (isActive)
                {
                    playerInput.SwitchCurrentActionMap("UI");
                }
                else
                {
                    playerInput.SwitchCurrentActionMap("Player");
                }
            }

            if (!isActive)
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
        }

        private Coroutine _hitStopRoutine;
        private Coroutine _bulletTimeRoutine;

        public void TriggerHitStop(float duration = 0.05f)
        {
            if (_hitStopRoutine != null) StopCoroutine(_hitStopRoutine);
            _hitStopRoutine = StartCoroutine(HitStopRoutine(duration));
        }

        private System.Collections.IEnumerator HitStopRoutine(float duration)
        {
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(duration);

            _hitStopRoutine = null;

            if (!isPaused)
            {
                if (_isBulletTimeActive)
                {
                    Time.timeScale = _currentBulletTimeScale;
                }
                else
                {
                    Time.timeScale = 1f;
                }
            }
        }

        private bool _isBulletTimeActive = false;
        private float _currentBulletTimeScale = 1f;

        public void TriggerBulletTime(float duration, float scale, bool cancelOnInput = false)
        {
            if (_bulletTimeRoutine != null) StopCoroutine(_bulletTimeRoutine);
            _bulletTimeRoutine = StartCoroutine(BulletTimeRoutine(duration, scale, cancelOnInput));
        }

        private System.Collections.IEnumerator BulletTimeRoutine(float duration, float scale, bool cancelOnInput)
        {
            _isBulletTimeActive = true;
            _currentBulletTimeScale = scale;
            
            // 만약 히트스탑이 실행 중이 아니라면 즉시 타임스케일을 내립니다.
            if (_hitStopRoutine == null)
            {
                Time.timeScale = scale;
            }

            float timer = 0f;
            float minDuration = 0.1f;

            Vector2 initialMove = _gameInput != null ? _gameInput.Player.Move.ReadValue<Vector2>() : Vector2.zero;

            while (timer < duration)
            {
                if (isPaused)
                {
                    yield return null;
                    continue;
                }

                if (cancelOnInput && _gameInput != null && timer > minDuration)
                {
                    Vector2 currentMove = _gameInput.Player.Move.ReadValue<Vector2>();
                    bool moveChanged = (currentMove - initialMove).sqrMagnitude > 0.01f;

                    bool isJumping = _gameInput.Player.Jump.WasPressedThisFrame();
                    bool isHooking = _gameInput.Player.Hook.WasPressedThisFrame();
                    bool isDashing = _gameInput.Player.Dash.WasPressedThisFrame();
                    bool isHacking = _gameInput.Player.Hack.WasPressedThisFrame();

                    if (moveChanged || isJumping || isHooking || isDashing || isHacking)
                    {
                        break;
                    }
                }

                yield return null;
                timer += Time.unscaledDeltaTime;
            }

            _isBulletTimeActive = false;
            _bulletTimeRoutine = null;

            if (!isPaused && _hitStopRoutine == null)
            {
                Time.timeScale = 1f;
            }
        }

        public void TriggerCameraShake(float intensity = 1f)
        {
            if (CameraEffectManager.Instance != null)
            {
                CameraEffectManager.Instance.AddUnscaledShake(intensity);
            }
        }

        public void TriggerPlayerRespawn()
        {
            OnPlayerRespawn?.Invoke();
        }
    }
}
