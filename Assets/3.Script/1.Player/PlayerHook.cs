using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerHook : MonoBehaviour
{
    [Header("🪝 Hook Settings")]
    [SerializeField] private float maxDistance = 15f;      // 사거리
    [SerializeField] private float hookAcceleration = 80f; // [NEW] 훅 당기는 가속도 (기존 pullSpeed 대체) - Fallback
    [SerializeField] private float retrieveSpeed = 30f;    // 적을 당겨오는 속도 - Fallback
    [SerializeField] private float throwSpeed = 60f;       // [NEW] 훅이 날아가는 속도

    [Header("🎯 Enemy Hook Settings (Fallback)")]
    [SerializeField] private float lightEnemyRetrieveSpeed = 30f;    // 가벼운 적을 당겨오는 속도
    [SerializeField] private float heavyEnemyPullAcceleration = 80f; // 무거운 적에게 날아가는 가속도
    
    [Header("🧗 Swing Settings")]
    [SerializeField] private float swingForce = 50f;       // 좌우 스윙 힘
    [SerializeField] [Range(0, 180)] private float swingAngleLimit = 80f; // [NEW] 스윙 최대 각도 (0: 수직, 90: 수평, 180: 무제한)

    [Header("🏗️ Winch Settings")]
    [SerializeField] private float winchUpForce = 0.8f;    // 올라갈 때 당기는 힘 비율
    [SerializeField] private float winchDownForce = 0.5f;  // 내려갈 때 미는 힘 비율
    
    [SerializeField] private float stopDistance = 0.5f;    // 목표 도달 판정 거리 (더 가까이 붙어야 끊김!)
    [SerializeField] private float hookRadius = 0.5f;      // [NEW] 훅 충돌 판정 범위

    [SerializeField] private LayerMask hookableLayer;      // 훅이 박히는 모든 레이어 (벽, 적)

    [Header("🏷️ Tags (구분용)")]
    [SerializeField] private string wallTag = "Wall";             // 이동 가능
    [SerializeField] private string heavyEnemyTag = "LargeEnemy"; // 이동 가능
    [SerializeField] private string frozenEnemyTag = "FrozenEnemy"; // 이동 가능 (정지된 적)
    [SerializeField] private string lightEnemyTag = "SmallEnemy"; // 당겨오기

    [Header("✨ Visuals")]
    [SerializeField] private HookRopeVisual ropeVisual;    // [유니] 젤리처럼 찰랑거리는 로프 효과!
    [SerializeField] private Transform firePoint;          // 발사 위치 (플레이어 중심)

    // 내부 변수
    private PlayerAim _playerAim;
    private PlayerMovement _playerMovement;
    private Camera _mainCamera;
    private bool _isHooking;
    private Transform _currentHookTarget; // 현재 꽂힌 대상
    private Vector3 _flyingHookPosition; // [유니] 날아가는 도중의 위치 저장용

    private void Awake()
    {
        _playerAim = GetComponent<PlayerAim>();
        _playerMovement = GetComponent<PlayerMovement>();
        _mainCamera = Camera.main;

        // 로프 비주얼 컴포넌트 찾기 (없으면 추가!)
        if (!TryGetComponent(out ropeVisual))
        {
            ropeVisual = gameObject.AddComponent<HookRopeVisual>();
        }
    }

    // ---------------------------------------------------------
    // 🖱️ Input Handling
    // ---------------------------------------------------------
    public void OnHook(InputAction.CallbackContext context)
    {
        // 버튼 누름 (발사)
        if (context.started && !_isHooking)
        {
            FireHook();
        }
        // 버튼 뗌 (취소)
        else if (context.canceled && _isHooking)
        {
            StopHook();
        }
    }

    // ---------------------------------------------------------
    // 🚀 Hook Logic
    // ---------------------------------------------------------
    private void FireHook()
    {
        // [유니] 발사 전 기존 타겟 정보 초기화 (Visual Lag 방지)
        _currentHookTarget = null;
        ropeVisual.ClearRope();

        // [유니] 이제 즉시 발사가 아니라, 날아가는 코루틴 시작!
        StartCoroutine(ThrowHookRoutine());
    }

    // ---------------------------------------------------------
    // 🚀 [NEW] Hook Projectile Routine (투사체)
    // ---------------------------------------------------------
    private IEnumerator ThrowHookRoutine()
    {
        _isHooking = true;
        
        Vector3 startPos = transform.position;
        Vector3 currentPos = startPos;
        Vector3 aimPos = _playerAim.GetAimWorldPosition();
        Vector3 dir = (aimPos - startPos).normalized;

        // 2.5D 보정
        dir = new Vector3(0, dir.y, dir.z).normalized;

        // [유니] 0. 발사 직전! 나랑 겹쳐있는 적이 있는지 확인 (제로 거리 사격)
        // SphereCast는 시작점이 콜라이더 내부면 감지를 못해서 뚫고 지나가버림! -> OverlapSphere로 해결
        Collider[] overlaps = Physics.OverlapSphere(currentPos, hookRadius, hookableLayer);
        if (overlaps.Length > 0)
        {
            // [유니] "가장 내 조준 방향에 가까운" 녀석 하나만 고르자!
            // 그냥 overlaps[0]을 쓰면 바닥(Ground)이 먼저 잡혀서 자꾸 미역줄기처럼 바닥에 꽂힘... 😭
            Collider bestCol = null;
            float maxDot = -1.0f;
            Vector3 bestHitPoint = Vector3.zero;

            foreach (var col in overlaps)
            {
                // [유니] 나 자신(플레이어)은 무시해야지! 자해 금지! 🙅‍♀️
                if (col.gameObject == gameObject) continue;

                // 1. 표면 지점 찾기
                Vector3 closest = col.ClosestPoint(currentPos);
                
                // 2. 방향 계산 (내 위치 -> 표면)
                Vector3 toTarget = (closest - currentPos).normalized;

                // [예외] 만약 내가 완전히 뱃속에 들어와서 closest == currentPos라면?
                // 방향이 0이 되니까, 그냥 "내 조준 방향"에 있는 것으로 쳐주자!
                if (Vector3.Distance(closest, currentPos) < 0.01f)
                {
                     toTarget = dir; 
                }

                // 3. 내 조준 방향(dir)과 얼마나 일치하는지 확인 (내적)
                float dot = Vector3.Dot(dir, toTarget);

                // 4. 가장 정면에 있는 녀석(Dot이 큰 녀석) 찾기
                // 최소한 내 시야 앞쪽(Dot > 0.5f, 약 60도)에는 있어야 함! (발밑 바닥 제외)
                if (dot > maxDot && dot > 0.3f) 
                {
                    maxDot = dot;
                    bestCol = col;
                    bestHitPoint = closest;
                }
            }

            // 적합한 타겟을 찾았다면? 바로 꽂아버리기!
            if (bestCol != null)
            {
                ropeVisual.DrawRope(transform.position, bestHitPoint);
                
                // [Hit Logic]
                if (bestCol.TryGetComponent(out BaseEnemy enemy) || (bestCol.transform.parent != null && bestCol.transform.parent.TryGetComponent(out enemy)))
                {
                     Debug.Log($"[유니] 훅 적중! 대상: {enemy.name}, 타입: {enemy.Type}, 얼음: {enemy.IsFrozen}");
                     enemy.OnHooked();

                     // [유니] 얼어있는 적은 "벽" 취급! (스윙 가능) ❄️
                     if (enemy.IsFrozen)
                     {
                         Debug.Log("[유니] 얼어있는 적! 벽 타기(Swing) 모드 발동!");
                         _currentHookTarget = new GameObject("HookTargetAnchor").transform;
                         _currentHookTarget.position = bestHitPoint;
                         _currentHookTarget.parent = bestCol.transform;
                         yield return StartCoroutine(PullSelfRoutine(_currentHookTarget));
                     }
                     else if (enemy.Type == EnemyType.Heavy)
                     {
                         // [유니] Heavy Enemy는 이제 "돌진(Zip)"이야! 스윙 아님! ⚡
                         Debug.Log("[유니] Heavy Enemy 감지! Zip 모드 발동!");
                         yield return StartCoroutine(ZipToTargetRoutine(enemy.transform)); // bestCol.transform 대신 enemy.transform 권장
                     }
                     else
                     {
                         yield return StartCoroutine(PullTargetRoutine(enemy.transform));
                     }
                }
                else
                {
                     string tag = bestCol.tag;

                     // [유니] 태그로직 분리: Wall(벽)은 스윙, Heavy(무거운 적)는 지퍼!
                     if (tag == wallTag || tag == frozenEnemyTag)
                     {
                         _currentHookTarget = new GameObject("HookTargetAnchor").transform;
                         _currentHookTarget.position = bestHitPoint;
                         _currentHookTarget.parent = bestCol.transform;
                         yield return StartCoroutine(PullSelfRoutine(_currentHookTarget));
                     }
                     else if (tag == heavyEnemyTag)
                     {
                         // [유니] 태그가 Heavy라면 돌진!
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
                yield break; // 투사체 로직 종료
            }
        }

        float traveledDistance = 0f;

        // [유니] 최대 거리까지 날아가거나 어딘가에 부딪힐 때까지 반복!
        while (traveledDistance < maxDistance)
        {
            // 이번 프레임에 이동할 거리 계산
            float moveStep = throwSpeed * Time.deltaTime;
            
            // 너무 멀리가지 않게 보정
            if (traveledDistance + moveStep > maxDistance)
            {
                moveStep = maxDistance - traveledDistance;
            }

            // 충돌 감지 (SphereCast로 굵게 쏘기)
            // 현재 위치에서 moveStep만큼 앞을 검사
            if (Physics.SphereCast(currentPos, hookRadius, dir, out RaycastHit hit, moveStep, hookableLayer))
            {
                // 충돌 발생! (현재 위치 업데이트)
                currentPos = hit.point;
                ropeVisual.DrawRope(transform.position, currentPos);

                // [유니] 먼저 BaseEnemy 컴포넌트가 있는지 확인해볼게!
                if (hit.collider.TryGetComponent(out BaseEnemy enemy))
                {
                    enemy.OnHooked(); // 훅 걸렸다고 알려주기!

                    if (enemy.IsFrozen)
                    {
                        // [유니] 얼어있는 적은 "벽" 취급! (Projectile)
                         Debug.Log("[유니] 얼어있는 적(Projectile)! 벽 타기(Swing) 모드 발동!");
                        _currentHookTarget = new GameObject("HookTargetAnchor").transform;
                        _currentHookTarget.position = hit.point;
                        _currentHookTarget.parent = hit.transform;
                        yield return StartCoroutine(PullSelfRoutine(_currentHookTarget));
                    }
                    else if (enemy.Type == EnemyType.Heavy)
                    {
                        // 묵직한 적이니까 내가 날아가야지! (Zip)
                        Debug.Log("[유니] Heavy Enemy (Projectile) 감지! Zip 모드 발동!");
                        yield return StartCoroutine(ZipToTargetRoutine(enemy.transform));
                    }
                    else
                    {
                        // 가벼운 적이니까 내 쪽으로 당겨올게!
                        yield return StartCoroutine(PullTargetRoutine(hit.transform));
                    }
                }
                // [유니] 적이 아니라면 기존처럼 태그(벽 등)로 판단하자!
                else
                {
                    string tag = hit.collider.tag;

                    if (tag == wallTag || tag == frozenEnemyTag)
                    {
                        _currentHookTarget = new GameObject("HookTargetAnchor").transform;
                        _currentHookTarget.position = hit.point;
                        _currentHookTarget.parent = hit.transform;
                        yield return StartCoroutine(PullSelfRoutine(_currentHookTarget));
                    }
                    else if (tag == heavyEnemyTag)
                    {
                         // [유니] Heavy Tag -> Zip!
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
                
                // 루프 종료 (처리 완료)
                yield break;
            }

            // 충돌 안 했으면 이동
            currentPos += dir * moveStep;
            traveledDistance += moveStep;
            
            // [유니] 날아가는 위치 업데이트 (LateUpdate에서 그리기 위해)
            _flyingHookPosition = currentPos;

            yield return null;
        }

        // 최대 사거리 도달 시 (허공)
        StopHook();
    }

    private void StopHook()
    {
        // [유니] 훅이 끝날 때, 만약 잡고 있던 게 순찰 중인 적이었다면 다시 순찰 지시!
        if (_currentHookTarget != null)
        {
            Transform targetToCheck = _currentHookTarget;
            // 만약 _currentHookTarget이 임시 앵커라면, 부모가 실제 타겟일 수 있음
            if (_currentHookTarget.name == "HookTargetAnchor" && _currentHookTarget.parent != null)
            {
                targetToCheck = _currentHookTarget.parent;
            }

            if (targetToCheck.TryGetComponent(out EnemyPatrol patrol))
            {
                // [유니] 만약 적이 얼어있는 상태라면 순찰을 다시 켜면 안 돼!
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
        _playerMovement.SetHookState(false); // 이동 권한 반납
        
        // [유니] 타겟 정보 즉시 삭제 (잔상 방지)
        if (_currentHookTarget != null)
        {
            Destroy(_currentHookTarget.gameObject); // 임시 앵커 파괴
            _currentHookTarget = null;
        }

        ropeVisual.ClearRope();
        StopAllCoroutines(); // 진행 중인 훅 로직 중단
    }

    // [유니] 비주얼 업데이트는 물리 스텝(FixedUpdate)이 아니라 
    // 화면 주사율(Frame Rate)에 맞춰야 부드러움! (LateUpdate 권장)
    private void LateUpdate()
    {
        if (_isHooking)
        {
            Vector3 endPos;

            // 1. 어딘가에 꽂혀있다면 -> 타겟 위치
            if (_currentHookTarget != null)
            {
                endPos = _currentHookTarget.position;
            }
            // 2. 날아가는 중이라면 -> 투사체 위치
            else
            {
                endPos = _flyingHookPosition;
            }

            ropeVisual.DrawRope(firePoint.position, endPos);
        }
    }

    // ---------------------------------------------------------
    // 🤸 Type A: Pull Self (내가 날아감)
    // ---------------------------------------------------------
    // ---------------------------------------------------------
    // 🤸 Type A: Pull Self (내가 날아감 + 스윙)
    // ---------------------------------------------------------
    private IEnumerator PullSelfRoutine(Transform targetTransform)
    {
        Vector3 targetPos = targetTransform.position;
        // [유니] 이미 ThrowHookRoutine에서 _isHooking = true가 되어있지만 안전하게 유지
        _playerMovement.SetHookState(true); 
        _playerMovement.AddDashStack(1); // [시너지] 성공 시 대시 충전

        // [유니] 훅 걸린 시점의 거리를 초기 로프 길이로 설정
        float currentRopeLength = Vector3.Distance(transform.position, targetPos);
        float startTime = Time.time; // [유니] 바로 끊김 방지용 타이머

        while (_isHooking)
        {
            // [유니] 타겟이 파괴되었는지 확인 (Destroy되면 null이 됨)
            if (targetTransform == null)
            {
                StopHook();
                yield break;
            }

            Vector3 myPos = transform.position;
            
            // [유니] 거리 계산을 두 가지로 분리해야 해!
            // 1. 물리 연산용: 고정된 앵커(타겟)까지의 거리 (그래야 원 궤도로 스윙이 됨!)
            float distToAnchor = Vector3.Distance(myPos, targetPos);

            // 2. 도착 판정용: 실제 표면까지의 거리 (ClosestPoint)
            float distToSurface = distToAnchor; // 기본값은 앵커 거리

            // 앵커(targetTransform)에는 콜라이더가 없으니 부모(적)를 찾아봐야 해!
            Collider targetCol = targetTransform.GetComponent<Collider>(); 
            if (targetCol == null && targetTransform.parent != null)
            {
                targetCol = targetTransform.parent.GetComponent<Collider>();
            }
            
            if (targetCol != null)
            {
                // 내 위치에서 가장 가까운 적의 표면 지점 찾기
                Vector3 closestPoint = targetCol.ClosestPoint(myPos);
                distToSurface = Vector3.Distance(myPos, closestPoint);
            }
            
            // 물리 계산에는 이제 distToAnchor를 써야 해!
            Vector3 hookToPlayer = myPos - targetPos; 
            Vector3 tensionDir = -hookToPlayer.normalized; // 타겟 방향

            // 1. 윈치 (W/S) : 로프 길이 조절 (물리 기반)
            float inputY = _playerMovement.MoveInput.y;

            // [유니] 적 정보 가져오기 (속도 설정을 위해)
            float currentAccel = heavyEnemyPullAcceleration; // 기본값
            if (targetTransform.parent != null && targetTransform.parent.TryGetComponent(out BaseEnemy enemy))
            {
                currentAccel = enemy.HookInteractSpeed;
            }

            if (Mathf.Abs(inputY) > 0.1f)
            {
                // W (위): 당기는 힘 적용
                if (inputY > 0)
                {
                    // [유니] 설정된 가속도(currentAccel) 사용!
                    Vector3 pullForce = tensionDir * currentAccel * inputY * winchUpForce; 
                    _playerMovement.AddHookForce(pullForce);

                    // [유니] 개선: 움직이는 적(또는 도망가는 적)을 상대로 물리 힘만으로는 부족할 때가 있음.
                    // 그래서 입력이 들어오면 "강제로" 로프 길이를 줄여버림! (적극적 윈치)
                    float reduceAmount = 5f * Time.fixedDeltaTime; // 감기 속도 (조절 가능)
                    currentRopeLength = Mathf.Max(currentRopeLength - reduceAmount, 1f); 
                    
                    // 물리적 거리가 더 짧아졌다면 그것도 반영
                    if (distToAnchor < currentRopeLength)
                    {
                        currentRopeLength = distToAnchor;
                    }
                }
                // S (아래): 줄 풀기
                else 
                {
                    Vector3 pushForce = -tensionDir * currentAccel * Mathf.Abs(inputY) * winchDownForce;
                    _playerMovement.AddHookForce(pushForce);

                    if (distToAnchor > currentRopeLength)
                    {
                        currentRopeLength = distToAnchor;
                    }
                }
                
                currentRopeLength = Mathf.Max(currentRopeLength, 1f); // 최소 1m
            }
            else
            {
                // [유니] 수정: "자동 감기" 기능 끄기! 🚫
                // 스윙을 하면 당연히 줄이 느슨해졌다가 팽팽해졌다가 하는데,
                // 이때마다 줄을 줄여버리면 점점 고립되어서 지그재그로 떨어지게 됨.
                // 줄 길이는 오직 'W'키를 눌렀을 때만 줄어들어야 함!

                /*
                 if (distToAnchor < currentRopeLength)
                 {
                     currentRopeLength = Mathf.Lerp(currentRopeLength, distToAnchor, Time.deltaTime * 5f);
                 }
                */
            }

            // -------------------------------------------------------------
            // [유니] 공기 저항(Drag) 동적 제어 (Dynamic Drag)
            // 키를 누르고 있을 때는 미끄러지듯(0) 나가야 하고,
            // 키를 떼면 공기 저항(1)으로 서서히 멈춰야 함.
            // 그래야 방향 전환할 때 "슈-웅(감속)" 하지 않고 "슝!(가속)" 할 수 있어!
            // -------------------------------------------------------------
            if (Mathf.Abs(_playerMovement.MoveInput.x) > 0.1f)
            {
                // 스윙 중일 땐 저항 0! (무마찰)
                _playerMovement.SetDrag(0f);
            }
            else
            {
                // 아무것도 안 할 땐 저항 1! (안정)
                _playerMovement.SetDrag(1.0f);
            }

            // 2. 물리 처리 (Rigid Rope) : 단단한 밧줄 구현
            // 스프링(Tension) 방식은 삭제하고, '거리 제한'과 '속도 제어'로 변경

            Rigidbody rb = GetComponent<Rigidbody>();
            
            // [유니] 중요 수정: 위치 보정 재적용 (Slippage Fix)
            // 아까 떨림 때문에 뺐더니, 중력 때문에 조금씩 흘러내리는 문제 발생!
            // -> 다시 넣되, 이번엔 떨림이 없도록 아주 부드럽게(Lerp) 적용하거나
            //    Rigidbody.position을 직접 건드려서 물리 엔진과 싸우지 않게 함.
            if (distToAnchor > currentRopeLength + 0.02f) // 허용 오차 0.02m (더 타이트하게)
            {
                float error = distToAnchor - currentRopeLength;
                
                // [핵심] transform.position 대신 rb.position을 쓰거나, 
                // MovePosition을 써야 물리 엔진이 "아, 이동했구나" 하고 인지함.
                Vector3 fixPos = transform.position + tensionDir * error;
                // rb.MovePosition(fixPos); // 이건 다음 프레임에 적용돼서 늦을 수 있음.
                
                // [유니] 직접 이동하되, 아주 미세하게 나눠서 떨림 방지
                transform.position = Vector3.Lerp(transform.position, fixPos, 0.5f); 
            }

            // -------------------------------------------------------------
            // [유니] 도착 판정! (Heavy Enemy 충돌 시 해제)
            // -------------------------------------------------------------
            // [유니] 최소 0.2초는 유지해야 함 (너무 가까워서 바로 끊기는 거 방지!)
            bool isMinTimePassed = (Time.time - startTime) > 0.2f;

            // [판정] 물리 거리는 멀어도, 표면 거리가 가까우면 멈춰야 함!
            if (distToSurface < stopDistance && isMinTimePassed)
            {
                // 타겟의 부모나 자신에게 BaseEnemy가 있는지 확인
                if (targetTransform.parent != null && targetTransform.parent.TryGetComponent(out BaseEnemy hitEnemy))
                {
                    Debug.Log($"[유니] 훅 도착! (거리: {distToSurface} < 설정값: {stopDistance})");
                    StopHook();
                    yield break;
                }
            }



            // [유니] 바닥 마찰 문제 해결: 땅에 있는데 훅을 당기거나 이동하려고 하면 살짝 띄워줌!
            if (_playerMovement.MoveInput.magnitude > 0.1f && Physics.Raycast(transform.position, Vector3.down, 1.1f, LayerMask.GetMask("Ground", "Wall")))
            {
                // 아주 살짝만 들어올려서 마찰(Friction)을 없앰
                // 힘(Force)으로 하면 잘 안 될 때가 있어서 위치(Position)를 아주 미세하게 보정
                transform.position += Vector3.up * Time.deltaTime * 0.5f;
            }

            // B. 속도 제어 (Velocity Projection) - 줄이 팽팽할 때 바깥으로 나가는 속도 제거
            if (distToAnchor >= currentRopeLength)
            {
                Vector3 velocity = rb.linearVelocity;
                // 밧줄 방향(tensionDir)과 내 속도의 내적 = 밧줄 쪽으로 이동하는 속도 성분
                // tensionDir은 타겟을 향하는 방향임.
                float speedTowardsTarget = Vector3.Dot(velocity, tensionDir);
                
                // 만약 타겟 반대 방향(바깥)으로 가려고 한다면? (speedTowardsTarget < 0)
                if (speedTowardsTarget < 0)
                {
                    // 그 속도 성분만 제거! (이게 바로 투영)
                    // velocityProjected = velocity - (mySpeed * dir)
                    Vector3 velocityAway = tensionDir * speedTowardsTarget; // 이게 음수니까 '나가는 속도'
                    
                    // 나가는 속도를 없앰 -> 원 궤도 접선 속도만 남음!
                    rb.linearVelocity -= velocityAway; 
                }
            }

            // [삭제된 코드] 스프링/댐핑 로직
            /*
            if (currentDist > currentRopeLength)
            {
                float stretch = currentDist - currentRopeLength;
                Vector3 springForce = tensionDir * (stretch * ropeStiffness);
                Vector3 dampingForce = -rb.linearVelocity * ropeDamping;
                _playerMovement.AddHookForce(springForce + dampingForce);
            }
            */

            // 3. 스윙 (A/D) : 줄의 옆방향(접선)으로 힘 가하기
            float inputX = _playerMovement.MoveInput.x;
            if (Mathf.Abs(inputX) > 0.1f)
            {
                // [유니] 수정: 단순히 오른쪽으로 밀면 위로 올라가는 현상 발생!
                // 로프의 수직 방향(접선, Tangent)을 구해서 그 쪽으로 밀어야 진짜 그네처럼 움직임.

                // 1. 로프 방향 (Hook -> Player)
                Vector3 ropeDir = (myPos - targetPos).normalized;

                // 2. 각도 제한 (현실적인 물리)
                // 맨 위(천장) 근처까지 갔을 때 더 이상 힘을 주면 안 됨!
                // 수직 아래(Vector3.down)와의 각도를 계산
                float angle = Vector3.Angle(Vector3.down, ropeDir);
                
                // 3. 접선 벡터 계산
                Vector3 axis = Vector3.right; 
                Vector3 tangent = Vector3.Cross(ropeDir, axis).normalized;

                // [유니] 수정: 각도 제한 로직 (Angle Limit)
                // 설정한 각도보다 높이 올라가면, 더 이상 위로 올라가는 힘을 주지 않음!
                bool isTooHigh = (angle > swingAngleLimit);

                // 너무 높으면 힘 차단 (단, 내려오는 방향 힘이나 중력은 허용해야 하지만,
                // 여기선 'swingForce' 자체가 가속이므로, 그냥 높으면 끄는 게 제일 깔끔함)
                if (!isTooHigh)
                {
                    _playerMovement.AddHookForce(tangent * inputX * swingForce);
                }
            }

            // [유니] 로프 그리기는 LateUpdate로 이동했음! (삭제)
            targetPos = targetTransform.position; // 타겟이 움직일 수 있으니 갱신

            yield return new WaitForFixedUpdate(); // [유니] 물리 연산 싱크 맞추기 (떨림 방지)
        }
    }

    // ---------------------------------------------------------
    // 🎣 Type B: Pull Target (적을 당겨옴)
    // ---------------------------------------------------------
    private IEnumerator PullTargetRoutine(Transform target)
    {
        _playerMovement.AddDashStack(1); // [시너지] 성공 시 대시 충전!

        Rigidbody targetRb = target.GetComponent<Rigidbody>();
        if (targetRb != null) targetRb.isKinematic = false; // 물리 켜기

        // [유니] 처음 훅이 걸렸을 때의 거리를 유지해줄게! (목줄 효과)
        float currentRopeLength = Vector3.Distance(transform.position, target.position);
        float startTime = Time.time; // [유니] 최소 지속 시간 체크용

        while (_isHooking && target != null)
        {
            Vector3 myPos = transform.position;
            Vector3 targetPos = target.position;

            // [유니] 표면 거리 계산 (ClosestPoint)
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
            Vector3 pullDir = -playerToTarget.normalized; // 플레이어를 향하는 방향

            float inputY = _playerMovement.MoveInput.y;

            // [유니] 적마다 다른 속도 적용!
            float currentRetrieveSpeed = lightEnemyRetrieveSpeed; // 기본값
            BaseEnemy enemyInfo = target.GetComponent<BaseEnemy>();
            
            // [유니] 훅 걸린 동안 순찰(Patrol) 끄기! (안 그러면 물리 엔진이랑 싸워서 덜덜 떨림)
            EnemyPatrol enemyPatrol = target.GetComponent<EnemyPatrol>();
            if (enemyPatrol != null) enemyPatrol.SetPatrol(false);

            if (enemyInfo != null)
            {
                currentRetrieveSpeed = enemyInfo.HookInteractSpeed;
            }

            // 1. [자동] 무조건 당기기! "이리 와!"
            // [유니] 설정된 속도(currentRetrieveSpeed) 사용!
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

            // 3. 내 코앞(stopDistance)까지 오면 훅 해제!
            // [유니] 너무 빨리 끊기는 거 방지 (0.2초 딜레이)
            if (currentDist < stopDistance && (Time.time - startTime) > 0.2f)
            {
                StopHook();
                yield break;
            }

            // [유니] 로프 그리기용 위치 갱신 (LateUpdate에서 처리해!)
            _flyingHookPosition = target.position; 

            yield return new WaitForFixedUpdate(); // 물리 싱크 맞추기!
        }
        StopHook(); // [유니] 루프가 끝나면(타겟이 사라지거나 훅이 끊기면) 정리
    }

    // ---------------------------------------------------------
    // ⚡ Type C: Zip To Target (적에게 돌진)
    // ---------------------------------------------------------
    private IEnumerator ZipToTargetRoutine(Transform target)
    {
        _playerMovement.AddDashStack(1); // 성공 시 대시 충전

        Rigidbody targetRb = target.GetComponent<Rigidbody>();
        Vector3 targetOffset = Vector3.zero;

        // 타겟의 중심보다는 살짝 위나 표면을 향하는 게 좋지만, 일단 심플하게!
        
        float startTime = Time.time;
        float zipSpeed = heavyEnemyPullAcceleration * 2f; // 당기는 힘보다 2배 빠르게 슉!
        
        while (_isHooking && target != null)
        {
            Vector3 myPos = transform.position;
            Vector3 targetPos = target.position; // 타겟 위치 갱신
            
            // 표면 거리 계산
            float distToSurface = Vector3.Distance(myPos, targetPos);
            Collider targetCol = target.GetComponent<Collider>();
            if (targetCol != null)
            {
                Vector3 closest = targetCol.ClosestPoint(myPos);
                distToSurface = Vector3.Distance(myPos, closest);
            }

            // 1. 방향 계산
            Vector3 zipDir = (targetPos - myPos).normalized;

            // 2. 이동 (MovePosition으로 물리 뚫고 감)
            // 너무 빠르면 통과해버릴 수 있으니 Rigidbody 이동 사용
            GetComponent<Rigidbody>().linearVelocity = zipDir * zipSpeed;

            // 3. 도착 판정
            if (distToSurface < stopDistance && (Time.time - startTime) > 0.1f)
            {
                // 충돌! (여기서 데미지 주거나 밀쳐내기 가능)
                StopHook();
                yield break;
            }
            
            // [유니] 로프 그리기용 위치 갱신
            _flyingHookPosition = target.position;

            yield return new WaitForFixedUpdate();
        }
        StopHook();
    }

    // [유니] 투사체 방식이므로 MissHookRoutine은 이제 필요 없음! (ThrowHookRoutine에서 처리)
}