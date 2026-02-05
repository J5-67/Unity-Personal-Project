using UnityEngine;
using Core; 

namespace Core
{
    public class SceneAudioController : MonoBehaviour
    {
        [Header("🎵 Scene BGM")]
        [SerializeField] private AudioClip sceneBGM;

        private void Start()
        {
            if (sceneBGM != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayBGM(sceneBGM);
            }
        }
    }
}
