using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core // GameManager와 같은 네임스페이스 사용 (편의상)
{
    public class Portal : MonoBehaviour
    {
        [Header("🚀 Portal Settings")]
        [Tooltip("이동할 씬의 정확한 이름을 적어주세요! (Build Settings에 등록 필수)")]
        [SerializeField] private string nextSceneName = "Name_Of_Scene";

        [Tooltip("플레이어 태그 (기본값: Player)")]
        [SerializeField] private string playerTag = "Player";

        private bool _isActivated = false; // 중복 발동 방지

        private void OnTriggerEnter(Collider other)
        {
            // [유니] 이미 발동했거나, 플레이어가 아니면 무시!
            if (_isActivated) return;
            
            // [유니] 태그 비교할 때 CompareTag가 더 가볍고 안전해!
            if (other.CompareTag(playerTag))
            {
                _isActivated = true;
                MoveToNextScene();
            }
        }

        private void MoveToNextScene()
        {
            // [유니] 씬 이름이 비어있으면 안 되니까 경고!
            if (string.IsNullOrEmpty(nextSceneName))
            {
                Debug.LogError("[Portal] 🚨 이동할 씬 이름이 비어있어! 인스펙터를 확인해줘!");
                _isActivated = false; // 다시 시도할 수 있게 풀어줌
                return;
            }

            Debug.Log($"[Portal] ✨ {nextSceneName} 씬으로 이동할게! 슝~");

            // [추후 확장] 여기에 페이드 아웃 효과나 사운드 재생을 넣으면 좋아!
            // 예: GameManager.Instance.LoadScene(nextSceneName); 로 변경 가능
            
            // 지금은 바로 이동!
            SceneManager.LoadScene(nextSceneName);
        }
    }
}
