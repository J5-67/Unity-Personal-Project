using UnityEngine;

// [유니] 이제 찰랑거리는 효과 없이, 아주 깔끔하고 빠른 직선 로프야! 📏
// LineRenderer 하나만 써서 성능도 훨씬 좋아!
[RequireComponent(typeof(LineRenderer))]
public class HookRopeVisual : MonoBehaviour
{
    [Header("🎨 Rope Settings")]
    [SerializeField] private int resolution = 20; // 곡선 부드러움 정도 (점의 개수)
    [SerializeField] private float textureScrollSpeed = 2f; // [NEW] 텍스처가 흐르는 속도
    [SerializeField] private float electricJitter = 0.1f;   // [NEW] 전기처럼 지지직거리는 정도
    [SerializeField] private Gradient ropeGradient;         // [NEW] 로프 색상 그라데이션

    private LineRenderer _lineRenderer;

    private void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        _lineRenderer.positionCount = 0;
        _lineRenderer.enabled = false;
        
        // [유니] 기본 그라데이션 설정 (안 되어 있으면)
        if (ropeGradient == null || ropeGradient.colorKeys.Length == 0)
        {
             // Cyan -> Magenta
             _lineRenderer.startColor = Color.cyan;
             _lineRenderer.endColor = Color.magenta;
        }
        else
        {
            _lineRenderer.colorGradient = ropeGradient;
        }
        
        // 텍스처 모드 설정 (Tile)
        _lineRenderer.textureMode = LineTextureMode.Tile;
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

        // [New] 텍스처 스크롤링 (전기 흐르는 느낌!)
        // 재질이 인스턴스화되지 않게 sharedMaterial 체크
        if (_lineRenderer.sharedMaterial != null)
        {
             float offset = Time.time * textureScrollSpeed;
             _lineRenderer.sharedMaterial.mainTextureOffset = new Vector2(-offset, 0); 
             // *주의* sharedMaterial을 바꾸면 모든 라인렌더러가 같이 변함.
             // 만약 개별로 다르게 하고 싶다면 PropertyBlock을 써야함. 
             // 하지만 플레이어 훅은 하나니까 괜찮아!
        }

        // 2. 웨이브 공식 적용
        for (int i = 0; i < resolution; i++)
        {
            float t = (float)i / (resolution - 1); // 0.0 ~ 1.0 비율
            
            // 직선 보간 위치 (Linear)
            Vector3 pos = Vector3.Lerp(startPos, endPos, t);

            // [Wave Logic]
            // Envelope: 양 끝점은 고정(0), 가운데가 가장 많이 흔들림(1)
            float envelope = Mathf.Sin(t * Mathf.PI);

            // Helix(나선) 또는 Sine Wave 추가
            float angle = t * freq * Mathf.PI * 2 + Time.time * 10f; 
            
            //기본 웨이브
            Vector3 waveOffset = (right * Mathf.Sin(angle) + up * Mathf.Cos(angle)) * amp * envelope;
            
            // [NEW] 전기 지지직 효과 (Random Jitter) ⚡
            // 웨이브가 없을 때도 약간의 떨림을 주면 "살아있는 전선" 같아!
            Vector3 randomJitter = Random.insideUnitSphere * electricJitter * envelope;
            // 팽팽할 때는 지터를 좀 줄여주자 (amp가 낮으면 지터도 낮게?) 아니면 팽팽할 때 더 떨리게?
            // "에너지 과부하" 느낌으로 항상 떨리게 하자!

            _lineRenderer.SetPosition(i, pos + waveOffset + randomJitter);
        }
    }

    public void ClearRope()
    {
        _lineRenderer.positionCount = 0;
        _lineRenderer.enabled = false;
    }
}
