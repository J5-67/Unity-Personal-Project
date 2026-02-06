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
        private Coroutine _aberrationRoutine;

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
                // 프로필에서 효과 찾기 시도
                if (globalVolume.profile.TryGet(out _chromaticAberration))
                {
                    _chromaticAberration.active = true;
                    // 디버그 로그: 효과 찾음
                    Debug.Log($"[유니] ChromaticAberration 찾기 성공! 기본값 조절 시작: {_chromaticAberration.intensity.value}");
                    _chromaticAberration.intensity.value = 0f;
                }
                else
                {
                    Debug.LogError("[유니] Volume Profile 안에 Chromatic Aberration이 없어! Add Override 했는지 확인해줘!");
                }
            }
            else
            {
                Debug.LogError("[유니] Global Volume을 찾을 수 없어! 인스펙터에 연결해줘!");
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
