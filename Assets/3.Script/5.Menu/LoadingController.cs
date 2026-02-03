using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro; // 텍스트 표시용

namespace UI
{
    public class LoadingController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Slider progressBar;       // 로딩 게이지
        [SerializeField] private TextMeshProUGUI progressText; // 퍼센트 텍스트 (옵션)
        [SerializeField] private CanvasGroup canvasGroup; // 페이드 효과용 (옵션)

        [Header("Settings")]
        [Tooltip("로딩이 너무 빨라도 최소 이 시간만큼은 보여줌 (UX)")]
        [SerializeField] private float minLoadingTime = 1.0f; 

        private void Start()
        {
            // [유니] 로딩 시작 전에 묵은 때(메모리) 싹 씻어내기! 🚿
            Resources.UnloadUnusedAssets();
            System.GC.Collect();

            // [유니] SceneLoader에 저장된 목표 씬을 불러오자!
            string targetScene = Core.SceneLoader.TargetSceneName;

            if (string.IsNullOrEmpty(targetScene))
            {
                Debug.LogError("[Loading] 🚨 갈 곳이 없어! SceneLoader를 통해서 이동해줘!");
                return; // 혹은 메인 메뉴로 튕겨내기
            }

            StartCoroutine(LoadSceneAsync(targetScene));
        }

        private IEnumerator LoadSceneAsync(string sceneName)
        {
            // [유니] 비동기 로딩 시작!
            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
            
            // 씬 자동 전환 막기 (90%에서 멈춰있게 함)
            op.allowSceneActivation = false; 

            float timer = 0.0f;

            // [유니] 로딩이 완료(0.9)될 때까지 반복
            while (!op.isDone)
            {
                yield return null; // 1프레임 대기

                timer += Time.unscaledDeltaTime;

                // 1. 실제 로딩 진행률 (0.0 ~ 1.0)
                float realProgress = Mathf.Clamp01(op.progress / 0.9f);
                
                // 2. 가짜 로딩 진행률 (시간 비례, 0.0 ~ 1.0)
                float fakeProgress = Mathf.Clamp01(timer / minLoadingTime);

                // [유니] 실제 로딩이 빨라도, 가짜 로딩(시간)보다 앞서가지 않도록 제한! 🐢
                // 둘 중 '더 작은 값'을 목표로 삼음.
                float targetProgress = Mathf.Min(realProgress, fakeProgress);

                // UI 부드럽게 갱신
                if (progressBar != null)
                {
                    progressBar.value = Mathf.Lerp(progressBar.value, targetProgress, Time.unscaledDeltaTime * 5f);
                    
                    // 거의 다 왔으면(0.99 이상) 1로 고정 (떨림 방지)
                    if (Mathf.Abs(progressBar.value - targetProgress) < 0.01f) progressBar.value = targetProgress;
                }
                
                if (progressText != null)
                {
                     progressText.text = $"{(progressBar.value * 100f):F0}%";
                }

                // [유니] 실제 로딩 완료 & 최소 시간 경과 & UI도 거의 다 참
                if (op.progress >= 0.9f && timer >= minLoadingTime && progressBar.value >= 0.99f)
                {
                    // 슬라이더 꽉 채우기
                    if (progressBar != null) progressBar.value = 1f;
                    if (progressText != null) progressText.text = "100%";

                    // 잠시 대기 (꽉 찬 거 보여줌)
                    yield return new WaitForSecondsRealtime(0.2f);

                    // 씬 전환 허용! 🚀
                    op.allowSceneActivation = true;
                }
            }
        }
    }
}
