using UnityEngine;
using UnityEngine.SceneManagement;
namespace UI
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("Scene Names")]
        [SerializeField] private string gameSceneName = "1.GameTest";
        [Header("Menu Groups")]
        [SerializeField] private GameObject mainMenuGroup;
        [SerializeField] private GameObject subMenuGroup;
        [Header("Sub Menus")]
        [SerializeField] private GameObject startSubMenu;
        [SerializeField] private GameObject settingsSubMenu;
        [Header("Continue Button Settings")]
        [SerializeField] private UnityEngine.UI.Button continueButton;
        private void Start()
        {
            SetContinueButtonState();
        }
        private void SetContinueButtonState()
        {
            if (continueButton == null) return;
            bool canContinue = false;
            if (Core.Data.DataManager.Instance != null)
            {
                canContinue = Core.Data.DataManager.Instance.HasSaveData;
            }
            continueButton.interactable = canContinue;
            if (continueButton.TryGetComponent(out CanvasGroup group))
            {
                group.alpha = canContinue ? 1.0f : 0.4f;
            }
        }
        public void OnClickPlay()
        {
            if (subMenuGroup) subMenuGroup.SetActive(true);
            ActivateSubMenu(startSubMenu);
        }
        public void OnClickSettings()
        {
            if (subMenuGroup) subMenuGroup.SetActive(true);
            ActivateSubMenu(settingsSubMenu);
        }
        private void ActivateSubMenu(GameObject targetMenu)
        {
            if (startSubMenu) startSubMenu.SetActive(false);
            if (settingsSubMenu) settingsSubMenu.SetActive(false);
            if (targetMenu) targetMenu.SetActive(true);
        }
        public void OnClickNewGame()
        {
            Core.SceneLoader.LoadScene(gameSceneName);
        }
        public void OnClickContinue()
        {
            if (Core.Data.DataManager.Instance != null)
            {
                string sceneName = Core.Data.DataManager.Instance.CurrentData.currentStageSceneName;
                Core.SceneLoader.LoadScene(sceneName);
            }
            else
            {
            }
        }
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
