using UnityEngine;

namespace Core.Data
{
    public class DataManager : MonoBehaviour
    {
        public static DataManager Instance { get; private set; }

        public GameData CurrentData { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject); // 씬 이동해도 데이터 유지!
                Initialize();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Initialize()
        {
            bool hadFile = SaveSystem.HasSaveFile();

            // 게임 시작하면 바로 로드!
            CurrentData = SaveSystem.Load();
            
            // [유니] 파일이 아예 없었다면, 바로 저장해서 파일을 눈에 보이게 만들어주자! 👀
            if (!hadFile)
            {
                Debug.Log("[DataManager] 첫 시작이네! 세이브 파일 생성! 📝");
                SaveGame();
            }
        }

        // [유니] 저장 함수 (외부에서 호출)
        public void SaveGame()
        {
            SaveSystem.Save(CurrentData);
        }

        // [유니] 진행 상황 저장 (씬 이름으로!)
        public void SaveProgress(string sceneName)
        {
            CurrentData.currentStageSceneName = sceneName;
            SaveGame();
            Debug.Log($"[DataManager] 진행 상황 저장 완료! 다음 시작 씬: {sceneName} 💾");
        }
    }
}
