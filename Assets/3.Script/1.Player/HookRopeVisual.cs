using UnityEngine;

// [유니] 이제 찰랑거리는 효과 없이, 아주 깔끔하고 빠른 직선 로프야! 📏
// LineRenderer 하나만 써서 성능도 훨씬 좋아!
[RequireComponent(typeof(LineRenderer))]
public class HookRopeVisual : MonoBehaviour
{
    private LineRenderer _lineRenderer;

    private void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        _lineRenderer.positionCount = 0; // 평소엔 숨김
        _lineRenderer.enabled = false;
    }

    // [유니] 시작점과 끝점을 이어서 직선 그리기
    public void DrawRope(Vector3 startPos, Vector3 endPos)
    {
        if (!_lineRenderer.enabled)
        {
            _lineRenderer.enabled = true;
            _lineRenderer.positionCount = 2; // 점 2개면 직선 완성!
        }

        _lineRenderer.SetPosition(0, startPos);
        _lineRenderer.SetPosition(1, endPos);
    }

    // [유니] 로프 지우기
    public void ClearRope()
    {
        _lineRenderer.positionCount = 0;
        _lineRenderer.enabled = false;
    }
}
