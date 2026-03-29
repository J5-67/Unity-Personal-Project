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
        public bool HasSaveData => SaveSystem.HasSaveFile();
        private void Initialize()
        {
            CurrentData = SaveSystem.Load();
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
            CurrentData.hasSavedPosition = false;
            SaveGame();
        }
    }
}
