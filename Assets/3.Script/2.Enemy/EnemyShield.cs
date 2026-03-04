using UnityEngine;

public class EnemyShield : MonoBehaviour
{
    [SerializeField] private float bounceForce = 20f;
    [SerializeField] private float blockDuration = 0.5f;

    public float BounceForce => bounceForce;

    public void OnBlock(Vector3 hitPoint)
    {

    }

    public void BreakShield()
    {

        if (Core.VFXManager.Instance != null)
        {
            Core.VFXManager.Instance.PlayKamikazeExplosion(transform.position);
        }

        gameObject.SetActive(false);
    }
}
