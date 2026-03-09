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
                DontDestroyOnLoad(gameObject);
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

            CurrentData = SaveSystem.Load();

            if (!hadFile)
            {

                SaveGame();
            }
        }

        public void SaveGame()
        {
            SaveSystem.Save(CurrentData);
        }

        public void SaveProgress(string sceneName, Vector3 checkpointPos)
        {
            CurrentData.currentStageSceneName = sceneName;
            CurrentData.lastCheckpointPosition = checkpointPos;
            CurrentData.hasSavedPosition = true;
            SaveGame();
        }

        public void SaveProgress(string sceneName)
        {
            CurrentData.currentStageSceneName = sceneName;
            CurrentData.hasSavedPosition = false; // 🎯 새 씬으로 갈 때는 저장된 위치를 초기화해서 기본 스폰 지점을 쓰게 할게!
            SaveGame();
        }
    }
}
