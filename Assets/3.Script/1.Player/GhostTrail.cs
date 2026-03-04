using UnityEngine;
using UnityEngine.Pool;

public class GhostTrail : MonoBehaviour
{
    [Header("👻 Ghost Settings")]
    [SerializeField] private Material ghostMaterial;
    [SerializeField] private Color ghostColor = new Color(0f, 1f, 1f, 0.5f);
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float meshRefreshRate = 0.05f;

    private ObjectPool<GameObject> _ghostPool;

    private SkinnedMeshRenderer[] _skinnedRenderers;
    private MeshFilter[] _meshFilters;
    private SpriteRenderer[] _spriteRenderers;

    private MaterialPropertyBlock _propertyBlock;

    private void Awake()
    {
        _ghostPool = new ObjectPool<GameObject>(CreateGhost, OnGetGhost, OnReleaseGhost, OnDestroyGhost, true, 20, 50);

        _skinnedRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
        _meshFilters = GetComponentsInChildren<MeshFilter>();
        _spriteRenderers = GetComponentsInChildren<SpriteRenderer>();

        _propertyBlock = new MaterialPropertyBlock();
    }

    private GameObject CreateGhost()
    {

        GameObject ghostObj = new GameObject("Ghost_Pool");

        GameObject meshObj = new GameObject("MeshGhost");
        meshObj.transform.SetParent(ghostObj.transform, false);
        MeshRenderer mr = meshObj.AddComponent<MeshRenderer>();
        MeshFilter mf = meshObj.AddComponent<MeshFilter>();
        mf.mesh = new Mesh();
        mr.material = ghostMaterial;

        GameObject spriteObj = new GameObject("SpriteGhost");
        spriteObj.transform.SetParent(ghostObj.transform, false);
        SpriteRenderer sr = spriteObj.AddComponent<SpriteRenderer>();
        sr.material = ghostMaterial;

        GhostEffect effect = ghostObj.AddComponent<GhostEffect>();
        effect.Initialize(this, fadeDuration, _propertyBlock, ghostColor, mr, sr, mf);

        return ghostObj;
    }

    private void OnGetGhost(GameObject ghost)
    {
        ghost.SetActive(true);
    }

    private void OnReleaseGhost(GameObject ghost)
    {
        ghost.SetActive(false);
    }

    private void OnDestroyGhost(GameObject ghost)
    {
        if (ghost != null)
        {
            GhostEffect effect = ghost.GetComponent<GhostEffect>();
            if (effect != null)
            {
                effect.DestroySharedMesh();
            }
            Destroy(ghost);
        }
    }

    public void ShowGhost()
    {
        if (_skinnedRenderers != null && _skinnedRenderers.Length > 0)
        {
            foreach (var skinned in _skinnedRenderers)
            {
                if (!skinned.gameObject.activeInHierarchy) continue;

                GameObject ghostObj = _ghostPool.Get();
                ghostObj.transform.SetParent(null);
                ghostObj.transform.SetPositionAndRotation(skinned.transform.position, skinned.transform.rotation);
                ghostObj.transform.localScale = skinned.transform.lossyScale;

                GhostEffect effect = ghostObj.GetComponent<GhostEffect>();
                skinned.BakeMesh(effect.GetMeshFilter().mesh);

                effect.SetupMesh();
                effect.StartFade();
            }
        }

        if (_meshFilters != null && _meshFilters.Length > 0)
        {
            foreach (var filter in _meshFilters)
            {
                if (!filter.gameObject.activeInHierarchy) continue;

                GameObject ghostObj = _ghostPool.Get();
                ghostObj.transform.SetParent(null);
                ghostObj.transform.SetPositionAndRotation(filter.transform.position, filter.transform.rotation);
                ghostObj.transform.localScale = filter.transform.lossyScale;

                GhostEffect effect = ghostObj.GetComponent<GhostEffect>();
                effect.GetMeshFilter().mesh = filter.sharedMesh;

                effect.SetupMesh();
                effect.StartFade();
            }
        }

        if (_spriteRenderers != null && _spriteRenderers.Length > 0)
        {
            foreach (var sr in _spriteRenderers)
            {
                if (!sr.gameObject.activeInHierarchy || sr.sprite == null) continue;

                GameObject ghostObj = _ghostPool.Get();
                ghostObj.transform.SetParent(null);
                ghostObj.transform.SetPositionAndRotation(sr.transform.position, sr.transform.rotation);
                ghostObj.transform.localScale = sr.transform.lossyScale;

                GhostEffect effect = ghostObj.GetComponent<GhostEffect>();
                effect.SetupSprite(sr.sprite, sr.flipX, sr.flipY);
                effect.StartFade();
            }
        }
    }

    public void ReturnToPool(GameObject ghost)
    {
        if (ghost != null)
        {
            ghost.transform.SetParent(transform);
            _ghostPool.Release(ghost);
        }
    }
}

public class GhostEffect : MonoBehaviour
{
    private GhostTrail _manager;
    private float _fadeDuration;
    private float _timeElapsed;
    private MaterialPropertyBlock _propertyBlock;
    private Color _initColor;

    private MeshRenderer _meshRenderer;
    private SpriteRenderer _spriteRenderer;
    private MeshFilter _meshFilter;

    private int _colorPropertyId;
    private bool _isSprite;

    public void Initialize(GhostTrail manager, float duration, MaterialPropertyBlock block, Color color, MeshRenderer mr, SpriteRenderer sr, MeshFilter mf)
    {
        _manager = manager;
        _fadeDuration = duration;
        _propertyBlock = block;
        _initColor = color;

        _meshRenderer = mr;
        _spriteRenderer = sr;
        _meshFilter = mf;

        _colorPropertyId = Shader.PropertyToID("_BaseColor");
        if (_meshRenderer != null && _meshRenderer.sharedMaterial != null && !_meshRenderer.sharedMaterial.HasProperty(_colorPropertyId))
        {
             _colorPropertyId = Shader.PropertyToID("_Color");
        }
    }

    public MeshFilter GetMeshFilter()
    {
        return _meshFilter;
    }

    public void DestroySharedMesh()
    {
        if (_meshFilter != null && _meshFilter.sharedMesh != null)
        {
            Destroy(_meshFilter.sharedMesh);
        }
    }

    public void SetupMesh()
    {
        _isSprite = false;
        if (_meshRenderer != null) _meshRenderer.enabled = true;
        if (_spriteRenderer != null) _spriteRenderer.enabled = false;
    }

    public void SetupSprite(Sprite sprite, bool flipX, bool flipY)
    {
        _isSprite = true;
        if (_meshRenderer != null) _meshRenderer.enabled = false;
        if (_spriteRenderer != null)
        {
            _spriteRenderer.enabled = true;
            _spriteRenderer.sprite = sprite;
            _spriteRenderer.flipX = flipX;
            _spriteRenderer.flipY = flipY;

            _spriteRenderer.color = _initColor;
        }
    }

    public void StartFade()
    {
        _timeElapsed = 0f;

        if (!_isSprite)
        {
            _propertyBlock.SetColor(_colorPropertyId, _initColor);
            _meshRenderer.SetPropertyBlock(_propertyBlock);
        }
        else
        {
            _spriteRenderer.color = _initColor;
        }

        enabled = true;
    }

    private void Update()
    {
        _timeElapsed += Time.deltaTime;

        if (_timeElapsed < _fadeDuration)
        {
            float alphaResults = Mathf.Lerp(_initColor.a, 0f, _timeElapsed / _fadeDuration);
            Color newColor = new Color(_initColor.r, _initColor.g, _initColor.b, alphaResults);

            if (!_isSprite)
            {
                _propertyBlock.SetColor(_colorPropertyId, newColor);
                _meshRenderer.SetPropertyBlock(_propertyBlock);
            }
            else
            {
                _spriteRenderer.color = newColor;
            }
        }
        else
        {
            enabled = false;
            _manager.ReturnToPool(gameObject);
        }
    }
}
