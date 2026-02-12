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

    private BaseEnemy _baseEnemy;
    private EnemyPatrol _patrol;
    private Transform _playerTr;
    private float _nextFireTime;
    private bool _isAiming = false;

    private void Awake()
    {
        _baseEnemy = GetComponent<BaseEnemy>();
        _patrol = GetComponent<EnemyPatrol>();
        
        if (aimLaser != null) 
        {
            aimLaser.positionCount = 2;
            aimLaser.enabled = false;
        }
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
        
        float dist = Vector3.Distance(transform.position, _playerTr.position);
        
        if (dist <= detectRange && Time.time >= _nextFireTime)
        {
            // 공격 시작 루틴
            StartCoroutine(AttackRoutine());
        }
    }

    private IEnumerator AttackRoutine()
    {
        _isAiming = true;
        
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
        float blinkPhase = 0f;
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

            // 플레이어 바라보기
            Vector3 dir = (_playerTr.position - transform.position).normalized;
            // 2D/3D 회전 처리
            transform.forward = Vector3.Lerp(transform.forward, dir, Time.deltaTime * 10f); // 부드럽게 회전

            // 레이저 깜빡임 (초반 50% 고정 -> 후반 가속 깜빡임)
            if (aimLaser != null)
            {
                float progress = timer / aimDuration;
                bool isVisible = true;

                if (progress > 0.5f)
                {
                    float blinkSpeed = Mathf.Lerp(5f, 30f, (progress - 0.5f) * 2f);
                    blinkPhase += Time.deltaTime * blinkSpeed;
                    isVisible = (blinkPhase % 1f) < 0.5f;
                }

                aimLaser.enabled = isVisible;

                if (isVisible)
                {
                    aimLaser.SetPosition(0, searchFirePoint().position); 
                    aimLaser.SetPosition(1, _playerTr.position);         
                }
            }

            timer += Time.deltaTime;
            yield return null;
        }

        // 3. 발사!
        Transform spawnPoint = searchFirePoint();
        if (spawnPoint != null && projectilePrefab != null)
        {
            Vector3 dir = (_playerTr.position - spawnPoint.position).normalized;
            Quaternion rot = Quaternion.LookRotation(dir);
            EnemyProjectile proj = Instantiate(projectilePrefab, spawnPoint.position, rot);
            
            // [Fix] 자기가 쏜 총알에 자기 몸(Body)이나 방패(Shield)가 맞는 문제 해결
            // 자식들(방패 포함)의 모든 콜라이더와 충돌 무시
            Collider[] myCols = GetComponentsInChildren<Collider>();
            Collider projCol = proj.GetComponent<Collider>();
            
            if (projCol != null)
            {
                foreach (var col in myCols)
                {
                    Physics.IgnoreCollision(col, projCol);
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
