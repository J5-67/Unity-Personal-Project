using System.IO;
using UnityEngine;

namespace Core.Data
{
    public static class SaveSystem
    {
#if UNITY_EDITOR
        private static string SaveDirectory => Path.Combine(Application.dataPath, "../SaveData");
#else
        private static string SaveDirectory => Path.Combine(Application.dataPath, "../SaveData"); 
#endif
        private static string SaveFileName = "save.json";

        public static void Save(GameData data)
        {
            if (!Directory.Exists(SaveDirectory))
            {
                Directory.CreateDirectory(SaveDirectory);
            }

            string json = JsonUtility.ToJson(data, true);

            string path = Path.Combine(SaveDirectory, SaveFileName);
            File.WriteAllText(path, json);

            Debug.Log($"[SaveSystem] Saved to: {Path.GetFullPath(path)}");
        }

        public static GameData Load()
        {
            string path = Path.Combine(SaveDirectory, SaveFileName);

            if (!File.Exists(path))
            {
                Debug.LogWarning($"[SaveSystem] No save file found at ({Path.GetFullPath(path)}). Creating new data.");
                return new GameData();
            }

            string json = File.ReadAllText(path);
            
            GameData data = JsonUtility.FromJson<GameData>(json);

            Debug.Log($"[SaveSystem] Loaded. Scene: {data.currentStageSceneName}");
            return data;
        }

        public static bool HasSaveFile()
        {
            return File.Exists(Path.Combine(SaveDirectory, SaveFileName));
        }
    }
}
