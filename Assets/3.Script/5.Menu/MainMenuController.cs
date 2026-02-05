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
                 Debug.LogWarning("[MainMenu] DataManager가 없어서 이어할 수 없어! 😱 (빈 오브젝트에 DataManager 컴포넌트 추가해줘!)");
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
