using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    [SerializeField] protected float speed = 20f;
    [SerializeField] protected int damage = 1;
    [SerializeField] private float lifeTime = 5f;
    [SerializeField] private GameObject hitVFX;

    private void Start()
    {
        Destroy(gameObject, lifeTime);
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

    protected void HitAndDestroy()
    {
        if (hitVFX != null)
        {
            Instantiate(hitVFX, transform.position, Quaternion.identity);
        }
        Destroy(gameObject);
    }
}
