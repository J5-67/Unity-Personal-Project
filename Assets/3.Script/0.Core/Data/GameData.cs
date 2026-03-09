using System;

namespace Core.Data
{
    [System.Serializable]
    public class GameData
    {
        public string currentStageSceneName = "1.GameTest";
        public UnityEngine.Vector3 lastCheckpointPosition = UnityEngine.Vector3.zero;
        public bool hasSavedPosition = false;
    }
}
