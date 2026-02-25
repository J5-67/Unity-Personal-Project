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
    [SerializeField] private float jumpCooldown = 0.2f; // [New] 연속 점프 방지용 쿨타임

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
    public bool CanMove => _canMove; // [New] 넉백 등 조작 불가 상태 확인용
    private bool _isHookingState = false; 

    // [New] Animation Events
    public event System.Action OnJumpEvent;
    public event System.Action OnDashEvent;
    
    private bool _isDashing;          
    public bool IsDashing => _isDashing; // [New] 외부에서 대시 상태 확인용
    private int _currentDashCharges;  
    public int CurrentDashCharges => _currentDashCharges; 
    public int MaxDashCharges => maxDashCharges;          
    private float _dashRechargeTimer; 

    private float _coyoteTimeCounter;
    private float _jumpBufferCounter;
    private float _lastJumpTime; // [New] 마지막 점프 시간 추적
    private PlatformFunction _currentFunctionPlatform;
    
    // [New] Moving Platform Tracking
    private Transform _currentPlatformTransform;
    private Vector3 _lastPlatformPosition;

    private void Awake()
    {
        TryGetComponent(out _rb);
        TryGetComponent(out _playerCollider);

        if (playerAim == null) playerAim = GetComponent<PlayerAim>();
        
        if (ghostTrail == null) ghostTrail = GetComponentInChildren<GhostTrail>();

        _rb.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezeRotation;
        
        // [Fix] 가속도 뚫림(터널링) 현상 완전 방어!
        // 대시나 빠른 낙하 시 오브젝트를 통과해버리는 현상을 막기 위해 물리 충돌 감지 방식을 Continuous로 상향!
        if (_rb != null)
        {
            _rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
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

        // [Fix] SetParent 대신 위치 변화량을 직접 추적해서 플레이어 위치에 더해줌
        HandleMovingPlatform();

        if (_isDashing)
        {
            return;
        }

        if (_canMove || (_isHookingState && _isGrounded))
        {
            Move();
        }

        // [Fix] 시선(조준) 방향은 캐릭터의 이동 가능(_canMove) 여부 및 훅 상태와 무관하게 항상 마우스를 따라가도록 독립적으로 실행
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
            // [Fix] 쿨타임 체크 추가 (연타 방지)
            if (Time.time >= _lastJumpTime + jumpCooldown)
            {
                _jumpBufferCounter = jumpBufferTime;
            }
            _isJumpPressed = true;
        }
        else if (context.canceled || !context.ReadValueAsButton())
        {
            _isJumpPressed = false;
            // 위로 올라가는 중에 점프 키 떼면 속도 깎기 (소프트 점프)
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
                OnDashEvent?.Invoke(); // [New] 애니메이션 연동
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
            
            // [Fix] 악마의 FindAnyObjectByType 제거! 
            // 맵 전체를 뒤지지 않고, 방금 스캔한 반경 20m(hits) 안에서만 타겟을 찾도록 변경! (엄청난 속도 향상🚀)
            Transform hackTarget = null;
            
            foreach (var hit in hits)
            {
                if (hit.TryGetComponent(out BossHealth boss) || (hit.transform.parent != null && hit.transform.parent.TryGetComponent(out boss)))
                {
                    hackTarget = boss.transform;
                    break; // 보스 찾으면 즉시 종료 (최우선 타겟)
                }
                else if (hackTarget == null)
                {
                    if (hit.TryGetComponent(out BaseEnemy enemy) || (hit.transform.parent != null && hit.transform.parent.TryGetComponent(out enemy)))
                    {
                        hackTarget = enemy.transform; // 일반 적은 임시 저장 (보스 못 찾으면 얘로 씀)
                    }
                }
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
                // [Fix] 고속 루프 속 Distance 무거운 계산을 sqrMagnitude로 변경!
                float sqrDistance = (transform.position - lastGhostPos).sqrMagnitude;
                if (sqrDistance >= ghostSpacing * ghostSpacing) 
                {
                    ghostTrail.ShowGhost();
                    lastGhostPos = transform.position;
                }
            }

            isOverlappingEnemy = false; 
            
            // [Fix] 적과 미사일의 속도 차이를 고려하여 판정 박스를 두 개로 분리!
            // 1. 적(Enemy) 판정: 오빠의 난이도 요청대로 초근접 타이트하게
            // 2. 미사일(Missile) 판정: 터널링 방지만 될 정도로 최소한으로 타이트하게 유지
            Vector3 dashBoxCenter = transform.position + Vector3.up * 1.0f;
            Vector3 tightExtents = new Vector3(0.8f, 0.5f, 0.3f); // 적 (초타이트)
            Vector3 looseExtents = new Vector3(1.2f, 0.8f, 1.5f); // 미사일 (넉넉함 축소)
            
            Collider[] tightHits = Physics.OverlapBox(dashBoxCenter, tightExtents, Quaternion.identity, dashPassLayer);
            Collider[] looseHits = Physics.OverlapBox(dashBoxCenter, looseExtents, Quaternion.identity, dashPassLayer);

            // 1. [Fix] 쉴드 정면 충돌 검사를 가장 먼저 수행 (타이트한 판정 사용)
            bool hitValidShield = false;
            EnemyShield blockingShield = null;

            foreach (var hit in tightHits)
            {
                if (hit.TryGetComponent(out EnemyShield shield) || (hit.transform.parent != null && hit.transform.parent.TryGetComponent(out shield)))
                {
                    BaseEnemy shieldOwner = shield.GetComponentInParent<BaseEnemy>();
                    if (shieldOwner != null && shieldOwner.IsFrozen) continue; // 이미 언 적은 무시

                    // 진행 방향과 방패 앞면 내적 확인 (반대 방향이면 정면 충돌)
                    if (Vector3.Dot(dashDir, shield.transform.forward) <= 0)
                    {
                        hitValidShield = true;
                        blockingShield = shield;
                        break; 
                    }
                }
            }

            // 정면 방패에 막혔을 때의 처리
            if (hitValidShield && blockingShield != null)
            {
                Vector3 bounceDir = (transform.position - blockingShield.transform.position).normalized;
                _rb.linearVelocity = bounceDir * blockingShield.BounceForce;
                blockingShield.OnBlock(transform.position); 
                
                // 방패 정면에 대시를 박으면 데미지 받음 (오빠 요청)
                if (TryGetComponent(out PlayerHealth ph))
                {
                    ph.TakeDamage(1); 
                }
                
                _isDashing = false;
                _currentDashCharges = 0; 
                yield break;
            }

            // 2. 적 본체 관통 로직 (타이트한 판정 사용)
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

            // 3. 미사일 관통 로직 (미사일은 엄청 빠르므로 looseHits 사용)
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
                    isOverlappingEnemy = true; // 적을 뚫은 것으로 간주 (불릿타임 발동 및 연장)

                    if (!hasRecharged)
                    {
                        AddDashStack(1); // 미사일 뚫어도 대시 충전! (혜자 판정)
                        hasRecharged = true;
                    }
                }
            }
            
            if (!isOverlappingEnemy && !hasRecharged)
            {
                // 적 연장 스캔 (초근접 타이트하게)
                if (Physics.BoxCast(dashBoxCenter, tightExtents, dashDir, out RaycastHit hit, Quaternion.identity, 0.5f, dashPassLayer))
                {
                     if (hit.collider.TryGetComponent(out BaseEnemy enemy) || (hit.transform.parent != null && hit.transform.parent.TryGetComponent(out enemy)))
                     {
                         isOverlappingEnemy = true;
                     }
                }
                
                // 미사일 연장 스캔 (길지 않게 3m까지만)
                if (!isOverlappingEnemy && Physics.BoxCast(dashBoxCenter, looseExtents, dashDir, out RaycastHit mHit, Quaternion.identity, 3.0f, dashPassLayer))
                {
                     if (mHit.collider.TryGetComponent(out EnemyMissile m) || (mHit.transform.parent != null && mHit.transform.parent.TryGetComponent(out m)))
                     {
                         isOverlappingEnemy = true;
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
            _lastJumpTime = Time.time; // [New] 쿨타임 측정 시작!
            
            OnJumpEvent?.Invoke(); // [New] 애니메이션 연동
        }
    }

    private void PerformWallJump() 
    { 
        // [Fix] 벽 점프 시, 벽의 방향을 시선(transform.forward)이 아닌 실제 물리적 벽의 위치로 판단
        float zDist = Mathf.Abs(wallCheckPos.localPosition.z);
        if (zDist < 0.1f) zDist = 0.5f; // 안전방어
        Vector3 rightPos = transform.position + new Vector3(0, wallCheckPos.localPosition.y, zDist);
        bool isRightWall = Physics.CheckSphere(rightPos, checkRadius, wallLayer);

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
        _lastJumpTime = Time.time; // [New] 벽점프도 쿨타임 측정 시작!
        OnJumpEvent?.Invoke(); // [New] 애니메이션 연동 (벽점프)
    }

    private void WallSlide()
    {
        // [Fix] 조준(마우스) 방향과 무관하게, 입력 방향(_moveInput)과 실제 물리적 벽의 위치를 비교하여 슬라이드 판단
        float zDist = Mathf.Abs(wallCheckPos.localPosition.z);
        if (zDist < 0.1f) zDist = 0.5f;
        Vector3 rightPos = transform.position + new Vector3(0, wallCheckPos.localPosition.y, zDist);
        Vector3 leftPos = transform.position + new Vector3(0, wallCheckPos.localPosition.y, -zDist);
        
        bool touchRight = Physics.CheckSphere(rightPos, checkRadius, wallLayer);
        bool touchLeft = Physics.CheckSphere(leftPos, checkRadius, wallLayer);

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
        } 
    }

    private void HandleMovingPlatform()
    {
        // [Fix] 넉백/벽점프 등 조작 불가 상태(공중에 떴을 때)에는 플랫폼에 억지로 실려가지 않도록 예외 처리!
        if (!_canMove)
        {
            _currentPlatformTransform = null;
            return;
        }

        // 바닥에 PlatformFunction 컴포넌트가 있다면 (이동 플랫폼 위라면)
        if (_currentFunctionPlatform != null)
        {
            // 발을 디딘 플랫폼이 바뀌었을 경우 초기화
            if (_currentPlatformTransform != _currentFunctionPlatform.transform)
            {
                _currentPlatformTransform = _currentFunctionPlatform.transform;
                _lastPlatformPosition = _currentPlatformTransform.position;
            }

            // 플랫폼이 이동한 거리(Delta) 계산
            Vector3 platformDelta = _currentPlatformTransform.position - _lastPlatformPosition;
            
            // 이동한 거리가 있다면 플레이어 좌표에 그대로 얹어줌 (SetParent의 속도 덮어쓰기 무시)
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
        // [Fix] 플레이어 캐릭터가 무조건 조준선(마우스) 방향을 바라보도록 설정
        if (playerAim != null)
        {
            float zDir = playerAim.GetAimWorldPosition().z > transform.position.z ? 1f : -1f;
            
            // [Fix] 훅(스윙) 상태일 때는 스윙 애니메이션(또는 리깅) 자체의 180도 반전을 보정하기 위해 방향을 뒤집음!
            if (_isHookingState && !_isGrounded)
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
        Collider[] colliders = Physics.OverlapSphere(groundCheckPos.position, checkRadius, groundLayer | wallLayer); 
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
        
        // [Fix] 시선(조준) 방향과 독립적으로 절대적 좌/우의 벽을 모두 체크해서 처리!
        float zDist = Mathf.Abs(wallCheckPos.localPosition.z);
        if (zDist < 0.1f) zDist = 0.5f;
        Vector3 rightPos = transform.position + new Vector3(0, wallCheckPos.localPosition.y, zDist);
        Vector3 leftPos = transform.position + new Vector3(0, wallCheckPos.localPosition.y, -zDist);
        
        bool touchRight = Physics.CheckSphere(rightPos, checkRadius, wallLayer);
        bool touchLeft = Physics.CheckSphere(leftPos, checkRadius, wallLayer);
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

    // [New] 함정(레이저/가시 등) 피격 시 무조건 날려버리는 기능
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