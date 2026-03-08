using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BaseEnemy))]
public class EnemyShooter : MonoBehaviour
{
    [Header("🎯 Combat Settings")]
    [SerializeField] private float detectRange = 15f;
    [SerializeField] private float attackRange = 12f;
    [SerializeField] private float fireRate = 2.0f;
    [SerializeField] private float aimDuration = 1.0f;

    [Header("🔫 Weapon")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private EnemyProjectile projectilePrefab;
    [SerializeField] private LineRenderer aimLaser;

    [Header("🔊 Audio")]
    [SerializeField] private AudioClip aimSound;
    [SerializeField] private AudioClip fireSound;

    private BaseEnemy _baseEnemy;
    private EnemyPatrol _patrol;
    private Transform _playerTr;
    private float _nextFireTime;
    private bool _isAiming = false;
    public bool IsAiming => _isAiming;

    private Material _aimMaterial; // 🎯 URP 색상 지원용! 🥰

    private UnityEngine.Pool.ObjectPool<EnemyProjectile> _projectilePool;

    private void Awake()
    {
        _baseEnemy = GetComponent<BaseEnemy>();
        _patrol = GetComponent<EnemyPatrol>();

        if (aimLaser != null)
        {
            aimLaser.positionCount = 2;
            aimLaser.enabled = false;
            
            // 🎯 URP에서 색깔이 잘 나오게 기본 머티리얼을 세팅할게 오빠!
            _aimMaterial = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            if (_aimMaterial.shader == null) _aimMaterial.shader = Shader.Find("Universal Render Pipeline/Unlit");
            aimLaser.material = _aimMaterial;
        }

        _projectilePool = new UnityEngine.Pool.ObjectPool<EnemyProjectile>(
            createFunc: () => {
                var proj = Instantiate(projectilePrefab);

                proj.OnReleaseToPool = (p) => _projectilePool.Release(p);
                return proj;
            },
            actionOnGet: (proj) => proj.gameObject.SetActive(true),
            actionOnRelease: (proj) => {
                proj.gameObject.SetActive(false);

                proj.transform.position = transform.position;
            },
            actionOnDestroy: (proj) => Destroy(proj.gameObject),
            defaultCapacity: 10,
            maxSize: 30
        );
    }

    private void Start()
    {
        if (Core.GameManager.Instance != null && FindAnyObjectByType<PlayerHealth>() != null)
        {
            _playerTr = FindAnyObjectByType<PlayerHealth>().transform;
        }

        _nextFireTime = Time.time + Random.Range(0f, 2.0f);
    }

    private void OnDisable()
    {
        _isAiming = false;
        if (aimLaser != null) aimLaser.enabled = false;
    }

    private void OnEnable()
    {
        _isAiming = false;
        _nextFireTime = Time.time + Random.Range(0f, 2.0f);
    }

    private void Update()
    {

        if (_baseEnemy.IsFrozen || _baseEnemy.IsDestroyed || _baseEnemy.IsOverloaded)
        {
            if (aimLaser != null) aimLaser.enabled = false;
            return;
        }

        if (_isAiming) return;

        if (_playerTr == null)
        {
            PlayerHealth ph = FindAnyObjectByType<PlayerHealth>();
            if (ph != null) _playerTr = ph.transform;
            else return;
        }

        float sqrDist = (transform.position - _playerTr.position).sqrMagnitude;

        if (sqrDist <= detectRange * detectRange && Time.time >= _nextFireTime)
        {

            StartCoroutine(AttackRoutine());
        }
    }

    private IEnumerator AttackRoutine()
    {
        _isAiming = true;

        if (aimSound != null && Core.AudioManager.Instance != null)
        {
            Core.AudioManager.Instance.PlaySFX(aimSound);
        }

        if (_patrol != null) _patrol.SetPatrol(false);

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
        }

        float timer = 0f;
        if (aimLaser != null)
        {
            aimLaser.enabled = true;
            aimLaser.positionCount = 2;
        }

        while (timer < aimDuration)
        {
            if (_baseEnemy.IsFrozen || _baseEnemy.IsDestroyed || _baseEnemy.IsOverloaded)
            {

                StopAttack();
                yield break;
            }

            Vector3 targetDir = _playerTr.position - transform.position;
            targetDir.y = 0;

            if (targetDir != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(targetDir);
                transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, Time.deltaTime * 10f);
            }

            if (aimLaser != null)
            {
                aimLaser.SetPosition(0, searchFirePoint().position);
                aimLaser.SetPosition(1, _playerTr.position);

                // 🎯 레이저 슈터랑 똑같은 쫀득한 빨-노 깜빡임 수식이야 오빠! 🥰
                float progress = timer / aimDuration;
                float blinkSpeed = Mathf.Lerp(8f, 40f, Mathf.Pow(progress, 2f)); 
                bool isRed = Mathf.Sin(Time.time * blinkSpeed * 2f * Mathf.PI) > 0f;
                Color blinkColor = isRed ? Color.red : Color.yellow;

                aimLaser.enabled = true; // 깜빡일 때 꺼지는 게 아니라 색이 변하게!
                aimLaser.startColor = blinkColor;
                aimLaser.endColor = blinkColor;
                
                if (_aimMaterial != null) _aimMaterial.SetColor("_BaseColor", blinkColor);
            }

            timer += Time.deltaTime;
            yield return null;
        }

        Transform spawnPoint = searchFirePoint();
        if (spawnPoint != null && projectilePrefab != null)
        {
            if (fireSound != null && Core.AudioManager.Instance != null)
            {
                Core.AudioManager.Instance.PlaySFX(fireSound);
            }

            Vector3 dir = (_playerTr.position - spawnPoint.position).normalized;
            Quaternion rot = Quaternion.LookRotation(dir);

            EnemyProjectile proj = _projectilePool.Get();
            proj.transform.position = spawnPoint.position;
            proj.transform.rotation = rot;

            if (proj is EnemyMissile missile)
            {
                missile.SetOwner(transform);
            }

            Collider[] myCols = GetComponentsInChildren<Collider>();
            Collider[] projCols = proj.GetComponentsInChildren<Collider>();

            foreach (var pCol in projCols)
            {
                foreach (var col in myCols)
                {
                    Physics.IgnoreCollision(col, pCol);
                }
            }
        }

        if (aimLaser != null) aimLaser.enabled = false;
        yield return new WaitForSeconds(0.5f);

        StopAttack();
        _nextFireTime = Time.time + fireRate;
    }

    public void CancelAttack()
    {
        if (_isAiming)
        {
            StopAllCoroutines();
            StopAttack();
        }
    }

    private void StopAttack()
    {
        _isAiming = false;
        if (aimLaser != null) aimLaser.enabled = false;

        if (_patrol != null && !_baseEnemy.IsFrozen && !_baseEnemy.IsDestroyed && !_baseEnemy.IsOverloaded)
        {
            _patrol.SetPatrol(true);
        }
    }

    private Transform searchFirePoint()
    {
        if (firePoint != null) return firePoint;
        return transform;
    }
}
