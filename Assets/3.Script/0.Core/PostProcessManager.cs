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

        [Header("🔊 Audio")]
        [SerializeField] private AudioClip heartbeatSound;
        
        private AudioSource _heartbeatSource;
        
        private ChromaticAberration _chromaticAberration; 
        private MotionBlur _motionBlur; // [New] 모션블러 제어용
        private Vignette _vignette;     // [New] 딸피 효과용 비네팅
        
        private Coroutine _aberrationRoutine;
        private Coroutine _heartbeatRoutine;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                
                // [New] 심장박동 소리용 오디오 소스 자동차 생성
                _heartbeatSource = gameObject.AddComponent<AudioSource>();
                _heartbeatSource.loop = true;
                _heartbeatSource.playOnAwake = false;
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
                // 크로마틱 찾아오기
                if (globalVolume.profile.TryGet(out _chromaticAberration))
                {
                    _chromaticAberration.active = true;
                    _chromaticAberration.intensity.value = 0f;
                }

                // [New] 모션블러 찾아오기
                if (globalVolume.profile.TryGet(out _motionBlur))
                {
                    _motionBlur.active = true;
                    _motionBlur.intensity.value = 0f; // 평소엔 꺼둠
                }

                // [New] 비네팅 찾아오기
                if (globalVolume.profile.TryGet(out _vignette))
                {
                    _vignette.active = true;
                    _vignette.intensity.value = 0f;
                }
            }
            else
            {
                Debug.LogError("[유니] Global Volume을 찾을 수 없어! 인스펙터에 연결해줘!");
            }
        }

        public void SetLowHealthEffect(bool isActive)
        {
            if (isActive)
            {
                if (_heartbeatRoutine == null)
                {
                    _heartbeatRoutine = StartCoroutine(HeartbeatRoutine());
                    
                    if (heartbeatSound != null && _heartbeatSource != null)
                    {
                        _heartbeatSource.clip = heartbeatSound;
                        _heartbeatSource.Play();
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

                if (_heartbeatSource != null) _heartbeatSource.Stop();

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
                // 심장 박동처럼 두 번 연속 뛰는 느낌을 주기 위한 수학적 곡선 (PingPong 2번)
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
                Debug.LogWarning("[유니] 효과가 없어서 실행 불가능!");
                return;
            }

            // 디버그 로그: 실행 신호 받음 (주석 해제!)
            Debug.Log($"[유니] 효과 발동! 강도: {intensity}, 시간: {duration}");

            if (_aberrationRoutine != null) StopCoroutine(_aberrationRoutine);
            _aberrationRoutine = StartCoroutine(AberrationRoutine(intensity, duration));
        }

        private IEnumerator AberrationRoutine(float targetIntensity, float duration)
        {
            float startVal = _chromaticAberration.intensity.value;
            float time = 0f;
            
            // 올라갈 때 (20%)
            while (time < duration * 0.2f) 
            {
                time += Time.deltaTime;
                float t = time / (duration * 0.2f);
                _chromaticAberration.intensity.value = Mathf.Lerp(startVal, targetIntensity, t);
                yield return null;
            }

            // 내려올 때 (80%)
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
