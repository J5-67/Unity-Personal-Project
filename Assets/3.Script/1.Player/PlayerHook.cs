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

    [Header("🎯 Enemy Hook Settings")]
    [SerializeField] private float enemyPullAcceleration = 80f; 
    [SerializeField] private float enemyZipSpeed = 120f;        
    [SerializeField] private float wallZipSpeed = 100f;              
    [SerializeField] private float safeZipDistance = 1.5f; // [New] 적에게 날아갈 때 충돌하지 않고 멈출 안전 거리
    
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
    [SerializeField] private string enemyTag = "Enemy"; 
    [SerializeField] private string frozenEnemyTag = "FrozenEnemy"; 

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
    
    private Collider _myCollider;
    private Collider _ignoredCollider; 

    private void Awake()
    {
        _playerAim = GetComponent<PlayerAim>();
        _playerMovement = GetComponent<PlayerMovement>();
        _playerAim = GetComponent<PlayerAim>();
        _playerMovement = GetComponent<PlayerMovement>();
        _myCollider = GetComponent<Collider>();
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
        
        Vector3 dir;
        // 조준 보정: 락온된 타겟이 있으면 그쪽으로 발사
        if (_playerAim.LockedTarget != null)
        {
            dir = (_playerAim.LockedTarget.position - startPos).normalized;
        }
        else
        {
            Vector3 aimPos = _playerAim.GetAimWorldPosition();
            dir = (aimPos - startPos).normalized;
        }

        // 2.5D 게임이므로 X축(깊이) 고정
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
                     else
                     {
                         // [Fix] Heavy, Light 구분 없이 오빠가 적한테 스파이더맨처럼 날아가게(ZipToTargetRoutine) 통일!
                         yield return StartCoroutine(ZipToTargetRoutine(enemy.transform));
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
                     else if (tag == enemyTag)
                     {
                         // [Fix] 태그로 잡혔을 때도 무조건 날아가기!
                         yield return StartCoroutine(ZipToTargetRoutine(bestCol.transform));
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
                    else
                    {
                        // [Fix] 궤적 충돌 시에도 Heavy 구분 없이 무조건 적에게 날아가기!
                        yield return StartCoroutine(ZipToTargetRoutine(hit.transform));
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
                    else if (tag == enemyTag)
                    {
                        // [Fix] 궤적 태그 판단 시에도 모두 적에게 날아가기!
                        yield return StartCoroutine(ZipToTargetRoutine(hit.transform));
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

        if (_ignoredCollider != null && _myCollider != null)
        {
            Physics.IgnoreCollision(_myCollider, _ignoredCollider, false);
            _ignoredCollider = null;
        }

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

            // [Fix] 훅(로프) 매달린 상태에서 대시(Dash)를 쓰면 즉시 훅을 끊어버립니다!
            // (이유: 대시 속도 40과 로프 물리 장력이 충돌하여 모서리나 벽을 뚫어버리는 텔레포트 현상 원천 차단)
            if (_playerMovement.IsDashing)
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
                    // [Fix] 가속도 힘(AddHookForce) 제거, 대신 최고 속도(climbSpeed)로 방향 지정 정속 부여!
                    Vector3 currentVel = rb.linearVelocity;
                    float speedAlongRope = Vector3.Dot(currentVel, tensionDir);
                    rb.linearVelocity = currentVel - (tensionDir * speedAlongRope) + (tensionDir * climbSpeed);

                    currentRopeLength -= changeAmount;
                    
                    // [Fix] 벽에 물리적으로 막혀서 더 이상 못 가는데 논리적 줄 길이만 계속 짧아지면
                    // Constraint Solver가 강제로 텔레포트시켜 벽을 뚫어버리는(터널링) 현상이 발생합니다.
                    // 특히 빠른 속도에서 치명적이므로, 오차를 허용하지 말고 철저하게 즉시 물리 거리에 맞춥니다.
                    if (currentRopeLength < distToAnchor)
                    {
                        currentRopeLength = distToAnchor;
                    }

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
                             // [Feeling Fix] 가속도 힘(AddHookForce) 제거, 일정한 속도(climbSpeed)로 쭈욱 내려가기
                             Vector3 currentVel = rb.linearVelocity;
                             float speedAlongRope = Vector3.Dot(currentVel, tensionDir);
                             rb.linearVelocity = currentVel - (tensionDir * speedAlongRope) + (-tensionDir * climbSpeed);

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
                  // 2. 공중에 있을 때
                  else
                  {
                       // [Change] 줄이 자동으로 짧아지는(Ratchet) 로직 제거.
                       // 대시나 넉백으로 위로 올라갔다가 다시 내려올 때, 줄이 짧아져서 턱 걸리는 문제 해결.
                       // 이제 줄 길이는 W키를 누를 때만 짧아짐.
                  }
            }

            _playerMovement.SetDrag(0.05f);

            if (_playerMovement != null)
            {
                // [Fix] 훅 로프가 벽에 부딪혀도 안 끊기고 통과해서 유지되도록 오프(Off)! (불쾌감 개선)
                // int occlusionMask = _playerMovement.GroundLayer | _playerMovement.WallLayer;
                // Vector3 checkStartPos = transform.position + Vector3.up * 0.5f;
                // if (Physics.Linecast(checkStartPos, targetPos, out RaycastHit lineHit, occlusionMask)) { ... }
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
                    // [Speed Fix] 정속 하강을 위해 최고 제한 속도도 완전히 동일한 속도(climbSpeed)로 변경
                    limitSpeed = climbSpeed; 
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
                
                bool isWinchingUp = (_playerMovement.MoveInput.y > 0.1f);

                // W키(당기기)나 S키(풀기) 등 사용자가 직접 줄을 조작 중일 때는 위치 보정(MovePosition)을 끕니다!
                // (이유: 위치 강제 이동은 물리를 무시하는 순간 텔레포트라서, 벽에 닿았을 때 속도가 빠르면 터널링을 심하게 유발함)
                if (!isWinchingDown && !isWinchingUp)
                {
                    // 입력 없이 가만히 매달려 있거나 스윙할 때는 단단하게 고정 (오차 0.01f)
                    if (distError > 0.01f) 
                    {
                        Vector3 fixPos = transform.position + tensionDir * distError; 
                        rb.MovePosition(fixPos); 
                    }
                }
                else
                {
                    // 조작 중일 때는 이미 속도(Velocity) 수식이 강하게 당겨주고 있기 때문에 위치 강제이동은 필요 없음.
                    // 혹시나 다른 물리 연산이 박살 나서 거리가 너무 비정상적으로 멀어질 때만(1.0f 이상) 비상 당김
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


    private IEnumerator ZipToTargetRoutine(Transform target, float speedOverride = -1f)
    {
        _playerMovement.AddDashStack(1); 

        Rigidbody targetRb = target.GetComponent<Rigidbody>();
        Vector3 targetOffset = Vector3.zero;

        float startTime = Time.time;
        
        float currentZipSpeed = enemyZipSpeed;

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
            // [Fix] 대시 관통 시 로프 반동(고무줄 현상) 방지!
            // 날아가는 도중에 오빠가 '대시'를 쓰면 즉시 훅을 끊어버려서 자유롭게 관통하게 만들어줌!
            if (_playerMovement.IsDashing || _playerMovement.ConsumeJumpInput())
            {
                StopHook();
                yield break;
            }

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

            Rigidbody myRb = GetComponent<Rigidbody>();
            myRb.linearVelocity = zipDir * currentZipSpeed;

            // [Fix] 적한테 날아갈 때는 너무 바싹 안 붙고 'safeZipDistance' 앞에서 브레이크!
            // 벽 등에는 기존처럼 'stopDistance' 사용
            bool isEnemy = target.TryGetComponent(out BaseEnemy _) || (target.parent != null && target.parent.TryGetComponent(out BaseEnemy _));
            float checkDist = isEnemy ? safeZipDistance : stopDistance;

            if (distToSurfaceSqr < checkDist * checkDist && (Time.time - startTime) > 0.1f)
            {
                // [Fix] 멈출 때 관성 때문에 앞으로 미끄러져서 적이랑 박치기하는 현상 방지 
                myRb.linearVelocity = Vector3.zero;
                
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