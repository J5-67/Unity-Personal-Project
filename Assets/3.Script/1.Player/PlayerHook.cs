using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Core; 

public class PlayerHook : MonoBehaviour
{
    [Header("🪝 Hook Settings")]
    [SerializeField] private float maxDistance = 15f;      
    [SerializeField] private float hookAcceleration = 80f; 
    [SerializeField] private float retrieveSpeed = 30f;    
    [SerializeField] private float throwSpeed = 60f;       

    [Header("🔊 Sound Settings")]
    [SerializeField] private AudioClip fireSound; 

    [Header("🎯 Enemy Hook Settings (Fallback)")]
    [SerializeField] private float lightEnemyRetrieveSpeed = 30f;    
    [SerializeField] private float heavyEnemyPullAcceleration = 80f; 
    [SerializeField] private float heavyEnemyZipSpeed = 120f;        
    [SerializeField] private float wallZipSpeed = 100f;              
    
    [Header("🧗 Swing Settings")]
    [SerializeField] private float swingForce = 50f;       
    [SerializeField] [Range(0, 180)] private float swingAngleLimit = 80f; 

    [SerializeField] private float winchUpForce = 0.8f;    
    [SerializeField] private float winchDownForce = 0.2f;
    [SerializeField] private float climbSpeed = 6f; // W, S 키 줄 조절 속도 (통일)  
    
    [SerializeField] private float stopDistance = 0.5f;    
    [SerializeField] private float hookRadius = 0.5f;      

    [SerializeField] private LayerMask hookableLayer;      

    [Header("🏷️ Tags (구분용)")]
    [SerializeField] private string wallTag = "Wall";             
    [SerializeField] private string heavyEnemyTag = "LargeEnemy"; 
    [SerializeField] private string frozenEnemyTag = "FrozenEnemy"; 
    [SerializeField] private string lightEnemyTag = "SmallEnemy"; 

    [Header("🏗️ Auto Winch Settings")]
    [SerializeField] private float autoWinchAmount = 3.0f; 
    [SerializeField] private float autoWinchSpeed = 5.0f;  

    [SerializeField] private HookRopeVisual ropeVisual;    
    [SerializeField] private float waveStrength = 1.0f;    
    [SerializeField] private float waveFrequency = 3.0f;   
    [SerializeField] private Transform firePoint;          

    public float MaxDistance => maxDistance;          

    private PlayerAim _playerAim;
    private PlayerMovement _playerMovement;
    private Camera _mainCamera;
    private bool _isHooking;
    private Transform _currentHookTarget; 
    private Transform _hookAnchor;       
    private Vector3 _flyingHookPosition; 

    private void Awake()
    {
        _playerAim = GetComponent<PlayerAim>();
        _playerMovement = GetComponent<PlayerMovement>();
        _mainCamera = Camera.main;

        if (!TryGetComponent(out ropeVisual))
        {
            ropeVisual = gameObject.AddComponent<HookRopeVisual>();
        }
        
        _hookAnchor = new GameObject("HookTargetAnchor_Pool").transform;
        _hookAnchor.SetParent(transform); 
        _hookAnchor.gameObject.SetActive(false); 
    }

    public void OnHook(InputAction.CallbackContext context)
    {
        if (context.started && !_isHooking)
        {
            FireHook();
        }
        else if (context.canceled && _isHooking)
        {
            StopHook();
        }
    }

    private void FireHook()
    {
        _currentHookTarget = null;
        ropeVisual.ClearRope();

        StartCoroutine(ThrowHookRoutine());
    }

    private IEnumerator ThrowHookRoutine()
    {
        _isHooking = true;
        
        if (AudioManager.Instance != null && fireSound != null)
        {
            AudioManager.Instance.PlaySFX(fireSound);
        }
        
        Vector3 startPos = transform.position;
        Vector3 currentPos = startPos;
        Vector3 aimPos = _playerAim.GetAimWorldPosition();
        Vector3 dir = (aimPos - startPos).normalized;

        dir = new Vector3(0, dir.y, dir.z).normalized;

        Collider[] overlaps = Physics.OverlapSphere(currentPos, hookRadius, hookableLayer);
        if (overlaps.Length > 0)
        {
            Collider bestCol = null;
            float maxDot = -1.0f;
            Vector3 bestHitPoint = Vector3.zero;

            foreach (var col in overlaps)
            {
                if (col.gameObject == gameObject) continue;

                Vector3 closest = col.ClosestPoint(currentPos);
                
                Vector3 toTarget = (closest - currentPos).normalized;

                if ((closest - currentPos).sqrMagnitude < 0.0001f)
                {
                     toTarget = dir; 
                }

                float dot = Vector3.Dot(dir, toTarget);

                if (dot > maxDot && dot > 0.3f) 
                {
                    maxDot = dot;
                    bestCol = col;
                    bestHitPoint = closest;
                }
            }

            if (bestCol != null)
            {
                ropeVisual.DrawRope(transform.position, bestHitPoint);
                
                if (bestCol.TryGetComponent(out Interaction.IInteractable interactable))
                {
                    interactable.OnInteract(gameObject);
                    StopHook();
                    yield break;
                }

                if (bestCol.TryGetComponent(out BaseEnemy enemy) || (bestCol.transform.parent != null && bestCol.transform.parent.TryGetComponent(out enemy)))
                {
                     enemy.OnHooked();

                     if (enemy.IsFrozen)
                     {
                         _hookAnchor.position = bestHitPoint;
                         _hookAnchor.SetParent(bestCol.transform);
                         _hookAnchor.gameObject.SetActive(true);
                         _currentHookTarget = _hookAnchor;
                         yield return StartCoroutine(PullSelfRoutine(_currentHookTarget));
                     }
                     else if (enemy.Type == EnemyType.Heavy)
                     {
                         yield return StartCoroutine(ZipToTargetRoutine(enemy.transform)); 
                     }
                     else
                     {
                         yield return StartCoroutine(PullTargetRoutine(enemy.transform));
                     }
                }
                else
                {
                     string tag = bestCol.tag;

                     if (tag == wallTag || tag == frozenEnemyTag)
                     {
                         _hookAnchor.position = bestHitPoint;
                         _hookAnchor.SetParent(bestCol.transform);
                         _hookAnchor.gameObject.SetActive(true);
                         _currentHookTarget = _hookAnchor;
                         yield return StartCoroutine(PullSelfRoutine(_currentHookTarget));
                     }
                     else if (tag == heavyEnemyTag)
                     {
                         yield return StartCoroutine(ZipToTargetRoutine(bestCol.transform));
                     }
                     else if (tag == lightEnemyTag)
                     {
                         yield return StartCoroutine(PullTargetRoutine(bestCol.transform));
                     }
                     else
                     {
                         StopHook();
                     }
                }
                yield break; 
            }
        }

        float traveledDistance = 0f;

        while (traveledDistance < maxDistance)
        {
            float moveStep = throwSpeed * Time.deltaTime;
            
            if (traveledDistance + moveStep > maxDistance)
            {
                moveStep = maxDistance - traveledDistance;
            }

            if (Physics.SphereCast(currentPos, hookRadius, dir, out RaycastHit hit, moveStep, hookableLayer))
            {
                currentPos = hit.point;
                ropeVisual.DrawRope(transform.position, currentPos);

                if (hit.collider.TryGetComponent(out Interaction.IInteractable interactable))
                {
                    ropeVisual.DrawRope(transform.position, hit.point); 
                    interactable.OnInteract(gameObject); 
                    StopHook(); 
                    yield break;
                }

                if (hit.collider.TryGetComponent(out BaseEnemy enemy))
                {
                    enemy.OnHooked(); 

                    if (enemy.IsFrozen)
                    {
                        _hookAnchor.position = hit.point;
                        _hookAnchor.SetParent(hit.transform);
                        _hookAnchor.gameObject.SetActive(true);
                        _currentHookTarget = _hookAnchor;
                        yield return StartCoroutine(PullSelfRoutine(_currentHookTarget));
                    }
                    else if (enemy.Type == EnemyType.Heavy)
                    {
                        yield return StartCoroutine(ZipToTargetRoutine(enemy.transform));
                    }
                    else
                    {
                        yield return StartCoroutine(PullTargetRoutine(hit.transform));
                    }
                }
                else
                {
                    string tag = hit.collider.tag;

                    if (tag == wallTag || tag == frozenEnemyTag)
                    {
                        _hookAnchor.position = hit.point;
                        _hookAnchor.SetParent(hit.transform);
                        _hookAnchor.gameObject.SetActive(true);
                        _currentHookTarget = _hookAnchor;
                        yield return StartCoroutine(PullSelfRoutine(_currentHookTarget));
                    }
                    else if (tag == heavyEnemyTag)
                    {
                         yield return StartCoroutine(ZipToTargetRoutine(hit.transform));
                    }
                    else if (tag == lightEnemyTag)
                    {
                        yield return StartCoroutine(PullTargetRoutine(hit.transform));
                    }
                    else
                    {
                        StopHook();
                    }
                }
                
                yield break;
            }

            currentPos += dir * moveStep;
            traveledDistance += moveStep;
            
            _flyingHookPosition = currentPos;

            yield return null;
        }

        StopHook();
    }

    public void StopHook()
    {
        if (_currentHookTarget != null)
        {
            Transform targetToCheck = _currentHookTarget;
            if (_currentHookTarget == _hookAnchor && _hookAnchor.parent != null)
            {
                targetToCheck = _hookAnchor.parent;
            }

            if (targetToCheck.TryGetComponent(out EnemyPatrol patrol))
            {
                bool isFrozen = false;
                if (targetToCheck.TryGetComponent(out BaseEnemy enemy))
                {
                    isFrozen = enemy.IsFrozen;
                }

                if (!isFrozen)
                {
                    patrol.SetPatrol(true);
                }
            }
        }

        _isHooking = false;
        _playerMovement.SetHookState(false); 
        
        if (_currentHookTarget == _hookAnchor)
        {
            if (_hookAnchor != null)
            {
                _hookAnchor.SetParent(transform); 
                _hookAnchor.gameObject.SetActive(false);
            }
            else
            {
                 _hookAnchor = new GameObject("HookTargetAnchor_Pool").transform;
                 _hookAnchor.SetParent(transform);
                 _hookAnchor.gameObject.SetActive(false);
            }
        }
        else if (_currentHookTarget != null)
        {
        }
        _currentHookTarget = null;

        ropeVisual.ClearRope();
        StopAllCoroutines(); 
    }

    private void LateUpdate()
    {
        if (_isHooking)
        {
            Vector3 endPos;
            float currentAmp = 0f;
            float currentFreq = waveFrequency;

            if (_currentHookTarget != null)
            {
                endPos = _currentHookTarget.position;
                currentAmp = 0.1f; 
            }
            else
            {
                endPos = _flyingHookPosition;
                currentAmp = waveStrength; 
            }

            ropeVisual.DrawRope(firePoint.position, endPos, currentAmp, currentFreq);
        }
    }

    private IEnumerator PullSelfRoutine(Transform targetTransform)
    {
        Vector3 targetPos = targetTransform.position;
        _playerMovement.SetHookState(true); 
        _playerMovement.AddDashStack(1); 

        float currentRopeLength = Vector3.Distance(transform.position, targetPos);
        
        float finalRopeLength = currentRopeLength;
        bool isAutoWinching = false;

        // [Fix] 루프 내 곳곳에서 쓰이는 Rigidbody를 미리 캐싱하여 선언 순서 문제 해결
        Rigidbody rb = GetComponent<Rigidbody>();

        if (_playerMovement.IsGrounded)
        {
             finalRopeLength = Mathf.Max(currentRopeLength - autoWinchAmount, 1.0f); 
             isAutoWinching = true;
        }

        float currentRopeLengthSqr = currentRopeLength * currentRopeLength; 
        
        float startTime = Time.time; 

        while (_isHooking)
        {
            if (targetTransform == null)
            {
                StopHook();
                yield break;
            }

            if (_playerMovement.ConsumeJumpInput())
            {
                bool allowZip = true;
                
                if (targetTransform.parent != null && targetTransform.parent.TryGetComponent(out BaseEnemy zipCheckEnemy))
                {
                    if (!zipCheckEnemy.IsFrozen) 
                    {
                        allowZip = false;
                    }
                }

                if (allowZip)
                {
                    yield return StartCoroutine(ZipToTargetRoutine(targetTransform, wallZipSpeed));
                    yield break; 
                }
            }

            Vector3 myPos = transform.position;
            
            float distToAnchor = Vector3.Distance(myPos, targetPos);

            float distToSurface = distToAnchor; 

            Collider targetCol = targetTransform.GetComponent<Collider>(); 
            if (targetCol == null && targetTransform.parent != null)
            {
                targetCol = targetTransform.parent.GetComponent<Collider>();
            }
            
            if (targetCol != null)
            {
                Vector3 closestPoint = targetCol.ClosestPoint(myPos);
                distToSurface = Vector3.Distance(myPos, closestPoint);
            }
            
            Vector3 hookToPlayer = myPos - targetPos; 
            Vector3 tensionDir = -hookToPlayer.normalized; 

            float inputY = _playerMovement.MoveInput.y;

            float currentAccel = heavyEnemyPullAcceleration; 
            if (targetTransform.parent != null && targetTransform.parent.TryGetComponent(out BaseEnemy enemy))
            {
                currentAccel = enemy.HookInteractSpeed;
            }

            if (Mathf.Abs(inputY) > 0.1f)
            {
                // 공통 조절 속도 적용
                float changeAmount = climbSpeed * Time.fixedDeltaTime;

                // [Fix] 땅에 닿아있을 때(IsGrounded) S키(Winch Down)를 누르면
                // 훅 물리 연산이 캐릭터를 미끄러지게 하거나 이동을 방해하는 문제 해결.
                // 땅에 있을 때는 줄 길이만 늘려주고, 물리 힘(AddForce)이나 제어는 하지 않음.
                if (_playerMovement.IsGrounded && inputY < -0.1f)
                {
                    currentRopeLength += changeAmount;
                    currentRopeLength = Mathf.Clamp(currentRopeLength, 1f, maxDistance);
                    
                    // [Critical Fix] continue를 사용하면 while 루프의 마지막에 있는 yield return new WaitForFixedUpdate()를 건너뛰어
                    // 한 프레임 내에서 무한 반복(Infinite Loop)이 발생하여 유니티 에디터가 멈춤(Freeze).
                    // 반드시 yield return을 호출하거나 continue 대신 아래 로직을 스킵하도록 구조를 변경해야 함.
                    
                    // [ADD] 땅에 있을 때는 마찰력(Drag)을 0으로 돌려서 미끄러짐 방지!
                    _playerMovement.SetDrag(0f);
                    
                    yield return new WaitForFixedUpdate(); 
                    continue; 
                }

                if (inputY > 0)
                {
                    // W: 줄 감기
                    Vector3 pullForce = tensionDir * currentAccel * inputY * winchUpForce; 
                    _playerMovement.AddHookForce(pullForce);

                    currentRopeLength -= changeAmount;
                    
                    // 래칫: 이미 줄보다 안쪽이면 줄 길이를 거리에 맞춤 (느슨함 방지)
                    if (distToAnchor < currentRopeLength)
                    {
                        currentRopeLength = distToAnchor;
                    }
                }
                else 
                {
                    // S: 줄 풀기 (Winch Down)

                    // [Final Fix] 줄 풀기를 시작할 때, 설정된 로프 길이(currentRopeLength)가 실제 거리(distToAnchor)보다 짧다면
                    // 현재 물리적 위치에 맞춰서 길이를 동기화(Snap)해줘야 튐 현상이 없음.
                    if (distToAnchor > currentRopeLength)
                    {
                         currentRopeLength = distToAnchor;
                    }

                    // [Final Fix] 줄 끝까지 닿았으면(maxDistance 근접) 더 이상 줄 늘리기 로직을 타지 않음.
                    if (distToAnchor >= maxDistance - 0.1f)
                    {
                        currentRopeLength = maxDistance;
                        
                        // [Anti-RubberBand] 줄 끝에 왔는데도 중력/속도로 인해 계속 내려가려 하면
                        // 줄이 늘어났다가 순간이동하는 현상이 발생함.
                        // 따라서 줄 밖으로 나가는 속도 성분(Outgoing Velocity)을 강제로 0으로 만들어야 함.
                        
                        // [Anti-RubberBand] 줄 끝에 왔는데도 계속 내려가려는 속도(Outgoing Velocity) 제거
                        Vector3 vel = rb.linearVelocity;
                        
                        // tensionDir = Anchor쪽. -tensionDir = 바깥쪽(Away).
                        float outgoingSpeed = Vector3.Dot(vel, -tensionDir);
                        if (outgoingSpeed > 0)
                        {
                            rb.linearVelocity -= (-tensionDir * outgoingSpeed);
                        }
                    }
                    else
                    {
                         // [Critical Fix V2] 부동소수점 오차 및 덜덜거림 방지 이중 체크
                         if (currentRopeLength < maxDistance - 0.1f)
                         {
                             // [Feeling Fix] 밀고 내려가는 힘 추가
                             Vector3 pushForce = -tensionDir * currentAccel * Mathf.Abs(inputY) * winchDownForce;
                             _playerMovement.AddHookForce(pushForce);

                             currentRopeLength += changeAmount; 
                         }
                    }
                }

                // 최소/최대 길이 제한
                currentRopeLength = Mathf.Clamp(currentRopeLength, 1f, maxDistance);
            }
            else
            {
                 // 입력이 없을 때:
                 // 1. 오토 윈치 (땅에 있을 때 짧아짐)
                 if (isAutoWinching)
                 {
                      currentRopeLength = Mathf.MoveTowards(currentRopeLength, finalRopeLength, autoWinchSpeed * Time.fixedDeltaTime);
                      
                      if (currentRopeLength <= finalRopeLength + 0.01f)
                      {
                           isAutoWinching = false;
                      }
                 }
                 // 2. 공중에 있을 때 (래칫 로직 적용)
                 else
                 {
                      // 줄 안쪽으로 들어오면 (반동 등으로 인해), 줄 길이를 그만큼 줄여버림!
                      // 이렇게 해야 다시 밖으로 나갈 때 줄이 늘어나 있지 않음.
                      if (distToAnchor < currentRopeLength)
                      {
                          currentRopeLength = distToAnchor;
                      }
                 }
            }

            _playerMovement.SetDrag(0.05f);

            if (_playerMovement != null)
            {
                int occlusionMask = _playerMovement.GroundLayer | _playerMovement.WallLayer;
                
                Vector3 checkStartPos = transform.position + Vector3.up * 0.5f;

                if (Physics.Linecast(checkStartPos, targetPos, out RaycastHit lineHit, occlusionMask))
                {
                    if (lineHit.transform != targetTransform && lineHit.transform != _hookAnchor && lineHit.transform != targetTransform.parent)
                    {
                        StopHook();
                        yield break;
                    }
                }
            }

            // ---------------------------------------------------------
            // ⛓️ CRITICAL: Rope Constraint Solver (통합 & 수정됨)
            // ---------------------------------------------------------
            if (distToAnchor > currentRopeLength) 
            {
                // 1. Velocity Correction (줄 밖으로 나가는 속도 제거)
                Vector3 velocity = rb.linearVelocity;
                float speedAway = Vector3.Dot(velocity, -tensionDir); // -tensionDir = Anchor -> Player 방향 (Away)

                // [수정] Winch Down (S키) 중일 때는 속도 제한(Velocity Limit)만으로 제어하고,
                // 위치 강제 이동(MovePosition)은 끕니다. (MovePosition이 덜덜거림/렉의 주범)
                // [Fix] 단, 줄 끝(Max Distance)이면 S키 눌러도 Winch Down 아님 (엄격한 고정 필요)
                bool isAtMaxDist = (currentRopeLength >= maxDistance - 0.05f);
                bool isWinchingDown = (_playerMovement.MoveInput.y < -0.1f) && !isAtMaxDist;
                
                float limitSpeed = 0f;
                if (isWinchingDown) 
                {
                    // [Speed Fix] 내려갈 때 중력 가속도가 붙어 너무 빨라지므로,
                    // 올라가는 속도(climbSpeed)의 절반 정도로 제한을 걸어줌. (스윙 속도 영향 없음)
                    limitSpeed = climbSpeed * 0.5f; 
                }

                if (speedAway > limitSpeed) 
                {
                    // 허용 속도보다 빠를 때만 그 차이만큼 제거 (브레이크)
                    // 예: 중력 때문에 20으로 떨어지려 하는데, climbSpeed가 6이면 -> 6으로 고정됨. 아주 부드러움.
                    Vector3 velocityCorrection = -tensionDir * (speedAway - limitSpeed);
                    rb.linearVelocity -= velocityCorrection; 
                }

                // 2. Position Correction (위치 강제 보정)
                float distError = distToAnchor - currentRopeLength;
                
                // W키(당기기)나 가만히 있을 때는 단단하게 고정 (0.01f)
                // S키(풀기) 때는 위치 보정을 끎! (Velocity가 잡아주므로 굳이 텔레포트 시킬 필요 없음)
                if (!isWinchingDown)
                {
                    if (distError > 0.01f) 
                    {
                        Vector3 fixPos = transform.position + tensionDir * distError; 
                        rb.MovePosition(fixPos); 
                    }
                }
                else
                {
                    // 혹시나 물리 연산이 뚫려서 너무 멀어지면 안전장치로 한번 당겨줌
                    if (distError > 1.0f)
                    {
                        Vector3 fixPos = transform.position + tensionDir * distError; 
                        rb.MovePosition(fixPos); 
                    }
                }
            }

            bool isMinTimePassed = (Time.time - startTime) > 0.2f;

            if (distToSurface < stopDistance && isMinTimePassed)
            {
                if (targetTransform.parent != null && targetTransform.parent.TryGetComponent(out BaseEnemy hitEnemy))
                {
                    StopHook();
                    yield break;
                }
            }

            float inputX = _playerMovement.MoveInput.x;
            if (Mathf.Abs(inputX) > 0.1f)
            {
                Vector3 ropeDir = (myPos - targetPos).normalized;

                float angle = Vector3.Angle(Vector3.down, ropeDir);
                
                Vector3 axis = Vector3.right; 
                Vector3 tangent = Vector3.Cross(ropeDir, axis).normalized;

                bool isTooHigh = (angle > swingAngleLimit);

                if (!isTooHigh)
                {
                    _playerMovement.AddHookForce(tangent * inputX * swingForce);
                }
            }

            targetPos = targetTransform.position; 

            yield return new WaitForFixedUpdate(); 
        }
    }

    private IEnumerator PullTargetRoutine(Transform target)
    {
        _playerMovement.AddDashStack(1); 

        Rigidbody targetRb = target.GetComponent<Rigidbody>();
        if (targetRb != null) targetRb.isKinematic = false; 

        float currentRopeLength = Vector3.Distance(transform.position, target.position);
        float currentRopeLengthSqr = currentRopeLength * currentRopeLength;
        float startTime = Time.time; 

        while (_isHooking && target != null)
        {
            Vector3 myPos = transform.position;
            Vector3 targetPos = target.position;

            float currentDist = 0f;
            Collider targetCol = target.GetComponent<Collider>();
            
            if (targetCol != null)
            {
                Vector3 closestPoint = targetCol.ClosestPoint(myPos);
                currentDist = Vector3.Distance(myPos, closestPoint);
            }
            else
            {
                currentDist = Vector3.Distance(myPos, targetPos);
            }

            Vector3 playerToTarget = targetPos - myPos;
            Vector3 pullDir = -playerToTarget.normalized; 

            float inputY = _playerMovement.MoveInput.y;

            float currentRetrieveSpeed = lightEnemyRetrieveSpeed; 
            BaseEnemy enemyInfo = target.GetComponent<BaseEnemy>();
            
            EnemyPatrol enemyPatrol = target.GetComponent<EnemyPatrol>();
            if (enemyPatrol != null) enemyPatrol.SetPatrol(false);

            if (enemyInfo != null)
            {
                currentRetrieveSpeed = enemyInfo.HookInteractSpeed;
            }

            if (targetRb != null)
            {
                targetRb.linearVelocity = pullDir * currentRetrieveSpeed;
            }
            else
            {
                target.position += pullDir * currentRetrieveSpeed * Time.deltaTime;
            }

            if (currentDist < currentRopeLength)
            {
                currentRopeLength = currentDist;
            }

            if (currentDist < stopDistance && (Time.time - startTime) > 0.2f)
            {
                StopHook();
                yield break;
            }

            _flyingHookPosition = target.position; 

            yield return new WaitForFixedUpdate(); 
        }
        StopHook(); 
    }

    private IEnumerator ZipToTargetRoutine(Transform target, float speedOverride = -1f)
    {
        _playerMovement.AddDashStack(1); 

        Rigidbody targetRb = target.GetComponent<Rigidbody>();
        Vector3 targetOffset = Vector3.zero;

        float startTime = Time.time;
        
        float currentZipSpeed = heavyEnemyZipSpeed;

        if (speedOverride > 0)
        {
            currentZipSpeed = speedOverride;
        }
        else if (target.TryGetComponent(out BaseEnemy enemy))
        {
             if (enemy.HookInteractSpeed > 0) 
             {
                 currentZipSpeed = enemy.HookInteractSpeed;
             }
        }
        
        float stuckTimer = 0f;
        Vector3 lastPos = transform.position;

        while (_isHooking && target != null)
        {
            Vector3 myPos = transform.position;
            Vector3 targetPos = target.position; 
            
            float distToSurfaceSqr = (myPos - targetPos).sqrMagnitude; 
            Collider targetCol = target.GetComponent<Collider>();
            if (targetCol != null)
            {
                Vector3 closest = targetCol.ClosestPoint(myPos);
                distToSurfaceSqr = (myPos - closest).sqrMagnitude;
            }

            Vector3 zipDir = (targetPos - myPos).normalized;

            GetComponent<Rigidbody>().linearVelocity = zipDir * currentZipSpeed;

            if (distToSurfaceSqr < stopDistance * stopDistance && (Time.time - startTime) > 0.1f)
            {
                StopHook();
                yield break;
            }
            
            float movedDist = Vector3.Distance(myPos, lastPos);
            if (movedDist < 0.01f) 
            {
                stuckTimer += Time.fixedDeltaTime;
                if (stuckTimer > 0.5f) 
                {
                    StopHook(); 
                    yield break;
                }
            }
            else
            {
                stuckTimer = 0f; 
                lastPos = myPos;
            }
            
            _flyingHookPosition = target.position;

            yield return new WaitForFixedUpdate();
        }
        StopHook();
    }
}