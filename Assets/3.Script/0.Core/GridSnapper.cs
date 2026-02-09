using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Core
{
    [ExecuteInEditMode]
    public class GridSnapper : MonoBehaviour
    {
        [Header("📏 Grid Settings")]
        [SerializeField] private bool snapEnabled = true;
        [SerializeField] private Vector3 gridSize = new Vector3(1f, 1f, 1f);
        [SerializeField] private Vector3 offset = Vector3.zero;

        [Header("🛠️ Gizmos")]
        [SerializeField] private bool showGizmos = true;
        [SerializeField] private Color gizmoColor = new Color(1f, 1f, 0f, 0.5f);

#if UNITY_EDITOR
        private void Update()
        {
            if (!snapEnabled || Application.isPlaying) return;

            // 선택된 오브젝트만 스냅 (불필요한 연산 방지)
            if (Selection.activeTransform != transform && !transform.hasChanged) return;

            SnapToGrid();
        }

        private void SnapToGrid()
        {
            Vector3 pos = transform.position;

            float x = Mathf.Round((pos.x - offset.x) / gridSize.x) * gridSize.x + offset.x;
            float y = Mathf.Round((pos.y - offset.y) / gridSize.y) * gridSize.y + offset.y;
            float z = Mathf.Round((pos.z - offset.z) / gridSize.z) * gridSize.z + offset.z;

            transform.position = new Vector3(x, y, z);
        }

        private void OnDrawGizmos()
        {
            if (!showGizmos) return;

            Gizmos.color = gizmoColor;
            
            // 현재 위치를 기준으로 그리드 박스 그리기
            Vector3 center = transform.position;
            Vector3 size = gridSize;
            
            Gizmos.DrawWireCube(center, size);
        }
#endif
    }
}
