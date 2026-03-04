using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core
{
    public class Portal : MonoBehaviour
    {
        [Header("🚀 Portal Settings")]
        [SerializeField] private string nextSceneName = "Name_Of_Scene";

        [SerializeField] private string playerTag = "Player";

        private bool _isActivated = false;

        private void OnTriggerEnter(Collider other)
        {
            if (_isActivated) return;

            if (other.CompareTag(playerTag))
            {
                _isActivated = true;
                MoveToNextScene();
            }
        }

        private void MoveToNextScene()
        {
            if (string.IsNullOrEmpty(nextSceneName))
            {

                _isActivated = false;
                return;
            }

            if (Core.Data.DataManager.Instance != null)
            {
                Core.Data.DataManager.Instance.SaveProgress(nextSceneName);
            }

            SceneLoader.LoadScene(nextSceneName);
        }
    }
}
