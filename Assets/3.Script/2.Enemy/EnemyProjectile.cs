using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    [SerializeField] protected float speed = 20f;
    [SerializeField] protected int damage = 1;
    [SerializeField] private float lifeTime = 5f;
    [SerializeField] private GameObject hitVFX;

    // [New] 매번 파괴(Destroy)하지 않고 웅덩이(Pool)로 돌아가기 위한 연락줄(Delegate)
    public System.Action<EnemyProjectile> OnReleaseToPool;

    protected virtual void OnEnable()
    {
        // Start 대신 OnEnable에서 타이머 작동시켜 재활용 시에도 5초 뒤 소멸하도록 수정
        Invoke(nameof(HitAndDestroy), lifeTime);
    }

    protected virtual void OnDisable()
    {
        CancelInvoke(nameof(HitAndDestroy));
    }

    protected virtual void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (other.TryGetComponent(out PlayerHealth health))
            {
                health.TakeDamage(damage);
            }
            HitAndDestroy();
        }
        else if (other.CompareTag("Untagged") || other.CompareTag("Wall")) // Ground 태그 제거 (에러 방지)
        {
            // [Fix] 혹시라도 적 방패(Shield)나 몸통 일부가 Untagged로 되어있어서 총알이 터지는 문제 방지
            if (other.GetComponentInParent<BaseEnemy>() != null) return;

            HitAndDestroy();
        }
    }

    protected virtual void HitAndDestroy()
    {
        if (hitVFX != null)
        {
            // [Todo] 폭발 이펙트도 나중에 파티클 풀링(VFXManager)으로 빼면 완벽! 
            Instantiate(hitVFX, transform.position, Quaternion.identity);
        }
        
        // [Fix] 파괴(Garbage) 금지! 재활용 바구니(Pool)로 돌려보냄!
        if (OnReleaseToPool != null) OnReleaseToPool.Invoke(this);
        else Destroy(gameObject);
    }
}
