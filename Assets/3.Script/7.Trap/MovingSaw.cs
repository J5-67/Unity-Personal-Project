using UnityEngine;

namespace Trap
{
    // [유니] 왔다갔다 움직이는 무서운 톱날! ⚙️
    public class MovingSaw : TrapBase
    {
        [Header("Movement")]
        [SerializeField] private Vector3 moveOffset = new Vector3(5, 0, 0); // 이동 거리
        [SerializeField] private float speed = 2.0f; // 왕복 속도
        [SerializeField] private float rotationSpeed = 360f; // 회전 속도 (도/초)
        
        [Header("Delay")]
        [SerializeField] private float waitTime = 0.5f; // 끝에서 대기 시간

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
            // 1. 제자리 회전 (윙윙 돌아가는 비주얼)
            transform.Rotate(Vector3.forward * rotationSpeed * Time.deltaTime);

            // 2. 왕복 이동 (PingPong)
            // Mathf.PingPong은 0 -> Length -> 0 으로 값을 왕복시켜줌!
            float distance = Vector3.Distance(_startPos, _endPos);
            if (distance > 0.01f) // 이동 거리가 있을 때만
            {
                // 시간 흐름 (속도 적용)
                _timer += Time.deltaTime * speed;
                
                // PingPong을 이용해서 0~1 사이의 값(t)을 만듦 (거리를 1로 정규화)
                // 거리 5를 속도 2로 가면 2.5초 걸림.
                // PingPong(시간, 1) -> 0 ~ 1 반복
                float t = Mathf.PingPong(_timer, 1.0f);

                // 부드러운 움직임 (Ease In Out)
                t = Mathf.SmoothStep(0f, 1f, t);

                // 위치 보간
                transform.position = Vector3.Lerp(_startPos, _endPos, t);
            }
        }

        // [유니] 에디터에서 이동 경로 미리보기!
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
