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
    [SerializeField] private float safeZipDistance = 1.5f;

    [Header("🧲 Aim Assist Settings")]
    [SerializeField] private bool useAimAssist = true;
    [SerializeField] private float aimAssistAngleLimit = 25f;
    [SerializeField] private float aimAssistStep = 1.5f;

    [Header("🧗 Swing Settings")]
    [SerializeField] private float swingForce = 50f;
    [SerializeField] [Range(0, 180)] private float swingAngleLimit = 80f;

    [SerializeField] private float winchUpForce = 0.8f;
    [SerializeField] private float winchDownForce = 0.2f;
    [SerializeField] private float climbSpeed = 6f;

    [SerializeField] private float stopDistance = 0.5f;
    [SerializeField] private float hookRadius = 0.2f;

    [SerializeField] private LayerMask hookableLayer;

    [Header("🏷️ Tags (구분용)")]
    [SerializeField] private string wallTag = "Wall";
    [SerializeField] private string enemyTag = "Enemy";
    [SerializeField] private string frozenEnemyTag = "FrozenEnemy";

    [Header("🏗️ Auto Winch Settings")]
    [SerializeField] private float autoWinchAmount = 3.0f;
    [SerializeField] private float autoWinchSpeed = 5.0f;

    [Header("✨ Smart Auto Winch (Sanabi Style)")]
    [SerializeField] private float groundCheckDistance = 3.0f;
    [SerializeField] private float minGroundClearance = 1.5f;
    [SerializeField] private float smartWinchLerpSpeed = 10.0f;

    [SerializeField] private HookRopeVisual ropeVisual;
    [SerializeField] private float waveStrength = 1.0f;
    [SerializeField] private float waveFrequency = 3.0f;
    [SerializeField] private Transform firePoint;

    public float MaxDistance => maxDistance;

    private PlayerAim _playerAim;
    private PlayerMovement _playerMovement;
    private Camera _mainCamera;
    private bool _isHooking;
    public bool IsHooking => _isHooking;
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

        Vector3 startPos = firePoint != null ? firePoint.position : transform.position + Vector3.up * 1.0f;
        Vector3 currentPos = startPos;

        Vector3 dir;

        if (_playerAim.LockedTarget != null)
        {
            dir = (_playerAim.LockedTarget.position - startPos).normalized;
        }
        else
        {
            Vector3 aimPos = _playerAim.GetAimWorldPosition();
            Vector3 rawDir = (aimPos - startPos).normalized;
            dir = GetSnappedAimDirection(startPos, rawDir);
        }

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
                Vector3 dynamicStart = firePoint != null ? firePoint.position : transform.position + Vector3.up * 1.0f;
                if (ropeVisual != null) ropeVisual.DrawRope(dynamicStart, bestHitPoint);

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
            if (Physics.Raycast(currentPos, dir, out RaycastHit hit, moveStep, hookableLayer))
            {
                if (hit.distance > 0f)
                {
                    currentPos = hit.point;
                }
                else
                {
                    // Fallback just in case point is exactly inside something
                    currentPos = currentPos + dir * 0.1f;
                }
                Vector3 dynamicStart = firePoint != null ? firePoint.position : transform.position + Vector3.up * 1.0f;
                if (ropeVisual != null) ropeVisual.DrawRope(dynamicStart, currentPos);

                if (hit.collider.TryGetComponent(out Interaction.IInteractable interactable))
                {
                    if (ropeVisual != null) ropeVisual.DrawRope(dynamicStart, currentPos);
                    interactable.OnInteract(gameObject);
                    StopHook();
                    yield break;
                }

                if (hit.collider.TryGetComponent(out BaseEnemy enemy))
                {
                    enemy.OnHooked();

                    if (enemy.IsFrozen)
                    {
                        _hookAnchor.position = currentPos;
                        _hookAnchor.SetParent(hit.transform);
                        _hookAnchor.gameObject.SetActive(true);
                        _currentHookTarget = _hookAnchor;
                        yield return StartCoroutine(PullSelfRoutine(_currentHookTarget));
                    }
                    else
                    {

                        yield return StartCoroutine(ZipToTargetRoutine(hit.transform));
                    }
                }
                else
                {
                    string tag = hit.collider.tag;

                    if (tag == wallTag || tag == frozenEnemyTag)
                    {
                        _hookAnchor.position = currentPos;
                        _hookAnchor.SetParent(hit.transform);
                        _hookAnchor.gameObject.SetActive(true);
                        _currentHookTarget = _hookAnchor;
                        yield return StartCoroutine(PullSelfRoutine(_currentHookTarget));
                    }
                    else if (tag == enemyTag)
                    {

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

        // 🎯 훅 걸자마자 리지드바디를 깨워서 멈칫거림을 방지해!
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.WakeUp();

        float currentRopeLength = Vector3.Distance(transform.position, targetPos);
        float baseRopeLength = currentRopeLength;
        bool isInitAutoWinching = false;

        if (_playerMovement.IsGrounded && targetPos.y > transform.position.y + 1.0f)
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

                float changeAmount = climbSpeed * Time.fixedDeltaTime;

                if (_playerMovement.IsGrounded && inputY < -0.1f)
                {
                    currentRopeLength += changeAmount;
                    currentRopeLength = Mathf.Clamp(currentRopeLength, 1f, maxDistance);

                    yield return new WaitForFixedUpdate();
                    continue;
                }

                if (inputY > 0)
                {
                    Vector3 currentVel = rb.linearVelocity;
                    float speedAlongRope = Vector3.Dot(currentVel, tensionDir);
                    rb.linearVelocity = currentVel - (tensionDir * speedAlongRope) + (tensionDir * climbSpeed);

                    currentRopeLength -= changeAmount;

                    if (currentRopeLength < distToAnchor - 0.05f)
                    {
                        currentRopeLength = distToAnchor - 0.05f;
                    }
                }
                else
                {
                    if (distToAnchor >= maxDistance - 0.1f)
                    {
                        currentRopeLength = maxDistance;

                        Vector3 vel = rb.linearVelocity;

                        float outgoingSpeed = Vector3.Dot(vel, -tensionDir);
                        if (outgoingSpeed > 0)
                        {
                            rb.linearVelocity -= (-tensionDir * outgoingSpeed);
                        }
                    }
                    else
                    {
                         if (currentRopeLength < maxDistance - 0.05f)
                         {
                             Vector3 currentVel = rb.linearVelocity;
                             float speedAlongRope = Vector3.Dot(currentVel, tensionDir);
                             rb.linearVelocity = currentVel - (tensionDir * speedAlongRope) + (-tensionDir * climbSpeed);

                             currentRopeLength += changeAmount;
                             
                             if (currentRopeLength > distToAnchor + 1.0f)
                             {
                                 currentRopeLength = distToAnchor + 1.0f;
                             }
                         }
                    }
                }

                currentRopeLength = Mathf.Clamp(currentRopeLength, 1f, maxDistance);
                baseRopeLength = currentRopeLength;
                isInitAutoWinching = false;
            }
            else
            {
                 if (isInitAutoWinching)
                 {
                      Vector3 currentVel = rb.linearVelocity;
                      float speedAlongRope = Vector3.Dot(currentVel, tensionDir);
                      rb.linearVelocity = currentVel - (tensionDir * speedAlongRope) + (tensionDir * autoWinchSpeed);

                      currentRopeLength = Mathf.MoveTowards(currentRopeLength, baseRopeLength, autoWinchSpeed * Time.fixedDeltaTime);

                      if (currentRopeLength < distToAnchor - 0.05f)
                      {
                           currentRopeLength = distToAnchor - 0.05f;
                      }

                      if (currentRopeLength <= baseRopeLength + 0.01f)
                      {
                           isInitAutoWinching = false;
                           baseRopeLength = currentRopeLength;
                      }
                 }
                 else if (!_playerMovement.IsGrounded)
                 {
                      float targetLength = baseRopeLength;

                      Vector3 velDir = rb.linearVelocity.normalized;
                      float speed = rb.linearVelocity.magnitude;
                      int obstacleLayer = hookableLayer | _playerMovement.GroundLayer | _playerMovement.WallLayer;

                      // 🎯 벽 슬라이딩 로직을 제거해서 벽을 타고 승천하는 현상을 막았어 오빠!


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

                      if (currentRopeLength > targetLength)
                      {
                           float smoothWinchSpeed = climbSpeed * 1.5f;
                           currentRopeLength = Mathf.MoveTowards(currentRopeLength, targetLength, Time.fixedDeltaTime * smoothWinchSpeed);
                           baseRopeLength = currentRopeLength;
                      }
                 }
                 else
                 {
                      baseRopeLength = currentRopeLength;
                 }
            }

            if (_playerMovement.IsGrounded)
            {
                 _playerMovement.SetDrag(0f);
            }
            else
            {
                 // 🎯 공기 저항을 0.05f에서 0.5f로 높여서 가만히 있으면 점점 멈추게 했어 오빠!
                 _playerMovement.SetDrag(0.5f);
            }

            if (_playerMovement != null)
            {

            }

            bool isWinchingUp = (_playerMovement.MoveInput.y > 0.1f);

            if (distToAnchor > currentRopeLength)
            {

                Vector3 velocity = rb.linearVelocity;
                float speedAway = Vector3.Dot(velocity, -tensionDir);

                bool isAtMaxDist = (currentRopeLength >= maxDistance - 0.05f);
                bool isWinchingDown = (_playerMovement.MoveInput.y < -0.1f) && !isAtMaxDist;

                float limitSpeed = 0f;
                if (isWinchingDown)
                {
                    limitSpeed = climbSpeed;
                }

                if (speedAway > limitSpeed)
                {
                    // 🎯 벽 근처 떨림(Jitter) 방지: 벽과 너무 가까우면 속도 보정을 살짝 유연하게 해줄게!
                    float jitterDamping = (distToSurface < 0.5f) ? 0.5f : 1.0f;
                    Vector3 velocityCorrection = -tensionDir * (speedAway - limitSpeed) * jitterDamping;
                    Vector3 newVel = velocity - velocityCorrection;

                    // 🎯 속도 보존 로직을 제거해서 공기 저항(Drag)이 정상적으로 작동하게 했어 오빠!
                    rb.linearVelocity = newVel;
                }

                float distError = distToAnchor - currentRopeLength;

                // 🎯 팅김 현상 해결: 강제 위치 보정 시 벽 충돌을 고려하도록 수정했어!
                if (!isWinchingDown && !isWinchingUp && !isInitAutoWinching)
                {
                    if (distError > 0.05f) // 허용 오차를 살짝 늘려서 미세한 떨림을 방지해.
                    {
                        float correctAmount = Mathf.Min(distError, 0.2f); // 보정 강도를 낮춰서 쫀득하게!
                        Vector3 targetOffset = tensionDir * correctAmount;
                        
                        // 🎯 기준점을 발 밑이 아닌 몸통(Vector3.up)으로 올리고, 반지름을 줄여서 더 정확하게 체크해!
                        Vector3 castOrigin = transform.position + Vector3.up;
                        if (!Physics.SphereCast(castOrigin, 0.3f, tensionDir, out _, correctAmount, hookableLayer))
                        {
                            rb.MovePosition(transform.position + targetOffset);
                        }
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

                // Ensure tangent always points in the positive Z direction (+Z = Right)
                // This guarantees that D (Positive Input) always swings the player to the Right (+Z)
                // and A (Negative Input) always swings the player to the Left (-Z), regardless
                // of whether the anchor is above or below the player.
                if (tangent.z < 0)
                {
                    tangent = -tangent;
                }

                if (_playerMovement.IsGrounded)
                {
                    float currentZSpeed = Mathf.Abs(rb.linearVelocity.z);
                    float boostMultiplier = 1.0f;

                    if (currentZSpeed < 5f) boostMultiplier = 15.0f;
                    else if (currentZSpeed < 15f) boostMultiplier = 5.0f;

                    Vector3 groundForce = tangent * (inputX * swingForce * boostMultiplier);
                    _playerMovement.AddHookForce(groundForce);
                }
                else
                {
                    Vector3 airForce = tangent * (inputX * swingForce);
                    _playerMovement.AddHookForce(airForce);
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

        float stuckTimer = 0f;
        Vector3 lastPos = transform.position;

        while (_isHooking && target != null)
        {

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

            bool isEnemy = target.TryGetComponent(out BaseEnemy _) || (target.parent != null && target.parent.TryGetComponent(out BaseEnemy _));
            float checkDist = isEnemy ? safeZipDistance : stopDistance;
            
            float distanceToSurface = Mathf.Sqrt(distToSurfaceSqr);

            // 다음 프레임(Time.fixedDeltaTime)에 도달할 예정이거나 이미 안전 거리 안에 있다면 즉시 멈춤
            // 초근접에서 쐈을 때 0.1초 동안 억지로 120m/s로 가속하여 적과 충돌하는 버그(몸통박치기) 해결
            if (distanceToSurface <= checkDist || (distanceToSurface - checkDist) <= currentZipSpeed * Time.fixedDeltaTime)
            {
                myRb.linearVelocity = Vector3.zero;
                StopHook();
                yield break;
            }

            myRb.linearVelocity = zipDir * currentZipSpeed;

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

    private Collider _lastSnappedCollider;
    private Vector3 _lastSnappedPoint;

    public Vector3 GetSnappedAimDirection(Vector3 startPos, Vector3 rawDir)
    {
        Vector3 dir = new Vector3(0, rawDir.y, rawDir.z).normalized;

        if (!useAimAssist) return dir;

        bool hasDirectHit = Physics.Raycast(startPos, dir, out RaycastHit directHit, maxDistance, hookableLayer);

        if (hasDirectHit)
        {
            _lastSnappedCollider = null;
            return dir;
        }

        if (_lastSnappedCollider != null)
        {
            Vector3 dirToLast = (_lastSnappedPoint - startPos).normalized;
            dirToLast = new Vector3(0, dirToLast.y, dirToLast.z).normalized;

            if (Vector3.Angle(dir, dirToLast) <= aimAssistAngleLimit)
            {
                if (Physics.Raycast(startPos, dirToLast, out RaycastHit stickHit, maxDistance, hookableLayer))
                {
                    if (stickHit.collider == _lastSnappedCollider)
                    {
                        return dirToLast;
                    }
                }
            }
        }

        _lastSnappedCollider = null;
        int maxSteps = Mathf.RoundToInt(aimAssistAngleLimit / aimAssistStep);

        for (int i = 1; i <= maxSteps; i++)
        {
            float currentAngle = i * aimAssistStep;

            Vector3 dirP = Quaternion.AngleAxis(currentAngle, Vector3.right) * dir;
            bool hitP = Physics.Raycast(startPos, dirP, out RaycastHit rcp, maxDistance, hookableLayer);

            Vector3 dirM = Quaternion.AngleAxis(-currentAngle, Vector3.right) * dir;
            bool hitM = Physics.Raycast(startPos, dirM, out RaycastHit rcm, maxDistance, hookableLayer);

            if (hitP && hitM)
            {
                bool useP = rcp.distance < rcm.distance;
                _lastSnappedPoint = useP ? rcp.point : rcm.point;
                _lastSnappedCollider = useP ? rcp.collider : rcm.collider;
                dir = (_lastSnappedPoint - startPos).normalized;
                break;
            }
            else if (hitP)
            {
                _lastSnappedPoint = rcp.point;
                _lastSnappedCollider = rcp.collider;
                dir = (_lastSnappedPoint - startPos).normalized;
                break;
            }
            else if (hitM)
            {
                _lastSnappedPoint = rcm.point;
                _lastSnappedCollider = rcm.collider;
                dir = (_lastSnappedPoint - startPos).normalized;
                break;
            }
        }

        return new Vector3(0, dir.y, dir.z).normalized;
    }
}