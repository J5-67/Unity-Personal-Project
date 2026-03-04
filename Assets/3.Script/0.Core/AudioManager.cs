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
        [SerializeField] private AudioClip heartbeatSFX;

        private AudioSource _heartbeatSource;

        private float _masterVolume = 1f;
        private float _bgmVolume = 1f;
        private float _sfxVolume = 1f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

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

            if (_heartbeatSource == null)
            {
                GameObject hbObj = new GameObject("Heartbeat_Source");
                hbObj.transform.SetParent(transform);
                _heartbeatSource = hbObj.AddComponent<AudioSource>();
                _heartbeatSource.loop = true;
                _heartbeatSource.playOnAwake = false;
            }

            LoadVolumes();
        }

        private void Start()
        {
            if (defaultBGM != null) PlayBGM(defaultBGM);
        }

        public void LoadVolumes()
        {
            _masterVolume = PlayerPrefs.GetFloat("MasterVolume", 100f) / 100f;
            _bgmVolume = PlayerPrefs.GetFloat("BGMVolume", 100f) / 100f;
            _sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 100f) / 100f;

            ApplyVolumes();
        }

        public void SetMasterVolume(float value)
        {
            _masterVolume = Mathf.Clamp01(value / 100f);
            ApplyVolumes();
        }

        public void SetBGMVolume(float value)
        {
            _bgmVolume = Mathf.Clamp01(value / 100f);
            ApplyVolumes();
        }

        public void SetSFXVolume(float value)
        {
            _sfxVolume = Mathf.Clamp01(value / 100f);
            ApplyVolumes();
        }

        private void ApplyVolumes()
        {
            if (bgmSource != null) bgmSource.volume = _masterVolume * _bgmVolume;
            if (sfxSource != null) sfxSource.volume = _masterVolume * _sfxVolume;
            if (_heartbeatSource != null) _heartbeatSource.volume = _masterVolume * _sfxVolume;
        }

        public void PlayBGM(AudioClip clip)
        {
            if (clip == null) return;
            if (bgmSource.clip == clip && bgmSource.isPlaying) return;

            bgmSource.clip = clip;
            bgmSource.Play();
        }

        public void PlaySFX(AudioClip clip)
        {
            if (clip == null) return;
            sfxSource.PlayOneShot(clip, sfxSource.volume);
        }

        public void PlayTestSFX()
        {
            PlaySFX(testSFX);
        }

        public void PlayHeartbeat()
        {
            if (heartbeatSFX == null) return;
            if (_heartbeatSource != null && !_heartbeatSource.isPlaying)
            {
                _heartbeatSource.clip = heartbeatSFX;
                _heartbeatSource.Play();
            }
        }

        public void StopHeartbeat()
        {
            if (_heartbeatSource != null && _heartbeatSource.isPlaying)
            {
                _heartbeatSource.Stop();
            }
        }
    }
}
