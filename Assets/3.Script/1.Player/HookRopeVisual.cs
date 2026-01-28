using UnityEngine;

// [유니] 이제 찰랑거리는 효과 없이, 아주 깔끔하고 빠른 직선 로프야! 📏
// LineRenderer 하나만 써서 성능도 훨씬 좋아!
[RequireComponent(typeof(LineRenderer))]
public class HookRopeVisual : MonoBehaviour
{
    [Header("🎨 Rope Settings")]
    [SerializeField] private int resolution = 20; // 곡선 부드러움 정도 (점의 개수)

    private LineRenderer _lineRenderer;

    private void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        _lineRenderer.positionCount = 0;
        _lineRenderer.enabled = false;
    }

    // [유니] S자 웨이브 그리기! (amp: 진폭, freq: 빈도)
    public void DrawRope(Vector3 startPos, Vector3 endPos, float amp = 0f, float freq = 0f)
    {
        if (!_lineRenderer.enabled)
        {
            _lineRenderer.enabled = true;
        }

        _lineRenderer.positionCount = resolution;

        // 1. 기본 축 계산 (로프 진행 방향의 수직 벡터들 찾기)
        Vector3 direction = (endPos - startPos).normalized;
        
        // 만약 direction이 위쪽이면 Right를, 아니면 Up을 기준으로 수직 벡터 생성
        Vector3 axis = Vector3.Cross(direction, Vector3.up);
        if (axis.sqrMagnitude < 0.001f) axis = Vector3.right; // 수직일 때 예외 처리
        
        Vector3 right = axis.normalized;
        Vector3 up = Vector3.Cross(direction, right).normalized;

        // 2. 웨이브 공식 적용
        float dist = Vector3.Distance(startPos, endPos);
        
        for (int i = 0; i < resolution; i++)
        {
            float t = (float)i / (resolution - 1); // 0.0 ~ 1.0 비율
            
            // 직선 보간 위치 (Linear)
            Vector3 pos = Vector3.Lerp(startPos, endPos, t);

            // [Wave Logic]
            // Envelope: 양 끝점은 고정(0), 가운데가 가장 많이 흔들림(1) -> Sin(PI * t) 사용
            float envelope = Mathf.Sin(t * Mathf.PI);

            // Helix(나선) 또는 Sine Wave 추가
            // 사진처럼 꼬불거리려면 Sine과 Cosine을 섞어서 회전시키는 게 좋음!
            float angle = t * freq * Mathf.PI * 2 + Time.time * 10f; // 시간 더해서 찰랑거림 추가!
            
            Vector3 offset = (right * Mathf.Sin(angle) + up * Mathf.Cos(angle)) * amp * envelope;

            _lineRenderer.SetPosition(i, pos + offset);
        }
    }

    public void ClearRope()
    {
        _lineRenderer.positionCount = 0;
        _lineRenderer.enabled = false;
    }
}
