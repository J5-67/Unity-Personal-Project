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
        // 웨이브에서 소환할 적 목록 (미리 배치해두고 비활성화한 상태로 참조)
        public List<BaseEnemy> enemiesToSpawn = new List<BaseEnemy>();
        public float delayBeforeWave = 2f; 
    }

    public class WaveManager : MonoBehaviour
    {
        [Header("🌊 Wave Settings")]
        [SerializeField] private List<WaveData> waves;
        [SerializeField] private GameObject exitDoorObject;
        [SerializeField] private bool resetOnRespawn = true;
        
        // 보스 등 특별한 이벤트용 보스 (마지막 웨이브 이후에 등장하거나 특정 웨이브에 포함)
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

            // 시작할 때 모든 적과 보스 비활성화
            InitializeAllEnemies();
            
            if (_doorComponent != null) _doorComponent.Close();
        }

        private void OnDestroy()
        {
            if (resetOnRespawn && GameManager.Instance != null)
            {
                GameManager.Instance.OnPlayerRespawn -= ResetWaves;
            }

            // 이벤트 구독 해제
            foreach (var wave in waves)
            {
                foreach (var enemy in wave.enemiesToSpawn)
                {
                    if (enemy != null) enemy.OnDeath -= OnEnemyDeath;
                }
            }
            if (boss != null)
            {
                // boss.OnDeath -= OnBossDeath; (보스 죽음 이벤트가 있다면)
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
                        enemy.gameObject.SetActive(false); // 일단 숨김
                        enemy.OnDeath -= OnEnemyDeath; // 중복 방지
                        enemy.OnDeath += OnEnemyDeath; // 구독
                    }
                }
            }

            if (boss != null) boss.gameObject.SetActive(false);
            if (bossUI != null) bossUI.SetActive(false);
        }

        // 플레이어가 트리거(콜라이더)에 닿으면 웨이브 시작
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
                // 모든 웨이브 끝남 -> 보스전 시작!
                StartBossFight();
                yield break;
            }

            WaveData currentWave = waves[waveIndex];
            _currentDeadCount = 0; // 데드 카운트 초기화
            
            Debug.Log($"[WaveManager] 웨이브 {waveIndex + 1} 대기 중... ({currentWave.delayBeforeWave}초)");
            yield return new WaitForSeconds(currentWave.delayBeforeWave);

            Debug.Log($"[WaveManager] 🚨 웨이브 {waveIndex + 1} 시작! 🚨");

            // 적 소환(활성화)
            foreach (var enemy in currentWave.enemiesToSpawn)
            {
                if (enemy != null)
                {
                    enemy.gameObject.SetActive(true);
                    // 죽었던 적 살려내기 (BaseEnemy 내부 로직에 따라 다름, 주로 ResetEnemy 호출 필요)
                    enemy.SendMessage("ResetEnemy", SendMessageOptions.DontRequireReceiver);
                }
            }
        }

        private void OnEnemyDeath(BaseEnemy enemy)
        {
            if (!_isWaveActive) return;

            _currentDeadCount++;

            // 현재 웨이브의 모든 적이 죽었는지 체크
            if (_currentWaveIndex < waves.Count)
            {
                if (_currentDeadCount >= waves[_currentWaveIndex].enemiesToSpawn.Count)
                {
                    Debug.Log($"[WaveManager] 웨이브 {_currentWaveIndex + 1} 클리어!");
                    _currentWaveIndex++;
                    StartCoroutine(SpawnWaveRoutine(_currentWaveIndex));
                }
            }
        }

        private void StartBossFight()
        {
            if (boss != null)
            {
                Debug.Log("[WaveManager] 👑 보스전 시작!");
                boss.gameObject.SetActive(true);
                // 보스 입장씬이나 등장 이펙트 추가 가능

                if (bossUI != null) bossUI.SetActive(true);

                // [ToDo] 보스가 죽었을 때 문 열리기 로직 (BossHealth 쪽에 이벤트 연동 요망)
                // 지금은 보스가 없다고 가정하고 클리어 처리
            }
            else
            {
                // 보스가 세팅 안 되어 있으면 그냥 스테이지 클리어
                ClearZone();
            }
        }

        public void ClearZone() // 보스가 죽거나 웨이브가 끝났을 때 외부에서 호출
        {
            if (_isCleared) return;

            Debug.Log("[WaveManager] 모든 웨이브 & 보스 클리어! 🚪✨");
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

            InitializeAllEnemies(); // 전부 숨기고 델리게이트 재연결

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
