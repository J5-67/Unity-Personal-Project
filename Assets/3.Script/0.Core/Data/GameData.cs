using System;

namespace Core.Data
{
    [System.Serializable]
    public class GameData
    {
        // [유니] 저장하고 싶은 변수들을 여기에 다 넣으면 돼!
        // 스테이지 씬 이름을 직접 저장! (오빠가 원하는 대로 이름 지어도 됨! 📝)
        public string currentStageSceneName = "1.GameTest"; 

        // [확장 가능]
        // public int coinCount = 0;
        // public float bgmVolume = 1.0f;
    }
}
