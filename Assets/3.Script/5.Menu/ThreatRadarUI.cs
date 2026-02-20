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
        
        // [최적화] Instantiate를 밥먹듯이 하지 않기 위한 UI 풀링!
        private ObjectPool<RectTransform> _indicatorPool;
        private List<RectTransform> _activeIndicators = new List<RectTransform>();

        private void Awake()
        {
            _mainCamera = Camera.main;

            if (canvasRect == null) canvasRect = GetComponent<RectTransform>();

            // UI 화살표 오브젝트 풀 초기화
            _indicatorPool = new ObjectPool<RectTransform>(
                createFunc: () => Instantiate(indicatorPrefab, canvasRect),
                actionOnGet: (indicator) => indicator.gameObject.SetActive(true),
                actionOnRelease: (indicator) => indicator.gameObject.SetActive(false),
                actionOnDestroy: (indicator) => Destroy(indicator.gameObject),
                defaultCapacity: 5,
                maxSize: 20
            );
            
            // 프리팹 원본은 화면에 안 보이게 꺼두기
            if (indicatorPrefab != null) indicatorPrefab.gameObject.SetActive(false);

            // Update에서 무거운 Find()를 쓰지 않기 위해 코루틴으로 탐지!
            StartCoroutine(ScanThreatsRoutine());
        }

        private System.Collections.IEnumerator ScanThreatsRoutine()
        {
            while (true)
            {
                _threats.Clear();

                // 1. 날아오고 있는 적 미사일 찾기
                var missiles = FindObjectsByType<EnemyMissile>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                foreach (var m in missiles)
                {
                    // 얼어있거나, 이미 해킹해서 보스한테 날아가는 내 편 미사일은 위협이 알림 안 띄움!
                    if (!m.IsFrozen && !m.IsHacked)
                    {
                        _threats.Add(m.transform);
                    }
                }

                // 2. 자폭 모드(카미카제) 켜진 소형 적 찾기
                var enemies = FindObjectsByType<BaseEnemy>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                foreach (var e in enemies)
                {
                    if (e.IsOverloaded && !e.IsDestroyed) 
                    {
                        _threats.Add(e.transform);
                        continue; // 자폭 카운트에 들어갔으면 조준 체크는 건너뜀 (중복 방지)
                    }

                    // 3. [New] 나를 인식하고 조준(레이저) 중인 적도 포함!
                    if (e.TryGetComponent(out EnemyShooter shooter))
                    {
                        if (shooter.IsAiming && !e.IsDestroyed && !e.IsFrozen)
                        {
                            _threats.Add(e.transform);
                        }
                    }
                }

                // 지정된 시간만큼 쉬었다가 다시 스캔 (매 프레임 X -> 가비지/프레임 방어)
                yield return new WaitForSeconds(scanInterval);
            }
        }

        private void Update()
        {
            if (_mainCamera == null || indicatorPrefab == null) return;

            // 저번 프레임에 켜놨던 화살표들 전부 일단 수거! (빛의 속도로 Pool에 반납)
            foreach (var indicator in _activeIndicators)
            {
                _indicatorPool.Release(indicator);
            }
            _activeIndicators.Clear();

            // 이번 스캔으로 잡힌 위협들 처리
            foreach (var threat in _threats)
            {
                if (threat == null) continue; // 그사이에 터진 거면 무시

                // 타겟의 3D 월드 좌표를 2D 화면 뷰포트 좌표(0~1)로 변환
                Vector3 viewportPos = _mainCamera.WorldToViewportPoint(threat.position);

                // Z값이 0보다 작다? = 카메라 뒤통수에 있다
                bool isBehind = viewportPos.z < 0;

                // 화면 안에(0~1) 들어와 있고 카메라 앞(z>0)이면 = "눈에 뻔히 보임" => 화살표 안 띄움!
                if (!isBehind && viewportPos.x >= 0f && viewportPos.x <= 1f && viewportPos.y >= 0f && viewportPos.y <= 1f)
                {
                    continue;
                }

                // ------------------ 화면 밖에 있다!! 레이더 발동! ------------------
                RectTransform indicator = _indicatorPool.Get();
                _activeIndicators.Add(indicator);

                // 카메라 뒤통수에 있다면 화면 아래쪽/바깥쪽으로 튕겨나가게 좌표를 뒤집어줌
                if (isBehind)
                {
                    viewportPos.x = 1f - viewportPos.x;
                    viewportPos.y = 1f - viewportPos.y;
                    viewportPos.z = 0f;
                    
                    // 화면 바깥으로 강력하게 튕겨내서 가장자리에 붙게 만듦
                    viewportPos = Vector3.Max(viewportPos, Vector3.one * -999f); 
                }

                // 스크린 중앙을 (0,0)으로 하는 로컬 좌표계로 변환 (캔버스가 화면 전체라고 가정)
                // Viewport (0~1) -> Canvas Local (-width/2 ~ width/2)
                Vector2 canvasSize = canvasRect.rect.size;
                Vector2 screenCenterPos = new Vector2(
                    (viewportPos.x - 0.5f) * canvasSize.x,
                    (viewportPos.y - 0.5f) * canvasSize.y
                );

                // 화살표가 위협을 바라보도록 뱅르르 회전 (Atan2 수학 공식!)
                float angle = Mathf.Atan2(screenCenterPos.y, screenCenterPos.x) * Mathf.Rad2Deg;
                indicator.localRotation = Quaternion.Euler(0, 0, angle);

                // 화살표가 화면 테두리를 뚫고 나가지 않게 가두리 양식 (Clamp)
                float maxX = canvasSize.x / 2f - edgePadding;
                float maxY = canvasSize.y / 2f - edgePadding;

                // X, Y 비율을 계산해서 화면 모서리에 이쁘게 착! 달라붙게 만듦
                Vector2 boundedPos = screenCenterPos;
                if (Mathf.Abs(boundedPos.x) > maxX || Mathf.Abs(boundedPos.y) > maxY)
                {
                    float xRatio = maxX / Mathf.Abs(boundedPos.x);
                    float yRatio = maxY / Mathf.Abs(boundedPos.y);
                    float shrinkRatio = Mathf.Min(xRatio, yRatio); // 둘 중 더 작게 줄여야 하는 비율 선택

                    boundedPos *= shrinkRatio;
                }

                // 최종 계산된 가장자리 찰떡 좌표 대입!
                indicator.anchoredPosition = boundedPos;
            }
        }
    }
}
