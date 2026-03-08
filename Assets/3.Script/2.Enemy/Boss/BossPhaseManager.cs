using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Enemy.Boss
{
    public class BossPhaseManager : MonoBehaviour
    {
        [Header("🔗 References")]
        [SerializeField] private BossHealth bossHealth;
        
        [Header("🏗️ Floor Settings")]
        [SerializeField] private Transform bossFloor;
        [SerializeField] private Vector3 phase2FloorScale = new Vector3(0.5f, 1f, 1f); // 🎯 오빠! X축을 줄이면 양옆에서 좁아지겠지? 😊
        [SerializeField] private float scalingDuration = 2.0f;

        [Header("⚔️ Hazards")]
        [SerializeField] private GameObject spikes;
        [SerializeField] private GameObject lasers;
        [SerializeField] private GameObject aerialPlatforms; // 🎯 오빠! 공중에 매달릴 수 있는 발판들이야! 🥰

        [Header("🏁 Checkpoints")]
        [SerializeField] private Transform phase2Checkpoint;
        [SerializeField] private Transform phase3Checkpoint;

        private int _currentPhase = 1;
        private Coroutine _scalingCoroutine;
        public event Action<int> OnPhaseChanged;

        private void OnEnable()
        {
            if (bossHealth != null)
            {
                bossHealth.OnHealthChanged += CheckPhaseChange;
            }
            
            // 초기 상태 세팅 (함정 다 꺼두기!)
            if (spikes != null) spikes.SetActive(false);
            if (lasers != null) lasers.SetActive(false);
            if (aerialPlatforms != null) aerialPlatforms.SetActive(false);

            // 🎯 오빠! 플레이어가 죽었을 때 보스 피를 깎아주기 위해 이벤트를 구독할게! 🥰
            PlayerHealth playerHealth = FindAnyObjectByType<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.OnDieEvent += OnPlayerDeath;
            }
        }

        private void OnDisable()
        {
            if (bossHealth != null)
            {
                bossHealth.OnHealthChanged -= CheckPhaseChange;
            }

            PlayerHealth playerHealth = FindAnyObjectByType<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.OnDieEvent -= OnPlayerDeath;
            }
        }

        private void OnPlayerDeath()
        {
            // 🎯 오빠가 죽었어도 보스가 이미 먼저 죽었으면(IsDead), 리셋하지 말고 오빠의 승리를 인정해줘야지! 🥰
            if (bossHealth == null || bossHealth.IsDead) return;

            float resetPercent = 100f;
            if (_currentPhase == 2) resetPercent = 50f;
            else if (_currentPhase == 3) resetPercent = 30f;

            bossHealth.ResetBossHealth(resetPercent);
        }

        private void CheckPhaseChange(float current, float max)
        {
            float healthPercentage = (current / max) * 100f;

            if (_currentPhase == 1 && healthPercentage <= 50f)
            {
                EnterPhase(2);
            }
            else if (_currentPhase == 2 && healthPercentage <= 30f)
            {
                EnterPhase(3);
            }
        }

        private void EnterPhase(int phase)
        {
            if (_currentPhase == phase) return;
            
            _currentPhase = phase;
            Debug.Log($"🔥 보스 {phase}페이즈 돌입!");

            HandlePhaseTransition(phase);
            SetPhaseCheckpoint(phase);
            
            if (Core.GameManager.Instance != null)
            {
                Core.GameManager.Instance.TriggerCameraShake(2.0f);
            }

            OnPhaseChanged?.Invoke(phase);
        }

        private void HandlePhaseTransition(int phase)
        {
            if (phase == 2)
            {
                // 🎯 2페이즈: 땅이 부드럽게 좁아지고 가시가 튀어나와! 😱
                if (bossFloor != null)
                {
                    if (_scalingCoroutine != null) StopCoroutine(_scalingCoroutine);
                    _scalingCoroutine = StartCoroutine(ScaleFloorRoutine(phase2FloorScale));
                }
                if (spikes != null) spikes.SetActive(true);
                if (aerialPlatforms != null) aerialPlatforms.SetActive(true);
            }
            else if (phase == 3)
            {
                // 🎯 3페이즈: 이제 진짜 레이저까지 추가! 🔥
                if (lasers != null) lasers.SetActive(true);
                // 레이저 패턴 중에도 발판은 계속 필요하니까 같이 켜둘게!
                if (aerialPlatforms != null) aerialPlatforms.SetActive(true);
            }
        }

        private System.Collections.IEnumerator ScaleFloorRoutine(Vector3 targetScale)
        {
            Vector3 startScale = bossFloor.localScale;
            float elapsed = 0f;

            while (elapsed < scalingDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / scalingDuration;
                
                // 부드러운 가감속 효과! ✨
                t = t * t * (3f - 2f * t);

                bossFloor.localScale = Vector3.Lerp(startScale, targetScale, t);
                yield return null;
            }

            bossFloor.localScale = targetScale;
            _scalingCoroutine = null;
        }

        private void SetPhaseCheckpoint(int phase)
        {
            PlayerHealth playerHealth = FindAnyObjectByType<PlayerHealth>();
            if (playerHealth == null) return;

            if (phase == 2 && phase2Checkpoint != null)
            {
                playerHealth.SetCheckpoint(phase2Checkpoint.position);
                Debug.Log("📍 2페이즈 체크포인트 저장 완료!");
            }
            else if (phase == 3 && phase3Checkpoint != null)
            {
                playerHealth.SetCheckpoint(phase3Checkpoint.position);
                Debug.Log("📍 3페이즈 체크포인트 저장 완료!");
            }
        }
    }
}
