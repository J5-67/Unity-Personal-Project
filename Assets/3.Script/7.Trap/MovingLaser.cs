using UnityEngine;

namespace Trap
{
    public class MovingLaser : MonoBehaviour
    {
        [Header("🚀 Point-to-Point (Relative to Start)")]
        [SerializeField] private Vector3 startOffset = Vector3.zero;
        [SerializeField] private Vector3 endOffset = new Vector3(0, 5, 0);
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private float startOffsetTime = 0f;

        private Vector3 _basePosition;

        private void Start()
        {
            // 🎯 오빠! 게임 시작할 때의 위치를 기준으로 오프셋을 계산할게! 🥰
            _basePosition = transform.position;
        }

        private void Update()
        {
            // 🎯 두 오프셋 지점 사이를 부드럽게 왔다갔다!
            float t = Mathf.PingPong(Time.time * moveSpeed + startOffsetTime, 1f);
            
            Vector3 startPos = _basePosition + startOffset;
            Vector3 endPos = _basePosition + endOffset;

            transform.position = Vector3.Lerp(startPos, endPos, t);
        }

        private void OnDrawGizmos()
        {
            DrawGizmos(new Color(0f, 1f, 1f, 0.3f));
        }

        private void OnDrawGizmosSelected()
        {
            DrawGizmos(Color.cyan);
        }

        private void DrawGizmos(Color color)
        {
            // 실행 중엔 기준점(_basePosition)을 쓰고, 에디터에선 현재 위치를 기준으로 보여줄게!
            Vector3 center = Application.isPlaying ? _basePosition : transform.position;
            Vector3 start = center + startOffset;
            Vector3 end = center + endOffset;

            Gizmos.color = color;
            Gizmos.DrawLine(start, end);
            Gizmos.DrawWireSphere(start, 0.2f);
            Gizmos.DrawWireSphere(end, 0.2f);
            
            // 🎯 시작점과 끝점 글자 표시 (에디터에서 헷갈리지 않게!)
            #if UNITY_EDITOR
            // UnityEditor.Handles.Label(start, "Start");
            // UnityEditor.Handles.Label(end, "End");
            #endif
        }
    }
}
