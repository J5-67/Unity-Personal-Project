using UnityEngine;

namespace Core
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("🔊 Audio Sources")]
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioSource sfxSource;

        [Header("🎶 Default Clips (Optional)")]
        [SerializeField] private AudioClip defaultBGM;
        [SerializeField] private AudioClip testSFX;

        // [유니] 볼륨 값 (0.0 ~ 1.0) - 내부 연산용
        private float _masterVolume = 1f;
        private float _bgmVolume = 1f;
        private float _sfxVolume = 1f;

        private void Awake()
        {
            // Singleton Setup
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // [유니] 오디오 소스 없으면 그 자리에서 만들어버리기! 뚝딱! 🔨
            if (bgmSource == null)
            {
                GameObject bgmObj = new GameObject("BGM_Source");
                bgmObj.transform.SetParent(transform);
                bgmSource = bgmObj.AddComponent<AudioSource>();
                bgmSource.loop = true;
                bgmSource.playOnAwake = false;
            }

            if (sfxSource == null)
            {
                GameObject sfxObj = new GameObject("SFX_Source");
                sfxObj.transform.SetParent(transform);
                sfxSource = sfxObj.AddComponent<AudioSource>();
                sfxSource.loop = false;
                sfxSource.playOnAwake = false;
            }

            // 초기 볼륨 로드
            LoadVolumes();
        }

        private void Start()
        {
            if (defaultBGM != null) PlayBGM(defaultBGM);
        }

        public void LoadVolumes()
        {
            // [유니] PlayerPrefs는 0~100으로 저장되어 있으니, 100으로 나눠서 0~1로 가져옴!
            _masterVolume = PlayerPrefs.GetFloat("MasterVolume", 100f) / 100f;
            _bgmVolume = PlayerPrefs.GetFloat("BGMVolume", 100f) / 100f;
            _sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 100f) / 100f;

            ApplyVolumes();
        }

        public void SetMasterVolume(float value) // value: 0~100
        {
            _masterVolume = Mathf.Clamp01(value / 100f);
            ApplyVolumes();
        }

        public void SetBGMVolume(float value) // value: 0~100
        {
            _bgmVolume = Mathf.Clamp01(value / 100f);
            ApplyVolumes();
        }

        public void SetSFXVolume(float value) // value: 0~100
        {
            _sfxVolume = Mathf.Clamp01(value / 100f);
            ApplyVolumes();
        }

        private void ApplyVolumes()
        {
            // 최종 볼륨 = 마스터 * 개별 볼륨
            if (bgmSource != null) bgmSource.volume = _masterVolume * _bgmVolume;
            if (sfxSource != null) sfxSource.volume = _masterVolume * _sfxVolume;
        }

        // 🎵 BGM 재생
        public void PlayBGM(AudioClip clip)
        {
            if (clip == null) return;
            if (bgmSource.clip == clip && bgmSource.isPlaying) return; // 이미 같은 곡 재생 중이면 패스

            bgmSource.clip = clip;
            bgmSource.Play();
        }

        // 🔊 SFX 재생 (중첩 가능하게 PlayOneShot 사용)
        public void PlaySFX(AudioClip clip)
        {
            if (clip == null) return;
            sfxSource.PlayOneShot(clip, sfxSource.volume); // OneShot은 볼륨 스케일을 별도로 받음 (소스 볼륨은 기본)
        }

        // [유니] 테스트를 위한 간편 함수
        public void PlayTestSFX()
        {
            PlaySFX(testSFX);
        }
    }
}
