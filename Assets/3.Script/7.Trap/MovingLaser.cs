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
            _basePosition = transform.position;
        }
        private void Update()
        {
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
            Vector3 center = Application.isPlaying ? _basePosition : transform.position;
            Vector3 start = center + startOffset;
            Vector3 end = center + endOffset;
            Gizmos.color = color;
            Gizmos.DrawLine(start, end);
            Gizmos.DrawWireSphere(start, 0.2f);
            Gizmos.DrawWireSphere(end, 0.2f);
            #if UNITY_EDITOR
            #endif
        }
    }
}
