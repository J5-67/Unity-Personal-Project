using UnityEngine;
using UnityEngine.VFX;

public class EnemyMissile : EnemyProjectile
{
    [Header("🚀 MISSILE PID SETTINGS")]
    [SerializeField] private float kp = 50f;
    [SerializeField] private float ki = 5f;
    [SerializeField] private float kd = 2f;

    [Header("🎯 HOMING SETTINGS")]
    [SerializeField] private float homingDuration = 3.0f;
    [SerializeField] private float maxHomingAngle = 120f;
    [SerializeField] private float minHomingAngle = 10f;

    private Transform _target;
    private Collider _targetCollider;
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

    private static Material _sharedGlitchMaterial;
    private static readonly int _MainTexId = Shader.PropertyToID("_MainTex");
    private static readonly int _BaseMapId = Shader.PropertyToID("_BaseMap");
    private static readonly int _GlitchPowerId = Shader.PropertyToID("_GlitchPower");
    private static readonly int _NoiseSpeedId = Shader.PropertyToID("_NoiseSpeed");
    private static readonly int _ColorId = Shader.PropertyToID("_Color");

    public bool IsFrozen => _isFrozen;

    private void Awake()
    {
        _pidController = new PID();
        _originalLayer = gameObject.layer;

        _renderer = GetComponentInChildren<Renderer>();
        if (_renderer != null) _originalMaterial = _renderer.sharedMaterial;
    }

    private float _originalSpeed = -1f;
    private int _originalDamage = -1;
    private float _originalTurnSpeed3D = -1f;
    private float _originalHomingDuration = -1f;
    private bool _originalIgnoreXAxis;

    protected override void OnEnable()
    {
        base.OnEnable();

        if (_originalSpeed < 0)
        {
            _originalSpeed = speed;
            _originalDamage = damage;
            _originalTurnSpeed3D = turnSpeed3D;
            _originalHomingDuration = homingDuration;
            _originalIgnoreXAxis = ignoreXAxis;
        }

        speed = _originalSpeed;
        damage = _originalDamage;
        turnSpeed3D = _originalTurnSpeed3D;
        homingDuration = _originalHomingDuration;
        ignoreXAxis = _originalIgnoreXAxis;
        gameObject.layer = _originalLayer;

        if (_renderer != null && _originalMaterial != null)
        {
            _renderer.sharedMaterial = _originalMaterial;
            _renderer.SetPropertyBlock(null);
        }

        _pidController.Reset();
        _isHoming = true;
        _isFrozen = false;
        _isHit = false;
        _isHacked = false;
        _timer = 0f;

        if (TryGetComponent(out Collider col)) col.enabled = true;
        if (_renderer != null) _renderer.enabled = true;

        if (_target == null)
        {

            GameObject p = GameObject.FindWithTag("Player");
            if (p != null)
            {
                _target = p.transform;
                _targetCollider = _target.GetComponent<Collider>();
            }
        }

    }

    protected override void OnDisable()
    {
        base.OnDisable();
        CancelInvoke(nameof(SelfDestroy));
        if (_isFrozen)
        {
            _isFrozen = false;
            gameObject.layer = _originalLayer;
        }
    }

    private void SelfDestroy()
    {

        if (!gameObject.activeInHierarchy) return;

        if (OnReleaseToPool != null) OnReleaseToPool.Invoke(this);
        else Destroy(gameObject);
    }

    private void Start() { }

    private Coroutine _autoHackCoroutine;
    [SerializeField] private float autoHackDelay = 1.5f;

    private Transform _owner;
    public void SetOwner(Transform owner)
    {
        _owner = owner;
    }

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

        if (_owner != null && _owner.gameObject.activeInHierarchy && !_owner.GetComponent<BaseEnemy>().IsDestroyed)
        {
            hackTarget = _owner;
        }
        else
        {

            Collider[] hits = Physics.OverlapSphere(transform.position, 30f);
            foreach (var hit in hits)
            {
                if (hit.TryGetComponent(out BossHealth boss) || (hit.transform.parent != null && hit.transform.parent.TryGetComponent(out boss)))
                {
                    hackTarget = boss.transform;
                    break;
                }
                else if (hackTarget == null)
                {
                    if (hit.TryGetComponent(out BaseEnemy enemy) || (hit.transform.parent != null && hit.transform.parent.TryGetComponent(out enemy)))
                    {
                        hackTarget = enemy.transform;
                    }
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
            SelfDestroy();
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

            if (_targetCollider != null) targetPos = _targetCollider.bounds.center;
            else targetPos.y += 1.0f;

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

        _targetCollider = _target.GetComponentInChildren<Collider>();

        _isHoming = true;
        _timer = 0f;

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

        turnSpeed3D *= 5f;
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

            if (TryGetComponent(out Collider col)) col.enabled = false;

            if (TryGetComponent(out Rigidbody rb))
            {
                rb.linearVelocity = Vector3.zero;
                rb.isKinematic = true;
            }

            if (_renderer != null) _renderer.enabled = false;

            CancelInvoke(nameof(SelfDestroy));
            Invoke(nameof(SelfDestroy), 2f);
        }
        else
        {
            base.HitAndDestroy();
        }
    }

    protected override void OnTriggerEnter(Collider other)
    {

        if (_isFrozen) return;

        if (_isHacked)
        {

            if (other.CompareTag("Player")) return;

            if (other.GetComponentInParent<EnemyProjectile>() != null) return;

            EnemyShield shield = other.GetComponentInParent<EnemyShield>();
            if (shield == null) shield = other.GetComponentInChildren<EnemyShield>();

            if (shield != null && shield.gameObject.activeInHierarchy)
            {

                shield.BreakShield();

                if (Core.VFXManager.Instance != null)
                {
                    Core.VFXManager.Instance.PlayHackExplosion(transform.position);
                }

                HitAndDestroy();
                return;
            }

            BaseEnemy baseEnemy = other.GetComponentInParent<BaseEnemy>();

            if (other.CompareTag("Enemy") || other.CompareTag("Boss") || baseEnemy != null)
            {

                 if (other.TryGetComponent(out BossHealth bossHealth))
                 {
                     bossHealth.TakeDamage(damage);
                 }
                 else if (baseEnemy != null)
                 {
                     baseEnemy.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
                 }

                 if (Core.VFXManager.Instance != null)
                 {
                     Core.VFXManager.Instance.PlayHackExplosion(transform.position);
                 }

                 HitAndDestroy();
                 return;
            }
            return;
        }

        if (other.CompareTag("Enemy") || other.CompareTag("Boss") || other.GetComponent<BaseEnemy>() != null) return;

        if (other.GetComponentInParent<BaseEnemy>() != null) return;

        if (other.GetComponentInParent<EnemyProjectile>() != null) return;

        if (other.CompareTag("Player"))
        {
            if (other.TryGetComponent(out PlayerHealth health))
            {
                health.TakeDamage(damage);
            }

            HitAndDestroy();
            return;
        }

        if (other.CompareTag("Untagged") || other.CompareTag("Wall"))
        {

            if (other.isTrigger) return;

            HitAndDestroy();
        }
    }
}
