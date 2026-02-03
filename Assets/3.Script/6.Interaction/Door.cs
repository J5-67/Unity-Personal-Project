using UnityEngine;
using System.Collections;

namespace Interaction
{
    public class Door : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private Vector3 moveOffset = new Vector3(0, 3, 0); // 열릴 때 이동할 방향과 거리
        [SerializeField] private float duration = 1.0f; // 열리는 데 걸리는 시간
        [SerializeField] private bool isOpen = false;

        private Vector3 _closedPosition;
        private Vector3 _openPosition;
        private Coroutine _currentCoroutine;

        private void Start()
        {
            _closedPosition = transform.position;
            _openPosition = _closedPosition + moveOffset;

            // 시작부터 열려있다면 위치 이동
            if (isOpen) transform.position = _openPosition;
        }

        public void Open()
        {
            if (isOpen) return;
            isOpen = true;
            MoveTo(_openPosition);
        }

        public void Close()
        {
            if (!isOpen) return;
            isOpen = false;
            MoveTo(_closedPosition);
        }

        public void Toggle()
        {
            if (isOpen) Close();
            else Open();
        }

        private void MoveTo(Vector3 targetPos)
        {
            if (_currentCoroutine != null) StopCoroutine(_currentCoroutine);
            _currentCoroutine = StartCoroutine(MoveRoutine(targetPos));
        }

        private IEnumerator MoveRoutine(Vector3 targetPos)
        {
            Vector3 startPos = transform.position;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                // [유니] SmoothStep을 쓰면 처음과 끝이 부드러워져! (Ease-In-Out)
                t = t * t * (3f - 2f * t); 

                transform.position = Vector3.Lerp(startPos, targetPos, t);
                yield return null;
            }

            transform.position = targetPos;
            _currentCoroutine = null;
        }

        // [유니] 에디터에서 이동 경로 미리보기! 👀
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;

            // 현재 위치 (닫힌 상태)
            Vector3 start = transform.position;
            // 목표 위치 (열린 상태)
            Vector3 end = start + moveOffset;

            // 1. 이동 경로 선 그리기
            Gizmos.DrawLine(start, end);

            // 2. 목표 지점에 박스(문 크기) 그리기
            Vector3 size = Vector3.one;
            if (TryGetComponent(out Collider col))
            {
                size = col.bounds.size;
            }
            Gizmos.DrawWireCube(end, size);
        }
    }
}
