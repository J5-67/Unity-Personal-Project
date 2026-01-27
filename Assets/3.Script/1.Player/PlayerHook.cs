using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerHook : MonoBehaviour
{
    [Header("🪝 Hook Settings")]
    [SerializeField] private float maxDistance = 15f;      // 사거리
    [SerializeField] private float hookAcceleration = 80f; // [NEW] 훅 당기는 가속도 (기존 pullSpeed 대체)
    [SerializeField] private float retrieveSpeed = 30f;    // 적을 당겨오는 속도
    [SerializeField] private float throwSpeed = 60f;       // [NEW] 훅이 날아가는 속도
    
    [Header("🧗 Swing Settings")]
    [SerializeField] private float swingForce = 50f;       // 좌우 스윙 힘
    [SerializeField] [Range(0, 180)] private float swingAngleLimit = 80f; // [NEW] 스윙 최대 각도 (0: 수직, 90: 수평, 180: 무제한)

    [Header("🏗️ Winch Settings")]
    [SerializeField] private float winchUpForce = 0.8f;    // 올라갈 때 당기는 힘 비율
    [SerializeField] private float winchDownForce = 0.5f;  // 내려갈 때 미는 힘 비율
    
    // [유니] 자동 모드라서 Speed 변수는 이제 다 필요 없어졌어!
    // Force(힘)만 조절하면 알아서 감기고 풀려!

    [SerializeField] private float stopDistance = 1.5f;    // 목표 도달 판정 거리
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

                // 태그 확인 및 분기 처리
               string tag = hit.collider.tag;

                // 1. 내가 날아가는 대상 (벽, 대형 적, 얼어붙은 적)
                if (tag == wallTag || tag == heavyEnemyTag || tag == frozenEnemyTag)
                {
                    // [유니] 바로 이동 시작!
                    _currentHookTarget = new GameObject("HookTargetAnchor").transform; // 임시 앵커 생성
                    _currentHookTarget.position = hit.point;
                    _currentHookTarget.parent = hit.transform; // 타겟에 붙임 (움직이는 발판 대응)
                    yield return StartCoroutine(PullSelfRoutine(_currentHookTarget));  
                }
                // 2. 끌고 오는 대상 (소형 적)
                else if (tag == lightEnemyTag)
                {
                    yield return StartCoroutine(PullTargetRoutine(hit.transform));
                }
                else
                {
                    // 쏘면 안 되는 물체에 맞았을 때 (예: 못 뚫는 장애물) -> 그냥 회수
                     StopHook();
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

        while (_isHooking)
        {
            Vector3 myPos = transform.position;
            Vector3 hookToPlayer = myPos - targetPos;
            float currentDist = hookToPlayer.magnitude;
            Vector3 tensionDir = -hookToPlayer.normalized; // 타겟 방향

            // 1. 윈치 (W/S) : 로프 길이 조절 (물리 기반)
            float inputY = _playerMovement.MoveInput.y;
            if (Mathf.Abs(inputY) > 0.1f)
            {
                // W (위): 당기는 힘 적용
                if (inputY > 0)
                {
                    // 당기는 힘 (User Setting)
                    Vector3 pullForce = tensionDir * hookAcceleration * inputY * winchUpForce; 
                    _playerMovement.AddHookForce(pullForce);

                    // [유니] 자동 감기 (Auto Winding)
                    // 줄어드는 속도를 따로 설정하지 않고, 플레이어가 힘에 의해 가까워진 만큼
                    // 즉시 줄 길이를 갱신해서 빈틈을 없앱니다. (Slack 방지)
                    if (currentDist < currentRopeLength)
                    {
                        currentRopeLength = currentDist;
                    }
                }
                // S (아래): 줄 풀기
                else 
                {
                    // 내려가는 힘 (User Setting)
                    Vector3 pushForce = -tensionDir * hookAcceleration * Mathf.Abs(inputY) * winchDownForce;
                    _playerMovement.AddHookForce(pushForce);

                    // [유니] 자동 풀기 (Auto Unwinding)
                    // 줄을 강제로 늘리는 게 아니라, "늘어나는 걸 허용"하는 방식. (Lock 해제)
                    // 현재 거리(currentDist)가 로프 길이보다 길어졌다면(내려갔다면), 로프 길이를 거기에 맞춰줌.
                    if (currentDist > currentRopeLength)
                    {
                        currentRopeLength = currentDist;
                    }
                }
                
                currentRopeLength = Mathf.Max(currentRopeLength, 1f); // 최소 1m
            }
            else
            {
                // [선택] 줄이 팽팽해질 때까지 자연스럽게 감기게 하려면:
                 if (currentDist < currentRopeLength)
                 {
                     currentRopeLength = Mathf.Lerp(currentRopeLength, currentDist, Time.deltaTime * 5f);
                 }
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
            if (currentDist > currentRopeLength + 0.02f) // 허용 오차 0.02m (더 타이트하게)
            {
                float error = currentDist - currentRopeLength;
                
                // [핵심] transform.position 대신 rb.position을 쓰거나, 
                // MovePosition을 써야 물리 엔진이 "아, 이동했구나" 하고 인지함.
                Vector3 fixPos = transform.position + tensionDir * error;
                // rb.MovePosition(fixPos); // 이건 다음 프레임에 적용돼서 늦을 수 있음.
                
                // 그냥 직접 이동하되, 아주 미세하게 나눠서 떨림 방지
                transform.position = Vector3.Lerp(transform.position, fixPos, 0.5f); 
                // 0.5f 정도면 절반씩 보정하니까 부드러움.
            }

            // B. 속도 제어 (Velocity Projection) - 줄이 팽팽할 때 바깥으로 나가는 속도 제거
            if (currentDist >= currentRopeLength)
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
        // [유니] ThrowHookRoutine에서 연결됨
        _playerMovement.AddDashStack(1); // [시너지] 성공 시 대시 충전

        // [중요] 적이 물리 효과를 받으려면 Rigidbody가 있어야 당겨짐
        Rigidbody targetRb = target.GetComponent<Rigidbody>();
        if (targetRb != null) targetRb.isKinematic = false; // 물리 켜기

        while (_isHooking && target != null)
        {
            float distance = Vector3.Distance(transform.position, target.position);

            // 내 앞에 오면 멈춤
            if (distance < stopDistance)
            {
                // [추가] 당겨온 후 적을 기절시키거나 처리하는 로직 필요
                // target.GetComponent<EnemyAI>()?.Stun(); 
                StopHook();
                yield break;
            }

            // 적을 내 쪽으로 당기기
            Vector3 dir = (transform.position - target.position).normalized;

            // Rigidbody가 있으면 속도로, 없으면 Transform으로
            if (targetRb != null)
            {
                targetRb.linearVelocity = dir * retrieveSpeed;
            }
            else
            {
                target.position += dir * retrieveSpeed * Time.deltaTime;
            }

            // 로프 그리기
            ropeVisual.DrawRope(transform.position, target.position);

            yield return null;
        }
        StopHook();
    }

    // [유니] 투사체 방식이므로 MissHookRoutine은 이제 필요 없음! (ThrowHookRoutine에서 처리)
}