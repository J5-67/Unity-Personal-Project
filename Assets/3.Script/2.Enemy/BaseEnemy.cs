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
        _originalTag = gameObject.tag;
    }

    public void Freeze()
    {
        if (IsFrozen) return; // 이미 얼었으면 무시
        StartCoroutine(FreezeRoutine());
    }

    [Header("⚡ Glitch Effect")]
    [SerializeField] private Shader glitchShader;           // [NEW] GlitchURP.shader 연결!
    [SerializeField] private float glitchIntensity = 0.5f;  // 쉐이더 파워
    [SerializeField] private float glitchSpeed = 20f;       // 노이즈 속도

    // [유니] 최적화를 위한 공유 자원 (Static)
    private static Material _sharedGlitchMaterial;
    private static int _mainTexId = Shader.PropertyToID("_MainTex");
    private static int _baseMapId = Shader.PropertyToID("_BaseMap");
    private static int _glitchPowerId = Shader.PropertyToID("_GlitchPower");
    private static int _noiseSpeedId = Shader.PropertyToID("_NoiseSpeed");
    private static int _colorId = Shader.PropertyToID("_Color"); // 혹은 _BaseColor

    // 개별 프로퍼티 블록 (메모리 할당 없이 값만 변경!)
    private MaterialPropertyBlock _propBlock;

    private IEnumerator FreezeRoutine()
    {
        IsFrozen = true;
        
        // 1. 초기 설정: 공유 재질 생성 (최초 1회만!)
        if (_sharedGlitchMaterial == null && glitchShader != null)
        {
             _sharedGlitchMaterial = new Material(glitchShader);
             _sharedGlitchMaterial.enableInstancing = true; // [유니] 배칭을 위해 켜두면 좋아!
        }

        if (_renderer != null && _sharedGlitchMaterial != null)
        {
            // 원래 텍스처 가져오기
            Texture originalTex = null;
            Material originalMat = _renderer.sharedMaterial; // [유니] sharedMaterial로 가져와야 함!

            if (originalMat.HasProperty(_mainTexId)) originalTex = originalMat.GetTexture(_mainTexId);
            else if (originalMat.HasProperty(_baseMapId)) originalTex = originalMat.GetTexture(_baseMapId);

            // [유니] 새 재질 생성 없이, 공유 재질을 덮어씌움!
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
                float noise = Mathf.PerlinNoise(Time.time * 10f, transform.position.x); // 위치값 섞어서 적마다 다르게!
                float currentPower = glitchIntensity * (0.5f + noise * 0.5f);
                
                // 블록 값 업데이트
                _renderer.GetPropertyBlock(_propBlock); // 현재 상태 가져오기
                _propBlock.SetFloat(_glitchPowerId, currentPower);

                // 색상도 가끔 빨강/시안으로 틴트 조절
                if (noise > 0.8f) _propBlock.SetColor(_colorId, Color.white); // 번쩍!
                else _propBlock.SetColor(_colorId, Color.cyan);

                _renderer.SetPropertyBlock(_propBlock); // 적용!
            }
 
            timer += Time.deltaTime;
            yield return null;
        }

        // 4. 파괴
        Destroy(gameObject);
    }

    // [유니] 나중에 여기에 데미지를 입거나 기절하는 로직을 넣으면 딱이겠지?
    public void OnHooked()
    {

    }
}
