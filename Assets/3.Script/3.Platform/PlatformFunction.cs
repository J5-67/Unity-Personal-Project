using UnityEngine;

public class PlatformFunction : MonoBehaviour
{
    public Collider platformCollider;

    private void Awake()
    {
        if (!TryGetComponent(out platformCollider)) { }
    }

    private void OnCollisionEnter(Collision collision)
    {

    }

    private void OnCollisionExit(Collision collision)
    {
    }
}