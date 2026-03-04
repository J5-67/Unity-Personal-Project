using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace UI
{
    public class LoadingController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Slider progressBar;
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Settings")]
        [SerializeField] private float minLoadingTime = 1.0f;

        private void Start()
        {
            Resources.UnloadUnusedAssets();
            System.GC.Collect();

            string targetScene = Core.SceneLoader.TargetSceneName;

            if (string.IsNullOrEmpty(targetScene))
            {

                return;
            }

            StartCoroutine(LoadSceneAsync(targetScene));
        }

        private IEnumerator LoadSceneAsync(string sceneName)
        {
            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);

            op.allowSceneActivation = false;

            float timer = 0.0f;

            while (!op.isDone)
            {
                yield return null;

                timer += Time.unscaledDeltaTime;

                float realProgress = Mathf.Clamp01(op.progress / 0.9f);

                float fakeProgress = Mathf.Clamp01(timer / minLoadingTime);

                float targetProgress = Mathf.Min(realProgress, fakeProgress);

                if (progressBar != null)
                {
                    progressBar.value = Mathf.Lerp(progressBar.value, targetProgress, Time.unscaledDeltaTime * 5f);

                    if (Mathf.Abs(progressBar.value - targetProgress) < 0.01f) progressBar.value = targetProgress;
                }

                if (progressText != null)
                {
                     progressText.text = $"{(progressBar.value * 100f):F0}%";
                }

                if (op.progress >= 0.9f && timer >= minLoadingTime && progressBar.value >= 0.99f)
                {
                    if (progressBar != null) progressBar.value = 1f;
                    if (progressText != null) progressText.text = "100%";

                    yield return new WaitForSecondsRealtime(0.2f);

                    op.allowSceneActivation = true;
                }
            }
        }
    }
}
