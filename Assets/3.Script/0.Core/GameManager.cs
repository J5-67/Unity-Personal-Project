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

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject); 
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            _gameInput = new GameInput();
            
            _gameInput.UI.Pause.performed += OnPausePerformed;
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

        private void OnPausePerformed(InputAction.CallbackContext context)
        {
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
                if (_isBulletTimeActive)
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
        
        public void TriggerHitStop(float duration = 0.05f)
        {
            StartCoroutine(HitStopRoutine(duration));
        }

        private System.Collections.IEnumerator HitStopRoutine(float duration)
        {
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(duration); 
            
            if (!_isBulletTimeActive) 
            {
                Time.timeScale = 1f;
            }
        }

        private bool _isBulletTimeActive = false; 
        private float _currentBulletTimeScale = 1f;

        public void TriggerBulletTime(float duration, float scale, bool cancelOnInput = false)
        {
            StopCoroutine(nameof(BulletTimeRoutine)); 
            StartCoroutine(BulletTimeRoutine(duration, scale, cancelOnInput));
        }

        private System.Collections.IEnumerator BulletTimeRoutine(float duration, float scale, bool cancelOnInput)
        {
            _isBulletTimeActive = true;
            _currentBulletTimeScale = scale;
            Time.timeScale = scale;
            
            float timer = 0f;
            float minDuration = 0.1f; 

            while (timer < duration)
            {
                if (cancelOnInput && _gameInput != null && timer > minDuration)
                {
                    bool isMoving = _gameInput.Player.Move.ReadValue<Vector2>().sqrMagnitude > 0.01f;
                    bool isJumping = _gameInput.Player.Jump.IsPressed();
                    bool isHooking = _gameInput.Player.Hook.IsPressed();
                    bool isDashing = _gameInput.Player.Dash.IsPressed();
                    bool isHacking = _gameInput.Player.Hack.IsPressed();

                    if (isMoving || isJumping || isHooking || isDashing || isHacking)
                    {
                        break; 
                    }
                }

                yield return null; 
                timer += Time.unscaledDeltaTime; 
            }
            
            Time.timeScale = 1f;
            _isBulletTimeActive = false;
        }

        public void TriggerCameraShake(float intensity = 1f)
        {
            if (impulseSource != null)
            {
                impulseSource.GenerateImpulse(intensity);
            }
            else
            {
                 Debug.LogWarning("[유니] CinemachineImpulseSource가 연결되지 않았어! 컴포넌트를 추가해줘! 📸");
            }
        }

        public void TriggerPlayerRespawn()
        {
            OnPlayerRespawn?.Invoke();
        }
    }
}
