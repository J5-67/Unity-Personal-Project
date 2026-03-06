using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BaseEnemy : MonoBehaviour
{
    [Header("🎯 Enemy Settings")]
    [SerializeField] private float freezeDuration = 5f;

    private Rigidbody _rb;

    public bool IsFrozen { get; private set; }

    private string _originalTag;
    private Color _originalColor;
    private Renderer _renderer;
    public Renderer EnemyRenderer => _renderer;
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
        _propBlock = new MaterialPropertyBlock();
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

    }

    private void OnDestroy()
    {

    }

    private Transform _playerTransform;

    private void FixedUpdate()
    {

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
        _isOverloaded = false;
        gameObject.SetActive(true);
        gameObject.tag = _originalTag;

        _rb.isKinematic = false;
        _rb.useGravity = true;
        _rb.linearVelocity = Vector3.zero;

        if (_renderer != null)
        {
            _renderer.SetPropertyBlock(null);

            if (_originalMaterial != null)
            {
                _renderer.sharedMaterial = _originalMaterial;
            }
        }

        EnemyShield[] shields = GetComponentsInChildren<EnemyShield>(true);
        foreach (var shield in shields)
        {
            shield.gameObject.SetActive(true);
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
    private static int _baseColorId = Shader.PropertyToID("_BaseColor");

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
        OnDeath?.Invoke(this);

        gameObject.SetActive(false);

        if (_renderer != null && _originalMaterial != null)
        {
            _renderer.sharedMaterial = _originalMaterial;
        }
    }

    [Header("💣 Kamikaze Settings")]
    [SerializeField] private bool canKamikaze = false;
    [SerializeField] private float kamikazeTriggerRadius = 10.0f;
    [SerializeField] private float overloadDelay = 1.0f;
    [SerializeField] private float kamikazeDuration = 3.0f;
    [SerializeField] private float kamikazeSpeed = 8.0f;
    [SerializeField] private float explosionRadius = 3.0f;
    [SerializeField] private int explosionDamage = 1;

    public bool IsDestroyed => _isDestroyed;

    private bool _isOverloaded = false;
    public bool IsOverloaded => _isOverloaded;

    public void OnHooked()
    {

        if (IsFrozen || _isDestroyed || _isOverloaded) return;

        if (TryGetComponent(out EnemyShooter shooter))
        {
            shooter.CancelAttack();
        }

        if (canKamikaze)
        {
            StartCoroutine(KamikazeRoutine());
        }
    }

    private IEnumerator KamikazeRoutine()
    {
        _isOverloaded = true;

        yield return new WaitForSeconds(overloadDelay);

        if (IsFrozen || _isDestroyed) yield break;

        if (_patrol != null) _patrol.SetPatrol(false);
        if (_rb != null)
        {
            _rb.isKinematic = false;
            _rb.useGravity = false;
        }

        float timer = kamikazeDuration;
        Transform playerTr = Core.GameManager.Instance != null ?
                             FindAnyObjectByType<PlayerHealth>()?.transform : null;

        PlayerMovement pm = playerTr != null ? playerTr.GetComponent<PlayerMovement>() : null;

        while (timer > 0)
        {
            if (IsFrozen || _isDestroyed) yield break;

            if (playerTr != null && _rb != null)
            {
                Vector3 dir = (playerTr.position - transform.position).normalized;
                _rb.linearVelocity = dir * kamikazeSpeed;
            }

            if (_renderer != null && _propBlock != null)
            {
                float flash = Mathf.PingPong(Time.time * 10f, 1f);
                Color blinkColor = Color.Lerp(Color.red, Color.yellow, flash);

                _propBlock.SetColor(_colorId, blinkColor);
                _propBlock.SetColor(_baseColorId, blinkColor);

                _renderer.SetPropertyBlock(_propBlock);
            }

            if (playerTr != null && (transform.position - playerTr.position).sqrMagnitude < 1.0f)
            {

                if (pm != null && pm.IsDashing)
                {

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

        Explode();
    }

    private void Explode()
    {
        if (_isDestroyed) return;
        _isDestroyed = true;

        OnDeath?.Invoke(this);

        if (Core.VFXManager.Instance != null)
        {
            Core.VFXManager.Instance.PlayKamikazeExplosion(transform.position);
        }

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

        gameObject.SetActive(false);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (canKamikaze)
        {

            Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, kamikazeTriggerRadius);

            Gizmos.color = new Color(1f, 0.5f, 0f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }
    }
#endif
}
