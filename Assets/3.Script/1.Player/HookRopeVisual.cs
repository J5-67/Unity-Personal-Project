using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class HookRopeVisual : MonoBehaviour
{
    [Header("🎨 Rope Settings")]
    [SerializeField] private int resolution = 20;
    [SerializeField] private float textureScrollSpeed = 2f;
    [SerializeField] private float electricJitter = 0.1f;
    [SerializeField] private Gradient ropeGradient;

    private LineRenderer _lineRenderer;

    private void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        _lineRenderer.positionCount = 0;
        _lineRenderer.enabled = false;

        if (ropeGradient == null || ropeGradient.colorKeys.Length == 0)
        {
             _lineRenderer.startColor = Color.cyan;
             _lineRenderer.endColor = Color.magenta;
        }
        else
        {
            _lineRenderer.colorGradient = ropeGradient;
        }

        _lineRenderer.textureMode = LineTextureMode.Tile;
    }

    public void DrawRope(Vector3 startPos, Vector3 endPos, float amp = 0f, float freq = 0f)
    {
        if (!_lineRenderer.enabled)
        {
            _lineRenderer.enabled = true;
        }

        _lineRenderer.positionCount = resolution;

        Vector3 direction = (endPos - startPos).normalized;

        Vector3 axis = Vector3.Cross(direction, Vector3.up);
        if (axis.sqrMagnitude < 0.001f) axis = Vector3.right;

        Vector3 right = axis.normalized;
        Vector3 up = Vector3.Cross(direction, right).normalized;

        if (_lineRenderer.sharedMaterial != null)
        {
             float offset = Time.time * textureScrollSpeed;
             _lineRenderer.sharedMaterial.mainTextureOffset = new Vector2(-offset, 0);
        }

        for (int i = 0; i < resolution; i++)
        {
            float t = (float)i / (resolution - 1);

            Vector3 pos = Vector3.Lerp(startPos, endPos, t);

            float envelope = Mathf.Sin(t * Mathf.PI);

            float angle = t * freq * Mathf.PI * 2 + Time.time * 10f;

            Vector3 waveOffset = (right * Mathf.Sin(angle) + up * Mathf.Cos(angle)) * amp * envelope;

            Vector3 randomJitter = Random.insideUnitSphere * electricJitter * envelope;

            _lineRenderer.SetPosition(i, pos + waveOffset + randomJitter);
        }
    }

    public void ClearRope()
    {
        _lineRenderer.positionCount = 0;
        _lineRenderer.enabled = false;
    }
}
