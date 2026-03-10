using UnityEngine;
using UnityEngine.InputSystem;
using UI;

namespace Core
{
    /// <summary>
    /// 게임의 전반적인 상태(일시정지, 대화, 시간 지연 효과 등)를 관리하는 매니저 클래스입니다.
    /// 싱글톤 패턴으로 구현되어 씬 전환 시에도 파괴되지 않습니다.
    /// </summary>
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
        private const float PauseCooldown = 0.25f;

        private Coroutine _hitStopRoutine;
        private Coroutine _bulletTimeRoutine;
        private bool _isBulletTimeActive = false;
        private float _currentBulletTimeScale = 1f;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                transform.SetParent(null);
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        /// <summary>
        /// PlayerInput 컴포넌트의 UnityEvent를 통해 호출될 일시정지 입력 처리 메서드입니다.
        /// </summary>
        /// <param name="context">Input System에서 전달해주는 입력 문맥 정보</param>
        public void OnPausePerformed(InputAction.CallbackContext context)
        {
            if (!context.performed) return;

            if (Time.unscaledTime < _lastPauseTime + PauseCooldown) return;
            _lastPauseTime = Time.unscaledTime;

            if (IsDialogueActive)
            {
                OnSkipDialogue?.Invoke();
                return;
            }

            Debug.Log($"[GameManager] Pause Toggled! Current State: {isPaused} -> {!isPaused}");
            TogglePause();
        }

        /// <summary>
        /// 게임의 일시정지 상태를 토글하고 관련 UI 및 TimeScale을 제어합니다.
        /// </summary>
        public void TogglePause()
        {
            isPaused = !isPaused;

            if (isPaused)
            {
                Time.timeScale = 0f;
                if (pauseUI != null) pauseUI.Show();
                else Debug.LogError("[GameManager] PauseUI is not connected!");

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

        public void TriggerBulletTime(float duration, float scale, bool cancelOnInput = false)
        {
            if (_bulletTimeRoutine != null) StopCoroutine(_bulletTimeRoutine);
            _bulletTimeRoutine = StartCoroutine(BulletTimeRoutine(duration, scale, cancelOnInput));
        }

        /// <summary>
        /// 불릿 타임(슬로우 모션) 효과를 처리하는 코루틴입니다.
        /// 사용자의 추가 입력이 감지되면 효과를 조기 종료할 수 있습니다.
        /// </summary>
        /// <param name="duration">지속 시간</param>
        /// <param name="scale">적용할 타임 스케일 값</param>
        /// <param name="cancelOnInput">입력 발생 시 취소 여부</param>
        /// <returns>IEnumerator</returns>
        private System.Collections.IEnumerator BulletTimeRoutine(float duration, float scale, bool cancelOnInput)
        {
            _isBulletTimeActive = true;
            _currentBulletTimeScale = scale;

            if (_hitStopRoutine == null)
            {
                Time.timeScale = scale;
            }

            float timer = 0f;
            float minDuration = 0.1f;

            PlayerInput playerInput = FindAnyObjectByType<PlayerInput>();
            Vector2 initialMove = Vector2.zero;

            if (playerInput != null && playerInput.actions != null)
            {
                initialMove = playerInput.actions["Move"].ReadValue<Vector2>();
            }

            while (timer < duration)
            {
                if (isPaused)
                {
                    yield return null;
                    continue;
                }

                if (cancelOnInput && playerInput != null && playerInput.actions != null && timer > minDuration)
                {
                    Vector2 currentMove = playerInput.actions["Move"].ReadValue<Vector2>();
                    bool moveChanged = (currentMove - initialMove).sqrMagnitude > 0.01f;

                    bool isJumping = playerInput.actions["Jump"].WasPressedThisFrame();
                    bool isHooking = playerInput.actions["Hook"].WasPressedThisFrame();
                    bool isDashing = playerInput.actions["Dash"].WasPressedThisFrame();
                    bool isHacking = playerInput.actions["Hack"].WasPressedThisFrame();

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
            // CameraEffectManager는 기존 프로젝트에 존재한다고 가정합니다.
            if (CameraEffectManager.Instance != null)
            {
                CameraEffectManager.Instance.AddUnscaledShake(intensity);
            }
        }

        public void TriggerPlayerRespawn()
        {
            OnPlayerRespawn?.Invoke();
        }
        private void Start()
        {
            InitPlayerInput();
        }
        /// <summary>
        /// 씬 내의 PlayerInput 컴포넌트를 찾아 일시정지 액션 이벤트를 구독합니다.
        /// 프리팹에서 GameManager를 직접 참조할 수 없는 문제를 해결합니다.
        /// </summary>
        public void InitPlayerInput()
        {
            PlayerInput playerInput = FindAnyObjectByType<PlayerInput>();

            if (playerInput != null)
            {
                // "Pause"라는 이름의 액션이 수행될 때(performed) 우리 함수가 실행되도록 연결!
                // 액션 이름은 오빠의 Input Action Asset에 적힌 이름과 같아야 해!
                playerInput.actions["Pause"].performed += OnPausePerformed;
                Debug.Log("[GameManager] PlayerInput 연결 성공! 이제 ESC만 눌러봐! ");
            }
            else
            {
                Debug.LogWarning("[GameManager] PlayerInput을 찾지 못했어! 플레이어가 아직 무대에 안 올라왔나봐? ");
            }
        }
    }
}
