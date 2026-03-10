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
        [Header("References")]
        [SerializeField] private PauseUI pauseUI;
        [SerializeField] private Unity.Cinemachine.CinemachineImpulseSource impulseSource;

        public event System.Action OnPlayerRespawn;

        public event System.Action OnSkipDialogue;

        public bool IsDialogueActive { get; private set; }
        public bool IsPaused => isPaused;

        private GameInput _gameInput;

        private float _debugLogTimer = 0f;

        private void Awake()
        {
            // 🎯 빌드 실행 시 무조건 로그를 찍게 해!
            Debug.LogError("!!! [GameManager] Awake Called !!!");

            if (Instance == null)
            {
                Instance = this;
                transform.SetParent(null); // 부모로부터 독립! 이게 중요해 오빠!
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
            // 🎯 수사 1단계: 어떤 키든 눌리면 일단 로그 찍기! (키보드 작동 확인용)
            if (Input.anyKeyDown)
            {
                // inputString이 비어있을 수 있으니 anyKeyDown 조건만으로 체크
                // Debug.Log($"[Input Check] Legacy KeyDown Detected."); 
            }

            // 🎯 1. Update 생존 신고 (10초마다 무조건 로그 찍기)
            _debugLogTimer += Time.unscaledDeltaTime;
            if (_debugLogTimer > 10f)
            {
                Debug.Log($"[GameManager] Update is Running... (isPaused: {isPaused})");
                _debugLogTimer = 0f;
            }

            // 🎯 2. 비상용 마우스 해제 키 (F1)
            if (Input.GetKeyDown(KeyCode.F1) || (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame))
            {
                Debug.LogError("!!! F1 Pressed: Force Unlock Cursor !!!");
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }

            // 🎯 3. 입력 감지 (ESC, P, F2)
            bool pauseTriggered = false;

            // 뉴 인풋 시스템 방식
            if (Keyboard.current != null)
            {
                if (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.pKey.wasPressedThisFrame || Keyboard.current.f2Key.wasPressedThisFrame)
                {
                    Debug.LogError("[Input Check] New Input System Key Detected!");
                    pauseTriggered = true;
                }
            }

            // 레거시 방식
            if (!pauseTriggered && (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P) || Input.GetKeyDown(KeyCode.F2)))
            {
                Debug.LogError("[Input Check] Legacy Input Key Detected!");
                pauseTriggered = true;
            }

            if (pauseTriggered)
            {
                HandlePauseToggle();
            }
        }

        // 🎯 오빠! 빌드에서 GameManager가 살아있는지 화면에 직접 그려서 확인해보자!
        private void OnGUI()
        {
            #if DEVELOPMENT_BUILD || UNITY_EDITOR
            Rect debugWindow = new Rect(5, 5, 450, 220);
            
            // 🎯 GUI 레벨에서 입력을 직접 낚아채보자! (Update가 안 돌아도 이건 작동해)
            Event e = Event.current;
            if (e.type == EventType.KeyDown)
            {
                if (e.keyCode == KeyCode.F1) { 
                    Cursor.visible = true; Cursor.lockState = CursorLockMode.None; 
                    Debug.LogError("!!! F1 from OnGUI (Event.current) !!!"); 
                }
                if (e.keyCode == KeyCode.F2 || e.keyCode == KeyCode.Escape) { 
                    Debug.LogError("!!! Pause Key from OnGUI (Event.current) !!!");
                    HandlePauseToggle(); 
                }
            }

            // 🎯 마우스 구원! 디버그 박스 위에 마우스가 있으면 커서 띄우기
            if (debugWindow.Contains(Event.current.mousePosition))
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }

            // 배경 박스
            GUI.backgroundColor = Color.black;
            GUI.Box(debugWindow, "");

            GUIStyle labelStyle = new GUIStyle();
            labelStyle.fontSize = 18;
            labelStyle.normal.textColor = Color.yellow;
            
            GUI.Label(new Rect(15, 10, 430, 25), $"[DEBUG] GameManager Monitoring...", labelStyle);
            
            GUI.color = isPaused ? Color.red : Color.green;
            GUI.Label(new Rect(15, 40, 430, 25), $"PAUSE STATE: {isPaused}", labelStyle);
            
            GUI.color = Color.white;
            GUI.Label(new Rect(15, 70, 430, 25), $"IsDialogueActive: {IsDialogueActive}", labelStyle);
            GUI.Label(new Rect(15, 100, 430, 25), $"Time.timeScale: {Time.timeScale}", labelStyle);
            GUI.Label(new Rect(15, 130, 430, 25), $"[F1]: Unlock Cursor | [ESC/F2]: Pause", labelStyle);

            // 🎯 마우스 강제 해제 버튼
            if (GUI.Button(new Rect(15, 170, 130, 35), "FORCE CURSOR"))
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                Debug.LogError("!!! GUI: FORCE CURSOR CLICKED !!!");
            }

            // 🎯 일시정지 토글 버튼
            if (GUI.Button(new Rect(150, 170, 130, 35), "TOGGLE PAUSE"))
            {
                Debug.LogError("!!! GUI: TOGGLE PAUSE CLICKED !!!");
                HandlePauseToggle();
            }

            // 🎯 대화 상태 강제 초기화
            if (GUI.Button(new Rect(285, 170, 150, 35), "RESET DIALOGUE"))
            {
                SetDialogueState(false);
                Debug.LogError("!!! GUI: RESET DIALOGUE CLICKED !!!");
            }
            #endif
        }

        private void HandlePauseToggle()
        {
            float currentTime = Time.unscaledTime;
            Debug.LogError($"[Pause Log] HandlePauseToggle Called. Time: {currentTime}, Last: {_lastPauseTime}");

            if (currentTime < _lastPauseTime + PauseCooldown) 
            {
                Debug.LogWarning("[Pause Log] Blocked by Cooldown.");
                return;
            }
            _lastPauseTime = currentTime;

            // 🎯 대화 중인지 체크!
            if (IsDialogueActive)
            {
                Debug.LogError("[Pause Log] Dialogue is ACTIVE. Triggering Skip.");
                OnSkipDialogue?.Invoke();
                return; 
            }

            Debug.LogError("[Pause Log] All Checks Passed. Calling TogglePause().");
            TogglePause();
        }

        public void TogglePause()
        {
            isPaused = !isPaused;
            Debug.LogError($"[Pause Log] TogglePause() - New State: {isPaused}");
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
