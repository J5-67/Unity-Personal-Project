using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core
{
    public class SceneLoader : MonoBehaviour
    {
        public static string TargetSceneName { get; private set; }

        public static void LoadScene(string sceneName)
        {
            TargetSceneName = sceneName;
            SceneManager.LoadScene("LoadingScene");
        }
    }
}
