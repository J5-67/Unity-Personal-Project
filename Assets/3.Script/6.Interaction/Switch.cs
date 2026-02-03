using UnityEngine;

namespace Interaction
{
    public class Switch : MonoBehaviour, IInteractable
    {
        [Header("Connection")]
        [SerializeField] private Door targetDoor; // 연결된 문 (없을 수도 있음)
        
        [Header("Settings")]
        [SerializeField] private bool activateOnce = true; // 한 번만 작동하는지?
        [SerializeField] private bool isActivated = false;

        [Header("Visuals (Optional)")]
        [SerializeField] private MeshRenderer meshRenderer; // 3D 오브젝트용
        [SerializeField] private SpriteRenderer spriteRenderer; // 2D 스프라이트용
        [SerializeField] private Sprite onSprite;
        [SerializeField] private Sprite offSprite;
        [SerializeField] private Color onColor = Color.green;
        [SerializeField] private Color offColor = Color.red;

        private void Start()
        {
            UpdateVisual();
        }

        // [유니] IInteractable 인터페이스 구현! 
        // 훅이 때리든 총이 때리든 이 함수가 불릴 거야!
        public void OnInteract(GameObject instigator)
        {
            if (activateOnce && isActivated) return; // 이미 켜졌으면 무시

            isActivated = !isActivated; // 토글 (켜짐 <-> 꺼짐)
            
            // 만약 한 번만 작동하는 스위치라면 다시 못 끄게 고정!
            if (activateOnce) isActivated = true;

            // 1. 시각적 피드백
            UpdateVisual();

            // 2. 문 작동!
            if (targetDoor != null)
            {
                if (activateOnce) targetDoor.Open();
                else targetDoor.Toggle();
            }

            // [추후] 사운드 재생 or 파티클 효과 추가 가능
            Debug.Log($"[Switch] 찰칵! 상태: {isActivated}");
        }

        private void UpdateVisual()
        {
            Color targetColor = isActivated ? onColor : offColor;

            // 1. 3D 오브젝트 (MeshRenderer) 처리
            if (meshRenderer != null)
            {
                // [유니] 머티리얼 인스턴스를 만들어서 색깔 변경! (주의: 배치 배칭 끊길 수 있음)
                meshRenderer.material.color = targetColor;
            }

            // 2. 2D 스프라이트 (SpriteRenderer) 처리
            if (spriteRenderer != null)
            {
                if (onSprite != null && offSprite != null)
                {
                    spriteRenderer.sprite = isActivated ? onSprite : offSprite;
                }
                else
                {
                    // 스프라이트가 없으면 색깔로라도 티 내기!
                    spriteRenderer.color = targetColor;
                }
            }
        }
    }
}
