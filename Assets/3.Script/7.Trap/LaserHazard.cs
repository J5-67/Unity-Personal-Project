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
        [SerializeField] private float knockbackForce = 15f;

        [Header("Portal Style Visual")]
        [SerializeField] private Color laserCoreColor = new Color(1f, 0.9f, 0.9f, 1f);
        [SerializeField] private Color laserGlowColor = new Color(1f, 0.1f, 0.1f, 1f);
        [SerializeField] private float hdrIntensity = 5f;

        private LineRenderer _lineRenderer;

        private void Awake()
        {
            _lineRenderer = GetComponent<LineRenderer>();
            _lineRenderer.positionCount = 2;
            _lineRenderer.useWorldSpace = true;

            _lineRenderer.startWidth = laserThickness;
            _lineRenderer.endWidth = laserThickness;

            SetupLaserVisuals();
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

            foreach (var col in hits)
            {

                if (col.TryGetComponent(out PlayerMovement pm) && pm.IsDashing)
                {

                    continue;
                }

                if (col.TryGetComponent(out PlayerHealth health))
                {

                    if (health.CurrentHealth > damage)
                    {
                        if (col.TryGetComponent(out PlayerMovement playerMove))
                        {
                            if (playerMove.CanMove)
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

                                if (knockbackDir.y < 0.5f)
                                {
                                    knockbackDir.y += 0.8f;
                                }

                                playerMove.ApplyKnockback(knockbackDir.normalized, knockbackForce, 0.25f);
                            }
                        }
                        else if (col.TryGetComponent(out Rigidbody rb))
                        {
                            Vector3 fallbackDir = transform.forward + Vector3.up * 0.5f;
                            rb.linearVelocity = Vector3.zero;
                            rb.AddForce(fallbackDir.normalized * knockbackForce, ForceMode.Impulse);
                        }
                    }

                    if (!health.IsInvincible)
                    {
                        health.TakeDamage(damage);
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
