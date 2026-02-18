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
    [SerializeField] private float dashSpeed = 40f;      
    [SerializeField] private float dashDuration = 0.15f; 
    [SerializeField] private int maxDashCharges = 2;     
    [SerializeField] private float dashCooldown = 3f;    
    [SerializeField] private LayerMask dashPassLayer;    
    [SerializeField] private float dashBulletTimeScale = 0.2f; 
    [SerializeField] private float dashBulletTimeDuration = 0.5f; 

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
    [SerializeField] private PlayerAim playerAim; 

    [Header("✨ Visuals")]
    [SerializeField] private GhostTrail ghostTrail;   
    [SerializeField] private float ghostSpacing = 0.5f; 
    [SerializeField] private float dashFovAmount = 10f;  
    [SerializeField] private float dashFovDuration = 0.3f; 

    private Rigidbody _rb;
    private Collider _playerCollider;
    private GameInput _input;
    private Vector2 _moveInput;
    public Vector2 MoveInput => _moveInput; 
    public LayerMask GroundLayer => groundLayer;
    public LayerMask WallLayer => wallLayer;

    private bool _isGrounded;
    public bool IsGrounded => _isGrounded; 
    private bool _isTouchingWall;
    private bool _isWallSliding;
    private bool _isJumpPressed;
    private bool _canMove = true;
    private bool _isHookingState = false; 
    
    private bool _isDashing;          
    private int _currentDashCharges;  
    public int CurrentDashCharges => _currentDashCharges; 
    public int MaxDashCharges => maxDashCharges;          
    private float _dashRechargeTimer; 

    private float _coyoteTimeCounter;
    private float _jumpBufferCounter;
    private PlatformFunction _currentFunctionPlatform;

    private void Awake()
    {
        TryGetComponent(out _rb);
        TryGetComponent(out _playerCollider);

        if (playerAim == null) playerAim = GetComponent<PlayerAim>();
        
        if (ghostTrail == null) ghostTrail = GetComponentInChildren<GhostTrail>();

        _rb.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezeRotation;

        _currentDashCharges = maxDashCharges;
    }

    private void Update()
    {
        UpdateTimers();
        HandleDashRecharge(); 
    }

    private void FixedUpdate()
    {
        CheckSurroundings();

        if (Core.GameManager.Instance != null && Core.GameManager.Instance.IsDialogueActive)
        {
            _moveInput = Vector2.zero;
        }

        if (_isDashing)
        {
            return;
        }

        if (_canMove || (_isHookingState && _isGrounded))
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

    public void OnMove(InputAction.CallbackContext context)
    {
        if (Core.GameManager.Instance != null && Core.GameManager.Instance.IsDialogueActive)
        {
            _moveInput = Vector2.zero;
            return;
        }

        _moveInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (Core.GameManager.Instance != null && Core.GameManager.Instance.IsDialogueActive) return;

        if (context.started || (context.performed && context.ReadValueAsButton()))
        {
            _jumpBufferCounter = jumpBufferTime;
            _isJumpPressed = true;
        }
        else if (context.canceled || !context.ReadValueAsButton())
        {
            _isJumpPressed = false;
            if (_rb.linearVelocity.y > 0f && !_isWallSliding && !_isDashing)
            {
                CutJumpVelocity();
            }
        }
    }

    public void OnDash(InputAction.CallbackContext context)
    {
        if (Core.GameManager.Instance != null && Core.GameManager.Instance.IsDialogueActive) return;

        if (context.started)
        {
            if (_currentDashCharges > 0 && !_isDashing)
            {
                StartCoroutine(DashRoutine());
            }
        }
    }

    public void OnHack(InputAction.CallbackContext context)
    {
        if (Core.GameManager.Instance != null && Core.GameManager.Instance.IsDialogueActive) return;

        if (context.started)
        {
            float hackRadius = 20f;
            Collider[] hits = Physics.OverlapSphere(transform.position, hackRadius);
            
            bool anyHacked = false;
            
            // [New] 해킹 시 타겟 찾기 (우선순위: 보스 -> 일반 적)
            Transform hackTarget = null;
            
            // 1. 보스 찾기
            BossHealth boss = FindAnyObjectByType<BossHealth>();
            if (boss != null) hackTarget = boss.transform;

            // 2. 보스 없으면 일반 적 찾기 (가장 가까운)
            if (hackTarget == null)
            {
                BaseEnemy randomEnemy = FindAnyObjectByType<BaseEnemy>();
                if (randomEnemy != null) hackTarget = randomEnemy.transform;
            }

            foreach (var hit in hits)
            {
                // 1. 일반 적 해킹 (기존 로직)
                if (hit.TryGetComponent(out BaseEnemy enemy) || (hit.transform.parent != null && hit.transform.parent.TryGetComponent(out enemy)))
                {
                    if (enemy.IsFrozen)
                    {
                        enemy.OnHack(); 
                        anyHacked = true;
                    }
                }

                // 2. [New] 미사일 해킹
                if (hit.TryGetComponent(out EnemyMissile missile) || (hit.transform.parent != null && hit.transform.parent.TryGetComponent(out missile)))
                {
                    // [Fix] 얼어있는 미사일만 해킹 가능 & 이미 해킹된 건 무시
                    if (!missile.IsFrozen) continue;

                    // 타겟이 있어야 해킹 의미가 있음
                    if (hackTarget != null) 
                    {
                        missile.HackReverse(hackTarget);
                        anyHacked = true;

                        // 해킹 이펙트
                        if (Core.VFXManager.Instance != null)
                        {
                             Core.VFXManager.Instance.PlayHackExplosion(missile.transform.position);
                        }
                    }
                    else
                    {
                        // 타겟이 아예 없으면 그냥 제자리 폭발 (자폭)
                        if (Core.VFXManager.Instance != null)
                        {
                             Core.VFXManager.Instance.PlayHackExplosion(missile.transform.position);
                        }
                        Destroy(missile.gameObject);
                        anyHacked = true;
                    }
                }
            }

            if (anyHacked)
            {
                // [Fix] VFXManager에서 쉐이크까지 처리하므로 여기서는 호출 안 함
                // if (Core.GameManager.Instance != null && !anyHacked) -> 해킹된 게 없으면 피드백 줄 수도 있지만 일단 생략
            }
        }
    }

    private IEnumerator DashRoutine()
    {
        _isDashing = true;
        _currentDashCharges--; 
        _dashRechargeTimer = 0f; 

        int playerLayer = gameObject.layer;
        
        // [New] Projectile 레이어 명시적 무시 (대시 중에는 미사일에 맞으면 안 됨)
        int projectileLayer = LayerMask.NameToLayer("Projectile");
        if (projectileLayer != -1)
        {
             Physics.IgnoreLayerCollision(playerLayer, projectileLayer, true);
        }

        for (int i = 0; i < 32; i++)
        {
            if ((dashPassLayer.value & (1 << i)) != 0)
            {
                Physics.IgnoreLayerCollision(playerLayer, i, true);
            }
        }
        
        Vector3 mousePos = playerAim.GetAimWorldPosition();
        Vector3 dashDir = (mousePos - transform.position).normalized;

        dashDir = new Vector3(0, dashDir.y, dashDir.z).normalized;

        _rb.linearVelocity = dashDir * dashSpeed;

        float elapsedTime = 0f;
        float maxExtensionTime = 0.5f; 
        bool isOverlappingEnemy = false;
        bool hasRecharged = false; 
        bool bulletTimeTriggered = false; 

        Vector3 lastGhostPos = transform.position; 

        if (ghostTrail != null) 
        {
            ghostTrail.ShowGhost();
            lastGhostPos = transform.position;
        }

        // 색수차 효과 (대시 시작 시 한 번만 호출)
        if (Core.PostProcessManager.Instance != null)
        {
            Core.PostProcessManager.Instance.TriggerChromaticAberration(1.0f, 0.5f); 
        }

        // FOV 킥 (카메라 줌아웃 효과)
        if (Core.CameraEffectManager.Instance != null)
        {
            Core.CameraEffectManager.Instance.PunchFOV(dashFovAmount, dashFovDuration); 
        }

        while (elapsedTime < dashDuration || (isOverlappingEnemy && elapsedTime < dashDuration + maxExtensionTime))
        {
            _rb.linearVelocity = dashDir * dashSpeed;

            if (ghostTrail != null)
            {
                float distance = Vector3.Distance(transform.position, lastGhostPos);
                if (distance >= ghostSpacing) 
                {
                    ghostTrail.ShowGhost();
                    lastGhostPos = transform.position;
                }
            }

            isOverlappingEnemy = false; 
            Collider[] hits = Physics.OverlapCapsule(transform.position + Vector3.up * 0.5f, transform.position + Vector3.up * 1.5f, 0.25f, dashPassLayer);
            foreach (var hit in hits)
            {
                // [Fix] 방패(Shield)에 부딪히면 튕겨나감 (대시 관통 불가)
                if (hit.TryGetComponent(out EnemyShield shield))
                {
                    // 1. 적이 이미 얼어있으면 방패 무시 (관통 허용)
                    // 부모나 자신에서 BaseEnemy 찾기
                    BaseEnemy shieldOwner = shield.GetComponentInParent<BaseEnemy>();
                    if (shieldOwner != null && shieldOwner.IsFrozen) continue;

                    // 2. 뒤에서 때리면 방패 무시 (백어택 허용)
                    // 내 진행 방향(dashDir)과 방패 앞면(forward)이 같은 방향(Dot > 0)이면 뒤에서 때린 것
                    // 반대 방향(Dot < 0)이면 정면 충돌
                    if (Vector3.Dot(dashDir, shield.transform.forward) > 0)
                    {
                        continue; // 뒤에서 때림 -> 통과
                    }

                    Vector3 bounceDir = (transform.position - shield.transform.position).normalized;
                    _rb.linearVelocity = bounceDir * shield.BounceForce;
                    shield.OnBlock(transform.position); 
                    
                    _isDashing = false;
                    _currentDashCharges = 0; 
                    yield break;
                }

                // [New] 미사일(투사체)도 뚫고 지나가면서 얼리기!
                if (hit.TryGetComponent(out EnemyMissile missile) || (hit.transform.parent != null && hit.transform.parent.TryGetComponent(out missile)))
                {
                    missile.SetFrozen(true);
                    isOverlappingEnemy = true; // 적을 뚫은 것으로 간주 (불릿타임 발동)

                    if (!hasRecharged)
                    {
                        AddDashStack(1); // 미사일 뚫어도 대시 충전! (혜자 판정)
                        hasRecharged = true;
                    }
                    continue; 
                }

                if (hit.TryGetComponent(out BaseEnemy enemy) || (hit.transform.parent != null && hit.transform.parent.TryGetComponent(out enemy)))
                {
                    if (enemy.IsFrozen) continue; 

                    enemy.Freeze(); 
                    isOverlappingEnemy = true; 

                    if (!hasRecharged)
                    {
                        AddDashStack(1);
                        hasRecharged = true;
                    }
                }
            }
            
            if (!isOverlappingEnemy && !hasRecharged)
            {
                LayerMask combinedMask = dashPassLayer | wallLayer;
                if (Physics.CapsuleCast(transform.position + Vector3.up * 0.5f, transform.position + Vector3.up * 1.5f, 0.4f, dashDir, out RaycastHit hit, 1.5f, combinedMask))
                {
                     if (((1 << hit.collider.gameObject.layer) & dashPassLayer.value) != 0)
                     {
                         if (hit.collider.TryGetComponent(out BaseEnemy enemy) || (hit.transform.parent != null && hit.transform.parent.TryGetComponent(out enemy)))
                         {
                             if (!enemy.IsFrozen)
                             {
                                 isOverlappingEnemy = true;
                             }
                         }
                     }
                }
            }
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        if (hasRecharged && !bulletTimeTriggered && Core.GameManager.Instance != null)
        {
             Core.GameManager.Instance.TriggerBulletTime(dashBulletTimeDuration, dashBulletTimeScale, true);
        }

        int projectileLayerEnd = LayerMask.NameToLayer("Projectile");
         if (projectileLayerEnd != -1)
        {
             Physics.IgnoreLayerCollision(playerLayer, projectileLayerEnd, false);
        }

        for (int i = 0; i < 32; i++)
        {
            if ((dashPassLayer.value & (1 << i)) != 0)
            {
                Physics.IgnoreLayerCollision(playerLayer, i, false);
            }
        }
        
        _isDashing = false;
    }

    private void HandleDashRecharge()
    {
        if (_currentDashCharges < maxDashCharges)
        {
            _dashRechargeTimer += Time.deltaTime;

            if (_dashRechargeTimer >= dashCooldown)
            {
                _currentDashCharges++;
                _dashRechargeTimer = 0;
            }
        }
    }

    private void Move()
    {
        float targetSpeedZ = _moveInput.x * moveSpeed;

        // [Fix] 훅을 걸고 있더라도(_isHookingState) 땅에 발이 닿아있다면(_isGrounded)
        // 정상적인 지상 이동(Ground Movement)이 가능해야 함. (미끄러짐 방지 & A/D 이동 허용)
        if (_isGrounded)
        {
            _rb.linearVelocity = new Vector3(0f, _rb.linearVelocity.y, targetSpeedZ);
        }
        else
        {
            float currentZ = _rb.linearVelocity.z;

            if (Mathf.Abs(targetSpeedZ) > 0.1f)
            {
                bool isMovingFast = Mathf.Abs(currentZ) > moveSpeed;
                bool isSameDir = Mathf.Sign(currentZ) == Mathf.Sign(targetSpeedZ);

                if (isMovingFast && isSameDir)
                {
                    float decayed = Mathf.MoveTowards(currentZ, targetSpeedZ, 10f * Time.deltaTime); 
                    _rb.linearVelocity = new Vector3(0f, _rb.linearVelocity.y, decayed);
                }
                else
                {
                    float newSpeed = Mathf.MoveTowards(currentZ, targetSpeedZ, moveSpeed * 5f * Time.deltaTime);
                    _rb.linearVelocity = new Vector3(0f, _rb.linearVelocity.y, newSpeed);
                }
            }
            else
            {
                float newSpeed = Mathf.MoveTowards(currentZ, 0f, moveSpeed * 2f * Time.deltaTime);
                _rb.linearVelocity = new Vector3(0f, _rb.linearVelocity.y, newSpeed);
            }
        }
    }

    public void SetHookState(bool isHooking)
    {
        _isHookingState = isHooking; 

        if (isHooking)
        {
            _canMove = false; 
            _rb.useGravity = true; 
            
            _rb.linearDamping = 0.05f;
        }
        else
        {
            _canMove = true;
            _rb.useGravity = true; 
            _rb.linearDamping = 0f; 
        }
    }

    public void SetDrag(float drag)
    {
        _rb.linearDamping = drag;
    }

    public void AddHookForce(Vector3 force)
    {
        _rb.AddForce(force, ForceMode.Acceleration);
    }

    public void AddDashStack(int amount)
    {
        _currentDashCharges = Mathf.Min(_currentDashCharges + amount, maxDashCharges);
    }

    private void TryJump()
    {
        if (_isHookingState) return;

        if (_moveInput.y < -0.5f)
        {
            if (_currentFunctionPlatform != null)
            {
                StartCoroutine(DisableCollisionRoutine(_currentFunctionPlatform));
            }

            _jumpBufferCounter = 0f;
            return;
        }

        if ((_isWallSliding || _isTouchingWall) && !_isGrounded)
        {
            PerformWallJump();
            return;
        }

        if (_coyoteTimeCounter > 0f)
        {
            _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
            _rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            _jumpBufferCounter = 0f;
            _coyoteTimeCounter = 0f;
        }
    }

    private void PerformWallJump() 
    { 
        float wallDir = transform.forward.z > 0 ? 1f : -1f; 
        float jumpDirection = -wallDir; 
        Vector3 force = new Vector3(0, wallJumpPower.y, jumpDirection * wallJumpPower.x); 
        _rb.linearVelocity = Vector3.zero; 
        _rb.AddForce(force, ForceMode.Impulse); 
        Vector3 lookDir = new Vector3(0, 0, jumpDirection); 
        transform.rotation = Quaternion.LookRotation(lookDir); 
        StartCoroutine(DisableMoveRoutine()); 
        _jumpBufferCounter = 0f; 
    }

    private void WallSlide()
    {
        bool isPushingWall = (_moveInput.x > 0 && transform.forward.z > 0) || (_moveInput.x < 0 && transform.forward.z < 0);

        if (_isTouchingWall && !_isGrounded && _rb.linearVelocity.y < 0 && isPushingWall)
        {
            _isWallSliding = true;
            _rb.linearVelocity = new Vector3(0, -wallSlideSpeed, _rb.linearVelocity.z);
        }
        else
        {
            _isWallSliding = false;
        }
    }

    private void HandleGravity() 
    { 
        if (!_isGrounded && !_isWallSliding) 
        { 
            _rb.AddForce(Vector3.down * 9.81f * (gravityScale - 1f), ForceMode.Acceleration); 
        } 
    }

    private void ApplyRotation() 
    { 
        if (_moveInput.x != 0) 
        { 
            Vector3 lookDir = new Vector3(0, 0, _moveInput.x); 
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), rotationSpeed * Time.deltaTime); 
        } 
    }

    private void CutJumpVelocity() 
    { 
        _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, _rb.linearVelocity.y * jumpCutMultiplier, _rb.linearVelocity.z); 
    }

    private void UpdateTimers() 
    { 
        if (_isGrounded) _coyoteTimeCounter = coyoteTime; else _coyoteTimeCounter -= Time.deltaTime; 
        if (_jumpBufferCounter > 0) _jumpBufferCounter -= Time.deltaTime; 
    }

    private void CheckSurroundings() 
    { 
        _isGrounded = false; 
        _currentFunctionPlatform = null; 
        Collider[] colliders = Physics.OverlapSphere(groundCheckPos.position, checkRadius, groundLayer); 
        if (colliders.Length > 0) 
        { 
            _isGrounded = true; 
            foreach (var col in colliders) 
            { 
                if (col.TryGetComponent(out PlatformFunction platform)) 
                { 
                    _currentFunctionPlatform = platform; 
                    break; 
                } 
            } 
        } 
        _isTouchingWall = Physics.CheckSphere(wallCheckPos.position, checkRadius, wallLayer); 
    }

    private IEnumerator DisableCollisionRoutine(PlatformFunction platform) 
    { 
        Collider platformCollider = platform.platformCollider; 
        Physics.IgnoreCollision(_playerCollider, platformCollider, true); 
        yield return new WaitForSeconds(dropDisableTime); 
        Physics.IgnoreCollision(_playerCollider, platformCollider, false); 
    }

    private IEnumerator DisableMoveRoutine() 
    { 
        _canMove = false; 
        yield return new WaitForSeconds(wallJumpStopControlTime); 
        _canMove = true; 
    }

    private void OnDrawGizmos() 
    { 
        if (groundCheckPos != null) 
        { 
            Gizmos.color = _isGrounded ? Color.green : Color.red; 
            Gizmos.DrawWireSphere(groundCheckPos.position, checkRadius); 
        } 
        if (wallCheckPos != null) 
        { 
            Gizmos.color = _isTouchingWall ? Color.blue : Color.red; 
            Gizmos.DrawWireSphere(wallCheckPos.position, checkRadius); 
        } 
    }

    public bool ConsumeJumpInput()
    {
        if (_jumpBufferCounter > 0)
        {
            _jumpBufferCounter = 0f; 
            return true;
        }
        return false;
    }
}