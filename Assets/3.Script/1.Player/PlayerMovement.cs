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
    [SerializeField] private float jumpCooldown = 0.2f;

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
    public bool CanMove => _canMove;
    private bool _isHookingState = false;
    public bool IsHookingState => _isHookingState;

    public event System.Action OnJumpEvent;
    public event System.Action OnDashEvent;

    private bool _isDashing;
    public bool IsDashing => _isDashing;
    private int _currentDashCharges;
    public int CurrentDashCharges => _currentDashCharges;
    public int MaxDashCharges => maxDashCharges;
    private float _dashRechargeTimer;

    private float _coyoteTimeCounter;
    private float _jumpBufferCounter;
    private float _lastJumpTime;
    private PlatformFunction _currentFunctionPlatform;

    private Transform _currentPlatformTransform;
    private Vector3 _lastPlatformPosition;

    private void Awake()
    {
        TryGetComponent(out _rb);
        TryGetComponent(out _playerCollider);

        if (playerAim == null) playerAim = GetComponent<PlayerAim>();

        if (ghostTrail == null) ghostTrail = GetComponentInChildren<GhostTrail>();

        _rb.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezeRotation;

        if (_rb != null)
        {
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

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

        HandleMovingPlatform();

        if (_isDashing)
        {
            return;
        }

        if (_canMove)
        {
            Move();
        }

        bool isSlidingFast = !_isHookingState && _isGrounded && Mathf.Abs(_rb.linearVelocity.z) > moveSpeed * 1.1f;

        if ((_isHookingState && _isGrounded) || isSlidingFast)
        {
            if (_playerCollider != null && _playerCollider.material != null)
            {
                _playerCollider.material.dynamicFriction = 0f;
                _playerCollider.material.staticFriction = 0f;
                _playerCollider.material.frictionCombine = PhysicsMaterialCombine.Minimum;
            }
        }
        else
        {

            if (_playerCollider != null && _playerCollider.material != null)
            {
                _playerCollider.material.dynamicFriction = 0.6f;
                _playerCollider.material.staticFriction = 0.6f;
                _playerCollider.material.frictionCombine = PhysicsMaterialCombine.Average;
            }
        }

        ApplyRotation();

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

            if (Time.time >= _lastJumpTime + jumpCooldown)
            {
                _jumpBufferCounter = jumpBufferTime;
            }
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
                OnDashEvent?.Invoke();
                StartCoroutine(DashRoutine());
            }
        }
    }

    public void OnHack(InputAction.CallbackContext context)
    {
        if (Core.GameManager.Instance != null && Core.GameManager.Instance.IsDialogueActive) return;

        if (context.started)
        {
            float hackRadius = 2000f;
            Collider[] hits = Physics.OverlapSphere(transform.position, hackRadius);

            bool anyHacked = false;

            Transform hackTarget = null;

            foreach (var hit in hits)
            {
                if (hit.TryGetComponent(out BossHealth boss) || (hit.transform.parent != null && hit.transform.parent.TryGetComponent(out boss)))
                {
                    hackTarget = boss.transform;
                    break;
                }
                else if (hackTarget == null)
                {
                    if (hit.TryGetComponent(out BaseEnemy enemy) || (hit.transform.parent != null && hit.transform.parent.TryGetComponent(out enemy)))
                    {
                        hackTarget = enemy.transform;
                    }
                }
            }

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

                if (hit.TryGetComponent(out EnemyMissile missile) || (hit.transform.parent != null && hit.transform.parent.TryGetComponent(out missile)))
                {

                    if (!missile.IsFrozen) continue;

                    if (hackTarget != null)
                    {
                        missile.HackReverse(hackTarget);
                        anyHacked = true;

                        if (Core.VFXManager.Instance != null)
                        {
                             Core.VFXManager.Instance.PlayHackExplosion(missile.transform.position);
                        }
                    }
                    else
                    {

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

            }
        }
    }

    private IEnumerator DashRoutine()
    {
        _isDashing = true;
        _currentDashCharges--;
        _dashRechargeTimer = 0f;

        int playerLayer = gameObject.layer;

        int projectileLayer = LayerMask.NameToLayer("Projectile");
        if (projectileLayer != -1)
        {
             Physics.IgnoreLayerCollision(playerLayer, projectileLayer, true);
        }

        int passMask = dashPassLayer.value;
        if (passMask == 0)
        {
            passMask = LayerMask.GetMask("Enemy", "Projectile", "Default");
        }

        for (int i = 0; i < 32; i++)
        {
            if ((passMask & (1 << i)) != 0)
            {
                Physics.IgnoreLayerCollision(playerLayer, i, true);
            }
        }

        Vector3 mousePos = playerAim.GetAimWorldPosition();
        Vector3 dashDir = (mousePos - transform.position).normalized;

        dashDir = new Vector3(0, dashDir.y, dashDir.z).normalized;

        float currentSpeed = _rb.linearVelocity.magnitude;
        float actualDashSpeed = Mathf.Max(dashSpeed, currentSpeed);

        _rb.linearVelocity = dashDir * actualDashSpeed;

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

        if (Core.PostProcessManager.Instance != null)
        {
            Core.PostProcessManager.Instance.TriggerChromaticAberration(1.0f, 0.5f);
        }

        if (Core.CameraEffectManager.Instance != null)
        {
            Core.CameraEffectManager.Instance.PunchFOV(dashFovAmount, dashFovDuration);
        }

        while (elapsedTime < dashDuration || (isOverlappingEnemy && elapsedTime < dashDuration + maxExtensionTime))
        {

            Vector3 currentVel = _rb.linearVelocity;
            float speedAlongDash = Vector3.Dot(currentVel, dashDir);

            if (elapsedTime > 0f && speedAlongDash < actualDashSpeed * 0.3f)
            {

                if (Mathf.Abs(dashDir.y) < 0.1f && currentVel.y <= 0f)
                {
                    _rb.linearVelocity = new Vector3(currentVel.x, 0f, currentVel.z);
                }
                else
                {
                    _rb.linearVelocity = currentVel;
                }
            }
            else
            {

                _rb.linearVelocity = dashDir * actualDashSpeed;
            }

            if (ghostTrail != null)
            {

                float sqrDistance = (transform.position - lastGhostPos).sqrMagnitude;
                if (sqrDistance >= ghostSpacing * ghostSpacing)
                {
                    ghostTrail.ShowGhost();
                    lastGhostPos = transform.position;
                }
            }

            isOverlappingEnemy = false;

            Vector3 dashBoxCenter = transform.position + Vector3.up * 1.0f;
            Vector3 tightExtents = new Vector3(0.8f, 0.5f, 0.3f);
            Vector3 looseExtents = new Vector3(1.2f, 0.8f, 1.5f);

            int castMask = dashPassLayer.value != 0 ? dashPassLayer.value : Physics.AllLayers;
            Collider[] tightHits = Physics.OverlapBox(dashBoxCenter, tightExtents, Quaternion.identity, castMask);
            Collider[] looseHits = Physics.OverlapBox(dashBoxCenter, looseExtents, Quaternion.identity, castMask);

            bool hitValidShield = false;
            EnemyShield blockingShield = null;

            foreach (var hit in tightHits)
            {
                if (hit.TryGetComponent(out EnemyShield shield) || (hit.transform.parent != null && hit.transform.parent.TryGetComponent(out shield)))
                {
                    BaseEnemy shieldOwner = shield.GetComponentInParent<BaseEnemy>();
                    if (shieldOwner != null && shieldOwner.IsFrozen) continue;

                    if (Vector3.Dot(dashDir, shield.transform.forward) <= 0)
                    {
                        hitValidShield = true;
                        blockingShield = shield;
                        break;
                    }
                }
            }

            if (hitValidShield && blockingShield != null)
            {
                Vector3 bounceDir = (transform.position - blockingShield.transform.position).normalized;
                _rb.linearVelocity = bounceDir * blockingShield.BounceForce;
                blockingShield.OnBlock(transform.position);

                if (TryGetComponent(out PlayerHealth ph))
                {
                    ph.TakeDamage(1);
                }

                _isDashing = false;
                _currentDashCharges = 0;
                yield break;
            }

            foreach (var hit in tightHits)
            {
                if (hit.TryGetComponent(out BaseEnemy enemy) || (hit.transform.parent != null && hit.transform.parent.TryGetComponent(out enemy)))
                {
                    if (enemy.IsFrozen)
                    {
                        isOverlappingEnemy = true;
                        continue;
                    }

                    enemy.Freeze();
                    isOverlappingEnemy = true;

                    if (!hasRecharged)
                    {
                        AddDashStack(1);
                        hasRecharged = true;
                    }
                }
            }

            foreach (var hit in looseHits)
            {
                if (hit.TryGetComponent(out EnemyMissile missile) || (hit.transform.parent != null && hit.transform.parent.TryGetComponent(out missile)))
                {
                    if (missile.IsHacked || missile.IsFrozen)
                    {
                        isOverlappingEnemy = true;
                        continue;
                    }

                    missile.SetFrozen(true);
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

                if (Physics.BoxCast(dashBoxCenter, tightExtents, dashDir, out RaycastHit hit, Quaternion.identity, 0.5f, castMask))
                {
                     if (hit.collider.TryGetComponent(out BaseEnemy enemy) || (hit.transform.parent != null && hit.transform.parent.TryGetComponent(out enemy)))
                     {
                         isOverlappingEnemy = true;
                     }
                }

                if (!isOverlappingEnemy && Physics.BoxCast(dashBoxCenter, looseExtents, dashDir, out RaycastHit mHit, Quaternion.identity, 3.0f, castMask))
                {
                     if (mHit.collider.TryGetComponent(out EnemyMissile m) || (mHit.transform.parent != null && mHit.transform.parent.TryGetComponent(out m)))
                     {
                         isOverlappingEnemy = true;
                     }
                }
            }

            if (hasRecharged && !bulletTimeTriggered && Core.GameManager.Instance != null)
            {
                 Core.GameManager.Instance.TriggerBulletTime(dashBulletTimeDuration, dashBulletTimeScale, true);
                 bulletTimeTriggered = true;
            }

            elapsedTime += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        int projectileLayerEnd = LayerMask.NameToLayer("Projectile");
         if (projectileLayerEnd != -1)
        {
             Physics.IgnoreLayerCollision(playerLayer, projectileLayerEnd, false);
        }

        int passMaskEnd = dashPassLayer.value;
        if (passMaskEnd == 0) passMaskEnd = LayerMask.GetMask("Enemy", "Projectile", "Default");

        for (int i = 0; i < 32; i++)
        {
            if ((passMaskEnd & (1 << i)) != 0)
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

        if (!_isHookingState)
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

                    float accel = _isGrounded ? moveSpeed * 10f : moveSpeed * 5f;
                    float newSpeed = Mathf.MoveTowards(currentZ, targetSpeedZ, accel * Time.deltaTime);
                    _rb.linearVelocity = new Vector3(0f, _rb.linearVelocity.y, newSpeed);
                }
            }
            else
            {

                float decel = _isGrounded ? moveSpeed * 7f : moveSpeed * 2f;
                float newSpeed = Mathf.MoveTowards(currentZ, 0f, decel * Time.deltaTime);
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
            _lastJumpTime = Time.time;

            OnJumpEvent?.Invoke();
        }
    }

    private void PerformWallJump()
    {

        float zDist = Mathf.Abs(wallCheckPos.localPosition.z);
        if (zDist < 0.1f) zDist = 0.5f;
        Vector3 rightPos = transform.position + new Vector3(0, wallCheckPos.localPosition.y, zDist);

        bool isRightWall = Physics.CheckSphere(rightPos, checkRadius, wallLayer | groundLayer);

        float wallDir = isRightWall ? 1f : -1f;
        float jumpDirection = -wallDir;
        Vector3 force = new Vector3(0, wallJumpPower.y, jumpDirection * wallJumpPower.x);
        _rb.linearVelocity = Vector3.zero;
        _rb.AddForce(force, ForceMode.Impulse);

        Vector3 lookDir = new Vector3(0, 0, jumpDirection);
        transform.rotation = Quaternion.LookRotation(lookDir);

        if (_disableMoveCoroutine != null) StopCoroutine(_disableMoveCoroutine);
        _disableMoveCoroutine = StartCoroutine(DisableMoveRoutine());

        _jumpBufferCounter = 0f;
        _lastJumpTime = Time.time;
        OnJumpEvent?.Invoke();
    }

    private void WallSlide()
    {

        float zDist = Mathf.Abs(wallCheckPos.localPosition.z);
        if (zDist < 0.1f) zDist = 0.5f;
        Vector3 rightPos = transform.position + new Vector3(0, wallCheckPos.localPosition.y, zDist);
        Vector3 leftPos = transform.position + new Vector3(0, wallCheckPos.localPosition.y, -zDist);

        bool touchRight = Physics.CheckSphere(rightPos, checkRadius, wallLayer | groundLayer);
        bool touchLeft = Physics.CheckSphere(leftPos, checkRadius, wallLayer | groundLayer);

        bool isPushingWall = (_moveInput.x > 0 && touchRight) || (_moveInput.x < 0 && touchLeft);

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

            float maxFallSpeed = fastFallSpeed * 2.5f;
            if (_rb.linearVelocity.y < -maxFallSpeed)
            {
                _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, -maxFallSpeed, _rb.linearVelocity.z);
            }
        }
    }

    private void HandleMovingPlatform()
    {

        if (!_canMove)
        {
            _currentPlatformTransform = null;
            return;
        }

        if (_currentFunctionPlatform != null)
        {

            if (_currentPlatformTransform != _currentFunctionPlatform.transform)
            {
                _currentPlatformTransform = _currentFunctionPlatform.transform;
                _lastPlatformPosition = _currentPlatformTransform.position;
            }

            Vector3 platformDelta = _currentPlatformTransform.position - _lastPlatformPosition;

            if (platformDelta.sqrMagnitude > 0f)
            {
                _rb.position += platformDelta;
            }

            _lastPlatformPosition = _currentPlatformTransform.position;
        }
        else
        {
            _currentPlatformTransform = null;
        }
    }

    private void ApplyRotation()
    {

        if (playerAim != null)
        {
            float zDir = playerAim.GetAimWorldPosition().z > transform.position.z ? 1f : -1f;

            if (_isHookingState)
            {
                zDir = -zDir;
            }

            Vector3 lookDir = new Vector3(0, 0, zDir);
            transform.rotation = Quaternion.LookRotation(lookDir);
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

        float zDist = Mathf.Abs(wallCheckPos.localPosition.z);
        if (zDist < 0.1f) zDist = 0.5f;
        Vector3 rightPos = transform.position + new Vector3(0, wallCheckPos.localPosition.y, zDist);
        Vector3 leftPos = transform.position + new Vector3(0, wallCheckPos.localPosition.y, -zDist);

        bool touchRight = Physics.CheckSphere(rightPos, checkRadius, wallLayer | groundLayer);
        bool touchLeft = Physics.CheckSphere(leftPos, checkRadius, wallLayer | groundLayer);
        _isTouchingWall = touchRight || touchLeft;
    }

    private IEnumerator DisableCollisionRoutine(PlatformFunction platform)
    {
        Collider platformCollider = platform.platformCollider;
        Physics.IgnoreCollision(_playerCollider, platformCollider, true);
        yield return new WaitForSeconds(dropDisableTime);
        Physics.IgnoreCollision(_playerCollider, platformCollider, false);
    }

    private Coroutine _disableMoveCoroutine;

    private IEnumerator DisableMoveRoutine(float overrideTime = -1f)
    {
        _canMove = false;
        float waitTime = overrideTime > 0f ? overrideTime : wallJumpStopControlTime;
        yield return new WaitForSeconds(waitTime);
        _canMove = true;
    }

    public void ApplyKnockback(Vector3 dir, float force, float disableTime = 0.25f)
    {
        _rb.linearVelocity = Vector3.zero;
        _rb.AddForce(dir.normalized * force, ForceMode.Impulse);

        _currentFunctionPlatform = null;
        _currentPlatformTransform = null;

        if (_disableMoveCoroutine != null) StopCoroutine(_disableMoveCoroutine);
        _disableMoveCoroutine = StartCoroutine(DisableMoveRoutine(disableTime));
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