using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BaseEnemy))]
public class EnemyShooter : MonoBehaviour
{
    [Header("🎯 Combat Settings")]
    [SerializeField] private float detectRange = 15f;
    [SerializeField] private float attackRange = 12f;
    [SerializeField] private float fireRate = 2.0f;     // 공격 주기
    [SerializeField] private float aimDuration = 1.0f;  // 조준 시간 (레이저)
    
    [Header("🔫 Weapon")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private EnemyProjectile projectilePrefab;
    [SerializeField] private LineRenderer aimLaser;     // 조준선

    [Header("🔊 Audio")]
    [SerializeField] private AudioClip aimSound;        // 조준 중 나는 소리 (징~)
    [SerializeField] private AudioClip fireSound;       // 쏘는 소리 (삐융!)

    private BaseEnemy _baseEnemy;
    private EnemyPatrol _patrol;
    private Transform _playerTr;
    private float _nextFireTime;
    private bool _isAiming = false;
    public bool IsAiming => _isAiming; // [New] 레이더 탐지용 프로퍼티

    // [New] 유니티 내장 오브젝트 풀! Instantiate/Destroy의 렉 원흉 제거!
    private UnityEngine.Pool.ObjectPool<EnemyProjectile> _projectilePool;

    private void Awake()
    {
        _baseEnemy = GetComponent<BaseEnemy>();
        _patrol = GetComponent<EnemyPatrol>();
        
        if (aimLaser != null) 
        {
            aimLaser.positionCount = 2;
            aimLaser.enabled = false;
        }

        // [Fix] 풀링 초기화 세팅 (미리 10개 만들어두고 재사용)
        _projectilePool = new UnityEngine.Pool.ObjectPool<EnemyProjectile>(
            createFunc: () => {
                var proj = Instantiate(projectilePrefab);
                // 총알이 자기가 죽을 때 내 풀로 돌아오게 연락줄(Delegate) 달아주기!
                proj.OnReleaseToPool = (p) => _projectilePool.Release(p);
                return proj;
            },
            actionOnGet: (proj) => proj.gameObject.SetActive(true),
            actionOnRelease: (proj) => {
                proj.gameObject.SetActive(false);
                // [Fix] 비활성화될 때 혹시 모를 잔상 방지를 위해 대기 위치로 이동
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
        
        // [Fix] 모든 적이 동시에 공격하는 것 방지 (랜덤 딜레이)
        _nextFireTime = Time.time + Random.Range(0f, 2.0f);
    }

    private void Update()
    {
        // 1. 상태 체크 (얼음, 죽음, 과부하)
        if (_baseEnemy.IsFrozen || _baseEnemy.IsDestroyed || _baseEnemy.IsOverloaded)
        {
            if (aimLaser != null) aimLaser.enabled = false;
            return;
        }

        // 이미 조준 중이면 아무것도 안 함 (코루틴이 알아서 함)
        if (_isAiming) return;

        // 2. 플레이어 탐지
        if (_playerTr == null) return;
        
        // [Fix] 매 프레임 도는 무거운 Distance 검사를 sqrMagnitude로 초고속 최적화! 🚀
        float sqrDist = (transform.position - _playerTr.position).sqrMagnitude;
        
        if (sqrDist <= detectRange * detectRange && Time.time >= _nextFireTime)
        {
            // 공격 시작 루틴
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

        // 1. 순찰 멈춤 (조준 집중)
        if (_patrol != null) _patrol.SetPatrol(false);
        
        // [Fix] 순찰 멈췄다고 물리(중력)가 켜져서 추락하는 문제 해결
        // 조준 중에는 공중에 고정(Kinematic) 상태 유지
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) 
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
        }

        // 2. 조준 (레이저 표시 & 회전)
        float timer = 0f;
        if (aimLaser != null) 
        {
            aimLaser.enabled = true;
            aimLaser.positionCount = 2; // 확실하게 2점으로 초기화
        }

        while (timer < aimDuration)
        {
            if (_baseEnemy.IsFrozen || _baseEnemy.IsDestroyed || _baseEnemy.IsOverloaded)
            {
                // 공격 취소 (얼거나 훅 당함)
                StopAttack();
                yield break;
            }

            // 플레이어 바라보기 (Y축 회전만 허용하여 덤블링 방지)
            Vector3 targetDir = _playerTr.position - transform.position;
            targetDir.y = 0; // 높이 차 무시 (수평 회전만)
            
            if (targetDir != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(targetDir);
                transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, Time.deltaTime * 10f);
            }

            // 레이저 갱신
            if (aimLaser != null)
            {
                aimLaser.SetPosition(0, searchFirePoint().position); 
                aimLaser.SetPosition(1, _playerTr.position);         

                // [Fix] 조준 시간에 비례한 레이저 깜빡임 효과 복구
                float progress = timer / aimDuration;
                
                // 처음엔 천천히 깜빡이다가 발사 직전에 엄청 빠르게 깜빡임
                // Mathf.Pow를 써서 진행될수록 주파수가 기하급수적으로 증가하게 함
                // [Fix] 더 극단적이고 날카로운 깜빡임을 위해 속도 범위 상향 및 Pow 수치 조절
                float blinkSpeed = Mathf.Lerp(10f, 80f, Mathf.Pow(progress, 3f));
                
                // [Fix] 부드러운 깜빡임(Sin 스무딩)을 버리고 0과 1로 극단적으로 팍팍 끊기게 (Flicker)
                float alpha = Mathf.Sin(Time.time * blinkSpeed) > 0f ? 1f : 0.05f;

                // 머티리얼 알파값 변경 (URP LineRenderer 대응)
                Color c1 = aimLaser.startColor;
                Color c2 = aimLaser.endColor;
                c1.a = alpha;
                c2.a = alpha;
                aimLaser.startColor = c1;
                aimLaser.endColor = c2;
            }

            timer += Time.deltaTime;
            yield return null;
        }

        // 3. 발사!
        Transform spawnPoint = searchFirePoint();
        if (spawnPoint != null && projectilePrefab != null)
        {
            if (fireSound != null && Core.AudioManager.Instance != null)
            {
                Core.AudioManager.Instance.PlaySFX(fireSound);
            }

            Vector3 dir = (_playerTr.position - spawnPoint.position).normalized;
            Quaternion rot = Quaternion.LookRotation(dir);
            
            // [Fix] 무심코 쓰던 Instantiate 대신 풀에서 꺼내오기! (렉 프레임 드랍 0%)
            EnemyProjectile proj = _projectilePool.Get();
            proj.transform.position = spawnPoint.position;
            proj.transform.rotation = rot;

            // [New] 만약 이게 영리한 '유도 미사일'이라면, 발사한 주인이 누구인지 명찰을 달아줌!
            // 그래야 나중에 오빠가 미사일을 해킹했을 때, "날 쏜 주인을 죽여라!!" 하고 완벽히 역추적함 😈
            if (proj is EnemyMissile missile)
            {
                missile.SetOwner(transform);
            }
            
            // [Fix] 자기가 쏜 총알에 자기 몸(Body)이나 방패(Shield)가 맞는 문제 해결
            // 자식들(방패 포함)의 모든 콜라이더와 총알(및 그 자식들)의 충돌 완벽 무시
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

        // 4. 후딜레이 (재장전)
        if (aimLaser != null) aimLaser.enabled = false;
        yield return new WaitForSeconds(0.5f);

        StopAttack();
        _nextFireTime = Time.time + fireRate;
    }

    private void StopAttack()
    {
        _isAiming = false;
        if (aimLaser != null) aimLaser.enabled = false;
        
        // [Fix] 얼어서 멈춘 건데 다시 순찰을 켜버리면 안 됨!
        // 얼지 않았을 때만 순찰 재개
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
