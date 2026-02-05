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
                Debug.Log("[DataManager] Created new save file.");
                SaveGame();
            }
        }

        public void SaveGame()
        {
            SaveSystem.Save(CurrentData);
        }

        public void SaveProgress(string sceneName)
        {
            CurrentData.currentStageSceneName = sceneName;
            SaveGame();
            Debug.Log($"[DataManager] Progress saved: {sceneName}");
        }
    }
}
