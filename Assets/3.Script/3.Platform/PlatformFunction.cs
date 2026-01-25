using UnityEngine;

public class PlatformFunction : MonoBehaviour
{
    public Collider platformCollider;

    private void Awake()
    {
        if (!TryGetComponent(out platformCollider)) Debug.Log(gameObject.name);
    }
}