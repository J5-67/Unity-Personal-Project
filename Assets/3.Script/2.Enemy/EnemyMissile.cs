using UnityEngine;

// 일반 EnemyShooter를 상속받거나 그대로 쓰기엔 Homing 기능만 바꾸면 됨.
// EnemyMissile 프리팹을 EnemyShooter의 Projectile Prefab에 넣으면 해결!
// 하지만 미사일 폭격(여러 발)을 하고 싶다면 새로운 EnemyHeavy가 필요.
// 일단 유저가 "미사일 발사"를 요청했으니, EnemyMissile.cs 를 완성했으므로
// EnemyShooter를 그대로 사용하고 프리팹만 교체하면 됨!

// 여기는 EnemyMissile.cs의 내용을 완성하겠음.
// 아까 write_to_file에서 base.Update()를 불렀는데,
// 부모 moveSpeed가 private였음. (Step 904에서 protected로 수정함)
// 그러니 이제 안심하고 EnemyMissile.cs를 제대로 작성.

using UnityEngine;

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

    protected override void Update()
    {
        // [New] 정지 상태(얼음 등)면 아예 움직이지 않음
        if (_isFrozen) return;

        if (_isHoming && _target != null)
        {
            // [Fix] 횡스크롤(Z) 게임 평면 보정
            Vector3 targetPos = _target.position;
            targetPos.x = transform.position.x;

            Vector3 directionToTarget = (targetPos - transform.position).normalized;
            Vector3 currentDirection = transform.forward;

            // 1. 오차 계산 (각도 차이)
            float angleError = Vector3.Angle(currentDirection, directionToTarget);

            // [New] 시간 흐름에 따른 추적 각도 제한 (점점 좁혀짐)
            // 시간이 지날수록 허용 각도가 좁아짐 (Lerp)
            float t = _timer / homingDuration;
            float currentLimitAngle = Mathf.Lerp(maxHomingAngle, minHomingAngle, t * t); // t*t로 후반에 급격히 좁힘

            // 현재 오차가 허용 각도를 벗어나면 유도 포기 (직진 모드 전환)
            if (angleError > currentLimitAngle)
            {
                _isHoming = false;
            }
            else
            {
                // 유도 진행 (PID)
                Vector3 cross = Vector3.Cross(currentDirection, directionToTarget);
                float directionSign = Mathf.Sign(cross.x);

                float signedError = angleError * directionSign;

                if (angleError < 1f) signedError = 0f;

                float rotationAmount = _pidController.GetOutput(signedError, Time.deltaTime, kp, ki, kd);

                transform.Rotate(Vector3.right, rotationAmount * Time.deltaTime, Space.World);
            }

            // 타이머 체크 & 종료 처리
            _timer += Time.deltaTime;
            if (_timer >= homingDuration)
            {
                // [New] 시간이 다 됐는데, 아직도 플레이어가 내 시야각(Min Angle) 안에 있다면?
                // 마지막으로 타겟을 딱! 바라보고 직진하게 해줌 (정확도 UP)
                if (angleError <= minHomingAngle)
                {
                    // 1. 타겟 방향 바라보기 (X축 보정된 타겟)
                    transform.LookAt(targetPos);

                    // 2. 횡스크롤이므로 불필요한 회전(Y, Z축 회전) 제거 및 X축 회전만 유지?
                    // LookAt은 기본적으로 Y축을 Up으로 쓰므로 횡스크롤에선 X축이 비틀어질 수 있음.
                    // 위에서 이미 X축 회전만 하고 있었으므로, LookAt 대신 방향 벡터를 대입하는 게 안전.

                    /*
                    Quaternion finalRotation = Quaternion.LookRotation(directionToTarget);
                    // 오직 X축 회전만 반영 (나머지 0)
                    Vector3 euler = finalRotation.eulerAngles;
                    transform.rotation = Quaternion.Euler(euler.x, 0, 0);
                    */

                    // 하지만 이미 PID로 충분히 따라왔다면 굳이 강제 정렬 안 해도 됨.
                    // [Fix] 이상한 방향 버그 수정: 마지막 순간 방향 벡터 재계산
                    Vector3 finalTargetPos = _target.position;
                    finalTargetPos.x = transform.position.x; // 횡스크롤 보정
                    Vector3 finalDir = (finalTargetPos - transform.position).normalized;

                    // 현재 내 방향과 마지막 타겟 방향의 각도 차이 재계산
                    float finalAngleError = Vector3.Angle(transform.forward, finalDir);

                    if (finalAngleError <= minHomingAngle)
                    {
                        // 오빠가 원한 대로 "플레이어 방향으로 직진" (강제 정렬)
                        transform.forward = finalDir;
                    }

                }
                
                _isHoming = false; // 이제부턴 진짜 직진 (유도 끝)
            }
        }

        // [Fix] 회전 보정: 횡스크롤(YZ 평면)에서 X축(Pitch) 회전은 살리고,
        // Y축(Yaw)은 발사 방향(0 or 180) 유지, Z축(Roll) 회전만 제거.
        Vector3 currentEuler = transform.eulerAngles;
        transform.rotation = Quaternion.Euler(currentEuler.x, currentEuler.y, 0f);

        // 전진 (유도 여부와 상관없이 항상 전진)
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    protected override void OnTriggerEnter(Collider other)
    {
        // [New] 얼어있으면 폭발하지 않음 (플레이어 대시 통과 허용)
        if (_isFrozen) return;

        base.OnTriggerEnter(other);
    }
}
