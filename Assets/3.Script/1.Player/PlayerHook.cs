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
    [SerializeField] private float winchDownForce = 0.5f;  
    
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
                if (inputY > 0)
                {
                    Vector3 pullForce = tensionDir * currentAccel * inputY * winchUpForce; 
                    _playerMovement.AddHookForce(pullForce);

                    float reduceAmount = 5f * Time.fixedDeltaTime; 
                    currentRopeLength = Mathf.Max(currentRopeLength - reduceAmount, 1f); 
                    
                    if (distToAnchor < currentRopeLength)
                    {
                        currentRopeLength = distToAnchor;
                    }
                }
                else 
                {
                    Vector3 pushForce = -tensionDir * currentAccel * Mathf.Abs(inputY) * winchDownForce;
                    _playerMovement.AddHookForce(pushForce);

                    if (distToAnchor > currentRopeLength)
                    {
                        currentRopeLength = distToAnchor;
                    }
                }
                
                currentRopeLength = Mathf.Max(currentRopeLength, 1f); 
            }
            else
            {
                 if (isAutoWinching)
                 {
                      currentRopeLength = Mathf.MoveTowards(currentRopeLength, finalRopeLength, autoWinchSpeed * Time.fixedDeltaTime);
                      
                      if (currentRopeLength <= finalRopeLength + 0.01f)
                      {
                           isAutoWinching = false;
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

            Rigidbody rb = GetComponent<Rigidbody>();
            
            if (distToAnchor > currentRopeLength) 
            {
                // 줄보다 멀어지면, 멀어지는 방향의 속도만 제거! (위치 강제 이동 X)
                Vector3 velocity = rb.linearVelocity;
                float speedAway = Vector3.Dot(velocity, -tensionDir); // tensionDir는 줄 당기는 방향

                if (speedAway < 0) // 줄 바깥으로 나가는 중이라면
                {
                    // 그 속도 성분만 제거
                    Vector3 velocityCorrection = -tensionDir * speedAway;
                    rb.linearVelocity -= velocityCorrection; 

                    // 그래도 너무 멀어지면 살짝 당겨줌 (MovePosition 사용)
                    float distError = distToAnchor - currentRopeLength;
                    if (distError > 0.1f)
                    {
                        Vector3 fixPos = transform.position + tensionDir * (distError * 0.1f); // 아주 살짝만
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

            if (distToAnchor >= currentRopeLength)
            {
                Vector3 velocity = rb.linearVelocity;
                float speedTowardsTarget = Vector3.Dot(velocity, tensionDir);
                
                if (speedTowardsTarget < 0)
                {
                    Vector3 velocityAway = tensionDir * speedTowardsTarget; 
                    
                    rb.linearVelocity -= velocityAway; 
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