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
        private float _lastPauseTime = 0f;
        private const float PauseCooldown = 0.3f;

        private GameInput _gameInput;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                // 다른 인스턴스가 살아있을 경우에만 중복 파괴!
                // 단, gameObject 전체를 파괴하면 같은 오브젝트에 붙은 다른 매니저도 죽을 수 있으므로 this만 파괴.
                Destroy(this);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            _gameInput = new GameInput();
        }

        private void OnEnable()
        {
            if (_gameInput == null)
            {
                _gameInput = new GameInput();
            }
            
            _gameInput.Enable();
            
            // 뉴 인풋 시스템 이벤트 연결!
            _gameInput.Player.Pause.performed -= OnPauseInput;
            _gameInput.UI.Pause.performed -= OnPauseInput;
            
            _gameInput.Player.Pause.performed += OnPauseInput;
            _gameInput.UI.Pause.performed += OnPauseInput;
        }

        private void OnDisable()
        {
            if (_gameInput != null)
            {
                _gameInput.Player.Pause.performed -= OnPauseInput;
                _gameInput.UI.Pause.performed -= OnPauseInput;
                _gameInput.Disable();
            }
        }

        private void OnPauseInput(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                Debug.Log($"[GameManager] 뉴 인풋 시스템 이벤트로 ESC 감지! (ActionMap: {context.action.actionMap.name})");
                HandlePauseToggle();
            }
        }

        private void Update()
        {
            // 보험용: 만약 이벤트 구독이 풀리더라도 무조건 작동하는 뉴 인풋 쌩-폴링 (Polling)
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Debug.LogWarning("[GameManager Update] Keyboard.current.escapeKey 로부터 ESC 직접 낚아챔!");
                HandlePauseToggle();
            }
        }

        private void ForceUnlockCursor()
        {
            Debug.LogError("!!! [Action] Force Unlock Cursor !!!");
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        private void HandlePauseToggle()
        {
            float currentTime = Time.unscaledTime;

            if (currentTime < _lastPauseTime + PauseCooldown) 
            {
                return;
            }
            _lastPauseTime = currentTime;

            if (IsDialogueActive)
            {
                OnSkipDialogue?.Invoke();
                return; 
            }

            TogglePause();
        }

        public void TogglePause()
        {
            isPaused = !isPaused;
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
