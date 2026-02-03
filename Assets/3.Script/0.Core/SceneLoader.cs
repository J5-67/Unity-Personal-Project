using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core
{
    public class SceneLoader : MonoBehaviour
    {
        // [유니] 이동할 씬의 이름을 저장해두는 정적 변수야! (LoadScene 호출 시 채워짐)
        public static string TargetSceneName { get; private set; }

        public static void LoadScene(string sceneName)
        {
            TargetSceneName = sceneName;
            // [유니] 로딩 전용 씬인 "LoadingScene"으로 먼저 이동!
            // (이 씬은 Build Settings에 꼭 등록되어 있어야 해!)
            SceneManager.LoadScene("LoadingScene");
        }
    }
}
