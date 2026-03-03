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

    [Header("✨ Smart Auto Winch (Sanabi Style)")]
    [SerializeField] private float groundCheckDistance = 3.0f; // 더듬이 레이저 길이
    [SerializeField] private float minGroundClearance = 1.5f;  // 장애물 경계선
    [SerializeField] private float smartWinchLerpSpeed = 10.0f; // 스무딩 스피드

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
        
        // [Fix] PlayerAim(UI 조준선)의 시작점과 동일하게 맞춰서 사거리(거리) 계산 오차를 없앰!
        // (발끝에서 발사하면 머리 위 조준선보다 사거리가 짧아져서 색깔만 변하고 안 닿는 버그 발생)
        Vector3 startPos = transform.position + Vector3.up * 1.0f;
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
        
        // [Sanabi Style] 기준 로프 길이
        float baseRopeLength = currentRopeLength;
        bool isInitAutoWinching = false;

        // [Fix] 루프 내 곳곳에서 쓰이는 Rigidbody를 미리 캐싱하여 선언 순서 문제 해결
        Rigidbody rb = GetComponent<Rigidbody>();

        if (_playerMovement.IsGrounded)
        {
             float heightDiff = Mathf.Max(targetPos.y - transform.position.y, 1.0f);
             float safeLength = Mathf.Max(heightDiff - minGroundClearance, 1.0f);
             float standardWinchLength = Mathf.Max(currentRopeLength - autoWinchAmount, 1.0f);
             
             baseRopeLength = Mathf.Min(standardWinchLength, safeLength); 
             isInitAutoWinching = true;
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
                baseRopeLength = currentRopeLength; // 수동 조작 시 기준 길이 갱신
                isInitAutoWinching = false;
            }
            else
            {
                 // 입력이 없을 때:
                 
                 // 1. 초반 오토 윈치 (안 깎이고 부드럽게 위로 당겨줌)
                 if (isInitAutoWinching)
                 {
                      // [Fix] 단순 길이 조작만 하면 로프가 허공에서 줄어들고 캐릭터가 강제로 텔레포트함!
                      // 수동 수축(W키)처럼 실질적인 '상승 속도(Velocity)'를 부여해야 오토 윈치가 끝났을 때 스윙으로 부드럽게 이어짐.
                      Vector3 currentVel = rb.linearVelocity;
                      float speedAlongRope = Vector3.Dot(currentVel, tensionDir);
                      rb.linearVelocity = currentVel - (tensionDir * speedAlongRope) + (tensionDir * autoWinchSpeed);

                      currentRopeLength = Mathf.MoveTowards(currentRopeLength, baseRopeLength, autoWinchSpeed * Time.fixedDeltaTime);
                      
                      // 래칫: 이미 오토윈치로 땡겼음에도 거리가 멀어지지 않게 철저히 조임
                      if (currentRopeLength < distToAnchor)
                      {
                           currentRopeLength = distToAnchor;
                      }

                      if (currentRopeLength <= baseRopeLength + 0.01f)
                      {
                           isInitAutoWinching = false;
                      }
                 }
                 else
                 {
                      // 2. 공중 스윙 상태: 산나비 다이내믹 로프 보정
                      float targetLength = baseRopeLength; 
                      
                      Vector3 velDir = rb.linearVelocity.normalized;
                      float speed = rb.linearVelocity.magnitude;
                      
                      // 1. 경사로 서핑 (Slope Surfing) - 전방 레이더 (일정 속도 이상일 때만)
                      // 스피드가 너무 느릴 때는 탈 경사로가 없으므로 무시
                      // 바닥 반경 확인을 위해 '훅이 걸리는 레이어'뿐만 아니라 진짜 '바닥(Ground) / 벽(Wall) 레이어'도 모두 합쳐서 스캔합니다!
                      int obstacleLayer = hookableLayer | _playerMovement.GroundLayer | _playerMovement.WallLayer;

                      if (speed > 2.0f)
                      {
                           // 캐릭터가 날아가는 정면 방향(velDir)에 부딪힐 오르막길이나 턱이 있는지 확인!
                           if (Physics.SphereCast(myPos, 0.4f, velDir, out RaycastHit forwardHit, 1.5f, obstacleLayer))
                           {
                                // [Fix] 평평한 바닥(Up) 위를 수평으로 날고 있을 때(Dot == 0)는 참견하면 안 됨!!
                                // 벽이나 경사로에 진짜 쾅 박힐 각도(충돌 각도)일 때만 서핑 허용
                                if (Vector3.Dot(velDir, forwardHit.normal) < -0.1f)
                                {
                                     // 벽의 법선(Normal)을 바탕으로, 쾅! 박지 않고 미끄럼틀 타듯 꺾어주기
                                     Vector3 projectedVel = Vector3.ProjectOnPlane(rb.linearVelocity, forwardHit.normal);
                                     
                                     // Y축 속도가 죽어서 평지 걷는 현상 방지: 방향만 꺾어주고 속력은 유지!
                                     rb.linearVelocity = projectedVel.normalized * speed;
                                }
                           }
                      }
                           
                      // [Fix] 바닥 충돌 회피(Anti-Drag) 레이더 삭제 완료!
                      // 바닥에 안 닿으려고 줄을 억지로 줄이면 저 멀리서부터 가로로 훅 당겨지는 기괴한 V자 꺾임(Yank) 궤적이 나옴.
                      // 진짜 산나비나 스파이더맨처럼 진짜 밧줄(Pendulum) 물리로 가도록 냅두면, 바닥에 닿았을 때 부드럽게 바닥을 썰면서 서핑(Surfing)하게 됨!
                      
                      // [Old] 바닥 끌림 방지용 산나비 레이더 부활 (오토 윈치)
                      Vector3 checkStartPos = myPos + Vector3.up * 0.5f;

                      if (Physics.Raycast(checkStartPos, Vector3.down, out RaycastHit hit, groundCheckDistance, obstacleLayer))
                      {
                           float distanceToGround = hit.distance;

                           if (distanceToGround < minGroundClearance)
                           {
                                float deficit = minGroundClearance - distanceToGround;
                                targetLength = baseRopeLength - deficit;
                           }
                      }

                      targetLength = Mathf.Clamp(targetLength, 1f, maxDistance);
                      
                      // [Fix] 고무줄처럼 늘어나거나 천장에서 덜렁거리는 현상 완벽 해결! (진짜 산나비 펜듈럼)
                      // 로프는 오직 '수축(당기기)'만 자동으로 작동해야 합니다. 장애물을 지났다고 해서 스스로 늘어나면 바닥으로 추락해버립니다.
                      if (currentRopeLength > targetLength)
                      {
                           float smoothWinchSpeed = climbSpeed * 1.5f; 
                           currentRopeLength = Mathf.MoveTowards(currentRopeLength, targetLength, Time.fixedDeltaTime * smoothWinchSpeed);
                      }
                      // 만약 targetLength가 더 길다면? (장애물/바닥을 피하고 허공으로 나갔을 때)
                      // 어떤 오토 로직도 줄을 임의로 늘려선 안 됩니다! 무시하고 현재 길이를 유지해야 예쁜 시계추 액션이 나옵니다.
                 }
            }

            // [Fix] 땅에 닿아있을 때는 댐핑(마찰)을 없애서 자유롭게 미끄러지면서 가속을 받을 수 있게 함!
            if (_playerMovement.IsGrounded)
            {
                 _playerMovement.SetDrag(0f);
            }
            else
            {
                 _playerMovement.SetDrag(0.05f);
            }

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
            bool isWinchingUp = (_playerMovement.MoveInput.y > 0.1f);

            // [Fix] 지상 슬라이딩 "그린 라인" 기능 완벽 구현 및 고무줄 버그 수정
            // 오빠가 바닥에 있을 땐 밧줄이 방해 안 되게 알아서 스무스하게 늘어납니다! (최대 maxDistance까지만)
            // 멀어지면 늘어나고, 다시 가까워지면 예쁘게 감겨서 수축하기 때문에 "무한정 늘어나는" 예전의 에러가 더이상 없어요!
            if (_playerMovement.IsGrounded && !isWinchingUp)
            {
                currentRopeLength = Mathf.Clamp(distToAnchor, 1f, maxDistance);
                baseRopeLength = currentRopeLength;
            }

            if (distToAnchor > currentRopeLength) 
            {
                // 1. Velocity Correction (줄 밖으로 나가는 속도 제거)
                Vector3 velocity = rb.linearVelocity;
                float speedAway = Vector3.Dot(velocity, -tensionDir); // -tensionDir = Anchor -> Player 방향 (Away)

                bool isAtMaxDist = (currentRopeLength >= maxDistance - 0.05f);
                bool isWinchingDown = (_playerMovement.MoveInput.y < -0.1f) && !isAtMaxDist;
                
                float limitSpeed = 0f;
                if (isWinchingDown) 
                {
                    limitSpeed = climbSpeed; 
                }

                if (speedAway > limitSpeed) 
                {
                    Vector3 velocityCorrection = -tensionDir * (speedAway - limitSpeed);
                    Vector3 newVel = velocity - velocityCorrection; 

                    // [Kinetic Energy Fix] 원심력으로 방향이 벗어날 때 속도가 깎이는 현상(Pendulum Decay) 완벽 해결!
                    // 밧줄이 당기면서 궤도를 바꿀 때, 기존 장력이 잡아먹던 속력(Magnitude)을 100% 복구해 줍니다!
                    // (에너지 보존 법칙: 줄이 팽팽해져서 수평 속도가 위로 꺾여도, 스피드 자체는 줄지 않고 그대로 시원하게 날아감)
                    if (limitSpeed == 0f && newVel.sqrMagnitude > 0.01f)
                    {
                        rb.linearVelocity = newVel.normalized * velocity.magnitude;
                    }
                    else
                    {
                        rb.linearVelocity = newVel; 
                    }
                }

                // 2. Position Correction (위치 강제 보정)
                float distError = distToAnchor - currentRopeLength;

                if (!isWinchingDown && !isWinchingUp && !isInitAutoWinching)
                {
                    if (distError > 0.01f) 
                    {
                        // [Fix] 땅 끝(절벽)에서 떨어질 때의 순간이동(Teleport) 버그 완벽 차단 !!
                        // 오빠가 빛의 속도로 달리다 땅이 뚝 끊기면 한 프레임만에 오차가 크게 확 벌어지는데, 
                        // 이 때 오류라고 생각해서 제자리로 강제 순간이동 시켜버린 게 문제였어요.
                        // 이제 한 프레임당 최대 0.4f씩만 무겁게 당기도록 캡(Cap)을 씌워서, 부드럽고 묵직한 줄 텐션감만 줍니다!
                        float correctAmount = Mathf.Min(distError, 0.4f);
                        Vector3 fixPos = transform.position + tensionDir * correctAmount; 
                        rb.MovePosition(fixPos); 
                    }
                }
                else
                {
                    if (distError > 1.0f)
                    {
                        float correctAmount = Mathf.Min(distError, 0.4f);
                        Vector3 fixPos = transform.position + tensionDir * correctAmount; 
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

                // [Fix] 벽에 훅을 걸었을 때(각도가 클 때) 조작이 먹통이 되는 현상 방지!!
                // isTooHigh 같은 쓸데없는 각도 제한을 완전 해제해서 벽면 스윙도 가능하게 틔워줍니다!
                if (_playerMovement.IsGrounded)
                {
                    // 땅에 있을 때는 로프의 탄젠트 방향(사선/수직) 때문에 스윙 힘이 바닥으로 흡수되는 현상 제거!
                    // 오로지 순수 수평 전진(루프/가속)에만 힘을 몰빵해서 미끄러짐을 폭발시킴!
                    
                    float currentZSpeed = Mathf.Abs(rb.linearVelocity.z);
                    float boostMultiplier = 1.0f;

                    if (currentZSpeed < 5f) boostMultiplier = 15.0f;      // 정지 상태 발진 (리얼 로켓 부스터)
                    else if (currentZSpeed < 15f) boostMultiplier = 5.0f; // 중간 가속 단계 (빠른 변속)

                    Vector3 groundForce = new Vector3(0f, 0f, inputX * swingForce * boostMultiplier);
                    _playerMovement.AddHookForce(groundForce);
                }
                else
                {
                    // [Fix] 공중에서 바닥에 훅을 꽂았을 때 조작이 반대로 되는 버그 원천 차단!
                    // Tangent(접선) 벡터는 훅 위치에 따라 시계/반시계 방향이 뒤집히는 수학적 맹점이 있었습니다.
                    // 대신 오빠의 입력 방향(좌/우 수평 힘) 그대로 물리 엔진에 직관적으로 꽂아 넣습니다!
                    // 밧줄의 장력(Tension)이 이미 완벽히 버티고 있기 때문에, 수평 힘만 가해도 알아서 완벽하게 위로 치솟는 스윙 아크를 그려줍니다!
                    _playerMovement.AddHookForce(new Vector3(0f, 0f, inputX * swingForce));
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