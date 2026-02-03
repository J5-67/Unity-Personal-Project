using System.IO;
using UnityEngine;

namespace Core.Data
{
    public static class SaveSystem
    {
        // [유니] 저장 경로 설정!
        // 에디터: Assets 폴더의 상위 폴더 (프로젝트 루트)/SaveData
        // 빌드: 실행 파일(EXE) 옆/SaveData
#if UNITY_EDITOR
        private static string SaveDirectory => Path.Combine(Application.dataPath, "../SaveData");
#else
        private static string SaveDirectory => Path.Combine(Application.dataPath, "../SaveData"); 
#endif
        private static string SaveFileName = "save.json";

        public static void Save(GameData data)
        {
            // 1. 폴더 없으면 만들기
            if (!Directory.Exists(SaveDirectory))
            {
                Directory.CreateDirectory(SaveDirectory);
            }

            // 2. 데이터 -> JSON 변환 (들여쓰기 예쁘게!)
            string json = JsonUtility.ToJson(data, true);

            // 3. 파일 쓰기
            string path = Path.Combine(SaveDirectory, SaveFileName);
            File.WriteAllText(path, json);

            // [유니] 절대 경로 찍어서 찾기 쉽게!
            Debug.Log($"[SaveSystem] 저장 완료! 💾 경로: {Path.GetFullPath(path)}");
        }

        public static GameData Load()
        {
            string path = Path.Combine(SaveDirectory, SaveFileName);

            // 1. 파일 있는지 확인
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[SaveSystem] 세이브 파일이 없어! ({Path.GetFullPath(path)}) 새로 만들게! ✨");
                return new GameData(); // 기본값(1스테이지) 반환
            }

            // 2. JSON 읽기
            string json = File.ReadAllText(path);
            
            // 3. JSON -> 데이터 변환
            GameData data = JsonUtility.FromJson<GameData>(json);

            Debug.Log($"[SaveSystem] 불러오기 완료! 📂 마지막 위치: {data.currentStageSceneName}");
            return data;
        }

        // [유니] 파일이 존재하는지 확인하는 함수 추가!
        public static bool HasSaveFile()
        {
            return File.Exists(Path.Combine(SaveDirectory, SaveFileName));
        }
    }
}
