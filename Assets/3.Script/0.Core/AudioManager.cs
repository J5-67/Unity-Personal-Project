using UnityEngine;
using System.Collections;
using System.Collections.Generic;
namespace Core
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }
        [Header("🔊 Audio Sources")]
        [SerializeField] private AudioSource bgmSourceA;
        [SerializeField] private AudioSource bgmSourceB;
        [SerializeField] private AudioSource sfxSource;
        [Header("🎶 Default Clips")]
        [SerializeField] private AudioClip defaultBGM;
        [SerializeField] private AudioClip heartbeatSFX;
        [Header("⛓️ Phase Pooling Settings")]
        [SerializeField] private int maxPhaseLayers = 5;
        private AudioSource _activeSource;
        private AudioSource _heartbeatSource;
        private List<AudioSource> _pooledPhaseSources = new List<AudioSource>();
        private List<AudioSource> _activePhaseSources = new List<AudioSource>();
        private Coroutine _fadeCoroutine;
        private float _masterVolume = 1f;
        private float _bgmVolume = 1f;
        private float _sfxVolume = 1f;
        private bool _isPhaseMusicActive = false;
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeSources();
            CreatePhasePool();
            LoadVolumes();
        }
        private void InitializeSources()
        {
            if (bgmSourceA == null) bgmSourceA = CreateSource("BGM_Source_A", true);
            if (bgmSourceB == null) bgmSourceB = CreateSource("BGM_Source_B", true);
            if (sfxSource == null) sfxSource = CreateSource("SFX_Source", false);
            if (_heartbeatSource == null) _heartbeatSource = CreateSource("Heartbeat_Source", true);
            _activeSource = bgmSourceA;
        }
        private void CreatePhasePool()
        {
            for (int i = 0; i < maxPhaseLayers; i++)
            {
                AudioSource source = CreateSource($"Pooled_Phase_Source_{i}", true);
                source.gameObject.SetActive(false);
                _pooledPhaseSources.Add(source);
            }
        }
        private AudioSource CreateSource(string name, bool loop)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(transform);
            AudioSource source = obj.AddComponent<AudioSource>();
            source.loop = loop;
            source.playOnAwake = false;
            return source;
        }
        private void Start()
        {
            if (defaultBGM != null) PlayBGM(defaultBGM);
        }
        private void Update()
        {
            if (!_isPhaseMusicActive && _fadeCoroutine == null)
            {
                ApplyVolumes();
            }
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
            float targetBGM = _masterVolume * _bgmVolume;
            float targetSFX = _masterVolume * _sfxVolume;
            if (bgmSourceA != null) bgmSourceA.volume = (_activeSource == bgmSourceA) ? targetBGM : 0f;
            if (bgmSourceB != null) bgmSourceB.volume = (_activeSource == bgmSourceB) ? targetBGM : 0f;
            if (sfxSource != null) sfxSource.volume = targetSFX;
            if (_heartbeatSource != null) _heartbeatSource.volume = targetSFX;
        }
        public void PreloadClips(params AudioClip[] clips)
        {
            if (clips == null) return;
            foreach (var clip in clips)
            {
                if (clip != null && clip.loadState != AudioDataLoadState.Loaded)
                {
                    clip.LoadAudioData();
                }
            }
        }
        public void PlayBGM(AudioClip clip)
        {
            if (clip == null || (_activeSource.clip == clip && _activeSource.isPlaying)) return;
            StopPhaseMusic();
            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
                _fadeCoroutine = null;
            }
            AudioSource inactiveSource = (_activeSource == bgmSourceA) ? bgmSourceB : bgmSourceA;
            inactiveSource.Stop();
            _activeSource.clip = clip;
            _activeSource.volume = _masterVolume * _bgmVolume;
            _activeSource.Play();
        }
        public void FadeBGM(AudioClip clip, float duration = 1.0f)
        {
            if (clip == null || (_activeSource.clip == clip && _activeSource.isPlaying)) return;
            StopPhaseMusic();
            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(CrossfadeBGMRoutine(clip, duration));
        }
        private IEnumerator CrossfadeBGMRoutine(AudioClip clip, float duration)
        {
            AudioSource newSource = (_activeSource == bgmSourceA) ? bgmSourceB : bgmSourceA;
            newSource.clip = clip;
            newSource.volume = 0;
            newSource.Play();
            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float currentMaxBGM = _masterVolume * _bgmVolume;
                newSource.volume = Mathf.Lerp(0, currentMaxBGM, t);
                _activeSource.volume = Mathf.Lerp(currentMaxBGM, 0, t);
                yield return null;
            }
            _activeSource.Stop();
            _activeSource = newSource;
            _fadeCoroutine = null;
        }
        public void StartPhaseMusic(params AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0) return;
            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = null;
            bgmSourceA.Stop();
            bgmSourceB.Stop();
            StopPhaseMusic(); 
            _isPhaseMusicActive = true;
            for (int i = 0; i < clips.Length; i++)
            {
                if (i >= _pooledPhaseSources.Count) break;
                AudioSource source = _pooledPhaseSources[i];
                source.gameObject.SetActive(true);
                source.clip = clips[i];
                source.volume = 0f;
                source.Play();
                _activePhaseSources.Add(source);
            }
            SetMusicPhase(1, 0.1f);
        }
        public void SetMusicPhase(int phaseIndex, float duration = 1.5f)
        {
            if (!_isPhaseMusicActive || _activePhaseSources.Count < phaseIndex) return;
            int targetIdx = phaseIndex - 1;
            float currentMaxBGM = _masterVolume * _bgmVolume;
            for (int i = 0; i < _activePhaseSources.Count; i++)
            {
                StartCoroutine(FadeSourceVolume(_activePhaseSources[i], (i == targetIdx) ? currentMaxBGM : 0f, duration));
            }
        }
        private IEnumerator FadeSourceVolume(AudioSource source, float targetVol, float duration)
        {
            float startVol = source.volume;
            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float currentMaxBGM = _masterVolume * _bgmVolume;
                float realTarget = (targetVol > 0.01f) ? currentMaxBGM : 0f;
                source.volume = Mathf.Lerp(startVol, realTarget, elapsed / duration);
                yield return null;
            }
            source.volume = (targetVol > 0.01f) ? (_masterVolume * _bgmVolume) : 0f;
        }
        public void StopPhaseMusic()
        {
            if (!_isPhaseMusicActive) return;
            foreach (var s in _activePhaseSources)
            {
                if (s != null)
                {
                    s.Stop();
                    s.clip = null;
                    s.gameObject.SetActive(false);
                }
            }
            _activePhaseSources.Clear();
            _isPhaseMusicActive = false;
        }
        public void PlaySFX(AudioClip clip)
        {
            if (clip == null) return;
            sfxSource.PlayOneShot(clip, sfxSource.volume);
        }
        public void PlayHeartbeat()
        {
            if (heartbeatSFX == null || (_heartbeatSource != null && _heartbeatSource.isPlaying)) return;
            if (_heartbeatSource != null)
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
