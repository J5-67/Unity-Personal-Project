using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAim : MonoBehaviour
{
    [Header("🎯 Aim Settings")]
    [SerializeField] private Transform crosshairTransform;
    [SerializeField] private float maxHookDistance = 15f;
    [SerializeField] private LayerMask aimLayerMask; 
    [SerializeField] private float aimRadius = 0.5f;

    [Header("✨ Visual Settings")]
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private float lineWidth = 0.1f;
    [SerializeField] private float animationSpeed = 3.0f; 
    
    [Header("📏 Density Settings (값이 클수록 촘촘함)")]
    [SerializeField] private float dashTiling = 1.0f;      
    [SerializeField] private float lightArrowTiling = 0.5f; 
    [SerializeField] private float heavyArrowTiling = 0.5f; 

    [Header("🎨 Colors")]
    [SerializeField] private Color defaultColor = new Color(0f, 1f, 0.82f); // 민트색
    [SerializeField] private Color lightEnemyColor = Color.green;           // 가벼운 적
    [SerializeField] private Color heavyEnemyColor = Color.red;             // 무거운 적

    private Camera _mainCamera;
    private GameInput _input; 
    private Vector2 _mouseScreenPosition;
    private Vector3 _aimWorldPosition;

    private Texture2D _arrowTexture;        // >
    private Texture2D _arrowTextureReverse; // <
    private Texture2D _dashTexture;         // -
    
    private Material _lineMaterial;
    private float _currentTextureOffset = 0f;

    private void Awake()
    {
        _mainCamera = Camera.main;
        _input = new GameInput(); 
        _input.Enable();         
        _input.Player.Aim.performed += OnAim;

        InitializeLineRenderer();
    }
    
    private void Start()
    {
        if (aimLayerMask.value == 0) aimLayerMask = -1;
    }

    private void OnEnable() => _input?.Enable();
    private void OnDisable() => _input?.Disable();

    private void InitializeLineRenderer()
    {
        if (lineRenderer != null && lineRenderer.gameObject == gameObject)
        {
            lineRenderer = null;
        }

        if (lineRenderer == null)
        {
            Transform existingChild = transform.Find("AimVisual");
            if (existingChild != null)
            {
                lineRenderer = existingChild.GetComponent<LineRenderer>();
            }
            else
            {
                GameObject aimObj = new GameObject("AimVisual");
                aimObj.transform.SetParent(transform);
                aimObj.transform.localPosition = Vector3.zero;
                aimObj.transform.localRotation = Quaternion.identity;
                lineRenderer = aimObj.AddComponent<LineRenderer>();
            }
        }

        GenerateArrowTexture();
        GenerateReverseArrowTexture();
        GenerateDashTexture();

        Shader shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
        if(shader == null) shader = Shader.Find("Particles/Alpha Blended"); 
        if(shader == null) shader = Shader.Find("Mobile/Particles/Alpha Blended"); 
        
        _lineMaterial = new Material(shader);
        
        lineRenderer.material = _lineMaterial;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.positionCount = 2;
        
        // [유니] Stretch 모드로 변경! 
        // 우리가 코드로 (거리 * tiling)을 계산해서 넣어줄 거니까, Unity는 0~1로 펴주기만 하면 됨!
        // Tile 모드면 Unity가 멋대로 반복해서 우리의 계산이랑 충돌남.
        lineRenderer.textureMode = LineTextureMode.Stretch; 
        lineRenderer.enabled = true;
    }

    private void GenerateArrowTexture()
    {
        int size = 64;
        _arrowTexture = CreateBaseTexture(size);
        int center = size / 2;
        int thickness = 4;
        for (int x = 10; x < 54; x++)
        {
            int distFromTip = 54 - x;
            int spread = distFromTip / 2;
            DrawPixelBlock(_arrowTexture, x, center + spread, thickness, Color.white);
            DrawPixelBlock(_arrowTexture, x, center - spread, thickness, Color.white);
        }
        _arrowTexture.Apply();
    }

    private void GenerateReverseArrowTexture()
    {
        int size = 64;
        _arrowTextureReverse = CreateBaseTexture(size);
        int center = size / 2;
        int thickness = 4;
        for (int x = 10; x < 54; x++)
        {
            int distFromTip = x - 10;
            int spread = distFromTip / 2;
            DrawPixelBlock(_arrowTextureReverse, x, center + spread, thickness, Color.white);
            DrawPixelBlock(_arrowTextureReverse, x, center - spread, thickness, Color.white);
        }
        _arrowTextureReverse.Apply();
    }

    private void GenerateDashTexture()
    {
        int size = 64;
        _dashTexture = CreateBaseTexture(size);
        int center = size / 2;
        int thickness = 10; 
        int width = 32;     
        int startX = (size - width) / 2;
        for (int x = startX; x < startX + width; x++)
        {
             DrawPixelBlock(_dashTexture, x, center, thickness, Color.white);
        }
        _dashTexture.Apply();
    }

    private Texture2D CreateBaseTexture(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Repeat;
        Color[] pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;
        tex.SetPixels(pixels);
        return tex;
    }

    private void DrawPixelBlock(Texture2D tex, int x, int y, int size, Color color)
    {
        for(int i=0; i<size; i++)
            for(int j=0; j<size; j++)
                if(x+i < 64 && y+j < 64 && x+i>=0 && y+j>=0) 
                    tex.SetPixel(x+i, y+j, color);
    }

    private void Update()
    {
        UpdateAimPosition();
        DrawAimLine();
    }

    public void OnAim(InputAction.CallbackContext context)
    {
        _mouseScreenPosition = context.ReadValue<Vector2>();
    }

    public Vector3 GetAimWorldPosition()
    {
        return _aimWorldPosition;
    }

    private void UpdateAimPosition()
    {
        Plane gameplayPlane = new Plane(Vector3.right, transform.position);
        Ray ray = _mainCamera.ScreenPointToRay(_mouseScreenPosition);

        if (gameplayPlane.Raycast(ray, out float enterDistance))
        {
            _aimWorldPosition = ray.GetPoint(enterDistance);
            _aimWorldPosition.x = transform.position.x; 

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
        Vector3 endPos = startPos + (direction * maxHookDistance);
        
        Color targetColor = defaultColor; 
        Texture2D targetTexture = _dashTexture; 
        float currentFlowSpeed = -animationSpeed * 0.5f; 
        float currentTiling = dashTiling; 

        RaycastHit obstructionHit;
        bool hasObstruction = Physics.Raycast(startPos, direction, out obstructionHit, maxHookDistance, aimLayerMask);
        if (hasObstruction) endPos = obstructionHit.point;

        RaycastHit[] hits = Physics.SphereCastAll(startPos, aimRadius, direction, maxHookDistance, aimLayerMask);
        Collider bestTarget = null;
        float maxScore = -100.0f;

        foreach (var hit in hits)
        {
            if (hit.collider.gameObject == gameObject) continue; 
            if (hit.collider.isTrigger) continue;
            if (hasObstruction && hit.distance > obstructionHit.distance + 1.0f) continue;

            BaseEnemy enemy = hit.collider.GetComponentInParent<BaseEnemy>();
            float dot = Vector3.Dot(direction, (hit.point - startPos).normalized);
            if (dot < 0.0f) continue;

            float score = dot;
            if (enemy != null)
            {
                score += 5.0f;
                if (hasObstruction && (obstructionHit.collider == hit.collider || obstructionHit.collider.transform.root == hit.collider.transform.root))
                    score += 5.0f;
            }
            else
            {
                score -= hit.distance * 0.1f;
            }

            if (score > maxScore)
            {
                maxScore = score;
                bestTarget = hit.collider;
                if (enemy != null) endPos = hit.point; 
            }
        }

        if (bestTarget != null)
        {
            BaseEnemy targetEnemy = bestTarget.GetComponentInParent<BaseEnemy>();

            if (targetEnemy != null)
            {
                if (targetEnemy.IsFrozen)
                {
                    targetColor = defaultColor;
                    targetTexture = _dashTexture;
                    currentFlowSpeed = 0f; 
                    currentTiling = dashTiling; 
                }
                else if (targetEnemy.Type == EnemyType.Light)
                {
                    targetColor = lightEnemyColor;
                    targetTexture = _arrowTextureReverse; 
                    currentFlowSpeed = animationSpeed; 
                    currentTiling = lightArrowTiling; 
                }
                else
                {
                    targetColor = heavyEnemyColor;
                    targetTexture = _arrowTexture;
                    currentFlowSpeed = -animationSpeed; 
                    currentTiling = heavyArrowTiling; 
                }
            }
            else
            {
                targetColor = defaultColor;
                targetTexture = _dashTexture;
                currentFlowSpeed = -animationSpeed * 0.5f; 
                currentTiling = dashTiling; 
            }
        }

        lineRenderer.SetPosition(0, startPos);
        lineRenderer.SetPosition(1, endPos);

        if (_lineMaterial != null)
        {
            if (_lineMaterial.HasProperty("_TintColor")) _lineMaterial.SetColor("_TintColor", targetColor);
            else if (_lineMaterial.HasProperty("_Color")) _lineMaterial.color = targetColor;

            _lineMaterial.mainTexture = targetTexture;

            float distance = Vector3.Distance(startPos, endPos);
            
            // [유니] 이제 Stretch 모드이므로, 우리가 직접 계산한 (거리 * tiling)이 곧 전체 반복 횟수가 됨!
            // 거리가 멀면 -> 반복 횟수가 많아짐 -> 간격 일정함!
            _lineMaterial.mainTextureScale = new Vector2(distance * currentTiling, 1f);

            _currentTextureOffset += currentFlowSpeed * Time.deltaTime;
            _lineMaterial.mainTextureOffset = new Vector2(_currentTextureOffset, 0f);
        }
    }
}