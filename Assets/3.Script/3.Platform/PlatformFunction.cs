using UnityEngine;

public class PlatformFunction : MonoBehaviour
{
    public Collider platformCollider;

    private void Awake()
    {
        if (!TryGetComponent(out platformCollider)) Debug.Log(gameObject.name);
    }

    private void OnCollisionEnter(Collision collision)
    {
        // 🚨 SetParent 방식은 Rigidbody.velocity 강제 할당과 충돌하여 플레이어를 굳게 만듭니다! 
        // 따라서 PlayerMovement 스크립트에서 Tracking 방식으로 수정합니다!
    }

    private void OnCollisionExit(Collision collision)
    {
    }
}