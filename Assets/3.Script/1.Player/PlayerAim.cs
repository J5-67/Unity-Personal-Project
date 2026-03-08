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
    [SerializeField] private Transform firePoint;
    [SerializeField] private float lineWidth = 0.1f;
    [SerializeField] private float animationSpeed = 3.0f;

    [Header("📉 조준선 투명도 (알파 그래프 조절)")]
    [Tooltip("우측 빈 공간을 클릭하고 키보드/마우스로 점을 찍어 원하는 구간(10% 단위 등)의 투명도를 맘대로 세팅하세요!\n(0.0 = 캐릭터 몸통 / 1.0 = 훅 타겟)")]
    [SerializeField]
    private AnimationCurve customAlphaCurve = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.05f, 0f),
        new Keyframe(0.2f, 1f),
        new Keyframe(1f, 1f)
    );

    [Header("📏 Density Settings (값이 클수록 촘촘함)")]
    [SerializeField] private float dashTiling = 1.0f;
    [SerializeField] private float enemyArrowTiling = 0.5f;

    [Header("🎨 Colors")]
    [SerializeField] private Color defaultColor = new Color(0f, 1f, 0.82f);
    [SerializeField] private Color enemyColor = Color.red;
    [SerializeField] private Color hookableColor = Color.yellow;

    private Camera _mainCamera;
    private GameInput _input;
    private Vector2 _virtualMousePos;
    private Vector3 _aimWorldPosition;

    private Texture2D _arrowTexture;
    private Texture2D _arrowTextureReverse;
    private Texture2D _dashTexture;

    private Material _lineMaterial;
    private float _currentTextureOffset = 0f;

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

        // 🎯 텍스처 반복 설정 (애니메이션을 위해 필수!)
        if (_arrowTexture != null) _arrowTexture.wrapMode = TextureWrapMode.Repeat;
        if (_arrowTextureReverse != null) _arrowTextureReverse.wrapMode = TextureWrapMode.Repeat;
        if (_dashTexture != null) _dashTexture.wrapMode = TextureWrapMode.Repeat;

        // 🎯 URP 최신 쉐이더로 세팅! 오빠 프로젝트는 URP니까! 😤
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");

        _lineMaterial = new Material(shader);

        // 투명도 설정
        _lineMaterial.SetFloat("_Surface", 1.0f); 
        _lineMaterial.SetFloat("_Blend", 0.0f);   
        _lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        _lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        _lineMaterial.SetInt("_ZWrite", 0);
        _lineMaterial.renderQueue = 3000;

        _lineGradient = new Gradient();
        _colorKeys = new GradientColorKey[2];
        _alphaKeys = new GradientAlphaKey[8];

        lineRenderer.material = _lineMaterial;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.positionCount = 10;
        
        lineRenderer.sortingLayerName = "Player";
        lineRenderer.sortingOrder = 1000;

        lineRenderer.textureMode = LineTextureMode.Tile;
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
        for (int i = 0; i < size; i++)
            for (int j = 0; j < size; j++)
                if (x + i < 64 && y + j < 64 && x + i >= 0 && y + j >= 0)
                    tex.SetPixel(x + i, y + j, color);
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
        float pX = transform.position.x;
        Plane gameplayPlane = new Plane(Vector3.right, new Vector3(pX, 0, 0));

        Ray ray = _mainCamera.ScreenPointToRay(_virtualMousePos);

        if (gameplayPlane.Raycast(ray, out float enterDistance))
        {
            _aimWorldPosition = ray.GetPoint(enterDistance);
            _aimWorldPosition.x = pX;

            if (crosshairTransform != null)
            {
                crosshairTransform.position = _aimWorldPosition;
                crosshairTransform.rotation = Quaternion.Euler(0, -90, 0);
                
                if (crosshairTransform.TryGetComponent(out MeshRenderer mr))
                {
                    mr.sortingLayerName = "Player";
                    mr.sortingOrder = 32767;
                }
            }
        }
    }

    public Transform LockedTarget { get; private set; }

    private void DrawAimLine()
    {
        if (_playerHook != null && _playerHook.IsHooking)
        {
            if (lineRenderer.enabled) lineRenderer.enabled = false;
            return;
        }

        if (!lineRenderer.enabled) lineRenderer.enabled = true;

        float pX = transform.position.x;
        float currentMaxDistance = maxHookDistance;
        if (_playerHook != null) currentMaxDistance = _playerHook.MaxDistance;

        Vector3 startPos = firePoint != null ? firePoint.position : transform.position + Vector3.up * 1.0f;
        startPos.x = pX;
        
        Vector3 rawDirection = (_aimWorldPosition - startPos).normalized;
        Vector3 direction = rawDirection;
        Vector3 endPos = startPos + (direction * currentMaxDistance);

        Color targetColor = defaultColor;
        Texture2D targetTexture = _dashTexture;
        float currentFlowSpeed = -animationSpeed * 0.5f;
        float currentTiling = dashTiling;

        RaycastHit[] hits = Physics.SphereCastAll(startPos, aimRadius, rawDirection, currentMaxDistance, aimLayerMask);
        Collider bestTarget = null;
        float maxScore = -100.0f;

        foreach (var hit in hits)
        {
            if (hit.collider.gameObject == gameObject) continue;
            if (hit.collider.isTrigger) continue;
            if (hit.distance <= 0f) continue;
            BaseEnemy enemy = hit.collider.GetComponentInParent<BaseEnemy>();
            float dot = Vector3.Dot(rawDirection, (hit.point - startPos).normalized);
            if (dot < 0.0f) continue;
            float score = dot;
            if (enemy != null) score += 5.0f;
            else score -= hit.distance * 0.1f;
            if (score > maxScore) { maxScore = score; bestTarget = hit.collider; }
        }

        BaseEnemy targetEnemy = bestTarget != null ? bestTarget.GetComponentInParent<BaseEnemy>() : null;

        if (targetEnemy != null)
        {
            LockedTarget = bestTarget.transform;
            direction = (bestTarget.transform.position - startPos).normalized;
            RaycastHit enemyHit;
            if (Physics.Raycast(startPos, direction, out enemyHit, currentMaxDistance, aimLayerMask)) endPos = enemyHit.point;
            else endPos = bestTarget.transform.position;

            if (targetEnemy.IsFrozen) { targetColor = hookableColor; currentFlowSpeed = 0f; }
            else { targetColor = enemyColor; targetTexture = _arrowTextureReverse; currentFlowSpeed = animationSpeed; currentTiling = enemyArrowTiling; }
        }
        else
        {
            LockedTarget = null;
            if (_playerHook != null) direction = _playerHook.GetSnappedAimDirection(startPos, rawDirection);
            endPos = startPos + (direction * currentMaxDistance);
            RaycastHit obstructionHit;
            if (Physics.Raycast(startPos, direction, out obstructionHit, currentMaxDistance, aimLayerMask)) { endPos = obstructionHit.point; targetColor = hookableColor; }
            else targetColor = defaultColor;
            currentFlowSpeed = -animationSpeed * 0.5f;
            currentTiling = dashTiling;
        }

        int segmentCount = 10;
        lineRenderer.positionCount = segmentCount;
        for (int i = 0; i < segmentCount; i++)
        {
            float t = i / (float)(segmentCount - 1);
            Vector3 point = Vector3.Lerp(startPos, endPos, t);
            point.x = pX;
            lineRenderer.SetPosition(i, point);
        }

        _colorKeys[0].color = targetColor; _colorKeys[0].time = 0f;
        _colorKeys[1].color = targetColor; _colorKeys[1].time = 1f;
        for (int i = 0; i < 8; i++) { float t = i / 7f; _alphaKeys[i].alpha = customAlphaCurve.Evaluate(t); _alphaKeys[i].time = t; }
        _lineGradient.SetKeys(_colorKeys, _alphaKeys);
        lineRenderer.colorGradient = _lineGradient;

        if (_lineMaterial != null)
        {
            if (_lineMaterial.HasProperty("_BaseColor")) _lineMaterial.SetColor("_BaseColor", targetColor);
            _lineMaterial.SetTexture("_BaseMap", targetTexture);
            float distance = Vector3.Distance(startPos, endPos);
            _currentTextureOffset += currentFlowSpeed * Time.deltaTime;
            _lineMaterial.SetTextureScale("_BaseMap", new Vector2(distance * currentTiling, 1f));
            _lineMaterial.SetTextureOffset("_BaseMap", new Vector2(_currentTextureOffset, 0f));
        }
    }
}