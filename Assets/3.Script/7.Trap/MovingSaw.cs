using UnityEngine;

namespace Trap
{
    public class MovingSaw : TrapBase
    {
        [Header("Movement")]
        [SerializeField] private Vector3 moveOffset = new Vector3(5, 0, 0);
        [SerializeField] private float speed = 2.0f;
        [SerializeField] private float rotationSpeed = 360f;

        [Header("Delay")]
        [SerializeField] private float waitTime = 0.5f;

        private Vector3 _startPos;
        private Vector3 _endPos;
        private float _timer;

        private void Start()
        {
            _startPos = transform.position;
            _endPos = _startPos + moveOffset;
        }

        private void Update()
        {
            transform.Rotate(Vector3.forward * rotationSpeed * Time.deltaTime);

            float distance = Vector3.Distance(_startPos, _endPos);
            if (distance > 0.01f)
            {
                _timer += Time.deltaTime * speed;

                float t = Mathf.PingPong(_timer, 1.0f);

                t = Mathf.SmoothStep(0f, 1f, t);

                transform.position = Vector3.Lerp(_startPos, _endPos, t);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Vector3 start = Application.isPlaying ? _startPos : transform.position;
            Vector3 end = start + moveOffset;

            Gizmos.DrawLine(start, end);
            Gizmos.DrawWireSphere(start, 0.2f);
            Gizmos.DrawWireSphere(end, 0.2f);
        }
    }
}
