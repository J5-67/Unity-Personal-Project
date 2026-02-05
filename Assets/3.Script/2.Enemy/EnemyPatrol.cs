using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyPatrol : MonoBehaviour
{
    [Header("📍 Patrol Settings")]
    [SerializeField] private List<Transform> waypoints;
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float waitTime = 1f;

    private Rigidbody _rb;
    private int _currentIndex = 0;
    private bool _isWaiting = false;
    private bool _isPatrolling = true;
    private List<Vector3> _targetPositions; 

    public void SetPatrol(bool active)
    {
        _isPatrolling = active;
        if (_rb != null)
        {
            _rb.isKinematic = active;
        }

        if (active)
        {
            StopAllCoroutines();
            _isWaiting = false;
        }
    }

    public void ResetPatrol()
    {
        _currentIndex = 0;
        _isWaiting = false;
        StopAllCoroutines();
        SetPatrol(true);
        
        if (_targetPositions.Count > 0)
        {
        }
    }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.isKinematic = true; 

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
        if (waypoints == null || waypoints.Count == 0) return;
        
        if (_isWaiting || !_isPatrolling) return;

        MoveToTarget();
    }

    private void MoveToTarget()
    {
        if (_targetPositions.Count == 0) return;

        Vector3 currentPos = transform.position;
        Vector3 targetPos = _targetPositions[_currentIndex];

        Vector3 dir = (targetPos - currentPos).normalized;
        float distSqr = (currentPos - targetPos).sqrMagnitude;
        
        float moveStep = moveSpeed * Time.fixedDeltaTime;

        if (distSqr <= moveStep * moveStep)
        {
            _rb.MovePosition(targetPos);
            StartCoroutine(WaitRoutine());
        }
        else
        {
            _rb.MovePosition(currentPos + dir * moveStep);
        }
    }

    private IEnumerator WaitRoutine()
    {
        _isWaiting = true;
        yield return new WaitForSeconds(waitTime);

        _currentIndex = (_currentIndex + 1) % _targetPositions.Count;
        _isWaiting = false;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;

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
