using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI
{
    public class PauseUI : MonoBehaviour
    {
        [Header("Menu Groups")]
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject settingsPanel; // 메인 메뉴 거 재사용 가능하면 좋고, 새로 만드셨으면 연결!

        private void Start()
        {
            // [유니] 시작할 때 GameManager에 나 자신을 등록! (GameManager가 DontDestroy라 캐싱 필요)
            if (Core.GameManager.Instance != null)
            {
                Core.GameManager.Instance.SetPauseUI(this);
            }
            
            // 처음엔 꺼두기
            Hide();
        }

        public void Show()
        {
            if (pausePanel) pausePanel.SetActive(true);
            if (settingsPanel) settingsPanel.SetActive(false); // 설정창은 닫힌 상태로
        }

        public void Hide()
        {
            if (pausePanel) pausePanel.SetActive(false);
            if (settingsPanel) settingsPanel.SetActive(false);
        }

        // [유니] Resume 버튼
        public void OnClickResume()
        {
            // GameManager를 통해 풀어야 TimeScale도 복구됨!
            if (Core.GameManager.Instance != null)
            {
                Core.GameManager.Instance.TogglePause();
            }
            else
            {
                // 비상용
                Hide();
                Time.timeScale = 1f;
            }
        }

        // [유니] Settings 버튼
        public void OnClickSettings()
        {
            if (settingsPanel) 
            {
                settingsPanel.SetActive(true);
                // pausePanel.SetActive(false); // 선택사항: 메뉴를 겹칠지 교체할지
            }
        }

        // [유니] Main Menu 버튼 (나가기)
        public void OnClickMainMenu()
        {
            // 시간은 다시 흐르게 해두고 나가야 함!
            Time.timeScale = 1f;
            
            // 메인 메뉴 씬 이름 (MainMenuController에 있던 거랑 맞춰주세요!)
            SceneManager.LoadScene("0.MenuTest"); // 혹은 0번 인덱스
        }
        
        // [유니] 설정창 닫기 (Back 버튼)
        public void OnClickCloseSettings()
        {
            if (settingsPanel) settingsPanel.SetActive(false);
            if (pausePanel) pausePanel.SetActive(true);
        }
    }
}
