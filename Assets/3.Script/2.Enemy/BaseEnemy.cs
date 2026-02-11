using System.Collections;
using UnityEngine;

public enum EnemyType
{
    Light,
    Heavy
}

[RequireComponent(typeof(Rigidbody))]
public class BaseEnemy : MonoBehaviour
{
    [Header("🎯 Enemy Settings")]
    [SerializeField] private EnemyType enemyType = EnemyType.Light;
    
    [SerializeField] private float hookInteractSpeed = 30f;
    [SerializeField] private float freezeDuration = 5f;

    private Rigidbody _rb;

    public EnemyType Type => enemyType;
    public float HookInteractSpeed => hookInteractSpeed;
    public bool IsFrozen { get; private set; }

    private string _originalTag;
    private Color _originalColor;
    private Renderer _renderer;
    private EnemyPatrol _patrol;

    private Vector3 _startPos;
    private Quaternion _startRot;
    private bool _isDestroyed = false;

    private void Awake()
    {
        if (!TryGetComponent(out _rb))
        {
            _rb = gameObject.AddComponent<Rigidbody>();
        }

        _rb.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezeRotation;

        _renderer = GetComponentInChildren<Renderer>();
        _patrol = GetComponent<EnemyPatrol>();

        _originalTag = gameObject.tag;
        _originalColor = _renderer.material.color;
        _originalMaterial = _renderer.sharedMaterial;
        _startPos = transform.position;
        _startRot = transform.rotation;
    }

    private void Start()
    {
        if (Core.GameManager.Instance != null)
        {
            Core.GameManager.Instance.OnPlayerRespawn += ResetEnemy;
        }
    }

    private void OnDestroy()
    {
        if (Core.GameManager.Instance != null)
        {
            Core.GameManager.Instance.OnPlayerRespawn -= ResetEnemy;
        }
    }

    private void FixedUpdate()
    {
        // [Safety Net] 얼음 상태일 때는 물리적으로 절대 움직이지 못하도록 강제함 (이중 잠금)
        if (IsFrozen)
        {
            if (_rb != null)
            {
                if (!_rb.isKinematic) _rb.isKinematic = true;
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
            }
        }
    }

    private void ResetEnemy()
    {
        StopAllCoroutines();
        
        transform.position = _startPos;
        transform.rotation = _startRot;
        
        IsFrozen = false;
        _isDestroyed = false;
        gameObject.SetActive(true);
        gameObject.tag = _originalTag;
        
        _rb.isKinematic = false;
        _rb.linearVelocity = Vector3.zero;

        if (_renderer != null)
        {
            _renderer.SetPropertyBlock(null);
            
            if (_originalMaterial != null)
            {
                _renderer.sharedMaterial = _originalMaterial;
            }
        }

        if (_patrol != null) _patrol.ResetPatrol();
    }

    public void Freeze()
    {
        if (IsFrozen || _isDestroyed) return;
        StartCoroutine(FreezeRoutine());
    }

    [Header("⚡ Glitch Effect")]
    [SerializeField] private Shader glitchShader;           
    [SerializeField] private float glitchIntensity = 0.5f;  
    [SerializeField] private float glitchSpeed = 20f;       

    private static Material _sharedGlitchMaterial;
    private static int _mainTexId = Shader.PropertyToID("_MainTex");
    private static int _baseMapId = Shader.PropertyToID("_BaseMap");
    private static int _glitchPowerId = Shader.PropertyToID("_GlitchPower");
    private static int _noiseSpeedId = Shader.PropertyToID("_NoiseSpeed");
    private static int _colorId = Shader.PropertyToID("_Color"); 
    
    private MaterialPropertyBlock _propBlock;
    
    private Material _originalMaterial; 

    private IEnumerator FreezeRoutine()
    {
        IsFrozen = true;
        
        if (_sharedGlitchMaterial == null && glitchShader != null)
        {
             _sharedGlitchMaterial = new Material(glitchShader);
             _sharedGlitchMaterial.enableInstancing = true; 
        }

        if (_renderer != null && _sharedGlitchMaterial != null)
        {
            Texture originalTex = null;
            if (_originalMaterial.HasProperty(_mainTexId)) originalTex = _originalMaterial.GetTexture(_mainTexId);
            else if (_originalMaterial.HasProperty(_baseMapId)) originalTex = _originalMaterial.GetTexture(_baseMapId);

            _renderer.sharedMaterial = _sharedGlitchMaterial;
            
            if (_propBlock == null) _propBlock = new MaterialPropertyBlock();

            if (originalTex != null) _propBlock.SetTexture(_mainTexId, originalTex);
            _propBlock.SetFloat(_noiseSpeedId, glitchSpeed);
            _renderer.SetPropertyBlock(_propBlock);
        }

        try { gameObject.tag = "FrozenEnemy"; } catch {}
        if (_patrol != null) _patrol.SetPatrol(false);
        
        if (_rb != null)
        { 
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _rb.isKinematic = true; 
        } 

        while (true)
        {
            if (_renderer != null)
            {
                float noise = Mathf.PerlinNoise(Time.time * 10f, transform.position.x); 
                float currentPower = glitchIntensity * (0.5f + noise * 0.5f);
                
                _renderer.GetPropertyBlock(_propBlock); 
                _propBlock.SetFloat(_glitchPowerId, currentPower);

                if (noise > 0.8f) _propBlock.SetColor(_colorId, Color.white); 
                else _propBlock.SetColor(_colorId, Color.cyan);

                _renderer.SetPropertyBlock(_propBlock); 
            }
 
            yield return null;
        }
    }

    public event System.Action<BaseEnemy> OnDeath;

    public void OnHack()
    {
        if (!IsFrozen || _isDestroyed) return;

        if (VFX.HackVFXManager.Instance != null)
        {
            VFX.HackVFXManager.Instance.PlayHackEffect(transform.position);
        }

        _isDestroyed = true;
        OnDeath?.Invoke(this); // 알림 발송
        
        gameObject.SetActive(false);
        
        if (_renderer != null && _originalMaterial != null)
        {
            _renderer.sharedMaterial = _originalMaterial;
        }
    }

    public void OnHooked()
    {

    }
}
