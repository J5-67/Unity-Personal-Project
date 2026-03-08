using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BaseEnemy))]
public class EnemyLaserShooter : MonoBehaviour
{
    [Header("🎯 Combat Settings")]
    [SerializeField] private float detectRange = 15f;
    [SerializeField] private float fireRate = 3.0f;
    [SerializeField] private float aimDuration = 1.2f;
    [SerializeField] private float laserDuration = 0.5f;

    [Header("🔫 Laser Setup")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private LineRenderer laserRenderer; // 🎯 이거 하나로 조준/발사 다 해요 오빠! 🥰
    [SerializeField] private float laserThickness = 0.4f;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private LayerMask playerLayer;

    [Header("💥 Damage Settings")]
    [SerializeField] private int damage = 1;
    [SerializeField] private float knockbackForce = 15f;

    [Header("🔊 Audio")]
    [SerializeField] private AudioClip aimSound;
    [SerializeField] private AudioClip fireSound;

    [Header("✨ Laser Hazard Visuals")]
    [SerializeField] private Color laserCoreColor = new Color(1f, 0.9f, 0.9f, 1f);
    [SerializeField] private Color laserGlowColor = new Color(1f, 0.1f, 0.1f, 1f);
    [SerializeField] private float hdrIntensity = 5f;

    private BaseEnemy _baseEnemy;
    private EnemyPatrol _patrol;
    private Transform _playerTr;
    private float _nextFireTime;
    private bool _isAttacking = false;

    private RigidbodyConstraints _originalConstraints;
    private Material _laserMaterial;
    private Material _aimMaterial;

    private void Awake()
    {
        _baseEnemy = GetComponent<BaseEnemy>();
        _patrol = GetComponent<EnemyPatrol>();

        if (laserRenderer != null)
        {
            laserRenderer.enabled = false;
            // 조준용은 기본 Unlit으로 깔끔하게! 🥰
            _aimMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            SetupLaserMaterial(); 
        }

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) _originalConstraints = rb.constraints;
    }

    private void Start()
    {
        PlayerHealth ph = Object.FindFirstObjectByType<PlayerHealth>();
        if (ph != null) _playerTr = ph.transform;

        _nextFireTime = Time.time + Random.Range(1f, 2f);
    }

    private void Update()
    {
        if (_baseEnemy.IsFrozen || _baseEnemy.IsDestroyed || _baseEnemy.IsOverloaded || _isAttacking) 
        {
             return;
        }

        if (_playerTr == null) return;

        float sqrDist = (transform.position - _playerTr.position).sqrMagnitude;
        if (sqrDist <= detectRange * detectRange && Time.time >= _nextFireTime)
        {
            StartCoroutine(LaserAttackRoutine());
        }
    }

    private IEnumerator LaserAttackRoutine()
    {
        _isAttacking = true;

        if (_patrol != null) _patrol.enabled = false;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }

        // 1. 🎯 조준 단계 (하나의 렌더러로 조준 모드!)
        if (laserRenderer != null)
        {
            laserRenderer.enabled = true;
            laserRenderer.material = _aimMaterial;
            laserRenderer.useWorldSpace = true;
            laserRenderer.positionCount = 2;
        }

        if (aimSound != null && Core.AudioManager.Instance != null)
            Core.AudioManager.Instance.PlaySFX(aimSound);

        float timer = 0f;
        while (timer < aimDuration)
        {
            if (_baseEnemy.IsFrozen || _baseEnemy.IsDestroyed || _baseEnemy.IsOverloaded) 
            {
                StopAttack();
                yield break;
            }

            Vector3 targetPos = _playerTr.position;
            targetPos.x = transform.position.x;
            
            Vector3 targetDir = (targetPos - transform.position).normalized;
            if (targetDir != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(targetDir);
                transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, Time.deltaTime * 10f);
            }

            // 🎯 깜빡임 속도를 오빠 말대로 좀 더 부드럽게 낮췄어! 🥰 (Max 40Hz)
            float progress = timer / aimDuration;
            float blinkSpeed = Mathf.Lerp(8f, 40f, Mathf.Pow(progress, 2f)); 
            bool isRed = Mathf.Sin(Time.time * blinkSpeed * 2f * Mathf.PI) > 0f;
            
            if (laserRenderer != null)
            {
                Color blinkColor = isRed ? Color.red : Color.yellow;
                laserRenderer.startColor = blinkColor;
                laserRenderer.endColor = blinkColor;
                // 🎯 머티리얼 색상도 직접 잡아줘야 오빠 화면에서 빨간색 노란색이 보여! 🥰
                if (_aimMaterial != null) _aimMaterial.SetColor("_BaseColor", blinkColor);
            }

            UpdateLaserLines(0.1f); // 조준선은 얇게!
            
            timer += Time.deltaTime;
            yield return null;
        }

        // 2. 🔥 발사 단계 (같은 렌더러로 레이저 모드 변신!)
        if (laserRenderer != null)
        {
            laserRenderer.material = _laserMaterial;
            laserRenderer.startColor = Color.white;
            laserRenderer.endColor = Color.white;
        }

        if (fireSound != null && Core.AudioManager.Instance != null)
            Core.AudioManager.Instance.PlaySFX(fireSound);

        if (Core.CameraEffectManager.Instance != null)
            Core.CameraEffectManager.Instance.AddUnscaledShake(0.3f);

        float attackTimer = 0f;
        while (attackTimer < laserDuration)
        {
            if (_baseEnemy.IsFrozen || _baseEnemy.IsDestroyed) break;

            UpdateLaserLines(laserThickness);
            CheckLaserCollision(); 

            attackTimer += Time.deltaTime;
            yield return null;
        }

        StopAttack();
    }

    private void UpdateLaserLines(float thickness)
    {
        if (laserRenderer == null) return;
        laserRenderer.startWidth = thickness;
        laserRenderer.endWidth = thickness;

        float fixedX = transform.position.x;
        Vector3 start = firePoint != null ? firePoint.position : transform.position;
        start.x = fixedX;

        Vector3 rayStart = start + transform.forward * 0.1f; 
        Vector3 end = start + transform.forward * detectRange;
        end.x = fixedX;

        if (Physics.Raycast(rayStart, transform.forward, out RaycastHit hit, detectRange, obstacleLayer))
        {
            end = hit.point;
            end.x = fixedX;
        }

        laserRenderer.SetPosition(0, start);
        laserRenderer.SetPosition(1, end);
        
        Debug.DrawLine(start, end, Color.red);
    }

    private void CheckLaserCollision()
    {
        float fixedX = transform.position.x;
        Vector3 start = firePoint != null ? firePoint.position : transform.position;
        start.x = fixedX;

        Vector3 end = start + transform.forward * detectRange;
        end.x = fixedX;
        
        if (Physics.Raycast(start + transform.forward * 0.1f, transform.forward, out RaycastHit wallHit, detectRange, obstacleLayer))
        {
            end = wallHit.point;
            end.x = fixedX;
        }

        Collider[] hits = Physics.OverlapCapsule(start, end, laserThickness * 0.5f, playerLayer);
        foreach (var col in hits)
        {
            if (col.TryGetComponent(out PlayerHealth health))
            {
                if (!health.IsInvincible)
                {
                    health.TakeDamage(damage);
                    if (col.TryGetComponent(out PlayerMovement pm))
                    {
                        Vector3 pushDir = (col.transform.position - transform.position).normalized;
                        pm.ApplyKnockback(pushDir, knockbackForce);
                    }
                }
            }
        }
    }

    private void StopAttack()
    {
        _isAttacking = false;
        if (laserRenderer != null) laserRenderer.enabled = false;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.constraints = _originalConstraints;

        if (_patrol != null && !_baseEnemy.IsFrozen && !_baseEnemy.IsDestroyed)
        {
            _patrol.enabled = true;
            _patrol.SetPatrol(true);
        }

        _nextFireTime = Time.time + fireRate;
    }

    private void SetupLaserMaterial()
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBAHalf, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < size; y++)
        {
            float t = Mathf.Abs((y / (float)(size - 1)) * 2f - 1f); 
            Color pixelColor = Color.clear;

            if (t < 0.2f)
            {
                pixelColor = laserCoreColor;
                pixelColor.a = 1f;
            }
            else
            {
                float glowT = (t - 0.2f) / 0.8f;
                float alpha = Mathf.Pow(1f - glowT, 1.5f); 
                pixelColor = laserGlowColor;
                pixelColor.a = alpha;
            }

            pixelColor.r *= hdrIntensity;
            pixelColor.g *= hdrIntensity;
            pixelColor.b *= hdrIntensity;

            for (int x = 0; x < size; x++) tex.SetPixel(x, y, pixelColor);
        }
        tex.Apply();

        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");

        _laserMaterial = new Material(shader);
        if (_laserMaterial.HasProperty("_BaseMap")) _laserMaterial.SetTexture("_BaseMap", tex);
        if (_laserMaterial.HasProperty("_MainTex")) _laserMaterial.SetTexture("_MainTex", tex);
        
        _laserMaterial.SetFloat("_Surface", 1); 
        _laserMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        _laserMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        _laserMaterial.SetInt("_ZWrite", 0);
        _laserMaterial.renderQueue = 3000;
        
        if (laserRenderer != null) laserRenderer.textureMode = LineTextureMode.Stretch;
    }
}
