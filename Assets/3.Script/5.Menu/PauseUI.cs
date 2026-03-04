using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI
{
    public class PauseUI : MonoBehaviour
    {
        [Header("Menu Groups")]
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject settingsPanel;

        private void Start()
        {
            if (Core.GameManager.Instance != null)
            {
                Core.GameManager.Instance.SetPauseUI(this);
            }

            Hide();
        }

        public void Show()
        {
            if (pausePanel) pausePanel.SetActive(true);
            if (settingsPanel) settingsPanel.SetActive(false);
        }

        public void Hide()
        {
            if (pausePanel) pausePanel.SetActive(false);
            if (settingsPanel) settingsPanel.SetActive(false);
        }

        public void OnClickResume()
        {
            if (Core.GameManager.Instance != null)
            {
                Core.GameManager.Instance.TogglePause();
            }
            else
            {
                Hide();
                Time.timeScale = 1f;
            }
        }

        public void OnClickSettings()
        {
            if (settingsPanel)
            {
                settingsPanel.SetActive(true);
            }
        }

        public void OnClickMainMenu()
        {
            Time.timeScale = 1f;

            SceneManager.LoadScene("0.MenuTest");
        }

        public void OnClickCloseSettings()
        {
            if (settingsPanel) settingsPanel.SetActive(false);
            if (pausePanel) pausePanel.SetActive(true);
        }
    }
}
