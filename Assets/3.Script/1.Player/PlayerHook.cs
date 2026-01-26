using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerHook : MonoBehaviour
{
    [Header("🪝 Hook Settings")]
    [SerializeField] private float maxDistance = 15f;      // 사거리
    [SerializeField] private float pullSpeed = 25f;        // 내가 날아가는 속도
    [SerializeField] private float retrieveSpeed = 30f;    // 적을 당겨오는 속도
    [SerializeField] private float stopDistance = 1.5f;    // 목표 도달 판정 거리
    [SerializeField] private LayerMask hookableLayer;      // 훅이 박히는 모든 레이어 (벽, 적)

    [Header("🏷️ Tags (구분용)")]
    [SerializeField] private string wallTag = "Wall";             // 이동 가능
    [SerializeField] private string heavyEnemyTag = "LargeEnemy"; // 이동 가능
    [SerializeField] private string frozenEnemyTag = "FrozenEnemy"; // 이동 가능 (정지된 적)
    [SerializeField] private string lightEnemyTag = "SmallEnemy"; // 당겨오기

    [Header("✨ Visuals")]
    [SerializeField] private LineRenderer lineRenderer;    // 로프 그리기용
    [SerializeField] private Transform firePoint;          // 발사 위치 (플레이어 중심)

    // 내부 변수
    private PlayerAim _playerAim;
    private PlayerMovement _playerMovement;
    private Camera _mainCamera;
    private bool _isHooking;
    private Transform _currentHookTarget; // 현재 꽂힌 대상

    private void Awake()
    {
        _playerAim = GetComponent<PlayerAim>();
        _playerMovement = GetComponent<PlayerMovement>();
        _mainCamera = Camera.main;

        // 라인 렌더러 자동 설정
        if (lineRenderer == null) lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 2;
        lineRenderer.enabled = false;
    }

    // ---------------------------------------------------------
    // 🖱️ Input Handling
    // ---------------------------------------------------------
    public void OnHook(InputAction.CallbackContext context)
    {
        // 버튼 누름 (발사)
        if (context.started && !_isHooking)
        {
            FireHook();
        }
        // 버튼 뗌 (취소)
        else if (context.canceled && _isHooking)
        {
            StopHook();
        }
    }

    // ---------------------------------------------------------
    // 🚀 Hook Logic
    // ---------------------------------------------------------
    private void FireHook()
    {
        Vector3 aimPos = _playerAim.GetAimWorldPosition();
        Vector3 dir = (aimPos - transform.position).normalized;

        // 2.5D 보정 (Z축 이동, X축 고정)
        dir = new Vector3(0, dir.y, dir.z).normalized;

        // 레이캐스트 발사
        if (Physics.Raycast(transform.position, dir, out RaycastHit hit, maxDistance, hookableLayer))
        {
            string tag = hit.collider.tag;

            // 1. 내가 날아가는 대상 (벽, 대형 적, 얼어붙은 적)
            if (tag == wallTag || tag == heavyEnemyTag || tag == frozenEnemyTag)
            {
                StartCoroutine(PullSelfRoutine(hit.point));
                _playerMovement.AddDashStack(1); // [시너지] 성공 시 대시 충전
            }
            // 2. 끌고 오는 대상 (소형 적)
            else if (tag == lightEnemyTag)
            {
                StartCoroutine(PullTargetRoutine(hit.transform));
                _playerMovement.AddDashStack(1); // [시너지] 성공 시 대시 충전
            }
        }
        else
        {
            // 허공에 쏘면 잠깐 선만 그렸다 끄기 (연출)
            StartCoroutine(MissHookRoutine(transform.position + dir * maxDistance));
        }
    }

    private void StopHook()
    {
        _isHooking = false;
        _playerMovement.SetHookState(false); // 이동 권한 반납
        lineRenderer.enabled = false;
        StopAllCoroutines(); // 진행 중인 훅 로직 중단
    }

    // ---------------------------------------------------------
    // 🤸 Type A: Pull Self (내가 날아감)
    // ---------------------------------------------------------
    private IEnumerator PullSelfRoutine(Vector3 targetPos)
    {
        _isHooking = true;
        _playerMovement.SetHookState(true); // 중력 끄고 이동 제어 시작
        lineRenderer.enabled = true;

        while (_isHooking)
        {
            // 목표 방향 계산
            Vector3 myPos = transform.position;
            float distance = Vector3.Distance(myPos, targetPos);

            // 도착했으면 종료
            if (distance < stopDistance)
            {
                StopHook();
                yield break;
            }

            // 플레이어 이동 (등속 운동 or 가속 운동)
            Vector3 dir = (targetPos - myPos).normalized;
            _playerMovement.SetVelocity(dir * pullSpeed);

            // 선 그리기
            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, targetPos);

            yield return null;
        }
    }

    // ---------------------------------------------------------
    // 🎣 Type B: Pull Target (적을 당겨옴)
    // ---------------------------------------------------------
    private IEnumerator PullTargetRoutine(Transform target)
    {
        _isHooking = true;
        lineRenderer.enabled = true;

        // [중요] 적이 물리 효과를 받으려면 Rigidbody가 있어야 당겨짐
        Rigidbody targetRb = target.GetComponent<Rigidbody>();
        if (targetRb != null) targetRb.isKinematic = false; // 물리 켜기

        while (_isHooking && target != null)
        {
            float distance = Vector3.Distance(transform.position, target.position);

            // 내 앞에 오면 멈춤
            if (distance < stopDistance)
            {
                // [추가] 당겨온 후 적을 기절시키거나 처리하는 로직 필요
                // target.GetComponent<EnemyAI>()?.Stun(); 
                StopHook();
                yield break;
            }

            // 적을 내 쪽으로 당기기
            Vector3 dir = (transform.position - target.position).normalized;

            // Rigidbody가 있으면 속도로, 없으면 Transform으로
            if (targetRb != null)
            {
                targetRb.linearVelocity = dir * retrieveSpeed;
            }
            else
            {
                target.position += dir * retrieveSpeed * Time.deltaTime;
            }

            // 선 그리기
            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, target.position);

            yield return null;
        }
        StopHook();
    }

    // ---------------------------------------------------------
    // ❌ Miss (허공) 연출
    // ---------------------------------------------------------
    private IEnumerator MissHookRoutine(Vector3 endPos)
    {
        lineRenderer.enabled = true;
        float timer = 0f;

        while (timer < 0.1f) // 0.1초 동안만 보여줌
        {
            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, endPos);
            timer += Time.deltaTime;
            yield return null;
        }
        lineRenderer.enabled = false;
    }
}