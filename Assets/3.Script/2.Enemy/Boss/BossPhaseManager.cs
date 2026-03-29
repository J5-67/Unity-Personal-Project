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
        [SerializeField] private Vector3 phase2FloorScale = new Vector3(0.5f, 1f, 1f); 
        [SerializeField] private float scalingDuration = 2.0f;
        [Header("⚔️ Hazards")]
        [SerializeField] private GameObject spikes;
        [SerializeField] private GameObject lasers;
        [SerializeField] private GameObject aerialPlatforms;
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
            if (spikes != null) spikes.SetActive(false);
            if (lasers != null) lasers.SetActive(false);
            if (aerialPlatforms != null) aerialPlatforms.SetActive(false);
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
            HandlePhaseTransition(phase);
            SetPhaseCheckpoint(phase);
            if (Core.GameManager.Instance != null)
            {
                Core.GameManager.Instance.TriggerCameraShake(2.0f);
            }
            if (Core.AudioManager.Instance != null)
            {
                Core.AudioManager.Instance.SetMusicPhase(phase);
            }
            OnPhaseChanged?.Invoke(phase);
        }
        private void HandlePhaseTransition(int phase)
        {
            if (phase == 2)
            {
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
                if (lasers != null) lasers.SetActive(true);
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
            }
            else if (phase == 3 && phase3Checkpoint != null)
            {
                playerHealth.SetCheckpoint(phase3Checkpoint.position);
            }
        }
    }
}
