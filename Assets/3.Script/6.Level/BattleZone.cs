using UnityEngine;
using UnityEngine.Pool;
using System.Collections.Generic;

namespace Level
{
    [System.Serializable]
    public class EnemySpawnInfo
    {
        [Tooltip("스폰할 적의 프리팹 (Hierarchy가 아닌 Project 창에 있는 것)")]
        public BaseEnemy enemyPrefab;
        public Transform point;
    }

    [System.Serializable]
    public class BattleWave
    {
        [Tooltip("이 웨이브가 시작되기 전 대기 시간 (초)")]
        public float delayBeforeWave = 0.5f;
        public List<EnemySpawnInfo> spawnInfos = new List<EnemySpawnInfo>();
    }

    [RequireComponent(typeof(BoxCollider))]
    public class BattleZone : MonoBehaviour
    {
        [Header("⚔️ Battle Waves")]
        [SerializeField] private List<BattleWave> waves = new List<BattleWave>();

        [Header("🚪 Door Reference")]
        [Tooltip("방에 들어올 때 닫힐 문 (선택)")]
        [SerializeField] private GameObject entranceDoorObject;
        [SerializeField] private GameObject exitDoorObject;

        [Header("🎥 Camera Sequence")]
        [Tooltip("클리어 시 문을 비춰줄 시네마틱 카메라 (선택)")]
        [SerializeField] private Unity.Cinemachine.CinemachineCamera doorCamera;
        [SerializeField] private float doorCameraDuration = 1.5f;

        private Interaction.Door _entranceDoor;
        private Interaction.Door _exitDoor;
        private PlayerHealth _playerHealth;
        [SerializeField] private bool resetOnRespawn = true;

        private int _currentWaveIndex = 0;
        private int _currentDeadCount = 0;
        private bool _isCleared = false;
        private bool _isBattleActive = false;

        private Dictionary<BaseEnemy, ObjectPool<BaseEnemy>> _activeEnemyPoolMap = new Dictionary<BaseEnemy, ObjectPool<BaseEnemy>>();

        private static Dictionary<BaseEnemy, ObjectPool<BaseEnemy>> _globalEnemyPools = new Dictionary<BaseEnemy, ObjectPool<BaseEnemy>>();

        private void Start()
        {
            if (entranceDoorObject != null) entranceDoorObject.TryGetComponent(out _entranceDoor);
            if (exitDoorObject != null) exitDoorObject.TryGetComponent(out _exitDoor);

            if (resetOnRespawn && Core.GameManager.Instance != null)
            {
                Core.GameManager.Instance.OnPlayerRespawn += ResetZone;
            }

            if (_entranceDoor != null) _entranceDoor.Open();
            if (_exitDoor != null) _exitDoor.Close();
        }

        private void OnDestroy()
        {
            if (resetOnRespawn && Core.GameManager.Instance != null)
            {
                Core.GameManager.Instance.OnPlayerRespawn -= ResetZone;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_isCleared || _isBattleActive) return;

            if (other.CompareTag("Player"))
            {

                if (_playerHealth == null) other.TryGetComponent(out _playerHealth);
                StartBattle();
            }
        }

        private void StartBattle()
        {
            _isBattleActive = true;
            _currentWaveIndex = 0;

            if (_entranceDoor != null)
            {
                _entranceDoor.Close();

            }

            if (waves.Count > 0)
            {
                StartCoroutine(SpawnWaveRoutine(_currentWaveIndex));
            }
            else
            {
                CheckClearCondition();
            }
        }

        private System.Collections.IEnumerator SpawnWaveRoutine(int waveIndex)
        {
            if (waveIndex >= waves.Count)
            {
                _isCleared = true;
                _isBattleActive = false;
                OpenExitDoor();
                yield break;
            }

            BattleWave currentWave = waves[waveIndex];
            _currentDeadCount = 0;

            if (currentWave.delayBeforeWave > 0f)
            {
                yield return new WaitForSeconds(currentWave.delayBeforeWave);
            }

            foreach (var info in currentWave.spawnInfos)
            {
                if (info.point != null && Core.VFXManager.Instance != null)
                {
                    Core.VFXManager.Instance.PlaySpawnEffect(info.point.position);
                }
            }

            if (currentWave.spawnInfos.Count > 0) yield return new WaitForSeconds(0.5f);

            foreach (var info in currentWave.spawnInfos)
            {
                if (info.enemyPrefab == null || info.point == null) continue;

                var pool = GetOrCreatePool(info.enemyPrefab);
                BaseEnemy enemy = pool.Get();

                enemy.transform.SetPositionAndRotation(info.point.position, info.point.rotation);
                enemy.SetStartTransform(info.point.position, info.point.rotation);

                enemy.ResetEnemy();

                enemy.OnDeath += OnEnemyDeath;

                _activeEnemyPoolMap.Add(enemy, pool);
            }

            if (currentWave.spawnInfos.Count == 0)
            {
                CheckClearCondition();
            }
        }

        private ObjectPool<BaseEnemy> GetOrCreatePool(BaseEnemy prefab)
        {
            if (!_globalEnemyPools.TryGetValue(prefab, out var pool))
            {
                pool = new ObjectPool<BaseEnemy>(
                    createFunc: () =>
                    {
                        var obj = Instantiate(prefab);

                        obj.name = prefab.name;
                        return obj;
                    },
                    actionOnGet: (e) => e.gameObject.SetActive(true),
                    actionOnRelease: (e) =>
                    {
                        e.gameObject.SetActive(false);
                        e.transform.SetParent(null);
                    },
                    actionOnDestroy: (e) => Destroy(e.gameObject),
                    defaultCapacity: 5,
                    maxSize: 30
                );
                _globalEnemyPools[prefab] = pool;
            }
            return pool;
        }

        private void OnEnemyDeath(BaseEnemy enemy)
        {
            enemy.OnDeath -= OnEnemyDeath;

            if (_activeEnemyPoolMap.TryGetValue(enemy, out var pool))
            {
                pool.Release(enemy);
                _activeEnemyPoolMap.Remove(enemy);
            }

            if (_isCleared) return;

            _currentDeadCount++;
            CheckClearCondition();
        }

        private void CheckClearCondition()
        {
            if (_currentWaveIndex < waves.Count)
            {
                var currentWave = waves[_currentWaveIndex];
                if (_currentDeadCount >= currentWave.spawnInfos.Count)
                {
                    _currentWaveIndex++;
                    StartCoroutine(SpawnWaveRoutine(_currentWaveIndex));
                }
            }
            else
            {
                _isBattleActive = false;
                OpenExitDoor();
            }
        }

        private void OpenExitDoor()
        {
            StartCoroutine(ClearSequenceRoutine());
        }

        private GameObject _tempAutoCamObj;

        private System.Collections.IEnumerator ClearSequenceRoutine()
        {

            yield return new WaitForSeconds(0.5f);

            if (_playerHealth != null && _playerHealth.CurrentHealth <= 0)
            {

                yield break;
            }

            _isCleared = true;

            if (Core.GameManager.Instance != null) Core.GameManager.Instance.SetDialogueState(true);

            Unity.Cinemachine.CinemachineCamera activeCam = doorCamera;

            if (activeCam == null && exitDoorObject != null)
            {
                if (_tempAutoCamObj != null) Destroy(_tempAutoCamObj);

                _tempAutoCamObj = new GameObject("Yuni_AutoDoorCamera");
                activeCam = _tempAutoCamObj.AddComponent<Unity.Cinemachine.CinemachineCamera>();

                Vector3 roomCenter = GetComponent<Collider>().bounds.center;
                Vector3 dirToRoom = (roomCenter - exitDoorObject.transform.position).normalized;
                dirToRoom.y = 0;

                if (dirToRoom.sqrMagnitude < 0.1f) dirToRoom = exitDoorObject.transform.forward;

                Vector3 targetCamPos = exitDoorObject.transform.position + dirToRoom * 12.0f + Vector3.up * 3.0f;
                _tempAutoCamObj.transform.position = targetCamPos;

                _tempAutoCamObj.transform.LookAt(exitDoorObject.transform.position + Vector3.up * 1.5f);

                var lens = activeCam.Lens;
                lens.FieldOfView = 70f;
                activeCam.Lens = lens;
            }

            if (activeCam != null) activeCam.Priority = 100;

            yield return new WaitForSeconds(0.8f);

            if (_exitDoor != null)
            {
                _exitDoor.Open();

            }

            yield return new WaitForSeconds(doorCameraDuration);

            if (activeCam != null) activeCam.Priority = 0;

            yield return new WaitForSeconds(0.8f);

            if (Core.GameManager.Instance != null) Core.GameManager.Instance.SetDialogueState(false);

            if (_tempAutoCamObj != null)
            {
                Destroy(_tempAutoCamObj);
                _tempAutoCamObj = null;
            }
        }

        private void ResetZone()
        {

            StopAllCoroutines();

            if (doorCamera != null) doorCamera.Priority = 0;
            if (_tempAutoCamObj != null)
            {
                Destroy(_tempAutoCamObj);
                _tempAutoCamObj = null;
            }
            if (Core.GameManager.Instance != null && Core.GameManager.Instance.IsDialogueActive)
            {
                Core.GameManager.Instance.SetDialogueState(false);
            }

            if (_isCleared) return;

            foreach (var kvp in _activeEnemyPoolMap)
            {
                BaseEnemy enemy = kvp.Key;
                ObjectPool<BaseEnemy> pool = kvp.Value;

                enemy.OnDeath -= OnEnemyDeath;
                if (enemy.gameObject.activeInHierarchy) pool.Release(enemy);
            }
            _activeEnemyPoolMap.Clear();

            _currentWaveIndex = 0;
            _currentDeadCount = 0;
            _isBattleActive = false;

            if (_entranceDoor != null) _entranceDoor.Open();
            if (_exitDoor != null) _exitDoor.Close();
        }

        [ContextMenu("Auto-Create Spawn Points from existing Scene Enemies")]
        private void AutoMigrateEnemies()
        {
            var oldEnemies = GetComponentsInChildren<BaseEnemy>();
            foreach(var e in oldEnemies)
            {
#if UNITY_EDITOR

                var newPoint = new GameObject(e.name + "_SpawnPoint");
                newPoint.transform.position = e.transform.position;
                newPoint.transform.rotation = e.transform.rotation;
                newPoint.transform.SetParent(this.transform);

                if (waves.Count == 0) waves.Add(new BattleWave());

                waves[0].spawnInfos.Add(new EnemySpawnInfo()
                {
                    enemyPrefab = null,
                    point = newPoint.transform
                });

                UnityEditor.Undo.RegisterCreatedObjectUndo(newPoint, "Create Spawn Point");
                UnityEditor.Undo.DestroyObjectImmediate(e.gameObject);
#endif
            }
        }
    }
}
