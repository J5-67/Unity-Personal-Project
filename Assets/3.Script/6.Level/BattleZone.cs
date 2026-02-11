using UnityEngine;
using System.Collections.Generic;

namespace Level
{
    public class BattleZone : MonoBehaviour
    {
        [Header("⚔️ Battle Zone Settings")]
        [Tooltip("List of enemies in this zone. Drag them here or use Auto-Find.")]
        [SerializeField] private List<BaseEnemy> enemies = new List<BaseEnemy>();
        
        [Header("🚪 Door Reference")]
        [Tooltip("The Door GameObject attached to the exit gate.")]
        [SerializeField] private GameObject exitDoorObject;
        
        private Interaction.Door _doorComponent;

        [Tooltip("If checked, the door will close again when the player respawns.")]
        [SerializeField] private bool resetOnRespawn = true;

        private int _currentDeadCount = 0;
        private bool _isCleared = false;

        private void Start()
        {
            // GameObject에서 Door 컴포넌트 가져오기 (인스펙터 연결 끊김 방지용)
            if (exitDoorObject != null)
            {
                if (!exitDoorObject.TryGetComponent(out _doorComponent))
                {
                    Debug.LogError("[BattleZone] 'Exit Door Object'에 'Door' 스크립트가 없습니다!");
                }
            }

            foreach (var enemy in enemies)
            {
                if (enemy != null)
                {
                    enemy.OnDeath += OnEnemyDeath;
                }
            }

            if (resetOnRespawn && Core.GameManager.Instance != null)
            {
                Core.GameManager.Instance.OnPlayerRespawn += ResetZone;
            }

            // 초기 상태 설정
            UpdateDoorState();
        }
        
        // ... (OnDestroy and others remain similar, modify usage below)

        private void OnDestroy()
        {
            foreach (var enemy in enemies)
            {
                if (enemy != null)
                {
                    enemy.OnDeath -= OnEnemyDeath;
                }
            }

            if (resetOnRespawn && Core.GameManager.Instance != null)
            {
                Core.GameManager.Instance.OnPlayerRespawn -= ResetZone;
            }
        }

        private void OnEnemyDeath(BaseEnemy enemy)
        {
            if (_isCleared) return;

            _currentDeadCount++;
            CheckClearCondition();
        }

        private void CheckClearCondition()
        {
            if (_currentDeadCount >= enemies.Count)
            {
                _isCleared = true;
                OpenDoor();
            }
        }

        private void OpenDoor()
        {
            if (_doorComponent != null)
            {
                _doorComponent.Open();
                Debug.Log($"[BattleZone] All enemies({_currentDeadCount}) defeated! Door Opening... 🚪✨");
            }
        }

        private void ResetZone()
        {
            _currentDeadCount = 0;
            _isCleared = false;

            if (_doorComponent != null)
            {
                _doorComponent.Close();
            }
        }

        private void UpdateDoorState()
        {
            if (enemies.Count == 0)
            {
                OpenDoor();
            }
            else
            {
                if (_doorComponent != null) _doorComponent.Close();
            }
        }

        // 에디터 편의 기능: 자식 오브젝트에 있는 적들을 자동으로 리스트에 추가
        [ContextMenu("Auto-Find Enemies in Children")]
        private void AutoFindEnemies()
        {
            enemies.Clear();
            enemies.AddRange(GetComponentsInChildren<BaseEnemy>(true));
            Debug.Log($"[BattleZone] Found {enemies.Count} enemies in children.");
        }
    }
}
