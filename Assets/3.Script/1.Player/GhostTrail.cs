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
    
    private MaterialPropertyBlock _propertyBlock;

    private void Awake()
    {
        _ghostPool = new ObjectPool<GameObject>(CreateGhost, OnGetGhost, OnReleaseGhost, OnDestroyGhost, true, 20, 50);
        
        _skinnedRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
        _meshFilters = GetComponentsInChildren<MeshFilter>();
        
        _propertyBlock = new MaterialPropertyBlock();
    }

    private GameObject CreateGhost()
    {
        GameObject ghostObj = new GameObject("Ghost_Pool");
        
        MeshRenderer mr = ghostObj.AddComponent<MeshRenderer>();
        MeshFilter mf = ghostObj.AddComponent<MeshFilter>();
        
        mf.mesh = new Mesh(); 
        
        mr.material = ghostMaterial; 
        
        GhostEffect effect = ghostObj.AddComponent<GhostEffect>();
        effect.Initialize(this, fadeDuration, _propertyBlock, ghostColor);

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
            MeshFilter mf = ghost.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
            {
                Destroy(mf.sharedMesh);
            }
            Destroy(ghost);
        }
    }

    public void ShowGhost()
    {
        if (_skinnedRenderers != null)
        {
            foreach (var skinned in _skinnedRenderers)
            {
                if (!skinned.gameObject.activeInHierarchy) continue;

                GameObject ghostObj = _ghostPool.Get();
                ghostObj.transform.SetParent(null);

                ghostObj.transform.SetPositionAndRotation(skinned.transform.position, skinned.transform.rotation);
                ghostObj.transform.localScale = skinned.transform.localScale;

                MeshFilter ghostFilter = ghostObj.GetComponent<MeshFilter>();
                
                skinned.BakeMesh(ghostFilter.mesh); 
                
                ghostObj.GetComponent<GhostEffect>().StartFade();
            }
        }

        if (_meshFilters != null)
        {
            foreach (var filter in _meshFilters)
            {
                if (!filter.gameObject.activeInHierarchy) continue;

                GameObject ghostObj = _ghostPool.Get();
                ghostObj.transform.SetParent(null);

                ghostObj.transform.SetPositionAndRotation(filter.transform.position, filter.transform.rotation);
                ghostObj.transform.localScale = filter.transform.localScale;

                MeshFilter ghostFilter = ghostObj.GetComponent<MeshFilter>();
                
                ghostFilter.mesh = filter.sharedMesh;
                
                ghostObj.GetComponent<GhostEffect>().StartFade();
            }
        }
    }

    public void ReturnToPool(GameObject ghost)
    {
        ghost.transform.SetParent(transform);
        _ghostPool.Release(ghost);
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
    private int _colorPropertyId;

    public void Initialize(GhostTrail manager, float duration, MaterialPropertyBlock block, Color color)
    {
        _manager = manager;
        _fadeDuration = duration;
        _propertyBlock = block;
        _initColor = color;
        _meshRenderer = GetComponent<MeshRenderer>();
        
        _colorPropertyId = Shader.PropertyToID("_BaseColor");
        if (!_meshRenderer.sharedMaterial.HasProperty(_colorPropertyId))
        {
             _colorPropertyId = Shader.PropertyToID("_Color");
        }
    }

    public void StartFade()
    {
        _timeElapsed = 0f;
        
        _propertyBlock.SetColor(_colorPropertyId, _initColor);
        _meshRenderer.SetPropertyBlock(_propertyBlock);
        
        enabled = true;
    }

    private void Update()
    {
        _timeElapsed += Time.deltaTime;

        if (_timeElapsed < _fadeDuration)
        {
            float alphaResults = Mathf.Lerp(_initColor.a, 0f, _timeElapsed / _fadeDuration);
            Color newColor = new Color(_initColor.r, _initColor.g, _initColor.b, alphaResults);

            _propertyBlock.SetColor(_colorPropertyId, newColor);
            _meshRenderer.SetPropertyBlock(_propertyBlock);
        }
        else
        {
            enabled = false;
            _manager.ReturnToPool(gameObject);
        }
    }
}
