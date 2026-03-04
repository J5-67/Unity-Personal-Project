using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Core;

namespace Level
{
    [System.Serializable]
    public class WaveData
    {
        public string waveName;

        public List<BaseEnemy> enemiesToSpawn = new List<BaseEnemy>();
        public float delayBeforeWave = 2f;
    }

    public class WaveManager : MonoBehaviour
    {
        [Header("🌊 Wave Settings")]
        [SerializeField] private List<WaveData> waves;
        [SerializeField] private GameObject exitDoorObject;
        [SerializeField] private bool resetOnRespawn = true;

        [Header("👑 Boss Settings (Optional)")]
        [SerializeField] private BossHealth boss;
        [SerializeField] private GameObject bossUI;

        private Interaction.Door _doorComponent;
        private int _currentWaveIndex = 0;
        private int _currentDeadCount = 0;
        private bool _isCleared = false;
        private bool _isWaveActive = false;

        private void Start()
        {
            if (exitDoorObject != null)
            {
                exitDoorObject.TryGetComponent(out _doorComponent);
            }

            if (resetOnRespawn && GameManager.Instance != null)
            {
                GameManager.Instance.OnPlayerRespawn += ResetWaves;
            }

            InitializeAllEnemies();

            if (_doorComponent != null) _doorComponent.Close();
        }

        private void OnDestroy()
        {
            if (resetOnRespawn && GameManager.Instance != null)
            {
                GameManager.Instance.OnPlayerRespawn -= ResetWaves;
            }

            foreach (var wave in waves)
            {
                foreach (var enemy in wave.enemiesToSpawn)
                {
                    if (enemy != null) enemy.OnDeath -= OnEnemyDeath;
                }
            }
            if (boss != null)
            {
                boss.OnDeath -= ClearZone;
            }
        }

        private void InitializeAllEnemies()
        {
            foreach (var wave in waves)
            {
                foreach (var enemy in wave.enemiesToSpawn)
                {
                    if (enemy != null)
                    {
                        enemy.gameObject.SetActive(false);
                        enemy.OnDeath -= OnEnemyDeath;
                        enemy.OnDeath += OnEnemyDeath;
                    }
                }
            }

            if (boss != null)
            {
                boss.gameObject.SetActive(false);
                boss.OnDeath -= ClearZone;
                boss.OnDeath += ClearZone;
            }
            if (bossUI != null) bossUI.SetActive(false);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") && !_isWaveActive && !_isCleared)
            {
                StartWaveSequence();
            }
        }

        public void StartWaveSequence()
        {
            _isWaveActive = true;
            _currentWaveIndex = 0;
            _currentDeadCount = 0;

            if (_doorComponent != null) _doorComponent.Close();

            StartCoroutine(SpawnWaveRoutine(_currentWaveIndex));
        }

        private IEnumerator SpawnWaveRoutine(int waveIndex)
        {
            if (waveIndex >= waves.Count)
            {

                StartBossFight();
                yield break;
            }

            WaveData currentWave = waves[waveIndex];
            _currentDeadCount = 0;

            yield return new WaitForSeconds(currentWave.delayBeforeWave);

            foreach (var enemy in currentWave.enemiesToSpawn)
            {
                if (enemy != null && VFXManager.Instance != null)
                {
                    VFXManager.Instance.PlaySpawnEffect(enemy.transform.position);
                }
            }

            yield return new WaitForSeconds(0.5f);

            foreach (var enemy in currentWave.enemiesToSpawn)
            {
                if (enemy != null)
                {
                    enemy.gameObject.SetActive(true);
                    enemy.ResetEnemy();
                }
            }
        }

        private void OnEnemyDeath(BaseEnemy enemy)
        {
            if (!_isWaveActive) return;

            _currentDeadCount++;

            if (_currentWaveIndex < waves.Count)
            {
                if (_currentDeadCount >= waves[_currentWaveIndex].enemiesToSpawn.Count)
                {

                    _currentWaveIndex++;
                    StartCoroutine(SpawnWaveRoutine(_currentWaveIndex));
                }
            }
        }

        private void StartBossFight()
        {
            if (boss != null)
            {

                boss.gameObject.SetActive(true);

                if (bossUI != null) bossUI.SetActive(true);

            }
            else
            {

                ClearZone();
            }
        }

        public void ClearZone()
        {
            if (_isCleared) return;

            _isCleared = true;
            _isWaveActive = false;

            if (bossUI != null) bossUI.SetActive(false);
            if (_doorComponent != null) _doorComponent.Open();
        }

        private void ResetWaves()
        {
            StopAllCoroutines();
            _isWaveActive = false;
            _isCleared = false;
            _currentWaveIndex = 0;
            _currentDeadCount = 0;

            InitializeAllEnemies();

            if (_doorComponent != null) _doorComponent.Close();
        }

        [ContextMenu("웨이브 자동 정리 (빈 칸 제거)")]
        private void CleanUpEmptySlots()
        {
            foreach (var wave in waves)
            {
                wave.enemiesToSpawn.RemoveAll(x => x == null);
            }
        }
    }
}
