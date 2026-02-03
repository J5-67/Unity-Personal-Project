using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("Scene Names")]
        [SerializeField] private string gameSceneName = "1.GameTest";

        [Header("Menu Groups")]
        [SerializeField] private GameObject mainMenuGroup; // MainMenu 오브젝트
        [SerializeField] private GameObject subMenuGroup;  // SubMenu 오브젝트

        [Header("Sub Menus")]
        [SerializeField] private GameObject startSubMenu;    // Start (New Game/Continue)
        [SerializeField] private GameObject settingsSubMenu; // Settings (Graphic/Mouse/Sound)

        // [유니] Play 버튼 클릭 (메인 메뉴 -> 서브 메뉴 Start)
        public void OnClickPlay()
        {
            // [유니] 메인은 끄고? 아니면 켜둔 상태에서 옆에 띄우나?
            // 일단 서브 메뉴 그룹을 켜고, Start 메뉴를 활성화!
            if (subMenuGroup) subMenuGroup.SetActive(true);
            
            ActivateSubMenu(startSubMenu);
        }

        // [유니] Settings 버튼 클릭
        public void OnClickSettings()
        {
            if (subMenuGroup) subMenuGroup.SetActive(true);

            ActivateSubMenu(settingsSubMenu);
        }

        // [유니] 서브 메뉴 교체 헬퍼
        private void ActivateSubMenu(GameObject targetMenu)
        {
            if (startSubMenu) startSubMenu.SetActive(false);
            if (settingsSubMenu) settingsSubMenu.SetActive(false);

            if (targetMenu) targetMenu.SetActive(true);
        }

        // [유니] New Game 버튼 (Start 서브 메뉴 내부)
        public void OnClickNewGame()
        {
            // [유니] 이제 비동기 로딩 씬을 거쳐서 게임 시작! 🚀
            Core.SceneLoader.LoadScene(gameSceneName);
        }

        // [유니] Continue 버튼 (나중 구현)
        public void OnClickContinue()
        {
            Debug.Log("[유니] 이어하기 기능은 아직 준비 중이야! 🚧");
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
