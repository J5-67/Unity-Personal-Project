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
            foreach (var hit in hits)
            {
                if (hit.TryGetComponent(out BaseEnemy enemy) || (hit.transform.parent != null && hit.transform.parent.TryGetComponent(out enemy)))
                {
                    if (enemy.IsFrozen)
                    {
                        enemy.OnHack(); 
                        anyHacked = true;
                    }
                }
            }

            if (anyHacked)
            {
                if (VFX.HackVFXManager.Instance != null)
                {
                    VFX.HackVFXManager.Instance.TriggerMassiveGlitch();
                }
                else if (Core.GameManager.Instance != null)
                {
                    Core.GameManager.Instance.TriggerCameraShake(0.5f); 
                }
            }
        }
    }

    private IEnumerator DashRoutine()
    {
        _isDashing = true;
        _currentDashCharges--; 
        _dashRechargeTimer = 0f; 

        int playerLayer = gameObject.layer;
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

        if (_isGrounded && !_isHookingState)
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