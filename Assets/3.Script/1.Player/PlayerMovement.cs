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
    [SerializeField] private float dashDistance = 8f; // 이제 거리를 직접 설정!
    [SerializeField] private float dashDuration = 0.2f; // 대시가 지속되는 시간
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
    [SerializeField] private float wallClimbSpeed = 4f;
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
    public bool IsWallSliding => _isWallSliding;
    private float _lastWallDir = 1f;
    private bool _isJumpPressed;
    private bool _canMove = true;
    public bool CanMove => _canMove;
    public Vector3 LastPosition { get; private set; }
    private bool _isHookingState = false;
    public bool IsHookingState => _isHookingState;
    private bool _isDead = false;

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
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            // 🎯 훅 스윙 중 멈칫하거나 떨리는 현상 해결! 리지드바디가 잠들지 않게 해줄게 오빠! 🥰
            _rb.sleepThreshold = 0f;
        }

        _currentDashCharges = maxDashCharges;
    }

    private void Update()
    {
        UpdateTimers();
        HandleDashRecharge();
    }

    public void SetDeadState(bool isDead)
    {
        _isDead = isDead;
        if (isDead)
        {
            _moveInput = Vector2.zero;
        }
    }

    private void FixedUpdate()
    {
        if (_isDead)
        {
            LastPosition = transform.position;
            return;
        }

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
        
        LastPosition = transform.position;
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        if (_isDead) return;
        if (Core.GameManager.Instance != null && Core.GameManager.Instance.IsDialogueActive)
        {
            _moveInput = Vector2.zero;
            return;
        }

        _moveInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (_isDead) return;
        if (Core.GameManager.Instance != null && Core.GameManager.Instance.IsDialogueActive) 
        {
            _isJumpPressed = false;
            _jumpBufferCounter = 0f;
            return;
        }

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
        if (_isDead) return;
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
        if (_isDead) return;
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

        if (TryGetComponent(out PlayerHealth ph))
        {
            ph.SetDashInvincible(true);
        }

        int playerLayer = gameObject.layer;
        int projectileLayer = LayerMask.NameToLayer("Projectile");
        if (projectileLayer != -1)
        {
            Physics.IgnoreLayerCollision(playerLayer, projectileLayer, true);
        }

        int passMask = dashPassLayer.value;
        if (passMask == 0) passMask = LayerMask.GetMask("Enemy", "Projectile", "Default");

        for (int i = 0; i < 32; i++)
        {
            if ((passMask & (1 << i)) != 0)
            {
                Physics.IgnoreLayerCollision(playerLayer, i, true);
            }
        }

        Vector3 mousePos = playerAim.GetAimWorldPosition();
        Vector3 dashVector = mousePos - transform.position;
        Vector3 dashDir = dashVector.normalized;

        dashDir = new Vector3(0, dashDir.y, dashDir.z).normalized;

        float currentSpeed = _rb.linearVelocity.magnitude;
        float baseDashSpeed = dashDistance / dashDuration;
        float actualDashSpeed = Mathf.Max(baseDashSpeed, currentSpeed); 
        float actualDashDuration = dashDuration; 

        _rb.linearVelocity = dashDir * actualDashSpeed;

        float elapsedTime = 0f;
        bool isOverlappingEnemy = false;
        bool hasRecharged = false;
        bool bulletTimeTriggered = false;
        Vector3 lastGhostPos = transform.position;

        // 🎯 렌더러와 글리치 세팅 (여기 있어야 루프 안에서도 누군지 알아요!)
        Renderer playerRen = GetComponentInChildren<Renderer>();
        MaterialPropertyBlock mpb = new MaterialPropertyBlock();
        int glitchId = Shader.PropertyToID("_GlitchPower");
        float postPierceTimer = 0f; // 🎯 관통 제동 타이머 복구!

        if (Core.PostProcessManager.Instance != null)
        {
            Core.PostProcessManager.Instance.TriggerChromaticAberration(1.2f, 0.4f);
        }

        if (Core.CameraEffectManager.Instance != null)
        {
            Core.CameraEffectManager.Instance.PunchFOV(dashFovAmount, dashFovDuration);
        }

        // 🎯 루프 조건: 기본 시간 내이거나, 아직 적과 겹쳐있거나, 관통 후 제동이 덜 끝났을 때!
        // 단, 1.0초 이상 대시가 지속되면 벽에 끼거나 버그가 생긴 걸로 보고 강제로 종료할게 오빠! 🥰
        while ((elapsedTime < actualDashDuration || isOverlappingEnemy || (hasRecharged && postPierceTimer > 0 && postPierceTimer < 0.05f)) 
               && elapsedTime < 1.0f) 
        {
            if (Core.GameManager.Instance != null && Core.GameManager.Instance.IsDialogueActive)
            {
                break;
            }

            // 🎯 플레이어 본체 글리치 연출
            if (playerRen != null)
            {
                playerRen.GetPropertyBlock(mpb);
                mpb.SetFloat(glitchId, Random.Range(0.25f, 0.55f));
                playerRen.SetPropertyBlock(mpb);
            }

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

            // 🎯 관통 및 충돌 감지
            bool isCurrentlyInside = false; // 현재 물리적으로 겹쳐 있는가?
            Vector3 dashBoxCenter = transform.position + Vector3.up * 1.0f;
            Vector3 tightExtents = new Vector3(0.8f, 0.5f, 0.8f); // 🎯 깊이를 좀 더 줘서 적 몸속에서 멈추지 않게!
            Vector3 looseExtents = new Vector3(1.2f, 0.8f, 1.8f);

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

                if (TryGetComponent(out PlayerHealth phDamage))
                {
                    phDamage.SetDashInvincible(false);
                    phDamage.TakeDamage(1);
                }

                _isDashing = false;
                _currentDashCharges = 0;
                
                if (playerRen != null)
                {
                    playerRen.GetPropertyBlock(mpb);
                    mpb.SetFloat(glitchId, 0f);
                    playerRen.SetPropertyBlock(mpb);
                }
                yield break;
            }

            foreach (var hit in tightHits)
            {
                if (hit.TryGetComponent(out BaseEnemy enemy) || (hit.transform.parent != null && hit.transform.parent.TryGetComponent(out enemy)))
                {
                    if (enemy.IsFrozen) { isCurrentlyInside = true; continue; }
                    enemy.Freeze();
                    isCurrentlyInside = true;
                    if (!hasRecharged) { AddDashStack(1); hasRecharged = true; }
                }
            }

            foreach (var hit in looseHits)
            {
                if (hit.TryGetComponent(out EnemyMissile missile) || (hit.transform.parent != null && hit.transform.parent.TryGetComponent(out missile)))
                {
                    if (missile.IsHacked || missile.IsFrozen) { isCurrentlyInside = true; continue; }
                    missile.SetFrozen(true);
                    isCurrentlyInside = true;
                    if (!hasRecharged) { AddDashStack(1); hasRecharged = true; }
                }
            }

            // 미래의 적 감지 (대시 연장용)
            bool isSeeingEnemyAhead = false;
            if (!isCurrentlyInside && !hasRecharged)
            {
                if (Physics.BoxCast(dashBoxCenter, tightExtents, dashDir, Quaternion.identity, 0.5f, castMask)) isSeeingEnemyAhead = true;
                if (!isSeeingEnemyAhead && Physics.BoxCast(dashBoxCenter, looseExtents, dashDir, Quaternion.identity, 2.5f, castMask)) isSeeingEnemyAhead = true;
            }

            // 최종 상태 업데이트
            isOverlappingEnemy = isCurrentlyInside || isSeeingEnemyAhead;

            // 🎯 관통 제동 (Penetration Brake) - 0.05초의 여운!
            if (hasRecharged && !isCurrentlyInside)
            {
                postPierceTimer += Time.deltaTime;
                if (postPierceTimer >= 0.05f)
                {
                    _rb.linearVelocity = Vector3.zero; 
                    isOverlappingEnemy = false; // 루프 종료 유도
                    break;
                }
            }
            else
            {
                postPierceTimer = 0f; 
            }

            if (hasRecharged && !bulletTimeTriggered && Core.GameManager.Instance != null)
            {
                Core.GameManager.Instance.TriggerBulletTime(dashBulletTimeDuration, dashBulletTimeScale, true);
                bulletTimeTriggered = true;
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        if (playerRen != null)
        {
            playerRen.GetPropertyBlock(mpb);
            mpb.SetFloat(glitchId, 0f);
            playerRen.SetPropertyBlock(mpb);
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
        if (TryGetComponent(out PlayerHealth phEnd))
        {
            phEnd.SetDashInvincible(false);
        }
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
                    float decayed = Mathf.MoveTowards(currentZ, targetSpeedZ, 10f * Time.fixedDeltaTime);
                    _rb.linearVelocity = new Vector3(0f, _rb.linearVelocity.y, decayed);
                }
                else
                {
                    float accel = _isGrounded ? moveSpeed * 10f : moveSpeed * 5f;
                    float newSpeed = Mathf.MoveTowards(currentZ, targetSpeedZ, accel * Time.fixedDeltaTime);
                    _rb.linearVelocity = new Vector3(0f, _rb.linearVelocity.y, newSpeed);
                }
            }
            else
            {
                float decel = _isGrounded ? moveSpeed * 7f : moveSpeed * 2f;
                float newSpeed = Mathf.MoveTowards(currentZ, 0f, decel * Time.fixedDeltaTime);
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
        // 🎯 훅을 건 상태에서는 벽타기가 시작되지 않게 막았어 오빠!
        if (_isHookingState)
        {
            _isWallSliding = false;
            return;
        }

        float zDist = Mathf.Abs(wallCheckPos.localPosition.z);
        if (zDist < 0.1f) zDist = 0.5f;
        Vector3 rightPos = transform.position + new Vector3(0, wallCheckPos.localPosition.y, zDist);
        Vector3 leftPos = transform.position + new Vector3(0, wallCheckPos.localPosition.y, -zDist);

        bool touchRight = Physics.CheckSphere(rightPos, checkRadius, wallLayer | groundLayer);
        bool touchLeft = Physics.CheckSphere(leftPos, checkRadius, wallLayer | groundLayer);

        if (touchRight) _lastWallDir = 1f;
        else if (touchLeft) _lastWallDir = -1f;

        bool isPushingWall = (_moveInput.x > 0 && touchRight) || (_moveInput.x < 0 && touchLeft);
        bool isPullingAway = (_moveInput.x < 0 && touchRight) || (_moveInput.x > 0 && touchLeft);

        if (_isWallSliding && !isPullingAway && _isTouchingWall)
        {
            isPushingWall = true;
        }

        if (_isTouchingWall && !_isGrounded && isPushingWall)
        {
            _isWallSliding = true;

            float vMove = _moveInput.y;

            if (vMove > 0.1f)
            {
                _rb.linearVelocity = new Vector3(0, wallClimbSpeed, _rb.linearVelocity.z);
            }
            else if (vMove < -0.1f)
            {
                _rb.linearVelocity = new Vector3(0, -wallClimbSpeed, _rb.linearVelocity.z);
            }
            else
            {
                if (_rb.linearVelocity.y < 0)
                {
                    _rb.linearVelocity = new Vector3(0, -wallSlideSpeed, _rb.linearVelocity.z);
                }
                else
                {
                    // If moving upwards from jump, let gravity reduce it or stay zero
                    _rb.linearVelocity = new Vector3(0, Mathf.Min(_rb.linearVelocity.y, 0f), _rb.linearVelocity.z);
                }
            }
        }
        else
        {
            if (_isWallSliding && _moveInput.y > 0.1f && !_isTouchingWall)
            {
                // Ledge Boost: Vault over the top of the wall!
                _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, 8f, _lastWallDir * 5f);
            }
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

    public void ApplyKnockback(Vector3 force)
    {
        if (_isDashing) return;
        _rb.linearVelocity = Vector3.zero;
        _rb.AddForce(force, ForceMode.Impulse);
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