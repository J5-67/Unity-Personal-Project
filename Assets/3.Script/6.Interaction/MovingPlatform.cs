using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Interaction
{
    [RequireComponent(typeof(Rigidbody))]
    public class MovingPlatform : MonoBehaviour
    {
        [Header("Path Settings")]
        [Tooltip("플랫폼이 이동할 경로점(Waypoints) 배열")]
        [SerializeField] private Transform[] waypoints;

        [Tooltip("플랫폼의 이동 속도 (초당 스피드)")]
        [SerializeField] private float speed = 5.0f;

        [Tooltip("각 웨이포인트(정점)에 도달했을 때 대기하는 시간")]
        [SerializeField] private float waitTimeAtPoint = 1.0f;

        [Header("Movement Logic")]
        [Tooltip("목적지를 처음부터 다시 도는 왕복 루프 여부 (체크 해제 시 도착 후 멈춤)")]
        [SerializeField] private bool loop = true;

        [Tooltip("순서를 역순으로 되돌아가며 왕복할지 여부 (탁구처럼 왔다갔다)")]
        [SerializeField] private bool pingPong = true;

        private Rigidbody _rb;
        private int _currentWaypointIndex = 0;
        private bool _isWaiting = false;
        private bool _movingForward = true;
        private List<Vector3> _targetPositions;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();

            _rb.isKinematic = true;
            _rb.useGravity = false;

            _rb.constraints = RigidbodyConstraints.FreezeRotation;

            _targetPositions = new List<Vector3>();
            if (waypoints != null)
            {
                foreach (Transform t in waypoints)
                {
                    if (t != null) _targetPositions.Add(t.position);
                }
            }
        }

        private void Start()
        {
            if (_targetPositions != null && _targetPositions.Count > 0)
            {

                transform.position = _targetPositions[0];
            }
        }

        private void FixedUpdate()
        {

            if (_targetPositions == null || _targetPositions.Count <= 1 || _isWaiting) return;

            MoveTowardsTarget();
        }

        private void MoveTowardsTarget()
        {
            Vector3 currentPos = transform.position;
            Vector3 targetPos = _targetPositions[_currentWaypointIndex];

            Vector3 newPos = Vector3.MoveTowards(currentPos, targetPos, speed * Time.fixedDeltaTime);

            _rb.MovePosition(newPos);

            if ((currentPos - targetPos).sqrMagnitude < 0.0025f)
            {
                StartCoroutine(WaitAndSetNextTarget());
            }
        }

        private IEnumerator WaitAndSetNextTarget()
        {
            _isWaiting = true;

            yield return new WaitForSeconds(waitTimeAtPoint);

            SetNextWaypointIndex();

            _isWaiting = false;
        }

        private void SetNextWaypointIndex()
        {
            if (_movingForward)
            {

                if (_currentWaypointIndex < _targetPositions.Count - 1)
                {
                    _currentWaypointIndex++;
                }
                else
                {

                    if (loop)
                    {
                        if (pingPong)
                        {
                            _movingForward = false;
                            _currentWaypointIndex--;
                        }
                        else
                        {
                            _currentWaypointIndex = 0;
                        }
                    }
                }
            }
            else
            {

                if (_currentWaypointIndex > 0)
                {
                    _currentWaypointIndex--;
                }
                else
                {
                    if (loop)
                    {
                        _movingForward = true;
                        _currentWaypointIndex++;
                    }
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (Application.isPlaying && _targetPositions != null && _targetPositions.Count >= 2)
            {
                Gizmos.color = Color.cyan;
                for (int i = 0; i < _targetPositions.Count - 1; i++)
                {
                    Gizmos.DrawLine(_targetPositions[i], _targetPositions[i + 1]);
                }

                if (loop && !pingPong)
                {
                    Gizmos.color = Color.gray;
                    Gizmos.DrawLine(_targetPositions[_targetPositions.Count - 1], _targetPositions[0]);
                }
            }
            else if (!Application.isPlaying && waypoints != null && waypoints.Length >= 2)
            {
                Gizmos.color = Color.cyan;
                for (int i = 0; i < waypoints.Length - 1; i++)
                {
                    if (waypoints[i] != null && waypoints[i + 1] != null)
                    {
                        Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
                    }
                }

                if (loop && !pingPong && waypoints[waypoints.Length - 1] != null && waypoints[0] != null)
                {
                    Gizmos.color = Color.gray;
                    Gizmos.DrawLine(waypoints[waypoints.Length - 1].position, waypoints[0].position);
                }
            }
        }
    }
}
