using UnityEngine;

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
    private float _timer;
    private bool _isHoming = true;
    private PID _pidController;

    private bool _isFrozen = false;
    private int _originalLayer;

    [Header("⚡ Glitch Visuals")]
    [SerializeField] private Shader glitchShader;           
    [SerializeField] private float glitchIntensity = 0.5f;  
    [SerializeField] private float glitchSpeed = 20f;  

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

    private void OnEnable()
    {
        _pidController.Reset();
        _isHoming = true;
        _isFrozen = false; // 재사용 시 얼음 상태 해제
        _timer = 0f;

        if (_target == null)
        {
            var p = FindAnyObjectByType<PlayerMovement>();
            if (p != null) _target = p.transform;
        }

        Invoke(nameof(SelfDestroy), 5f);
    }

    private void OnDisable()
    {
        CancelInvoke();
        if (_isFrozen) 
        {
            // 비활성화 되면 얼음 상태도 해제하고 레이어 복구 (풀링 대비)
            _isFrozen = false;
            gameObject.layer = _originalLayer;
        }
    }

    private void SelfDestroy()
    {
        Destroy(gameObject);
    }

    // [Fix] 부모(EnemyProjectile)의 Start()가 실행되면 Destroy(gameObject, lifeTime)이 걸려서
    // CancelInvoke를 해도 사라지는 문제가 있었음. 빈 Start를 만들어서 부모 Start를 차단!
    private void Start() { }

    public void SetFrozen(bool state)
    {
        _isFrozen = state;

        if (_isFrozen)
        {
            // 1. 파괴 취소 (영원히 남아서 발판이 됨)
            CancelInvoke(nameof(SelfDestroy));

            // 2. 훅이 걸리도록 레이어를 "Wall"로 변경
            gameObject.layer = LayerMask.NameToLayer("Wall");
            
            // [New] 태그도 "Wall"로 변경 (훅 판정 확실하게)
            gameObject.tag = "Wall"; 

            // 3. 물리 엔진 정지 (혹시 모를 충돌 밀림 방지)
            if (TryGetComponent(out Rigidbody rb))
            {
                rb.isKinematic = true;
                rb.linearVelocity = Vector3.zero;
            }

            // [New] Glitch 효과 시작
            if (_glitchCoroutine != null) StopCoroutine(_glitchCoroutine);
            _glitchCoroutine = StartCoroutine(GlitchRoutine());
        }
        else
        {
            // 얼음 땡! 다시 원래대로
            gameObject.layer = _originalLayer;
            gameObject.tag = "Untagged"; // 태그 복구 (기본값)
            
            Invoke(nameof(SelfDestroy), 5f); // 5초 뒤 파괴 재예약

             if (TryGetComponent(out Rigidbody rb))
            {
                rb.isKinematic = false; 
            }

            // VFX 및 Glitch 효과 제거
            if (_glitchCoroutine != null) 
            {
                StopCoroutine(_glitchCoroutine);
                _glitchCoroutine = null;
            }
            
            // 원래 재질 복구
            if (_renderer != null && _originalMaterial != null)
            {
                _renderer.sharedMaterial = _originalMaterial;
                _renderer.SetPropertyBlock(null);
            }
        }
    }

    private System.Collections.IEnumerator GlitchRoutine()
    {
        // 1. 셰이더 생성 (Static 공유 - 메모리 절약)
        if (_sharedGlitchMaterial == null && glitchShader != null)
        {
             _sharedGlitchMaterial = new Material(glitchShader);
             _sharedGlitchMaterial.enableInstancing = true; 
        }

        // 2. 머티리얼 교체 및 텍스쳐 복사
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
                _propBlock.SetTexture(_BaseMapId, originalTex); // HDRP/URP 호환성
            }
            _propBlock.SetFloat(_NoiseSpeedId, glitchSpeed);
            _renderer.SetPropertyBlock(_propBlock);
        }

        // 3. 글리치 애니메이션 루프
        while (true)
        {
            if (_renderer != null)
            {
                float noise = Mathf.PerlinNoise(Time.time * 10f, transform.position.x); 
                float currentPower = glitchIntensity * (0.5f + noise * 0.5f);
                
                _renderer.GetPropertyBlock(_propBlock); 
                _propBlock.SetFloat(_GlitchPowerId, currentPower);

                // 색상 깜빡임 (Cyberpunk Cyan)
                if (noise > 0.8f) _propBlock.SetColor(_ColorId, Color.white); 
                else _propBlock.SetColor(_ColorId, Color.cyan);

                _renderer.SetPropertyBlock(_propBlock); 
            }
 
            yield return null;
        }
    }

    [Header("⚙️ MODE SETTINGS")]
    [SerializeField] private bool ignoreXAxis = true; // true: 2D 전용 (X축 무시), false: 3D 추적 (보스용)
    [SerializeField] private float turnSpeed3D = 5.0f; // 3D 모드 회전 속도

    private Vector3 _initialDirection; // [New] 초기 발사 방향
    private float _homingDelay = 0f;   // [New] 유도 시작 대기 시간

    public void Launch(Vector3 direction, float delay)
    {
        _initialDirection = direction.normalized;
        _homingDelay = delay;
        _isHoming = false; // 처음엔 유도 꺼둠
        _timer = 0f;

        // 초기 방향 설정
        transform.forward = _initialDirection;
    }

    public void Set3DHoming(bool enable)
    {
        ignoreXAxis = !enable; // enable=true면 ignore=false (3D 모드)
    }

    protected override void Update()
    {
        // [New] 정지 상태(얼음 등)면 아예 움직이지 않음
        if (_isFrozen) return;

        // [New] 유도 대기 시간 처리
        if (_homingDelay > 0f)
        {
            _homingDelay -= Time.deltaTime;
            
            // 대기 시간 중에는 초기 방향으로 직진
            transform.Translate(Vector3.forward * speed * Time.deltaTime); 
            return;
        }
        else 
        {
            // 대기 시간이 끝났다면 유도 활성화 (한 번만)
            if (!_isHoming && _timer == 0f) _isHoming = true;
        }

        if (_isHoming && _target != null)
        {
            Vector3 targetPos = _target.position;
            
            // [Option] 2D 모드일 때만 X축 평면 보정 (기존 로직)
            if (ignoreXAxis) 
            {
                targetPos.x = transform.position.x;
            }

            Vector3 directionToTarget = (targetPos - transform.position).normalized;

            if (ignoreXAxis)
            {
                // [Mode A] 2D PID 유도 (기존 로직 유지)
                Vector3 currentDirection = transform.forward;
                float angleError = Vector3.Angle(currentDirection, directionToTarget);

                // 시간 흐름에 따른 추적 각도 제한
                float t = _timer / homingDuration;
                float currentLimitAngle = Mathf.Lerp(maxHomingAngle, minHomingAngle, t * t);

                if (angleError > currentLimitAngle)
                {
                    _isHoming = false; // 각도 너무 벌어지면 포기
                }
                else
                {
                    Vector3 cross = Vector3.Cross(currentDirection, directionToTarget);
                    float directionSign = Mathf.Sign(cross.x);
                    float signedError = angleError * directionSign;
                    if (angleError < 1f) signedError = 0f;

                    float rotationAmount = _pidController.GetOutput(signedError, Time.deltaTime, kp, ki, kd);
                    transform.Rotate(Vector3.right, rotationAmount * Time.deltaTime, Space.World);
                }
            }
            else
            {
                // [Mode B] 3D 유도 (보스용, 단순 회전)
                // 3D 공간에서는 PID보다 Quaternion.Slerp가 훨씬 안정적임
                Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed3D * Time.deltaTime);
                
                // 3D 모드는 각도 제한 없이 끝까지 쫓아감 (보스니까 무섭게!)
            }

            // 타이머 체크 & 종료 처리
            _timer += Time.deltaTime;
            if (_timer >= homingDuration)
            {
                // 종료 시 마지막 정렬 (2D/3D 공통)
                if (Vector3.Angle(transform.forward, directionToTarget) <= minHomingAngle)
                {
                    if (ignoreXAxis)
                    {
                        // 2D 마무리
                        Vector3 finalTargetPos = _target.position;
                        finalTargetPos.x = transform.position.x;
                         Vector3 finalDir = (finalTargetPos - transform.position).normalized;
                         transform.forward = finalDir;
                    }
                    else
                    {
                        // 3D 마무리
                        transform.LookAt(targetPos);
                    }
                }
                _isHoming = false;
            }
        }

        // [Fix] 회전 보정 (2D 모드일 때만 강제 정렬)
        if (ignoreXAxis)
        {
            Vector3 currentEuler = transform.eulerAngles;
            transform.rotation = Quaternion.Euler(currentEuler.x, currentEuler.y, 0f);
        }

        // 전진
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    private bool _isHacked = false; // [New] 해킹 여부 플래그

    // [New] 미사일 해킹: 역추적 (Reverse Homing)
    public void HackReverse(Transform newTarget)
    {
        // [New] 중복 해킹 방지 및 즉시 기동
        _isFrozen = false; 
        
        _isHacked = true;
        _target = newTarget;
        _isHoming = true;
        _timer = 0f;

        // [Safe] 태그/레이어 변경 시도 (없으면 기본값 유지)
        int pProjLayer = LayerMask.NameToLayer("PlayerProjectile");
        if (pProjLayer != -1) gameObject.layer = pProjLayer;
        else gameObject.layer = LayerMask.NameToLayer("Default"); // PlayerProjectile 없으면 Default로

        // 태그는 굳이 안 바꿔도 내부 로직(_isHacked)으로 처리 가능하므로 생략!
        // (에러 로그 방지)

        // 2. 3D 추적 활성화 (보스 맞추러 가야 하니까)
        Set3DHoming(true); 

        // 3. 속도 & 데미지 증가 (카운터 펀치!)
        speed *= 1.5f; 
        damage *= 5; // 보스한테 아프게!

        // 4. PID 리셋 (새 타겟 적응)
        _pidController.Reset();

        // 5. 시각 효과 (빨강 -> 초록)
        if (_propBlock != null && _renderer != null)
        {
             _propBlock.SetColor(_ColorId, Color.green); // 해킹 성공 색상
             _renderer.SetPropertyBlock(_propBlock);
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

            // 적(Enemy)이나 보스(Boss) 공격
            if (other.CompareTag("Enemy") || other.CompareTag("Boss") || other.GetComponent<BaseEnemy>() != null)
            {
                 // 보스 체력 깎기
                 if (other.TryGetComponent(out BossHealth bossHealth))
                 {
                     bossHealth.TakeDamage(damage);
                 }
                 // 일반 적 (BaseEnemy에 TakeDamage가 있다고 가정하고 호출)
                 else if (other.TryGetComponent(out BaseEnemy baseEnemy))
                 {
                     baseEnemy.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
                 }

                 // 폭발 이펙트 (해킹용)
                 if (Core.VFXManager.Instance != null)
                 {
                     Core.VFXManager.Instance.PlayHackExplosion(transform.position);
                 }

                 Destroy(gameObject);
                 return;
            }
            return; // 해킹 상태에선 그 외 충돌(벽 등)은 일단 무시하거나 로직 추가 가능
        }

        // --- 아래는 일반(적군) 상태일 때 로직 ---
        
        // 적군끼리는 충돌 무시 (보스 몸에서 나올 때 바로 터지면 안 되니까)
        if (other.CompareTag("Enemy") || other.CompareTag("Boss")) return;

        base.OnTriggerEnter(other);
    }
}
