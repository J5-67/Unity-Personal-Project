using UnityEngine;
namespace Trap
{
    [RequireComponent(typeof(LineRenderer))]
    public class LaserHazard : MonoBehaviour
    {
        [Header("Laser Settings")]
        [SerializeField] private float maxDistance = 50f;
        [Tooltip("레이저의 실질적인 두께 (렌더러 및 충돌 판정)")]
        [SerializeField] private float laserThickness = 0.5f;
        [Tooltip("이 레이저를 통과하지 못하게 막을 지형 레이어(Wall 등)")]
        [SerializeField] private LayerMask obstacleLayer;
        [SerializeField] private LayerMask playerLayer;
        [Header("Damage & Knockback")]
        [SerializeField] private int damage = 1;
        [SerializeField] private float damageKnockbackDelay = 0.5f;
        [SerializeField] private float knockbackForce = 15f;
        [Header("Portal Style Visual")]
        [SerializeField] private Color laserCoreColor = new Color(1f, 0.9f, 0.9f, 1f);
        [SerializeField] private Color laserGlowColor = new Color(1f, 0.1f, 0.1f, 1f);
        [SerializeField] private float texTileSpeed = 2f;
        [SerializeField] private float hdrIntensity = 5f;
        [Header("⏰ Timer Settings")]
        [SerializeField] private bool useTimer = false;
        [SerializeField] private float activeTime = 2f;
        [SerializeField] private float inactiveTime = 2f;
        private LineRenderer _lineRenderer;
        private PlayerMovement _playerMove;
        private bool _isActive = true;
        private float _timer;
        private void Awake()
        {
            _playerMove = FindFirstObjectByType<PlayerMovement>();
            _lineRenderer = GetComponent<LineRenderer>();
            _lineRenderer.positionCount = 2;
            _lineRenderer.useWorldSpace = true;
            _lineRenderer.startWidth = laserThickness;
            _lineRenderer.endWidth = laserThickness;
            SetupLaserVisuals();
            _timer = activeTime;
        }
        private void SetupLaserVisuals()
        {
            if (_lineRenderer == null) return;
            int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBAHalf, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            for (int y = 0; y < size; y++)
            {
                float t = Mathf.Abs((y / (float)(size - 1)) * 2f - 1f); 
                Color pixelColor = Color.clear;
                if (t < 0.2f)
                {
                    pixelColor = laserCoreColor;
                    pixelColor.a = 1f;
                }
                else
                {
                    float glowT = (t - 0.2f) / 0.8f;
                    float alpha = 1f - glowT;
                    alpha = Mathf.Pow(alpha, 1.5f); 
                    pixelColor = laserGlowColor;
                    pixelColor.a = alpha;
                }
                pixelColor.r *= hdrIntensity;
                pixelColor.g *= hdrIntensity;
                pixelColor.b *= hdrIntensity;
                for (int x = 0; x < size; x++)
                {
                    tex.SetPixel(x, y, pixelColor);
                }
            }
            tex.Apply();
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
            Material mat = new Material(shader);
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
            Color whiteAlpha = new Color(1f, 1f, 1f, 1f);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", whiteAlpha);
            if (mat.HasProperty("_TintColor")) mat.SetColor("_TintColor", whiteAlpha);
            mat.SetFloat("_Surface", 1); 
            mat.SetFloat("_Blend", 0);   
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = 3000;
            _lineRenderer.material = mat;
            _lineRenderer.textureMode = LineTextureMode.Stretch;
        }
        private void FixedUpdate()
        {
            if (useTimer)
            {
                _timer -= Time.fixedDeltaTime;
                if (_timer <= 0f)
                {
                    _isActive = !_isActive;
                    _timer = _isActive ? activeTime : inactiveTime;
                    _lineRenderer.enabled = _isActive;
                }
            }
            else
            {
                _isActive = true;
                _lineRenderer.enabled = true;
            }
            if (!_isActive) return;
            float currentDistance = maxDistance;
            int mask = ~(playerLayer.value | (1 << 2));
            if (Physics.Raycast(transform.position, transform.forward, out RaycastHit wallHit, maxDistance, mask, QueryTriggerInteraction.Ignore))
            {
                currentDistance = wallHit.distance;
            }
            Vector3 startPos = transform.position;
            Vector3 endPos = startPos + transform.forward * currentDistance;
            _lineRenderer.SetPosition(0, startPos);
            _lineRenderer.SetPosition(1, endPos);
            Collider[] hits = Physics.OverlapCapsule(startPos, endPos, laserThickness / 2f, playerLayer);
            if (hits.Length == 0 && _playerMove != null && _playerMove.gameObject.activeInHierarchy)
            {
                if (!_playerMove.IsDashing)
                {
                    Vector3 playerPos = _playerMove.transform.position;
                    Vector3 lastPos = _playerMove.LastPosition;
                    if ((playerPos - lastPos).sqrMagnitude > 0.01f)
                    {
                        Vector2 p1 = new Vector2(startPos.z, startPos.y);
                        Vector2 p2 = new Vector2(endPos.z, endPos.y);
                        Vector2 p3 = new Vector2(lastPos.z, lastPos.y);
                        Vector2 p4 = new Vector2(playerPos.z, playerPos.y);
                        float den = (p4.y - p3.y) * (p2.x - p1.x) - (p4.x - p3.x) * (p2.y - p1.y);
                        if (Mathf.Abs(den) > 0.0001f)
                        {
                            float ua = ((p4.x - p3.x) * (p1.y - p3.y) - (p4.y - p3.y) * (p1.x - p3.x)) / den;
                            float ub = ((p2.x - p1.x) * (p1.y - p3.y) - (p2.y - p1.y) * (p1.x - p3.x)) / den;
                            if (ua >= -0.1f && ua <= 1.1f && ub >= 0f && ub <= 1f)
                            {
                                Vector2 intersect2D = p1 + ua * (p2 - p1);
                                Vector3 intersect3D = new Vector3(playerPos.x, intersect2D.y, intersect2D.x);
                                Vector3 moveDir = (playerPos - lastPos).normalized;
                                _playerMove.transform.position = intersect3D - moveDir * (laserThickness * 2f);
                                if (_playerMove.TryGetComponent(out Collider playerCol))
                                {
                                    hits = new Collider[] { playerCol };
                                }
                            }
                        }
                    }
                }
            }
            foreach (var col in hits)
            {
                if (col.TryGetComponent(out PlayerMovement pm) && pm.IsDashing)
                {
                    continue;
                }
                if (col.TryGetComponent(out PlayerHealth health))
                {
                        if (col.TryGetComponent(out PlayerMovement playerMove))
                        {
                            Vector3 lineDir = transform.forward;
                            Vector3 toPlayer = col.transform.position - startPos;
                            float dot = Vector3.Dot(toPlayer, lineDir);
                            Vector3 closestPoint = startPos + lineDir * Mathf.Clamp(dot, 0, currentDistance);
                            Vector3 knockbackDir = (col.transform.position - closestPoint);
                            knockbackDir.x = 0f;
                            if (knockbackDir.sqrMagnitude < 0.01f)
                            {
                                knockbackDir = -playerMove.transform.forward;
                            }
                            knockbackDir = knockbackDir.normalized;
                            Rigidbody rb = playerMove.GetComponent<Rigidbody>();
                            if (rb != null)
                            {
                                float dotVelocity = Vector3.Dot(knockbackDir, rb.linearVelocity.normalized);
                                if (dotVelocity < 0f && rb.linearVelocity.sqrMagnitude > 1f)
                                {
                                    knockbackDir = -rb.linearVelocity.normalized;
                                    knockbackDir.x = 0f;
                                    col.transform.position += knockbackDir * (laserThickness * 2f);
                                }
                            }
                            if (knockbackDir.y < 0.5f)
                            {
                                knockbackDir.y += 0.8f;
                            }
                            playerMove.ApplyKnockback(knockbackDir.normalized, knockbackForce, 0.25f);
                        }
                    if (!health.IsInvincible)
                    {
                        health.TakeDamage(damage, false);
                    }
                }
            }
        }
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Vector3 startPos = transform.position;
            Vector3 endPos = startPos + transform.forward * maxDistance;
            Gizmos.DrawLine(startPos, endPos);
            Gizmos.DrawWireSphere(startPos, laserThickness / 2f);
            Gizmos.DrawWireSphere(endPos, laserThickness / 2f);
        }
    }
}
