using UnityEngine;

// 일반 EnemyShooter를 상속받거나 그대로 쓰기엔 Homing 기능만 바꾸면 됨.
// EnemyMissile 프리팹을 EnemyShooter의 Projectile Prefab에 넣으면 해결!
// 하지만 미사일 폭격(여러 발)을 하고 싶다면 새로운 EnemyHeavy가 필요.
// 일단 유저가 "미사일 발사"를 요청했으니, EnemyMissile.cs 를 완성했으므로
// EnemyShooter를 그대로 사용하고 프리팹만 교체하면 됨!

// 여기는 EnemyMissile.cs의 내용을 완성하겠음.
// 아까 write_to_file에서 base.Update()를 불렀는데,
// 부모 moveSpeed가 private였음. (Step 904에서 protected로 수정함)
// 그러니 이제 안심하고 EnemyMissile.cs를 제대로 작성.

public class EnemyMissile : EnemyProjectile
{
    [Header("🚀 Missile Settings")]
    [SerializeField] private float turnSpeed = 60f;       // 도는 속도
    [SerializeField] private float homingDuration = 2.0f; // 유도 시간
    
    private Transform _target;
    private float _timer;
    private bool _isHoming = true;
    
    private void Start()
    {
        // 타겟 찾기
        var p = FindAnyObjectByType<PlayerMovement>();
        if (p != null) _target = p.transform;
        
        // 부모의 Start() (LifeTime Destroy) 호출 여부는? 
        // 부모 Start가 private면 호출 안 됨. -> 부모 Start도 protected virtual로?
        // 아니면 그냥 여기서 Destroy 호출.
        Destroy(gameObject, 5f); 
    }

    protected override void Update()
    {
        // 유도 로직
        if (_isHoming && _target != null)
        {
            Vector3 direction = (_target.position - transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            
            // 부드럽게 회전
            transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRotation, turnSpeed * Time.deltaTime);

            _timer += Time.deltaTime;
            if (_timer >= homingDuration)
            {
                _isHoming = false; // 유도 끝, 직진 모드
            }
        }

        // 전진 (부모의 speed 변수는 protected라 접근 가능!)
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }
}
