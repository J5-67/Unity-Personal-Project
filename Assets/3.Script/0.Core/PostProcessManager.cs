using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Core
{
    public class PostProcessManager : MonoBehaviour
    {
        public static PostProcessManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private Volume globalVolume;

        private ChromaticAberration _chromaticAberration;
        private MotionBlur _motionBlur;
        private Vignette _vignette;

        private Coroutine _aberrationRoutine;
        private Coroutine _heartbeatRoutine;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            if (globalVolume == null)
            {
                globalVolume = FindAnyObjectByType<Volume>();
            }

            if (globalVolume != null)
            {

                if (globalVolume.profile.TryGet(out _chromaticAberration))
                {
                    _chromaticAberration.active = true;
                    _chromaticAberration.intensity.value = 0f;
                }

                if (globalVolume.profile.TryGet(out _motionBlur))
                {
                    _motionBlur.active = true;
                    _motionBlur.intensity.value = 0f;
                }

                if (globalVolume.profile.TryGet(out _vignette))
                {
                    _vignette.active = true;
                    _vignette.intensity.value = 0f;
                }
            }
            else
            {

            }
        }

        public void SetLowHealthEffect(bool isActive)
        {
            if (isActive)
            {
                if (_heartbeatRoutine == null)
                {
                    _heartbeatRoutine = StartCoroutine(HeartbeatRoutine());

                    if (Core.AudioManager.Instance != null)
                    {
                        Core.AudioManager.Instance.PlayHeartbeat();
                    }
                }
            }
            else
            {
                if (_heartbeatRoutine != null)
                {
                    StopCoroutine(_heartbeatRoutine);
                    _heartbeatRoutine = null;
                }

                if (Core.AudioManager.Instance != null)
                {
                    Core.AudioManager.Instance.StopHeartbeat();
                }

                if (_vignette != null)
                {
                    _vignette.intensity.value = 0f;
                    _vignette.color.value = Color.black;
                }
            }
        }

        private IEnumerator HeartbeatRoutine()
        {
            while (true)
            {

                float time = Time.time * 3f;
                float pulse = Mathf.PingPong(time, 1f) * Mathf.PingPong(time * 0.8f, 1f);

                if (_vignette != null)
                {
                    _vignette.intensity.value = Mathf.Lerp(0.3f, 0.45f, pulse);
                    _vignette.color.value = Color.Lerp(new Color(0.2f, 0f, 0f), Color.red, pulse);
                }

                yield return null;
            }
        }

        public void SetMotionBlur(float intensity)
        {
            if (_motionBlur != null)
            {
                _motionBlur.intensity.value = intensity;
            }
        }

        public void TriggerChromaticAberration(float intensity, float duration)
        {
            if (_chromaticAberration == null)
            {

                return;
            }

            if (_aberrationRoutine != null) StopCoroutine(_aberrationRoutine);
            _aberrationRoutine = StartCoroutine(AberrationRoutine(intensity, duration));
        }

        private IEnumerator AberrationRoutine(float targetIntensity, float duration)
        {
            float startVal = _chromaticAberration.intensity.value;
            float time = 0f;

            while (time < duration * 0.2f)
            {
                time += Time.deltaTime;
                float t = time / (duration * 0.2f);
                _chromaticAberration.intensity.value = Mathf.Lerp(startVal, targetIntensity, t);
                yield return null;
            }

            time = 0f;
            while (time < duration * 0.8f)
            {
                time += Time.deltaTime;
                float t = time / (duration * 0.8f);
                _chromaticAberration.intensity.value = Mathf.Lerp(targetIntensity, 0f, t);
                yield return null;
            }

            _chromaticAberration.intensity.value = 0f;
            _aberrationRoutine = null;
        }
    }
}
