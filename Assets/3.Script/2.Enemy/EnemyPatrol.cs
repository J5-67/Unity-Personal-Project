using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyPatrol : MonoBehaviour
{
    [Header("📍 Patrol Settings")]
    [SerializeField] private List<Transform> waypoints; // 순찰할 지점들 (빈 오브젝트 사용)
    [SerializeField] private float moveSpeed = 3f;      // 이동 속도
    [SerializeField] private float waitTime = 1f;       // 각 지점 대기 시간

    private Rigidbody _rb;
    private int _currentIndex = 0;
    private bool _isWaiting = false;
    private bool _isPatrolling = true; // [유니] 순찰 활성화 상태
    private List<Vector3> _targetPositions; 

    public void SetPatrol(bool active)
    {
        _isPatrolling = active;
        if (_rb != null)
        {
            // 순찰 중일 땐 Kinematic (플랫폼 역할)
            // 끌려갈 땐 Dynamic (물리 적용)
            _rb.isKinematic = active;
        }
    }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        // [유니] 순찰 중에는 정해진 궤도를 따라가야 하니까 Kinematic 권장!
        // Heavy Enemy의 경우 움직이는 플랫폼 역할을 하려면 Kinematic이어야 흔들리지 않음.
        _rb.isKinematic = true; 

        // [유니] 중요! 웨이포인트를 적의 자식으로 넣었을 때, 
        // 적이 움직이면 웨이포인트도 같이 움직이는 문제 해결!
        // 게임 시작 시점의 '월드 좌표'만 딱 기억해두고, 그 좌표로 이동하게 함.
        _targetPositions = new List<Vector3>();
        if (waypoints != null)
        {
            foreach (Transform t in waypoints)
            {
                if (t != null) _targetPositions.Add(t.position);
            }
        }
    }

    private void FixedUpdate()
    {
        // 웨이포인트가 없으면 작동 안 함
        if (waypoints == null || waypoints.Count == 0) return;
        
        // 대시 중이거나 순찰이 꺼져있으면 중단
        if (_isWaiting || !_isPatrolling) return;

        MoveToTarget();
    }

    private void MoveToTarget()
    {
        // 타겟 좌표가 없으면 중단
        if (_targetPositions.Count == 0) return;

        Vector3 currentPos = transform.position;
        // [유니] Transform 대신 기억해둔 좌표 사용!
        Vector3 targetPos = _targetPositions[_currentIndex];

        // 1. 방향 및 거리 계산
        Vector3 dir = (targetPos - currentPos).normalized;
        float dist = Vector3.Distance(currentPos, targetPos);

        // 2. 이동 (MovePosition 사용)
        // 이번 프레임에 이동할 거리
        float moveStep = moveSpeed * Time.fixedDeltaTime;

        if (dist <= moveStep)
        {
            // 도착! (정확히 위치 맞춤)
            _rb.MovePosition(targetPos);
            StartCoroutine(WaitRoutine());
        }
        else
        {
            // 이동 중
            _rb.MovePosition(currentPos + dir * moveStep);
        }
    }

    private IEnumerator WaitRoutine()
    {
        _isWaiting = true;
        yield return new WaitForSeconds(waitTime);

        // 다음 웨이포인트 선택 (Loop)
        _currentIndex = (_currentIndex + 1) % _targetPositions.Count;
        _isWaiting = false;
    }

    // [유니] 유니티 에디터에서 웨이포인트 경로 보여주기 (디버깅용) ✨
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;

        // [게임 중] 기억해둔 고정 좌표 표시
        if (Application.isPlaying && _targetPositions != null)
        {
            for (int i = 0; i < _targetPositions.Count; i++)
            {
                Vector3 p1 = _targetPositions[i];
                Vector3 p2 = _targetPositions[(i + 1) % _targetPositions.Count];
                
                Gizmos.DrawLine(p1, p2);
                Gizmos.DrawSphere(p1, 0.2f);
            }
        }
        // [에디터] 기존 Transform 연결 표시
        else if (waypoints != null && waypoints.Count >= 2)
        {
            for (int i = 0; i < waypoints.Count; i++)
            {
                Transform t1 = waypoints[i];
                Transform t2 = waypoints[(i + 1) % waypoints.Count];
                
                if (t1 != null && t2 != null)
                {
                    Gizmos.DrawLine(t1.position, t2.position);
                    Gizmos.DrawSphere(t1.position, 0.2f); 
                }
            }
        }
    }
}
