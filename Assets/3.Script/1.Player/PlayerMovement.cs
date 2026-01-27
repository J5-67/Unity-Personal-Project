using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("⚙️ Move Settings")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float rotationSpeed = 15f;

    [Header("💨 Dash Settings (New!)")]
    [SerializeField] private float dashSpeed = 40f;      // 대시 속도
    [SerializeField] private float dashDuration = 0.15f; // 대시 지속 시간
    [SerializeField] private int maxDashCharges = 2;     // 최대 스택 (2개)
    [SerializeField] private float dashCooldown = 3f;    // 스택 1개 충전 시간

    [Header("🦘 Jump & Gravity")]
    [SerializeField] private float jumpForce = 18f;
    [SerializeField] private float jumpCutMultiplier = 0.5f;
    [SerializeField] private float gravityScale = 2.5f;
    [SerializeField] private float fastFallSpeed = 20f;

    [Header("🧗 Wall Mechanics")]
    [SerializeField] private float wallSlideSpeed = 2f;
    [SerializeField] private Vector2 wallJumpPower = new Vector2(12f, 16f);
    [SerializeField] private float wallJumpStopControlTime = 0.2f;

    [Header("⏱️ Input Feel")]
    [SerializeField] private float coyoteTime = 0.15f;
    [SerializeField] private float jumpBufferTime = 0.2f;
    [SerializeField] private float dropDisableTime = 0.5f;

    [Header("📍 Checks & References")]
    [SerializeField] private Transform groundCheckPos;
    [SerializeField] private Transform wallCheckPos;
    [SerializeField] private float checkRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private PlayerAim playerAim; // [연결 필요] 조준 스크립트

    // --- 내부 변수 ---
    private Rigidbody _rb;
    private Collider _playerCollider;
    private Vector2 _moveInput;
    public Vector2 MoveInput => _moveInput; // [유니] 외부에서 입력값 확인용 (Hook 등)

    // 상태 변수
    private bool _isGrounded;
    private bool _isTouchingWall;
    private bool _isWallSliding;
    private bool _isJumpPressed;
    private bool _canMove = true;

    // [대시 관련 상태]
    private bool _isDashing;          // 현재 대시 중인가?
    private int _currentDashCharges;  // 현재 남은 스택
    private float _dashRechargeTimer; // 충전 타이머

    private float _coyoteTimeCounter;
    private float _jumpBufferCounter;
    private PlatformFunction _currentFunctionPlatform;

    private void Awake()
    {
        TryGetComponent(out _rb);
        TryGetComponent(out _playerCollider);

        // [자동 연결 시도] 만약 Inspector에서 안 넣었으면 찾음
        if (playerAim == null) playerAim = GetComponent<PlayerAim>();

        _rb.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezeRotation;

        // 대시 스택 초기화
        _currentDashCharges = maxDashCharges;
    }

    private void Update()
    {
        UpdateTimers();
        HandleDashRecharge(); // 대시 스택 충전 로직
    }

    private void FixedUpdate()
    {
        CheckSurroundings();

        // [중요] 대시 중일 때는 다른 움직임(이동, 중력, 벽타기) 무시!
        if (_isDashing)
        {
            return;
        }

        if (_canMove)
        {
            Move();
            ApplyRotation();
        }

        HandleGravity();
        WallSlide();

        if (_jumpBufferCounter > 0)
        {
            TryJump();
        }
    }

    // ---------------------------------------------------------
    // 🎮 Input System
    // ---------------------------------------------------------
    public void OnMove(InputAction.CallbackContext context)
    {
        _moveInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        // [유니] PassThrough 타입은 started가 안 올 수 있어서 performed도 체크! 그리고 진짜 눌렸는지 확인!
        if (context.started || (context.performed && context.ReadValueAsButton()))
        {
            _jumpBufferCounter = jumpBufferTime;
            _isJumpPressed = true;
        }
        else if (context.canceled || !context.ReadValueAsButton())
        {
            _isJumpPressed = false;
            // [유니] 점프 키 뗐을 때 (상승 중이면 컷!)
            if (_rb.linearVelocity.y > 0f && !_isWallSliding && !_isDashing)
            {
                CutJumpVelocity();
            }
        }
    }

    // [대시 입력 추가]
    public void OnDash(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            // 스택이 있고, 이미 대시 중이 아닐 때만 발동
            if (_currentDashCharges > 0 && !_isDashing)
            {
                StartCoroutine(DashRoutine());
            }
            else
            {
                Debug.Log("⚠️ 대시 불가 (스택 부족 or 사용 중)");
            }
        }
    }

    // ---------------------------------------------------------
    // 💨 Dash Logic (핵심!)
    // ---------------------------------------------------------
    private IEnumerator DashRoutine()
    {
        _isDashing = true;
        _currentDashCharges--; // 스택 소모
        _dashRechargeTimer = 0f; // 쿨타임 타이머 초기화 (충전 시작)

        // 1. 방향 계산 (마우스 좌표 - 내 위치)
        Vector3 mousePos = playerAim.GetAimWorldPosition();
        Vector3 dashDir = (mousePos - transform.position).normalized;

        // 2.5D 보정: X축(깊이)으로 휘지 않게 0으로 고정
        dashDir = new Vector3(0, dashDir.y, dashDir.z).normalized;

        // 2. 물리 적용 (중력 무시하고 직선으로 쏘기)
        _rb.linearVelocity = dashDir * dashSpeed;

        // [선택] 대시 중에는 잠깐 무적 판정을 넣거나 레이어를 바꿀 수도 있어

        // 3. 대시 지속 시간 대기
        yield return new WaitForSeconds(dashDuration);

        // 4. 대시 종료 (속도 초기화 or 관성 유지? 일단 정밀 조작을 위해 초기화)
        _rb.linearVelocity = Vector3.zero;
        _isDashing = false;
    }

    private void HandleDashRecharge()
    {
        // 스택이 꽉 차지 않았을 때만 충전
        if (_currentDashCharges < maxDashCharges)
        {
            _dashRechargeTimer += Time.deltaTime;

            // 쿨타임 다 차면 스택 +1
            if (_dashRechargeTimer >= dashCooldown)
            {
                _currentDashCharges++;
                _dashRechargeTimer = 0;
                Debug.Log($"⚡ 대시 충전 완료! (현재: {_currentDashCharges})");
            }
        }
    }

    // ---------------------------------------------------------
    // (기존 이동 로직들... 그대로 유지)
    // ---------------------------------------------------------
    private void Move()
    {
        float targetSpeedZ = _moveInput.x * moveSpeed;

        // [유니] 땅에 있을 때는 빠릿하게! (기존 로직 유지)
        if (_isGrounded)
        {
            _rb.linearVelocity = new Vector3(0f, _rb.linearVelocity.y, targetSpeedZ);
        }
        // [유니] 공중에 있을 때는 관성(Momentum)을 지켜주자! 🚀
        else
        {
            float currentZ = _rb.linearVelocity.z;

            // 1. 입력이 있을 때
            if (Mathf.Abs(targetSpeedZ) > 0.1f)
            {
                // 입력 방향과 같은 방향으로 이미 기본 속도보다 빠르다면? -> 건드리지 마! (스윙 가속 유지)
                bool isMovingFast = Mathf.Abs(currentZ) > moveSpeed;
                bool isSameDir = Mathf.Sign(currentZ) == Mathf.Sign(targetSpeedZ);

                if (isMovingFast && isSameDir)
                {
                    // [유니] 수정: 관성을 유지하되, 너무 과하지 않게 서서히 줄어들도록 변경 (과속 방지)
                    // 기존: 완전 유지 (decayed X) -> 변경: 서서히 원래 moveSpeed로 복귀
                    float decayed = Mathf.MoveTowards(currentZ, targetSpeedZ, 10f * Time.deltaTime); // 10f 정도로 서서히 감속
                    _rb.linearVelocity = new Vector3(0f, _rb.linearVelocity.y, decayed);
                }
                else
                {
                    // 속도가 느리거나, 방향을 바꿀 때는 가속/감속 적용 (공중 제어력 Air Control)
                    // 땅보다 조금 더 부드럽게 (가속도 5배)
                    float newSpeed = Mathf.MoveTowards(currentZ, targetSpeedZ, moveSpeed * 5f * Time.deltaTime);
                    _rb.linearVelocity = new Vector3(0f, _rb.linearVelocity.y, newSpeed);
                }
            }
            // 2. 입력이 없을 때 (키를 뗐을 때)
            else
            {
                // 천천히 멈추기 (공기 저항 느낌)
                float newSpeed = Mathf.MoveTowards(currentZ, 0f, moveSpeed * 2f * Time.deltaTime);
                _rb.linearVelocity = new Vector3(0f, _rb.linearVelocity.y, newSpeed);
            }
        }
    }

    public void SetHookState(bool isHooking)
    {
        if (isHooking)
        {
            _canMove = false; // 키보드 이동 차단
            _rb.useGravity = true; 
            
            // [유니] 초기 드래그 설정 (기본은 1.0f)
            // 하지만 실제 스윙 중에는 PlayerHook에서 매 프레임 조절할 거야!
            _rb.linearDamping = 1.0f;
        }
        else
        {
            _canMove = true;
            _rb.useGravity = true; // 중력 복구
            _rb.linearDamping = 0f; // 원래대로 복구
        }
    }

    // [유니] 외부(PlayerHook)에서 드래그를 조절할 수 있게 허용!
    public void SetDrag(float drag)
    {
        _rb.linearDamping = drag;
    }

    // 2. 훅으로 당겨질 때 가속도 적용 (ForceMode.Acceleration)
    public void AddHookForce(Vector3 force)
    {
        _rb.AddForce(force, ForceMode.Acceleration);
        
        // [선택] 너무 빨라지면 속도 제한을 걸 수도 있어 (일단은 시원하게 뚫리게 둠!)
    }

    // 3. 대시 스택 충전 (훅 적중 시 호출)
    public void AddDashStack(int amount)
    {
        _currentDashCharges = Mathf.Min(_currentDashCharges + amount, maxDashCharges);
        Debug.Log($"🔋 대시 스택 충전! (현재: {_currentDashCharges})");
    }

    // ... (TryJump, PerformWallJump 등 기존 코드와 동일) ...
    // ... (아래는 코드가 너무 길어지니 생략했지만, 오빠가 쓰던 함수들 그대로 두면 돼!) ...
    // ... (빠른 복붙을 위해 필요한 함수들만 다시 적어줄게) ...

    private void TryJump()
    {
        // [유니] 아래 방향키를 누르고 있을 때! (드랍을 하거나, 점프를 안 하거나)
        if (_moveInput.y < -0.5f)
        {
            // [유니] 드랍 가능한 플랫폼 위에 있다면? 슝~ 아래로 통과!
            if (_currentFunctionPlatform != null)
            {
                StartCoroutine(DisableCollisionRoutine(_currentFunctionPlatform));
            }

            // [유니] 일반 바닥이든 드랍 플랫폼이든, 아래키 누른 상태면 점프 입력을 먹어버리자! (점프 실행 X)
            _jumpBufferCounter = 0f;
            return;
        }

        // [유니] 벽타기 중이거나 벽에 붙어있을 때 점프 (벽 점프)
        if ((_isWallSliding || _isTouchingWall) && !_isGrounded)
        {
            PerformWallJump();
            return;
        }

        // [유니] 땅에 있거나 코요테 타임이 남았을 때 일반 점프!
        if (_coyoteTimeCounter > 0f)
        {
            _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
            _rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            _jumpBufferCounter = 0f;
            _coyoteTimeCounter = 0f;
        }
    }
    private void PerformWallJump() { float wallDir = transform.forward.z > 0 ? 1f : -1f; float jumpDirection = -wallDir; Vector3 force = new Vector3(0, wallJumpPower.y, jumpDirection * wallJumpPower.x); _rb.linearVelocity = Vector3.zero; _rb.AddForce(force, ForceMode.Impulse); Vector3 lookDir = new Vector3(0, 0, jumpDirection); transform.rotation = Quaternion.LookRotation(lookDir); StartCoroutine(DisableMoveRoutine()); _jumpBufferCounter = 0f; }
    private void WallSlide() { bool isPushingWall = (_moveInput.x > 0 && transform.forward.z > 0) || (_moveInput.x < 0 && transform.forward.z < 0); if (_isTouchingWall && !_isGrounded && _rb.linearVelocity.y < 0 && isPushingWall) { _isWallSliding = true; _rb.linearVelocity = new Vector3(0, Mathf.Max(_rb.linearVelocity.y, -wallSlideSpeed), _rb.linearVelocity.z); } else { _isWallSliding = false; } }
    private void HandleGravity() { if (!_isGrounded && !_isWallSliding) { _rb.AddForce(Vector3.down * 9.81f * (gravityScale - 1f), ForceMode.Acceleration); /* [유니] 빠른 낙하 삭제 요청으로 주석 처리! */ } }
    private void ApplyRotation() { if (_moveInput.x != 0) { Vector3 lookDir = new Vector3(0, 0, _moveInput.x); transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), rotationSpeed * Time.deltaTime); } }
    private void CutJumpVelocity() { _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, _rb.linearVelocity.y * jumpCutMultiplier, _rb.linearVelocity.z); }
    private void UpdateTimers() { if (_isGrounded) _coyoteTimeCounter = coyoteTime; else _coyoteTimeCounter -= Time.deltaTime; if (_jumpBufferCounter > 0) _jumpBufferCounter -= Time.deltaTime; }
    private void CheckSurroundings() { _isGrounded = false; _currentFunctionPlatform = null; Collider[] colliders = Physics.OverlapSphere(groundCheckPos.position, checkRadius, groundLayer); if (colliders.Length > 0) { _isGrounded = true; foreach (var col in colliders) { if (col.TryGetComponent(out PlatformFunction platform)) { _currentFunctionPlatform = platform; break; } } } _isTouchingWall = Physics.CheckSphere(wallCheckPos.position, checkRadius, wallLayer); }
    private IEnumerator DisableCollisionRoutine(PlatformFunction platform) { Collider platformCollider = platform.platformCollider; Physics.IgnoreCollision(_playerCollider, platformCollider, true); yield return new WaitForSeconds(dropDisableTime); Physics.IgnoreCollision(_playerCollider, platformCollider, false); }
    private IEnumerator DisableMoveRoutine() { _canMove = false; yield return new WaitForSeconds(wallJumpStopControlTime); _canMove = true; }
    private void OnDrawGizmos() { if (groundCheckPos != null) { Gizmos.color = _isGrounded ? Color.green : Color.red; Gizmos.DrawWireSphere(groundCheckPos.position, checkRadius); } if (wallCheckPos != null) { Gizmos.color = _isTouchingWall ? Color.blue : Color.red; Gizmos.DrawWireSphere(wallCheckPos.position, checkRadius); } }
}