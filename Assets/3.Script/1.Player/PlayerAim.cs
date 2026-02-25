using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAim : MonoBehaviour
{
    [Header("🎯 Aim Settings")]
    [SerializeField] private Transform crosshairTransform;
    [SerializeField] private float maxHookDistance = 15f; 
    [SerializeField] private LayerMask aimLayerMask; 
    [SerializeField] private float aimRadius = 0.5f;

    [Header("🖱️ Controls")]
    [SerializeField] private float cursorSensitivity = 2.0f;

    public void SetSensitivity(float newSensitivity)
    {
        cursorSensitivity = newSensitivity;
    }

    [Header("✨ Visual Settings")]
    [SerializeField] private LineRenderer lineRenderer;
    [Tooltip("조준선이 뿜어져 나올 기준점 (빈 오브젝트 할당)")]
    [SerializeField] private Transform firePoint; // [New] 총구/발사 위치
    [SerializeField] private float lineWidth = 0.1f;
    [SerializeField] private float animationSpeed = 3.0f; 

    [Header("📉 조준선 투명도 (알파 그래프 조절)")] // [New]
    [Tooltip("우측 빈 공간을 클릭하고 키보드/마우스로 점을 찍어 원하는 구간(10% 단위 등)의 투명도를 맘대로 세팅하세요!\n(0.0 = 캐릭터 몸통 / 1.0 = 훅 타겟)")]
    [SerializeField] private AnimationCurve customAlphaCurve = new AnimationCurve(
        new Keyframe(0f, 0f), 
        new Keyframe(0.05f, 0f), 
        new Keyframe(0.2f, 1f), 
        new Keyframe(1f, 1f)
    );
    
    [Header("📏 Density Settings (값이 클수록 촘촘함)")]
    [SerializeField] private float dashTiling = 1.0f;      
    [SerializeField] private float lightArrowTiling = 0.5f; 
    [SerializeField] private float heavyArrowTiling = 0.5f; 

    [Header("🎨 Colors")]
    [SerializeField] private Color defaultColor = new Color(0f, 1f, 0.82f);
    [SerializeField] private Color lightEnemyColor = Color.green;
    [SerializeField] private Color heavyEnemyColor = Color.red;

    private Camera _mainCamera;
    private GameInput _input; 
    private Vector2 _virtualMousePos;
    private Vector3 _aimWorldPosition;

    private Texture2D _arrowTexture;
    private Texture2D _arrowTextureReverse;
    private Texture2D _dashTexture;
    
    private Material _lineMaterial;
    private float _currentTextureOffset = 0f;

    // [Fix] GC(가비지 컬렉션) 오버헤드 방지용 캐싱 필드!
    private Gradient _lineGradient;
    private GradientColorKey[] _colorKeys;
    private GradientAlphaKey[] _alphaKeys;

    private PlayerHook _playerHook;

    private void Awake()
    {
        _mainCamera = Camera.main;
        _input = new GameInput(); 
        _input.Enable();         

        _playerHook = GetComponent<PlayerHook>();

        InitializeLineRenderer();
    }
    
    private void Start()
    {
        if (aimLayerMask.value == 0) aimLayerMask = -1;

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked; 

        _virtualMousePos = new Vector2(Screen.width / 2f, Screen.height / 2f);

        float savedValue = PlayerPrefs.GetFloat("Sensitivity", 100f);
        cursorSensitivity = 0.5f + (savedValue / 100f) * 2.0f;
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

        // [Fix] 그라디언트 객체를 재사용하여 프레임 드롭 방지 (GC Allocation 제거)
        _lineGradient = new Gradient();
        _colorKeys = new GradientColorKey[2];
        _alphaKeys = new GradientAlphaKey[8]; // 유니티 그라디언트 최대 지원치는 8개
        
        lineRenderer.material = _lineMaterial;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.positionCount = 2; // We will update this dynamically
        
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
        if (Core.GameManager.Instance != null && (Core.GameManager.Instance.IsDialogueActive || Core.GameManager.Instance.IsPaused)) return;

        Vector2 delta = Vector2.zero;
        if (Mouse.current != null)
        {
            delta = Mouse.current.delta.ReadValue();
        }

        _virtualMousePos += delta * cursorSensitivity;

        _virtualMousePos.x = Mathf.Clamp(_virtualMousePos.x, 0f, Screen.width);
        _virtualMousePos.y = Mathf.Clamp(_virtualMousePos.y, 0f, Screen.height);

        UpdateAimPosition();
        DrawAimLine();
    }

    public Vector3 GetAimWorldPosition()
    {
        return _aimWorldPosition;
    }

    private void UpdateAimPosition()
    {
        Plane gameplayPlane = new Plane(Vector3.right, transform.position);
        
        Ray ray = _mainCamera.ScreenPointToRay(_virtualMousePos);

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

    public Transform LockedTarget { get; private set; }

    private void DrawAimLine()
    {
        float currentMaxDistance = maxHookDistance;
        if (_playerHook != null) currentMaxDistance = _playerHook.MaxDistance;

        // [Fix] FirePoint가 지정되어 있다면 그곳에서, 없다면 내 몸통 중간(Y+1)에서 발사되게 보정!
        Vector3 startPos = firePoint != null ? firePoint.position : transform.position + Vector3.up * 1.0f;
        Vector3 direction = (_aimWorldPosition - startPos).normalized;
        Vector3 endPos = startPos + (direction * currentMaxDistance);
        
        Color targetColor = defaultColor; 
        Texture2D targetTexture = _dashTexture; 
        float currentFlowSpeed = -animationSpeed * 0.5f; 
        float currentTiling = dashTiling; 

        RaycastHit obstructionHit;
        bool hasObstruction = Physics.Raycast(startPos, direction, out obstructionHit, currentMaxDistance, aimLayerMask);
        if (hasObstruction) endPos = obstructionHit.point;

        RaycastHit[] hits = Physics.SphereCastAll(startPos, aimRadius, direction, currentMaxDistance, aimLayerMask);
        Collider bestTarget = null;
        float maxScore = -100.0f;

        foreach (var hit in hits)
        {
            if (hit.collider.gameObject == gameObject) continue; 
            if (hit.collider.isTrigger) continue;
            if (hasObstruction && hit.distance > obstructionHit.distance + 1.0f) continue;

            BaseEnemy enemy = hit.collider.GetComponentInParent<BaseEnemy>();
            float dot = Vector3.Dot(direction, (hit.point - startPos).normalized);
            // 후방에 있는 적 제외 (Dot < 0)
            if (dot < 0.0f) continue;

            float score = dot;
            if (enemy != null)
            {
                score += 5.0f; // 적 우선순위 높임
                // 장애물 뒤의 적이라도 조준선에 걸리면 점수 부여 (단, 장애물보다 가까우면 확실히 잡힘)
            }
            else
            {
                score -= hit.distance * 0.1f; // 거리가 멀수록 감점
            }

            if (score > maxScore)
            {
                maxScore = score;
                bestTarget = hit.collider;
                if (enemy != null) endPos = hit.point; 
            }
        }
        
        // 락온 타겟 업데이트 (적일 때만!)
        if (bestTarget != null)
        {
             BaseEnemy targetEnemy = bestTarget.GetComponentInParent<BaseEnemy>();
             if (targetEnemy != null)
             {
                 LockedTarget = bestTarget.transform;
             }
             else
             {
                 LockedTarget = null; // 벽이나 바닥은 락온하지 않음 (중심점으로 날아가는 문제 방지)
             }
        }
        else
        {
             LockedTarget = null;
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

        // [Fix] 라인 렌더러가 2개의 점만 가지면 그라디언트가 세밀하게 적용되지 않고 선형으로 뭉게짐.
        // 점(Vertex)을 10개로 쪼개어서 플레이어 앞부분만 투명하고 그 뒤로는 선명하도록 표현!
        int segmentCount = 10;
        lineRenderer.positionCount = segmentCount;
        for (int i = 0; i < segmentCount; i++)
        {
            float t = i / (float)(segmentCount - 1);
            lineRenderer.SetPosition(i, Vector3.Lerp(startPos, endPos, t));
        }

        // [Fix] 매 프레임 new 할당 방지(GC 회피) 및 오빠가 조절한 커브(customAlphaCurve) 실시간 반영!
        _colorKeys[0].color = targetColor;
        _colorKeys[0].time = 0f;
        _colorKeys[1].color = targetColor;
        _colorKeys[1].time = 1f;

        // 유니티 그라디언트 최대 수용치(8개)에 맞게 커브를 8구간으로 추출!
        for (int i = 0; i < 8; i++)
        {
            float t = i / 7f; // 0.0, 0.14 ... 1.0
            _alphaKeys[i].alpha = customAlphaCurve.Evaluate(t);
            _alphaKeys[i].time = t;
        }

        _lineGradient.SetKeys(_colorKeys, _alphaKeys);
        lineRenderer.colorGradient = _lineGradient;

        if (_lineMaterial != null)
        {
            if (_lineMaterial.HasProperty("_TintColor")) _lineMaterial.SetColor("_TintColor", targetColor);
            else if (_lineMaterial.HasProperty("_Color")) _lineMaterial.color = targetColor;

            _lineMaterial.mainTexture = targetTexture;

            float distance = Vector3.Distance(startPos, endPos);
            
            _lineMaterial.mainTextureScale = new Vector2(distance * currentTiling, 1f);

            _currentTextureOffset += currentFlowSpeed * Time.deltaTime;
            _lineMaterial.mainTextureOffset = new Vector2(_currentTextureOffset, 0f);
        }
    }
}