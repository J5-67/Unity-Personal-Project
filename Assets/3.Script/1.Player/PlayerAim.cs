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

        TryGetComponent(out lineRenderer);

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