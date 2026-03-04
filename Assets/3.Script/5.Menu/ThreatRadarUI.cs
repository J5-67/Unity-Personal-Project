using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Pool;

namespace UI
{
    public class ThreatRadarUI : MonoBehaviour
    {
        [Header("🎯 Radar Settings")]
        [Tooltip("화면에 그려질 경고 화살표(프리팹). UI Image로 만들어주세요.")]
        [SerializeField] private RectTransform indicatorPrefab;

        [Tooltip("레이더가 활동할 캔버스. (보통 부모 Canvas)")]
        [SerializeField] private RectTransform canvasRect;

        [Tooltip("화면 가장자리에서 얼마나 띄울지 (여백)")]
        [SerializeField] private float edgePadding = 50f;

        [Tooltip("위협 탐지 주기 (최적화를 위해 매 프레임 찾지 않고 0.2초마다 스캔!)")]
        [SerializeField] private float scanInterval = 0.2f;

        private Camera _mainCamera;
        private List<Transform> _threats = new List<Transform>();

        private ObjectPool<RectTransform> _indicatorPool;
        private List<RectTransform> _activeIndicators = new List<RectTransform>();

        private void Awake()
        {
            _mainCamera = Camera.main;

            if (canvasRect == null) canvasRect = GetComponent<RectTransform>();

            _indicatorPool = new ObjectPool<RectTransform>(
                createFunc: () => Instantiate(indicatorPrefab, canvasRect),
                actionOnGet: (indicator) => indicator.gameObject.SetActive(true),
                actionOnRelease: (indicator) => indicator.gameObject.SetActive(false),
                actionOnDestroy: (indicator) => Destroy(indicator.gameObject),
                defaultCapacity: 5,
                maxSize: 20
            );

            if (indicatorPrefab != null) indicatorPrefab.gameObject.SetActive(false);

            StartCoroutine(ScanThreatsRoutine());
        }

        private System.Collections.IEnumerator ScanThreatsRoutine()
        {
            while (true)
            {
                _threats.Clear();

                var missiles = FindObjectsByType<EnemyMissile>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                foreach (var m in missiles)
                {

                    if (!m.IsFrozen && !m.IsHacked)
                    {

                        if (Time.timeScale > 0f)
                        {
                            _threats.Add(m.transform);
                        }
                    }
                }

                var enemies = FindObjectsByType<BaseEnemy>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                foreach (var e in enemies)
                {
                    if (e.IsOverloaded && !e.IsDestroyed && !e.IsFrozen)
                    {

                        if (Time.timeScale > 0f)
                        {
                            _threats.Add(e.transform);
                        }
                        continue;
                    }

                    if (e.TryGetComponent(out EnemyShooter shooter))
                    {
                        if (shooter.IsAiming && !e.IsDestroyed && !e.IsFrozen)
                        {

                            if (Time.timeScale > 0f)
                            {
                                _threats.Add(e.transform);
                            }
                        }
                    }
                }

                yield return new WaitForSeconds(scanInterval);
            }
        }

        private void Update()
        {
            if (_mainCamera == null || indicatorPrefab == null) return;

            foreach (var indicator in _activeIndicators)
            {
                _indicatorPool.Release(indicator);
            }
            _activeIndicators.Clear();

            foreach (var threat in _threats)
            {
                if (threat == null) continue;

                Vector3 viewportPos = _mainCamera.WorldToViewportPoint(threat.position);

                bool isBehind = viewportPos.z < 0;

                if (!isBehind && viewportPos.x >= 0f && viewportPos.x <= 1f && viewportPos.y >= 0f && viewportPos.y <= 1f)
                {
                    continue;
                }

                RectTransform indicator = _indicatorPool.Get();
                _activeIndicators.Add(indicator);

                if (isBehind)
                {
                    viewportPos.x = 1f - viewportPos.x;
                    viewportPos.y = 1f - viewportPos.y;
                    viewportPos.z = 0f;

                    viewportPos = Vector3.Max(viewportPos, Vector3.one * -999f);
                }

                Vector2 canvasSize = canvasRect.rect.size;
                Vector2 screenCenterPos = new Vector2(
                    (viewportPos.x - 0.5f) * canvasSize.x,
                    (viewportPos.y - 0.5f) * canvasSize.y
                );

                float angle = Mathf.Atan2(screenCenterPos.y, screenCenterPos.x) * Mathf.Rad2Deg;
                indicator.localRotation = Quaternion.Euler(0, 0, angle);

                float maxX = canvasSize.x / 2f - edgePadding;
                float maxY = canvasSize.y / 2f - edgePadding;

                Vector2 boundedPos = screenCenterPos;
                if (Mathf.Abs(boundedPos.x) > maxX || Mathf.Abs(boundedPos.y) > maxY)
                {
                    float xRatio = maxX / Mathf.Abs(boundedPos.x);
                    float yRatio = maxY / Mathf.Abs(boundedPos.y);
                    float shrinkRatio = Mathf.Min(xRatio, yRatio);

                    boundedPos *= shrinkRatio;
                }

                indicator.anchoredPosition = boundedPos;
            }
        }
    }
}
