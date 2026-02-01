using UnityEngine;
using Core; // AudioManager 사용

namespace Core
{
    // [유니] 씬마다 이 스크립트를 빈 오브젝트에 붙여두고 BGM을 지정하면 돼! 🎵
    public class SceneAudioController : MonoBehaviour
    {
        [Header("🎵 Scene BGM")]
        [SerializeField] private AudioClip sceneBGM;

        private void Start()
        {
            // [유니] "오디오 매니저야, 이 씬에서는 이 노래 틀어줘!"
            // (만약 sceneBGM이 비어있으면 아무것도 안 함 or 기존 음악 유지)
            if (sceneBGM != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayBGM(sceneBGM);
            }
        }
    }
}
