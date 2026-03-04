using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    [SerializeField] protected float speed = 20f;
    [SerializeField] protected int damage = 1;
    [SerializeField] private float lifeTime = 5f;
    [SerializeField] private GameObject hitVFX;

    public System.Action<EnemyProjectile> OnReleaseToPool;

    protected virtual void OnEnable()
    {

        Invoke(nameof(HitAndDestroy), lifeTime);
        if (Core.GameManager.Instance != null)
        {
            Core.GameManager.Instance.OnPlayerRespawn += ForceRelease;
        }
    }

    protected virtual void OnDisable()
    {
        CancelInvoke(nameof(HitAndDestroy));
        if (Core.GameManager.Instance != null)
        {
            Core.GameManager.Instance.OnPlayerRespawn -= ForceRelease;
        }
    }

    private void ForceRelease()
    {
        if (gameObject.activeInHierarchy)
        {
            if (OnReleaseToPool != null) OnReleaseToPool.Invoke(this);
            else Destroy(gameObject);
        }
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
        else if (other.CompareTag("Untagged") || other.CompareTag("Wall"))
        {

            if (other.GetComponentInParent<BaseEnemy>() != null) return;

            HitAndDestroy();
        }
    }

    protected virtual void HitAndDestroy()
    {

        if (!gameObject.activeInHierarchy) return;

        if (hitVFX != null)
        {

            Instantiate(hitVFX, transform.position, Quaternion.identity);
        }

        if (OnReleaseToPool != null) OnReleaseToPool.Invoke(this);
        else Destroy(gameObject);
    }
}
