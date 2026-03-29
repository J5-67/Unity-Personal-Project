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
    [SerializeField] private float smartWinchMultiplier = 1.5f;
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
    private Rigidbody _rb;
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
        _rb = GetComponent<Rigidbody>();
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
        if (Core.GameManager.Instance != null && Core.GameManager.Instance.IsDialogueActive) return;
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
        if (_rb != null) _rb.WakeUp();
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
        float startTime = Time.time;
        while (_isHooking)
        {
            if (targetTransform == null || _playerMovement.IsDashing || (Core.GameManager.Instance != null && Core.GameManager.Instance.IsDialogueActive))
            {
                StopHook();
                yield break;
            }
            if (_playerMovement.ConsumeJumpInput())
            {
                bool allowZip = true;
                if (targetTransform.parent != null && targetTransform.parent.TryGetComponent(out BaseEnemy zipCheckEnemy))
                {
                    if (!zipCheckEnemy.IsFrozen) allowZip = false;
                }
                if (allowZip)
                {
                    yield return StartCoroutine(ZipToTargetRoutine(targetTransform, wallZipSpeed));
                    yield break;
                }
            }
            Vector3 myPos = transform.position;
            Vector3 diff = myPos - targetPos;
            float distToAnchorSqr = diff.sqrMagnitude;
            float distToAnchor = Mathf.Sqrt(distToAnchorSqr);
            Vector3 tensionDir = -(diff / (distToAnchor + 0.0001f));
            float inputY = _playerMovement.MoveInput.y;
            if (Mathf.Abs(inputY) > 0.1f)
            {
                float changeAmount = climbSpeed * Time.fixedDeltaTime;
                if (_playerMovement.IsGrounded && inputY < -0.1f)
                {
                    currentRopeLength = Mathf.Clamp(currentRopeLength + changeAmount, 1f, maxDistance);
                }
                else if (inputY > 0)
                {
                    Vector3 currentVel = _rb.linearVelocity;
                    _rb.linearVelocity = currentVel - (tensionDir * Vector3.Dot(currentVel, tensionDir)) + (tensionDir * climbSpeed);
                    currentRopeLength = Mathf.Max(currentRopeLength - changeAmount, distToAnchor - 0.05f);
                }
                else
                {
                    if (distToAnchor >= maxDistance - 0.1f)
                    {
                        currentRopeLength = maxDistance;
                        float outgoingSpeed = Vector3.Dot(_rb.linearVelocity, -tensionDir);
                        if (outgoingSpeed > 0) _rb.linearVelocity -= (-tensionDir * outgoingSpeed);
                    }
                    else if (currentRopeLength < maxDistance - 0.05f)
                    {
                         Vector3 currentVel = _rb.linearVelocity;
                         _rb.linearVelocity = currentVel - (tensionDir * Vector3.Dot(currentVel, tensionDir)) + (-tensionDir * climbSpeed);
                         currentRopeLength = Mathf.Min(currentRopeLength + changeAmount, distToAnchor + 1.0f);
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
                      Vector3 currentVel = _rb.linearVelocity;
                      _rb.linearVelocity = currentVel - (tensionDir * Vector3.Dot(currentVel, tensionDir)) + (tensionDir * autoWinchSpeed);
                      currentRopeLength = Mathf.MoveTowards(currentRopeLength, baseRopeLength, autoWinchSpeed * Time.fixedDeltaTime);
                      if (currentRopeLength < distToAnchor - 0.05f) currentRopeLength = distToAnchor - 0.05f;
                      if (currentRopeLength <= baseRopeLength + 0.01f) { isInitAutoWinching = false; baseRopeLength = currentRopeLength; }
                 }
                 else if (!_playerMovement.IsGrounded)
                 {
                      float targetLength = baseRopeLength;
                      int obstacleLayer = hookableLayer | _playerMovement.GroundLayer | _playerMovement.WallLayer;
                      if (Physics.SphereCast(myPos + Vector3.up * 0.5f, 0.4f, Vector3.down, out RaycastHit groundHit, groundCheckDistance, obstacleLayer))
                      {
                           if (groundHit.distance < minGroundClearance)
                           {
                                targetLength = Mathf.Clamp(baseRopeLength - (minGroundClearance - groundHit.distance), 1f, maxDistance);
                           }
                      }
                      if (currentRopeLength > targetLength)
                      {
                           currentRopeLength = Mathf.MoveTowards(currentRopeLength, targetLength, Time.fixedDeltaTime * (climbSpeed * smartWinchMultiplier));
                           baseRopeLength = currentRopeLength;
                      }
                 }
                 else baseRopeLength = currentRopeLength;
            }
            _playerMovement.SetDrag(_playerMovement.IsGrounded ? 0f : 0.5f);
            if (distToAnchorSqr > currentRopeLength * currentRopeLength)
            {
                Vector3 velocity = _rb.linearVelocity;
                float speedAway = Vector3.Dot(velocity, -tensionDir);
                bool isWinchingDown = (inputY < -0.1f) && (currentRopeLength < maxDistance - 0.05f);
                float limitSpeed = isWinchingDown ? climbSpeed : 0f;
                if (speedAway > limitSpeed)
                {
                    _rb.linearVelocity = velocity - (-tensionDir * (speedAway - limitSpeed));
                }
                float distError = distToAnchor - currentRopeLength;
                if (distError > 0.05f && inputY >= -0.1f && !isInitAutoWinching)
                {
                    if (!Physics.SphereCast(myPos + Vector3.up, 0.3f, tensionDir, out _, Mathf.Min(distError, 0.2f), hookableLayer))
                    {
                        _rb.MovePosition(myPos + tensionDir * Mathf.Min(distError, 0.2f));
                    }
                }
            }
            if ((Time.time - startTime) > 0.2f)
            {
                Collider targetCol = targetTransform.GetComponent<Collider>() ?? targetTransform.parent?.GetComponent<Collider>();
                if (targetCol != null && (myPos - targetCol.ClosestPoint(myPos)).sqrMagnitude < stopDistance * stopDistance)
                {
                    if (targetTransform.parent != null && targetTransform.parent.TryGetComponent(out BaseEnemy _)) { StopHook(); yield break; }
                }
            }
            float inputX = _playerMovement.MoveInput.x;
            if (Mathf.Abs(inputX) > 0.1f)
            {
                Vector3 axis = Vector3.right;
                Vector3 tangent = Vector3.Cross((myPos - targetPos).normalized, axis).normalized;
                if (tangent.z < 0) tangent = -tangent;
                float boost = _playerMovement.IsGrounded ? (Mathf.Abs(_rb.linearVelocity.z) < 5f ? 15.0f : 5.0f) : 1.0f;
                _playerMovement.AddHookForce(tangent * (inputX * swingForce * boost));
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
            if (Core.GameManager.Instance != null && Core.GameManager.Instance.IsDialogueActive)
            {
                StopHook();
                yield break;
            }
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