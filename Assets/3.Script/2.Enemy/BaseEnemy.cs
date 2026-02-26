using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BaseEnemy : MonoBehaviour
{
    [Header("🎯 Enemy Settings")]
    [SerializeField] private float hookInteractSpeed = 30f;
    [SerializeField] private float freezeDuration = 5f;

    private Rigidbody _rb;

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
        _propBlock = new MaterialPropertyBlock(); // [Fix] 자폭 깜빡임 등에서도 쓸 수 있게 미리 할당
        _startPos = transform.position;
        _startRot = transform.rotation;
    }

    public void SetStartTransform(Vector3 pos, Quaternion rot)
    {
        _startPos = pos;
        _startRot = rot;
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

    private Transform _playerTransform;

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
        else if (canKamikaze && !_isOverloaded && !_isDestroyed)
        {
            if (_playerTransform == null)
            {
                PlayerHealth ph = FindAnyObjectByType<PlayerHealth>();
                if (ph != null) _playerTransform = ph.transform;
            }

            if (_playerTransform != null)
            {
                if ((transform.position - _playerTransform.position).sqrMagnitude <= kamikazeTriggerRadius * kamikazeTriggerRadius)
                {
                    StartCoroutine(KamikazeRoutine());
                }
            }
        }
    }

    public void ResetEnemy()
    {
        StopAllCoroutines();
        
        transform.position = _startPos;
        transform.rotation = _startRot;
        
        IsFrozen = false;
        _isDestroyed = false;
        _isOverloaded = false; // [Fix] 과부하 상태 초기화
        gameObject.SetActive(true);
        gameObject.tag = _originalTag;
        
        _rb.isKinematic = false;
        _rb.useGravity = true; // [Fix] 카미카제 때 껐던 중력 복구
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
    private static int _baseColorId = Shader.PropertyToID("_BaseColor"); // [New] URP 대응용
    
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

        if (Core.VFXManager.Instance != null)
        {
            Core.VFXManager.Instance.PlayHackExplosion(transform.position);
        }

        _isDestroyed = true;
        OnDeath?.Invoke(this); // 알림 발송
        
        gameObject.SetActive(false);
        
        if (_renderer != null && _originalMaterial != null)
        {
            _renderer.sharedMaterial = _originalMaterial;
        }
    }

    [Header("💣 Kamikaze Settings")]
    [SerializeField] private bool canKamikaze = false; // 카미카제(자폭) 가능 여부
    [SerializeField] private float kamikazeTriggerRadius = 10.0f;
    [SerializeField] private float overloadDelay = 1.0f; // 훅 당겨진 후 대기 시간
    [SerializeField] private float kamikazeDuration = 3.0f; // 추적 시간
    [SerializeField] private float kamikazeSpeed = 8.0f;
    [SerializeField] private float explosionRadius = 3.0f;
    [SerializeField] private int explosionDamage = 1;

    public bool IsDestroyed => _isDestroyed; // [Fix] 외부 접근 허용

    private bool _isOverloaded = false;
    public bool IsOverloaded => _isOverloaded;

    public void OnHooked()
    {
        // 이미 얼었거나 죽었거나 과부하 상태면 무시
        if (IsFrozen || _isDestroyed || _isOverloaded) return;

        // 설정된 적만 훅에 당겨지면 자폭 시퀀스 가동
        if (canKamikaze)
        {
            StartCoroutine(KamikazeRoutine());
        }
    }

    private IEnumerator KamikazeRoutine()
    {
        _isOverloaded = true;
        
        // 1. 대기 (당겨진 직후 멍 때리기)
        yield return new WaitForSeconds(overloadDelay);

        if (IsFrozen || _isDestroyed) yield break;

        // 2. 카운트다운 & 추적 시작
        if (_patrol != null) _patrol.SetPatrol(false);
        if (_rb != null) 
        {
            _rb.isKinematic = false;
            _rb.useGravity = false; // 공중 부양 추적
        }

        float timer = kamikazeDuration;
        Transform playerTr = Core.GameManager.Instance != null ? 
                             FindAnyObjectByType<PlayerHealth>()?.transform : null;
        
        PlayerMovement pm = playerTr != null ? playerTr.GetComponent<PlayerMovement>() : null;

        while (timer > 0)
        {
            if (IsFrozen || _isDestroyed) yield break; // 얼면 자폭 취소

            // 플레이어 추적
            if (playerTr != null && _rb != null)
            {
                Vector3 dir = (playerTr.position - transform.position).normalized;
                _rb.linearVelocity = dir * kamikazeSpeed; // MovePosition 대신 Velocity로 부드럽게
            }

            // 깜빡거림 (경고)
            if (_renderer != null && _propBlock != null)
            {
                float flash = Mathf.PingPong(Time.time * 10f, 1f);
                Color blinkColor = Color.Lerp(Color.red, Color.yellow, flash);
                
                // [Fix] URP Lit 매테리얼 대응 (URP는 _BaseColor, 기본은 _Color 사용)
                _propBlock.SetColor(_colorId, blinkColor);
                _propBlock.SetColor(_baseColorId, blinkColor);
                
                _renderer.SetPropertyBlock(_propBlock);
            }

            // 거리 체크 (닿으면 폭발)
            if (playerTr != null && (transform.position - playerTr.position).sqrMagnitude < 1.0f)
            {
                // [Fix] 플레이어가 대시 중일 때는 폭발하지 않고 통과(관통) 대기!
                if (pm != null && pm.IsDashing)
                {
                    // 아무것도 안 함 (다음 프레임에 대시 충돌 박스가 얼려주길 기다림)
                }
                else
                {
                    Explode();
                    yield break;
                }
            }

            timer -= Time.deltaTime;
            yield return null;
        }

        // 3. 시간 종료 시 자폭
        Explode();
    }

    private void Explode()
    {
        if (_isDestroyed) return;
        _isDestroyed = true;
        
        // [Fix] 자폭도 죽은 것으로 간주 (BattleZone 카운트)
        OnDeath?.Invoke(this);

        // 폭발 이펙트 (중앙 관리)
        if (Core.VFXManager.Instance != null)
        {
            Core.VFXManager.Instance.PlayKamikazeExplosion(transform.position);
        }

        // 범위 데미지
        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                if (hit.TryGetComponent(out PlayerHealth ph))
                {
                    ph.TakeDamage(explosionDamage);
                }
            }
        }

        // 자폭 완료 (비활성화 - 리스폰을 위해 Destroy 하지 않음)
        gameObject.SetActive(false);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (canKamikaze)
        {
            // Trigger Radius 표시 (빨간색 투명)
            Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, kamikazeTriggerRadius);
            
            // Explosion Radius 표시 (주황색 투명)
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }
    }
#endif
}
