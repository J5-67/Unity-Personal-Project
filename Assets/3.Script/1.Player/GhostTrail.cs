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
    
    // [유니] 두 종류의 렌더러를 모두 찾아야 해!
    private SkinnedMeshRenderer[] _skinnedRenderers;
    private MeshFilter[] _meshFilters;
    
    // 색상 블록
    private MaterialPropertyBlock _propertyBlock;

    private void Awake()
    {
        _ghostPool = new ObjectPool<GameObject>(CreateGhost, OnGetGhost, OnReleaseGhost, OnDestroyGhost, true, 20, 50);
        
        // [유니] 모든 렌더러 찾기
        _skinnedRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
        _meshFilters = GetComponentsInChildren<MeshFilter>();
        
        _propertyBlock = new MaterialPropertyBlock();
    }

    // ---------------------------------------------------------
    // 🏭 Object Pool Standard Methods
    // ---------------------------------------------------------
    private GameObject CreateGhost()
    {
        GameObject ghostObj = new GameObject("Ghost_Pool");
        
        // 메쉬 렌더러 & 필터 추가
        MeshRenderer mr = ghostObj.AddComponent<MeshRenderer>();
        MeshFilter mf = ghostObj.AddComponent<MeshFilter>();
        
        // [유니] 베이킹용 빈 메쉬 생성 (재사용!)
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
        // [유니] 씬이 넘어갈 때 만들어둔 메쉬 삭제 (메모리 누수 방지)
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

    // ---------------------------------------------------------
    // ✨ Public API
    // ---------------------------------------------------------
    public void ShowGhost()
    {
        // 1. SkinnedMeshRenderer (애니메이션 O) 처리
        if (_skinnedRenderers != null)
        {
            foreach (var skinned in _skinnedRenderers)
            {
                if (!skinned.gameObject.activeInHierarchy) continue; // 꺼져있으면 패스

                GameObject ghostObj = _ghostPool.Get();
                ghostObj.transform.SetParent(null); // 월드 고정

                ghostObj.transform.SetPositionAndRotation(skinned.transform.position, skinned.transform.rotation);
                ghostObj.transform.localScale = skinned.transform.localScale;

                MeshFilter ghostFilter = ghostObj.GetComponent<MeshFilter>();
                
                // [유니] 현재 자세를 그대로 구워버림! (Bake) 🔥
                skinned.BakeMesh(ghostFilter.mesh); 
                
                ghostObj.GetComponent<GhostEffect>().StartFade();
            }
        }

        // 2. MeshFilter (애니메이션 X, 무기 등) 처리
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
                
                // 정적 메쉬는 그냥 복사
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

// [유니] 잔상 개별 관리 스크립트 (페이드 아웃 담당)
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
        
        // [유니] 쉐이더 프로퍼티 이름 호환성 체크 (_BaseColor: URP / _Color: Standard, Legacy)
        _colorPropertyId = Shader.PropertyToID("_BaseColor");
        if (!_meshRenderer.sharedMaterial.HasProperty(_colorPropertyId))
        {
             _colorPropertyId = Shader.PropertyToID("_Color");
        }
    }

    public void StartFade()
    {
        _timeElapsed = 0f;
        
        // 초기 색상 설정
        _propertyBlock.SetColor(_colorPropertyId, _initColor);
        _meshRenderer.SetPropertyBlock(_propertyBlock);
        
        // 코루틴 대신 Update에서 처리 (간단한 연출이라)
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
            // 시간 다 되면 반납
            enabled = false;
            _manager.ReturnToPool(gameObject);
        }
    }
}
