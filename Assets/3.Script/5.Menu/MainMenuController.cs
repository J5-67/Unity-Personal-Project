using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("Scene Names")]
        [SerializeField] private string gameSceneName = "1.GameTest";

        [Header("Panels")]
        [SerializeField] private GameObject settingsPanel;

        private void Start()
        {
        }

        // [유니] 게임 시작 버튼 연결용
        public void OnClickStart()
        {
            SceneManager.LoadScene(gameSceneName);
        }

        // [유니] 설정 버튼
        public void OnClickSettings()
        {
            if (settingsPanel != null)
            {
                
            }
        }

        // [유니] 종료 버튼
        public void OnClickQuit()
        {
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }
    }
}
