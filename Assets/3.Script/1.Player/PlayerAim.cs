using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAim : MonoBehaviour
{
    [Header("Aim Settings")]
    [SerializeField] private Transform crosshairTransform;
    [SerializeField] private float maxHookDistance = 15f;
    [SerializeField] private LayerMask aimLayerMask;

    [Header("Visual Settings")]
    [SerializeField] private LineRenderer lineRenderer;

    private Camera _mainCamera;
    private Vector2 _mouseScreenPosition;
    private Vector3 _aimWorldPosition;
    private bool _isFire; 

    private void Awake()
    {
        _mainCamera = Camera.main;

        // [유니] 중요! 만약 인스펙터에 넣은 LineRenderer가 내 몸통(Player)에 있는 거라면?
        // Hook이랑 같이 쓰게 되니까 갖다 버리고 새로 만들어야 해!
        if (lineRenderer != null && lineRenderer.gameObject == gameObject)
        {
            lineRenderer = null; 
        }

        // [유니] 중요! HookRopeVisual이랑 LineRenderer를 같이 쓰면 충돌나!
        // 그래서 조준선은 따로 자식 오브젝트를 만들어서 관리할게!
        if (lineRenderer == null)
        {
            // 1. AimVisual이라는 자식 오브젝트 만들기
            GameObject aimObj = new GameObject("AimVisual");
            aimObj.transform.SetParent(transform);
            aimObj.transform.localPosition = Vector3.zero;

            // 2. 거기에 LineRenderer 붙이기
            lineRenderer = aimObj.AddComponent<LineRenderer>();
            
            // 3. 기본 설정 (얇은 선)
            lineRenderer.startWidth = 0.05f;
            lineRenderer.endWidth = 0.05f;
            
            // 4. 재질이 없으면 기본 핑크색이 뜨니까, 기본 재질 하나 넣어줄게!
            if (lineRenderer.material == null)
            {
                 lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            }
        }

        lineRenderer.positionCount = 2;
        lineRenderer.enabled = true;
    }

    private void Update()
    {
        UpdateAimPosition();
        DrawAimLine();
    }

    public Vector3 GetAimWorldPosition()
    {
        return _aimWorldPosition;
    }

    public void OnAim(InputAction.CallbackContext context)
    {
        _mouseScreenPosition = context.ReadValue<Vector2>();
    }

    private void UpdateAimPosition()
    {
        Plane gameplayPlane = new Plane(Vector3.right, transform.position);

        Ray ray = _mainCamera.ScreenPointToRay(_mouseScreenPosition);

        if (gameplayPlane.Raycast(ray, out float enterDistance))
        {
            _aimWorldPosition = ray.GetPoint(enterDistance);

            if (crosshairTransform != null)
            {
                crosshairTransform.position = _aimWorldPosition;

                crosshairTransform.rotation = Quaternion.Euler(0, -90, 0);
            }
        }
    }

    private void DrawAimLine()
    {
        Vector3 startPos = transform.position;

        Vector3 direction = (_aimWorldPosition - startPos).normalized;

        bool isHit = Physics.Raycast(startPos, direction, out RaycastHit hitInfo, maxHookDistance, aimLayerMask);

        Vector3 endPos;

        if (isHit)
        {
            endPos = hitInfo.point;

            // [유니] 나중에 여기에 '닿았다'는 표시(작은 원)를 띄우면 더 좋아!
        }
        else
        {
            endPos = startPos + (direction * maxHookDistance);
        }

        lineRenderer.SetPosition(0, startPos);
        lineRenderer.SetPosition(1, endPos);
    }
}