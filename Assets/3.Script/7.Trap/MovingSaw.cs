using UnityEngine;
namespace Trap
{
    public class MovingSaw : TrapBase
    {
        [Header("Movement")]
        [SerializeField] private Vector3 moveOffset = new Vector3(5, 0, 0);
        [SerializeField] private float speed = 2.0f;
        [Header("Rotation")]
        [SerializeField] private Transform visualTransform;
        [SerializeField] private float rotationSpeed = 360f;
        [SerializeField] private Vector3 rotationAxis = Vector3.forward;
        [SerializeField] private Space rotationSpace = Space.Self;
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
            if (visualTransform != null)
            {
                visualTransform.Rotate(rotationAxis * (rotationSpeed * Time.deltaTime), rotationSpace);
            }
            else
            {
                transform.Rotate(rotationAxis * (rotationSpeed * Time.deltaTime), rotationSpace);
            }
            float distance = Vector3.Distance(_startPos, _endPos);
            if (distance > 0.01f)
            {
                _timer += Time.deltaTime * speed;
                float t = Mathf.PingPong(_timer, 1.0f);
                t = Mathf.SmoothStep(0f, 1f, t);
                transform.position = Vector3.Lerp(_startPos, _endPos, t);
            }
        }
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Vector3 start = Application.isPlaying ? _startPos : transform.position;
            Vector3 end = start + moveOffset;
            Gizmos.DrawLine(start, end);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(start, 0.3f);
            Gizmos.DrawWireSphere(end, 0.3f);
        }
    }
}
