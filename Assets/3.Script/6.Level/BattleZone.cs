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
        [Tooltip("적이 스폰될 위치를 지정할 빈 게임오브젝트")]
        public Transform point;
    }

    [System.Serializable]
    public class BattleWave
    {
        [Tooltip("이 웨이브가 시작되기 전 대기 시간 (초)")]
        public float delayBeforeWave = 0.5f;
        [Tooltip("스폰할 적 목록")]
        public List<EnemySpawnInfo> spawnInfos = new List<EnemySpawnInfo>();
    }

    [RequireComponent(typeof(BoxCollider))]
    public class BattleZone : MonoBehaviour
    {
        [Header("⚔️ Battle Waves")]
        [Tooltip("웨이브 목록. 1개만 넣으면 일반 방, 여러 개 넣으면 웨이브 방!")]
        [SerializeField] private List<BattleWave> waves = new List<BattleWave>();
        
        [Header("🚪 Door Reference")]
        [Tooltip("방에 들어올 때 닫힐 문 (선택)")]
        [SerializeField] private GameObject entranceDoorObject;
        [Tooltip("방을 클리어하면 열릴 문")]
        [SerializeField] private GameObject exitDoorObject;
        
        private Interaction.Door _entranceDoor;
        private Interaction.Door _exitDoor;

        [Tooltip("If checked, the zone will reset when the player respawns.")]
        [SerializeField] private bool resetOnRespawn = true;

        private int _currentWaveIndex = 0;
        private int _currentDeadCount = 0;
        private bool _isCleared = false;
        private bool _isBattleActive = false;

        // 현재 방에서 활성화된 몬스터들과 그들이 나온 풀(Pool)의 매핑 테이블
        private Dictionary<BaseEnemy, ObjectPool<BaseEnemy>> _activeEnemyPoolMap = new Dictionary<BaseEnemy, ObjectPool<BaseEnemy>>();

        // 씬 전체에서 프리팹 기준으로 공유하는 몬스터 풀!! (메모리 최적화의 핵심)
        private static Dictionary<BaseEnemy, ObjectPool<BaseEnemy>> _globalEnemyPools = new Dictionary<BaseEnemy, ObjectPool<BaseEnemy>>();

        private void Start()
        {
            if (entranceDoorObject != null) entranceDoorObject.TryGetComponent(out _entranceDoor);
            if (exitDoorObject != null) exitDoorObject.TryGetComponent(out _exitDoor);

            if (resetOnRespawn && Core.GameManager.Instance != null)
            {
                Core.GameManager.Instance.OnPlayerRespawn += ResetZone;
            }

            // 첫 상태 (입구는 열려있고, 출구는 닫혀있음)
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
                StartBattle();
            }
        }

        private void StartBattle()
        {
            _isBattleActive = true;
            _currentWaveIndex = 0;

            // 입구 문 닫기 (플레이어 가두기!)
            if (_entranceDoor != null)
            {
                _entranceDoor.Close();
                Debug.Log($"[BattleZone] Battle Started! Entrance Locked 🔒");
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

            // 파티클(모이는 빛) 먼저 보여주기
            foreach (var info in currentWave.spawnInfos)
            {
                if (info.point != null && Core.VFXManager.Instance != null)
                {
                    Core.VFXManager.Instance.PlaySpawnEffect(info.point.position);
                }
            }
            
            // 0.5초 대기 후 소환!
            if (currentWave.spawnInfos.Count > 0) yield return new WaitForSeconds(0.5f);

            // 적들 스폰
            foreach (var info in currentWave.spawnInfos)
            {
                if (info.enemyPrefab == null || info.point == null) continue;

                var pool = GetOrCreatePool(info.enemyPrefab);
                BaseEnemy enemy = pool.Get();
                
                // 위치 지정 및 시작 위치 동기화! (풀에서 나왔으므로 새로운 위치를 _startPos로 덮어씌움)
                enemy.transform.SetPositionAndRotation(info.point.position, info.point.rotation);
                enemy.SetStartTransform(info.point.position, info.point.rotation);
                
                enemy.ResetEnemy(); // 완벽한 전투 준비 상태로 초기화

                // 이벤트 등록
                enemy.OnDeath += OnEnemyDeath;
                
                // 어떤 풀에서 나왔는지 기억해둠 (나중에 돌려주려고)
                _activeEnemyPoolMap.Add(enemy, pool);
            }
            
            // 만약 적이 하나도 없으면 바로 다음 조건 체크
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
                        // 프리팹을 만들 때 클론의 이름이 너무 지저분해지지 않도록
                        obj.name = prefab.name;
                        return obj;
                    },
                    actionOnGet: (e) => e.gameObject.SetActive(true), 
                    actionOnRelease: (e) => 
                    {
                        e.gameObject.SetActive(false);
                        e.transform.SetParent(null); // 혹시나 계층이 꼬이는 것 방지
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
            
            // 풀에 반납
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
                _isCleared = true;
                _isBattleActive = false;
                OpenExitDoor();
            }
        }

        private void OpenExitDoor()
        {
            if (_exitDoor != null)
            {
                _exitDoor.Open();
                Debug.Log($"[BattleZone] Room Cleared! Door Opening... 🚪✨");
            }
        }

        private void ResetZone()
        {
            if (_isCleared) return; 

            StopAllCoroutines();

            // 플레이어가 죽으면 방 안의 몬스터들 싹 다 풀로 반납하고 방 초기화
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
                // 원래 있던 애들은 지우고 스폰 포인트로 대체 (에디터 전용)
                var newPoint = new GameObject(e.name + "_SpawnPoint");
                newPoint.transform.position = e.transform.position;
                newPoint.transform.rotation = e.transform.rotation;
                newPoint.transform.SetParent(this.transform);
                
                if (waves.Count == 0) waves.Add(new BattleWave());

                waves[0].spawnInfos.Add(new EnemySpawnInfo()
                {
                    enemyPrefab = null, // 오빠가 직접 인스펙터에서 프리팹을 넣어줘야 해!
                    point = newPoint.transform
                });

                UnityEditor.Undo.RegisterCreatedObjectUndo(newPoint, "Create Spawn Point");
                UnityEditor.Undo.DestroyObjectImmediate(e.gameObject);
#endif
            }
        }
    }
}
