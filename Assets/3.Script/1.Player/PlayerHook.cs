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
            dir = (aimPos - startPos).normalized;
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
            if (Physics.SphereCast(currentPos, hookRadius, dir, out RaycastHit hit, moveStep, hookableLayer))
            {
                if (hit.distance > 0f)
                {
                    currentPos = hit.point;
                }
                else
                {
                    Vector3 closest = hit.collider.ClosestPoint(currentPos);
                    if (closest == Vector3.zero && currentPos != Vector3.zero)
                    {
                        currentPos = currentPos + dir * 0.1f; // 안전하게 약간 앞의 좌표 사용
                    }
                    else
                    {
                        currentPos = closest;
                    }
                }
                ropeVisual.DrawRope(transform.position, currentPos);

                if (hit.collider.TryGetComponent(out Interaction.IInteractable interactable))
                {
                    ropeVisual.DrawRope(transform.position, currentPos);
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

        float currentRopeLength = Vector3.Distance(transform.position, targetPos);

        float baseRopeLength = currentRopeLength;
        bool isInitAutoWinching = false;

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

                    if (currentRopeLength < distToAnchor)
                    {
                        currentRopeLength = distToAnchor;
                    }

                    if (distToAnchor < currentRopeLength)
                    {
                        currentRopeLength = distToAnchor;
                    }
                }
                else
                {

                    if (distToAnchor > currentRopeLength)
                    {
                         currentRopeLength = distToAnchor;
                    }

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

                         if (currentRopeLength < maxDistance - 0.1f)
                         {

                             Vector3 currentVel = rb.linearVelocity;
                             float speedAlongRope = Vector3.Dot(currentVel, tensionDir);
                             rb.linearVelocity = currentVel - (tensionDir * speedAlongRope) + (-tensionDir * climbSpeed);

                             currentRopeLength += changeAmount;
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

                      float targetLength = baseRopeLength;

                      Vector3 velDir = rb.linearVelocity.normalized;
                      float speed = rb.linearVelocity.magnitude;

                      int obstacleLayer = hookableLayer | _playerMovement.GroundLayer | _playerMovement.WallLayer;

                      if (speed > 2.0f)
                      {

                           if (Physics.SphereCast(myPos, 0.4f, velDir, out RaycastHit forwardHit, 1.5f, obstacleLayer))
                           {

                                if (Vector3.Dot(velDir, forwardHit.normal) < -0.1f)
                                {

                                     Vector3 projectedVel = Vector3.ProjectOnPlane(rb.linearVelocity, forwardHit.normal);

                                     rb.linearVelocity = projectedVel.normalized * speed;
                                }
                           }
                      }

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
                      }

                 }
            }

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

            }

            bool isWinchingUp = (_playerMovement.MoveInput.y > 0.1f);

            if (_playerMovement.IsGrounded && !isWinchingUp)
            {
                currentRopeLength = Mathf.Clamp(distToAnchor, 1f, maxDistance);
                baseRopeLength = currentRopeLength;
            }

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
                    Vector3 velocityCorrection = -tensionDir * (speedAway - limitSpeed);
                    Vector3 newVel = velocity - velocityCorrection;

                    if (limitSpeed == 0f && newVel.sqrMagnitude > 0.01f)
                    {
                        rb.linearVelocity = newVel.normalized * velocity.magnitude;
                    }
                    else
                    {
                        rb.linearVelocity = newVel;
                    }
                }

                float distError = distToAnchor - currentRopeLength;

                if (!isWinchingDown && !isWinchingUp && !isInitAutoWinching)
                {
                    if (distError > 0.01f)
                    {

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

                if (_playerMovement.IsGrounded)
                {

                    float currentZSpeed = Mathf.Abs(rb.linearVelocity.z);
                    float boostMultiplier = 1.0f;

                    if (currentZSpeed < 5f) boostMultiplier = 15.0f;
                    else if (currentZSpeed < 15f) boostMultiplier = 5.0f;

                    Vector3 groundForce = new Vector3(0f, 0f, inputX * swingForce * boostMultiplier);
                    _playerMovement.AddHookForce(groundForce);
                }
                else
                {

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

            bool isEnemy = target.TryGetComponent(out BaseEnemy _) || (target.parent != null && target.parent.TryGetComponent(out BaseEnemy _));
            float checkDist = isEnemy ? safeZipDistance : stopDistance;

            if (distToSurfaceSqr < checkDist * checkDist && (Time.time - startTime) > 0.1f)
            {

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