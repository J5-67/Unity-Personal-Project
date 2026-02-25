using UnityEngine;
using UnityEngine.VFX;
// 일반 EnemyShooter를 상속받거나 그대로 쓰기엔 Homing 기능만 바꾸면 됨.
// 하지만 미사일 폭격(여러 발)을 하고 싶다면 새로운 EnemyHeavy가 필요.
// 일단 유저가 "미사일 발사"를 요청했으니, EnemyMissile.cs 를 완성했으므로
// EnemyShooter를 그대로 사용하고 프리팹만 교체하면 됨!

// 여기는 EnemyMissile.cs의 내용을 완성하겠음.
// 아까 write_to_file에서 base.Update()를 불렀는데,
// 부모 moveSpeed가 private였음. (Step 904에서 protected로 수정함)
// 그러니 이제 안심하고 EnemyMissile.cs를 제대로 작성.

// [Fix] Redundant using removed

public class EnemyMissile : EnemyProjectile
{
    [Header("🚀 MISSILE PID SETTINGS")]
    [SerializeField] private float kp = 50f;  // 비례 (P): 오차에 비례하여 회전
    [SerializeField] private float ki = 5f;   // 적분 (I): 누적 오차 보정
    [SerializeField] private float kd = 2f;   // 미분 (D): 급격한 변화(오버슈트) 방지

    [Header("🎯 HOMING SETTINGS")]
    [SerializeField] private float homingDuration = 3.0f; // 유도 지속 시간
    [SerializeField] private float maxHomingAngle = 120f; // 초기 추적 가능 각도 (넓음)
    [SerializeField] private float minHomingAngle = 10f;  // 마지막 조여지는 각도 (직진 강제)

    private Transform _target;
    private Collider _targetCollider; // [New] 타겟 몸통(Center) 조준을 위한 콜라이더 캐싱
    private float _timer;
    private bool _isHoming = true;
    private PID _pidController;

    private bool _isFrozen = false;
    private int _originalLayer;

    [Header("⚡ Glitch Visuals")]
    [SerializeField] private Shader glitchShader;           
    [SerializeField] private float glitchIntensity = 0.5f;  
    [SerializeField] private float glitchSpeed = 20f;  

    [Header("✨ VFX Components")]
    [SerializeField] private VisualEffect _vfx;
    private bool _isHit = false;

    private Renderer _renderer;
    private Material _originalMaterial;
    private MaterialPropertyBlock _propBlock;
    private Coroutine _glitchCoroutine;
    
    // Shader Property IDs
    private static Material _sharedGlitchMaterial;
    private static readonly int _MainTexId = Shader.PropertyToID("_MainTex");
    private static readonly int _BaseMapId = Shader.PropertyToID("_BaseMap");
    private static readonly int _GlitchPowerId = Shader.PropertyToID("_GlitchPower");
    private static readonly int _NoiseSpeedId = Shader.PropertyToID("_NoiseSpeed");
    private static readonly int _ColorId = Shader.PropertyToID("_Color");

    // [New] 해킹 가능 여부 확인용 프로퍼티
    public bool IsFrozen => _isFrozen;

    private void Awake()
    {
        _pidController = new PID();
        _originalLayer = gameObject.layer; // 원래 레이어 저장 (Projectile)
        
        _renderer = GetComponentInChildren<Renderer>();
        if (_renderer != null) _originalMaterial = _renderer.sharedMaterial;
    }

    protected override void OnEnable()
    {
        base.OnEnable(); // [Fix] 부모 클래스(EnemyProjectile)의 초기화도 함께 실행!
        
        _pidController.Reset();
        _isHoming = true;
        _isFrozen = false; // 재사용 시 얼음 상태 해제
        _isHit = false;
        _timer = 0f;

        if (TryGetComponent(out Collider col)) col.enabled = true;
        if (_renderer != null) _renderer.enabled = true;

        if (_target == null)
        {
            // [Fix] 느려터진 FindAnyObjectByType 대신, 유니티 내부 해시를 써서 10 배 이상 빠른 태그 검색으로 교체!
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) 
            {
                _target = p.transform;
                _targetCollider = _target.GetComponent<Collider>();
            }
        }

        // [Fix] base.OnEnable()에서 처리해주므로 수동 카운트다운 제거
    }
    
    // ... 기존 코드 유지 (OnDisable, SelfDestroy, Start, SetFrozen, AutoHackRoutine, GlitchRoutine 등) ... //
    protected override void OnDisable()
    {
        base.OnDisable(); // [Fix] 부모의 CancelInvoke도 같이 실행!
        if (_isFrozen) 
        {
            _isFrozen = false;
            gameObject.layer = _originalLayer;
        }
    }

    private void SelfDestroy()
    {
        // [Fix] Destroy 대신 웅덩이로 귀향!
        if (OnReleaseToPool != null) OnReleaseToPool.Invoke(this);
        else Destroy(gameObject);
    }

    private void Start() { }

    private Coroutine _autoHackCoroutine;
    [SerializeField] private float autoHackDelay = 1.5f; 

    public void SetFrozen(bool state)
    {
        if (_isFrozen == state) return;

        _isFrozen = state;

        if (_isFrozen)
        {
            CancelInvoke(nameof(SelfDestroy));
            gameObject.layer = LayerMask.NameToLayer("Wall");
            gameObject.tag = "Wall"; 

            if (TryGetComponent(out Rigidbody rb))
            {
                rb.isKinematic = true;
                rb.linearVelocity = Vector3.zero;
            }

            if (_glitchCoroutine != null) StopCoroutine(_glitchCoroutine);
            _glitchCoroutine = StartCoroutine(GlitchRoutine());

            if (_autoHackCoroutine != null) StopCoroutine(_autoHackCoroutine);
            _autoHackCoroutine = StartCoroutine(AutoHackRoutine());
        }
        else
        {
            gameObject.layer = _originalLayer;
            gameObject.tag = "Untagged"; 
            
            Invoke(nameof(SelfDestroy), 5f); 

             if (TryGetComponent(out Rigidbody rb))
            {
                rb.isKinematic = false; 
            }

            if (_glitchCoroutine != null) 
            {
                StopCoroutine(_glitchCoroutine);
                _glitchCoroutine = null;
            }

            if (_autoHackCoroutine != null)
            {
                StopCoroutine(_autoHackCoroutine);
                _autoHackCoroutine = null;
            }
            
            if (_renderer != null && _originalMaterial != null)
            {
                _renderer.sharedMaterial = _originalMaterial;
                _renderer.SetPropertyBlock(null);
            }
        }
    }

    private System.Collections.IEnumerator AutoHackRoutine()
    {
        yield return new WaitForSeconds(autoHackDelay);

        Transform hackTarget = null;
        
        // [Fix] 악마의 FindAnyObjectByType 제거! (반경 30m 내의 적들만 스캔해서 역해킹 타겟 설정)
        Collider[] hits = Physics.OverlapSphere(transform.position, 30f);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent(out BossHealth boss) || (hit.transform.parent != null && hit.transform.parent.TryGetComponent(out boss)))
            {
                hackTarget = boss.transform;
                break; // 보스 최우선 타겟!
            }
            else if (hackTarget == null)
            {
                if (hit.TryGetComponent(out BaseEnemy enemy) || (hit.transform.parent != null && hit.transform.parent.TryGetComponent(out enemy)))
                {
                    hackTarget = enemy.transform; // 일반 적
                }
            }
        }

        if (hackTarget != null)
        {
            HackReverse(hackTarget);
            
            if (Core.VFXManager.Instance != null)
            {
                 Core.VFXManager.Instance.PlayHackExplosion(transform.position);
            }
        }
        else
        {
            if (Core.VFXManager.Instance != null)
            {
                 Core.VFXManager.Instance.PlayHackExplosion(transform.position);
            }
            SelfDestroy(); // [Fix] 웅덩이로 돌려보냄
        }
    }

    private System.Collections.IEnumerator GlitchRoutine()
    {
        if (_sharedGlitchMaterial == null && glitchShader != null)
        {
             _sharedGlitchMaterial = new Material(glitchShader);
             _sharedGlitchMaterial.enableInstancing = true; 
        }

        if (_renderer != null && _sharedGlitchMaterial != null)
        {
            Texture originalTex = null;
            if (_originalMaterial.HasProperty(_MainTexId)) originalTex = _originalMaterial.GetTexture(_MainTexId);
            else if (_originalMaterial.HasProperty(_BaseMapId)) originalTex = _originalMaterial.GetTexture(_BaseMapId);

            _renderer.sharedMaterial = _sharedGlitchMaterial;
            
            if (_propBlock == null) _propBlock = new MaterialPropertyBlock();

            if (originalTex != null) 
            {
                _propBlock.SetTexture(_MainTexId, originalTex);
                _propBlock.SetTexture(_BaseMapId, originalTex); 
            }
            _propBlock.SetFloat(_NoiseSpeedId, glitchSpeed);
            _renderer.SetPropertyBlock(_propBlock);
        }

        while (true)
        {
            if (_renderer != null)
            {
                float noise = Mathf.PerlinNoise(Time.time * 10f, transform.position.x); 
                float currentPower = glitchIntensity * (0.5f + noise * 0.5f);
                
                _renderer.GetPropertyBlock(_propBlock); 
                _propBlock.SetFloat(_GlitchPowerId, currentPower);

                if (noise > 0.8f) _propBlock.SetColor(_ColorId, Color.white); 
                else _propBlock.SetColor(_ColorId, Color.cyan);

                _renderer.SetPropertyBlock(_propBlock); 
            }
 
            yield return null;
        }
    }

    [Header("⚙️ MODE SETTINGS")]
    [SerializeField] private bool ignoreXAxis = true; 
    [SerializeField] private float turnSpeed3D = 5.0f; 

    private Vector3 _initialDirection; 
    private float _homingDelay = 0f;   

    public void Launch(Vector3 direction, float delay)
    {
        _initialDirection = direction.normalized;
        _homingDelay = delay;
        _isHoming = false; 
        _timer = 0f;

        transform.forward = _initialDirection;

        if (_vfx != null)
        {
             _vfx.SendEvent("create");
        }
    }

    public void Set3DHoming(bool enable)
    {
        ignoreXAxis = !enable; 
    }

    protected override void Update()
    {
        if (_isFrozen || _isHit) return;

        if (_homingDelay > 0f)
        {
            _homingDelay -= Time.deltaTime;
            transform.Translate(Vector3.forward * speed * Time.deltaTime); 
            return;
        }
        else 
        {
            if (!_isHoming && _timer == 0f) _isHoming = true;
        }

        if (_isHoming && _target != null)
        {
            Vector3 targetPos = _target.position;
            
            // [Fix] 빙빙 도는 공전(Orbit) 완화 및 바닥 충돌 방지를 위해 대상의 정중앙(Center) 조준
            if (_targetCollider != null) targetPos = _targetCollider.bounds.center;
            else targetPos.y += 1.0f; // 콜라이더 없으면 대충 가슴/머리 높이로 1m 보정

            if (ignoreXAxis) 
            {
                targetPos.x = transform.position.x;
            }

            Vector3 directionToTarget = (targetPos - transform.position).normalized;

            if (ignoreXAxis)
            {
                Vector3 currentDirection = transform.forward;
                float angleError = Vector3.Angle(currentDirection, directionToTarget);

                float t = _timer / homingDuration;
                float currentLimitAngle = Mathf.Lerp(maxHomingAngle, minHomingAngle, t * t);

                if (angleError > currentLimitAngle)
                {
                    _isHoming = false; 
                }
                else
                {
                    Vector3 cross = Vector3.Cross(currentDirection, directionToTarget);
                    float directionSign = Mathf.Sign(cross.x);
                    float signedError = angleError * directionSign;
                    if (angleError < 1f) signedError = 0f;

                    float rotationAmount = _pidController.GetOutput(signedError, Time.deltaTime, kp, ki, kd);
                    rotationAmount = Mathf.Clamp(rotationAmount, -720f, 720f);
                    transform.Rotate(Vector3.right, rotationAmount * Time.deltaTime, Space.World);
                }
            }
            else
            {
                // [Mode B] 3D 유도 (보스용 & 해킹역추적용)
                // [Fix] Slerp는 목표점 부근에서 점근적 타협을 하므로 너무 빠르면 중심 궤도를 돌기만 함(Orbit).
                // 항상 확실하게 머리를 꺾도록 일정한 각속도(RotateTowards)를 사용하여 꽂히게 만듦!
                Vector3 newDir = Vector3.RotateTowards(transform.forward, directionToTarget, turnSpeed3D * Time.deltaTime, 0f);
                if (newDir.sqrMagnitude > 0.001f)
                {
                    transform.rotation = Quaternion.LookRotation(newDir);
                }
            }

            _timer += Time.deltaTime;
            if (_timer >= homingDuration)
            {
                _isHoming = false; 
            }
        }

        if (ignoreXAxis)
        {
            Vector3 fwd = transform.forward;
            fwd.x = 0f; 
            if (fwd.sqrMagnitude > 0.001f)
            {
                transform.forward = fwd.normalized;
            }
        }

        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    private bool _isHacked = false; 
    public bool IsHacked => _isHacked; 

    public void HackReverse(Transform newTarget)
    {
        _isFrozen = false; 
        
        _isHacked = true;
        _target = newTarget;
        
        // [Fix] 최상위에 콜라이더가 없을 수 있으므로 자식까지 뒤져서 실질적인 타겟 바운즈 캐싱
        _targetCollider = _target.GetComponentInChildren<Collider>(); 
        
        _isHoming = true;
        _timer = 0f;

        // [Fix] ⭐️핵심 원인⭐️ 자기가 쏜 미사일은 발사 시점에서 서로 영원히 충돌 무시(IgnoreCollision)가 걸려있음!
        // 그래서 해킹 후 다시 원래 주인에게 돌아가도 유령처럼 그냥 통과하고 빙빙 돌기만 했던 것!
        // 다시 칠 수 있도록 대상의 모든 콜라이더와 나의 콜라이더 사이의 무시를 풀어줌(false).
        Collider[] myCols = GetComponentsInChildren<Collider>();
        Collider[] targetCols = _target.GetComponentsInChildren<Collider>();

        foreach (var mCol in myCols)
        {
            foreach (var tCol in targetCols)
            {
                if (mCol != null && tCol != null)
                {
                    Physics.IgnoreCollision(mCol, tCol, false);
                }
            }
        }

        int pProjLayer = LayerMask.NameToLayer("PlayerProjectile");
        if (pProjLayer != -1) gameObject.layer = pProjLayer;
        else gameObject.layer = LayerMask.NameToLayer("Default"); 

        Set3DHoming(true); 

        speed *= 1.5f; 
        damage *= 5; 
        
        // [Fix] 해킹 시 엄청 빠르게 꺾이도록 회전 속도를 어마어마하게 증가시킴 (거의 유도탄 100% 명중수준)
        turnSpeed3D *= 5f; // Slerp 시절엔 5배도 밀렸지만, RotateTowards 방식에선 초당 1432도(!) 꺾이는 미친 추적력 발휘
        homingDuration = 10f; 

        _pidController.Reset();

        if (_propBlock != null && _renderer != null)
        {
             _propBlock.SetColor(_ColorId, Color.green); 
             _renderer.SetPropertyBlock(_propBlock);
        }
    }

    protected override void HitAndDestroy()
    {
        if (_isHit) return;
        _isHit = true;

        if (_vfx != null)
        {
            _vfx.SendEvent("hit");
            
            // [Fix] 움직임 정지 및 메쉬 숨기기 (VFX 자연스러운 소멸 대기)
            if (TryGetComponent(out Collider col)) col.enabled = false;
            
            if (TryGetComponent(out Rigidbody rb)) 
            {
                rb.linearVelocity = Vector3.zero;
                rb.isKinematic = true;
            }

            if (_renderer != null) _renderer.enabled = false;
            
            CancelInvoke(nameof(SelfDestroy));
            Invoke(nameof(SelfDestroy), 2f); // 2초 뒤 파괴
        }
        else
        {
            base.HitAndDestroy();
        }
    }

    protected override void OnTriggerEnter(Collider other)
    {
        // [New] 얼어있으면 폭발하지 않음 (플레이어 대시 통과 허용)
        if (_isFrozen) return;

        // [New] 해킹된 상태 처리
        if (_isHacked)
        {
            // 플레이어는 무시 (팀킬 방지)
            if (other.CompareTag("Player")) return;

            // [Fix] 내가 쏜 총알에 내가 맞는 것 방지 (안전 장치)
            if (other.GetComponentInParent<EnemyProjectile>() != null) return;

            // [New] 1. 쉴드 타격 체크 (직접 쉴드 콜라이더를 맞혔거나, 본체를 맞혔지만 자식에 쉴드가 켜져있을 때)
            EnemyShield shield = other.GetComponentInParent<EnemyShield>(); 
            if (shield == null) shield = other.GetComponentInChildren<EnemyShield>();

            if (shield != null && shield.gameObject.activeInHierarchy)
            {
                // 쉴드가 켜져있다면 본체 대신 쉴드만 파괴!
                shield.BreakShield();
                
                if (Core.VFXManager.Instance != null)
                {
                    Core.VFXManager.Instance.PlayHackExplosion(transform.position);
                }
                
                HitAndDestroy(); // [Fix] 미사일 소멸 시 VFX 처리
                return;
            }

            // [Fix] 2. 적(Enemy)이나 보스(Boss) 공격 (방어막 없을 때)
            // 본체에 부딪히지 않고 무기 등 자식 콜라이더에 부딪힐 수도 있으니 GetComponentInParent 사용
            BaseEnemy baseEnemy = other.GetComponentInParent<BaseEnemy>();
            
            if (other.CompareTag("Enemy") || other.CompareTag("Boss") || baseEnemy != null)
            {
                 // 보스 체력 깎기
                 if (other.TryGetComponent(out BossHealth bossHealth))
                 {
                     bossHealth.TakeDamage(damage);
                 }
                 else if (baseEnemy != null)
                 {
                     baseEnemy.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
                 }

                 // 폭발 이펙트 (해킹용)
                 if (Core.VFXManager.Instance != null)
                 {
                     Core.VFXManager.Instance.PlayHackExplosion(transform.position);
                 }

                 HitAndDestroy(); // [Fix] 미사일 소멸 시 VFX 처리
                 return;
            }
            return; // 해킹 상태에선 그 외 충돌(벽 등)은 일단 무시하거나 로직 추가 가능
        }

        // --- 아래는 일반(적군) 상태일 때 로직 ---
        
        // 적군끼리는 충돌 무시 (보스 몸에서 나올 때 바로 터지면 안 되니까)
        if (other.CompareTag("Enemy") || other.CompareTag("Boss") || other.GetComponent<BaseEnemy>() != null) return;

        // [Fix] 내가 쏜 거니까 나(EnemyShooter 등 부모) 무시
        if (other.GetComponentInParent<BaseEnemy>() != null) return;

        // [Fix] 미사일(또는 다른 적의 투사체)끼리 부딪혀서 터지는 현상(2번째 미사일 폭발 등) 방지
        if (other.GetComponentInParent<EnemyProjectile>() != null) return;

        // 플레이어 피격 처리 (base를 안 부르고 여기서 직접 명확하게 처리)
        if (other.CompareTag("Player"))
        {
            if (other.TryGetComponent(out PlayerHealth health))
            {
                health.TakeDamage(damage);
            }
            
            // [Fix] 없는 카미카제 이펙트 대신, 프리팹에 할당된(또는 부모의) HitAndDestroy() 정식 호출
            HitAndDestroy();
            return;
        }

        // 그 외 지형(Wall 등)에 부딪히면 터짐
        if (other.CompareTag("Untagged") || other.CompareTag("Wall"))
        {
            // [Fix] 눈에 보이지 않는 이벤트용 트리거 구역(예: 웨이브 시작 존 등)에 닿아서 공중 폭발하는 것 방지
            if (other.isTrigger) return;

            HitAndDestroy();
        }
    }
}
