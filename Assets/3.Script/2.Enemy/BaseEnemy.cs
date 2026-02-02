using System.Collections;
using UnityEngine;

// [유니] 적의 타입을 구분하기 위한 열거형이야!
public enum EnemyType
{
    Light, // [유니] 플레이어에게 끌려오는 가벼운 적
    Heavy  // [유니] 플레이어가 날아가는 묵직한 적
}

[RequireComponent(typeof(Rigidbody))]
public class BaseEnemy : MonoBehaviour
{
    [Header("🎯 Enemy Settings")]
    [SerializeField] private EnemyType enemyType = EnemyType.Light; // [유니] 인스펙터에서 골라줘!
    
    [Tooltip("Light: 당겨오는 속도 / Heavy: 플레이어가 날아가는 가속도")]

    [SerializeField] private float hookInteractSpeed = 30f; // [유니] 적마다 다른 힘을 설정할 수 있어!
    [SerializeField] private float freezeDuration = 5f;     // [유니] 얼어있는 시간 (끝나면 파괴됨!)

    private Rigidbody _rb;

    // [유니] 외부에서 타입을 확인할 수 있게 프로퍼티로 만들었어!
    public EnemyType Type => enemyType;
    public float HookInteractSpeed => hookInteractSpeed;
    public bool IsFrozen { get; private set; } // [유니] 얼음 상태 체크!

    // [유니] 원래 태그와 색깔 저장용
    private string _originalTag;
    private Color _originalColor;
    private Renderer _renderer;
    private EnemyPatrol _patrol;

    // [유니] 초기 상태 저장용 변수들
    private Vector3 _startPos;
    private Quaternion _startRot;
    private bool _isDestroyed = false; // "죽은 척" 상태 체크

    private void Awake()
    {
        // [유니] 물리 연산을 위해 Rigidbody는 필수!
        if (!TryGetComponent(out _rb))
        {
            _rb = gameObject.AddComponent<Rigidbody>();
        }

        // [유니] 2.5D 게임이니까 옆으로 쓰러지거나 뒤로 밀리지 않게 고정해줄게!
        _rb.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezeRotation;

        _renderer = GetComponentInChildren<Renderer>();
        _patrol = GetComponent<EnemyPatrol>();

        // [유니] 초기 상태 저장 (부활을 위해!)
        _originalTag = gameObject.tag;
        _originalColor = _renderer.material.color; // material.color로 가져옴
        _startPos = transform.position;
        _startRot = transform.rotation;
    }

    private void Start()
    {
        // [유니] 게임 매니저의 부활 이벤트 구독!
        // OnEnable/Disable이 아니라 Start/OnDestroy에서 구독해야 꺼져있는 애들도 신호를 받을 수 있어!
        if (Core.GameManager.Instance != null)
        {
            Core.GameManager.Instance.OnPlayerRespawn += ResetEnemy;
        }
    }

    private void OnDestroy()
    {
        // [유니] 진짜로 파괴될 때 구독 해제! (씬 이동 등)
        if (Core.GameManager.Instance != null)
        {
            Core.GameManager.Instance.OnPlayerRespawn -= ResetEnemy;
        }
    }

    // [유니] 플레이어 부활 시 적 상태 리셋!
    private void ResetEnemy()
    {
        StopAllCoroutines(); // 진행 중인 얼음 땡/글리치 멈춤!
        
        // 위치 및 회전 복구
        transform.position = _startPos;
        transform.rotation = _startRot;
        
        // 상태 복구
        IsFrozen = false;
        _isDestroyed = false;
        gameObject.SetActive(true);
        gameObject.tag = _originalTag;
        
        // 물리 복구
        _rb.isKinematic = false;
        _rb.linearVelocity = Vector3.zero;

        // 렌더러/쉐이더 복구
        if (_renderer != null)
        {
            // 글리치 매테리얼 대신 원래 매테리얼로 복구하는 로직이 필요해.
            // 하지만 지금은 sharedMaterial 방식이라, 
            // 가장 쉬운 건 얼리기 전의 그 상태(프로퍼티 블록 제거)로 돌리는 거야.
            _renderer.SetPropertyBlock(null);
            
            // 만약 매테리얼 자체를 바꿨다면 다시 복구해줘야 해.
            // (이건 Awake에서 originalMaterial을 저장해두는 식으로 개선 가능)
        }

        if (_patrol != null) _patrol.SetPatrol(true); // 순찰 다시 시작!
    }

    public void Freeze()
    {
        if (IsFrozen || _isDestroyed) return; // 이미 얼었거나 죽었으면 무시
        StartCoroutine(FreezeRoutine());
    }

    [Header("⚡ Glitch Effect")]
    [SerializeField] private Shader glitchShader;           
    [SerializeField] private float glitchIntensity = 0.5f;  
    [SerializeField] private float glitchSpeed = 20f;       

    // [유니] 최적화를 위한 공유 자원 (Static)
    private static Material _sharedGlitchMaterial;
    private static int _mainTexId = Shader.PropertyToID("_MainTex");
    private static int _baseMapId = Shader.PropertyToID("_BaseMap");
    private static int _glitchPowerId = Shader.PropertyToID("_GlitchPower");
    private static int _noiseSpeedId = Shader.PropertyToID("_NoiseSpeed");
    private static int _colorId = Shader.PropertyToID("_Color"); 
    
    private MaterialPropertyBlock _propBlock;
    
    // [유니] 원래 매테리얼 복구를 위한 변수 추가
    private Material _originalMaterial; 

    private IEnumerator FreezeRoutine()
    {
        IsFrozen = true;
        
        // 1. 초기 설정: 공유 재질 생성 (최초 1회만!)
        if (_sharedGlitchMaterial == null && glitchShader != null)
        {
             _sharedGlitchMaterial = new Material(glitchShader);
             _sharedGlitchMaterial.enableInstancing = true; 
        }

        if (_renderer != null && _sharedGlitchMaterial != null)
        {
            // 원래 텍스처 및 매테리얼 백업
            if (_originalMaterial == null) _originalMaterial = _renderer.sharedMaterial;
            
            Texture originalTex = null;
            if (_originalMaterial.HasProperty(_mainTexId)) originalTex = _originalMaterial.GetTexture(_mainTexId);
            else if (_originalMaterial.HasProperty(_baseMapId)) originalTex = _originalMaterial.GetTexture(_baseMapId);

            // [유니] 글리치 매테리얼 적용
            _renderer.sharedMaterial = _sharedGlitchMaterial;
            
            // 프로퍼티 블록 준비
            if (_propBlock == null) _propBlock = new MaterialPropertyBlock();

            // 텍스처 및 초기값 설정
            if (originalTex != null) _propBlock.SetTexture(_mainTexId, originalTex);
            _propBlock.SetFloat(_noiseSpeedId, glitchSpeed);
            _renderer.SetPropertyBlock(_propBlock);
        }

        try { gameObject.tag = "FrozenEnemy"; } catch {}
        if (_patrol != null) _patrol.SetPatrol(false);
        if (_rb != null) _rb.isKinematic = true; 

        // 2. 글리치 루프 (쉐이더 프로퍼티 조절)
        float timer = 0f;
        while (timer < freezeDuration)
        {
            if (_renderer != null)
            {
                // [유니] 시간이 지날수록 더 심하게 깨지거나, 불규칙하게 튀게 만듦
                float noise = Mathf.PerlinNoise(Time.time * 10f, transform.position.x); 
                float currentPower = glitchIntensity * (0.5f + noise * 0.5f);
                
                // 블록 값 업데이트
                _renderer.GetPropertyBlock(_propBlock); 
                _propBlock.SetFloat(_glitchPowerId, currentPower);

                // 색상도 가끔 빨강/시안으로 틴트 조절
                if (noise > 0.8f) _propBlock.SetColor(_colorId, Color.white); 
                else _propBlock.SetColor(_colorId, Color.cyan);

                _renderer.SetPropertyBlock(_propBlock); 
            }
 
            timer += Time.deltaTime;
            yield return null;
        }

        // 4. 파괴 대신 비활성화 (부활을 위해!)
        _isDestroyed = true;
        gameObject.SetActive(false);
        
        // [유니] 쉐이더 상태 복구는 ResetEnemy에서!
        if (_renderer != null && _originalMaterial != null)
        {
            _renderer.sharedMaterial = _originalMaterial;
        }
    }

    public void OnHooked()
    {

    }
}
