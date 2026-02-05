using UnityEngine;
using System.Collections;

namespace Interaction
{
    public class Door : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private Vector3 moveOffset = new Vector3(0, 3, 0);
        [SerializeField] private float duration = 1.0f;
        [SerializeField] private bool isOpen = false;

        private Vector3 _closedPosition;
        private Vector3 _openPosition;
        private Coroutine _currentCoroutine;

        private void Start()
        {
            _closedPosition = transform.position;
            _openPosition = _closedPosition + moveOffset;

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
                
                t = t * t * (3f - 2f * t); 

                transform.position = Vector3.Lerp(startPos, targetPos, t);
                yield return null;
            }

            transform.position = targetPos;
            _currentCoroutine = null;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;

            Vector3 start = transform.position;
            Vector3 end = start + moveOffset;

            Gizmos.DrawLine(start, end);

            Vector3 size = Vector3.one;
            if (TryGetComponent(out Collider col))
            {
                size = col.bounds.size;
            }
            Gizmos.DrawWireCube(end, size);
        }
    }
}
