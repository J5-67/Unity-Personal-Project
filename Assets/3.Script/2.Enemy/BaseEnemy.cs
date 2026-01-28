using System.Collections;
using UnityEngine;

// [유니] 적의 타입을 구분하기 위한 열거형이야!
public enum EnemyType
{
    Light, // [유니] 플레이어에게 끌려오는 가벼운 적
    Heavy  // [유니] 플레이어가 날아가는 묵직한 적
}

[RequireComponent(typeof(Rigidbody))]
public class BaseEnemy : MonoBehaviour
{
    [Header("🎯 Enemy Settings")]
    [SerializeField] private EnemyType enemyType = EnemyType.Light; // [유니] 인스펙터에서 골라줘!
    
    [Tooltip("Light: 당겨오는 속도 / Heavy: 플레이어가 날아가는 가속도")]

    [SerializeField] private float hookInteractSpeed = 30f; // [유니] 적마다 다른 힘을 설정할 수 있어!
    [SerializeField] private float freezeDuration = 5f;     // [유니] 얼어있는 시간 (끝나면 파괴됨!)

    private Rigidbody _rb;

    // [유니] 외부에서 타입을 확인할 수 있게 프로퍼티로 만들었어!
    public EnemyType Type => enemyType;
    public float HookInteractSpeed => hookInteractSpeed;
    public bool IsFrozen { get; private set; } // [유니] 얼음 상태 체크!

    // [유니] 원래 태그와 색깔 저장용
    private string _originalTag;
    private Color _originalColor;
    private Renderer _renderer;
    private EnemyPatrol _patrol;

    private void Awake()
    {
        // [유니] 물리 연산을 위해 Rigidbody는 필수!
        if (!TryGetComponent(out _rb))
        {
            _rb = gameObject.AddComponent<Rigidbody>();
        }

        // [유니] 2.5D 게임이니까 옆으로 쓰러지거나 뒤로 밀리지 않게 고정해줄게!
        _rb.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezeRotation;

        _renderer = GetComponentInChildren<Renderer>();
        _patrol = GetComponent<EnemyPatrol>();
        _originalTag = gameObject.tag;
    }

    public void Freeze()
    {
        if (IsFrozen) return; // 이미 얼었으면 무시
        StartCoroutine(FreezeRoutine());
    }

    private IEnumerator FreezeRoutine()
    {
        IsFrozen = true;
        
        // 1. 비주얼 변경 (파란색!)
        if (_renderer != null)
        {
            _originalColor = _renderer.material.color;
            _renderer.material.color = Color.cyan;
        }

        // 2. 태그 변경
        try 
        { 
            gameObject.tag = "FrozenEnemy"; 
        }
        catch (System.Exception) 
        { 
            Debug.LogWarning("[유니] 'FrozenEnemy' 태그가 프로젝트에 없어! Inspector에서 추가해줘!"); 
        }

        // 3. 행동 정지 (순찰 끄기)
        if (_patrol != null) _patrol.SetPatrol(false);
        if (_rb != null) _rb.isKinematic = true; 

        Debug.Log($"[유니] {name} 꽁꽁 얼어라! ❄️ ({freezeDuration}초 후 파괴)");

        yield return new WaitForSeconds(freezeDuration);

        // 4. 파괴 (Shatter!)
        Debug.Log($"[유니] {name} 산산조각 났어! 💥");
        Destroy(gameObject);
    }

    // [유니] 나중에 여기에 데미지를 입거나 기절하는 로직을 넣으면 딱이겠지?
    public void OnHooked()
    {
        Debug.Log($"[유니] {gameObject.name} (타입: {enemyType})가 훅에 걸렸어!");
    }
}
