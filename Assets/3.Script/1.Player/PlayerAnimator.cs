using UnityEngine;

namespace Player
{
    [RequireComponent(typeof(Animator))]
    public class PlayerAnimator : MonoBehaviour
    {
        private Animator _animator;
        private PlayerMovement _playerMovement;
        private PlayerHealth _playerHealth;
        private Rigidbody _rb;

        // 애니메이션 최적화를 위한 Hash 변환 (String 노가다보다 훨씬 빠름!)
        private readonly int _forwardHash = Animator.StringToHash("Forward");
        private readonly int _strafeHash = Animator.StringToHash("Strafe");
        private readonly int _jumpHash = Animator.StringToHash("Jump");
        private readonly int _dashHash = Animator.StringToHash("SpinAttack"); // 대시 연출용
        private readonly int _takeDamageHash = Animator.StringToHash("Take Damage");
        private readonly int _dieHash = Animator.StringToHash("Die");

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            
            // 모델이 플레이어의 최상단 자식으로 들어갈 경우를 상정! ('PlayerMovement' 등을 자동으로 찾음)
            _playerMovement = GetComponentInParent<PlayerMovement>();
            _playerHealth = GetComponentInParent<PlayerHealth>();
            _rb = GetComponentInParent<Rigidbody>();
        }

        private void OnEnable()
        {
            // 유니의 완벽한 징검다리! Player 코드를 매 프레임 감시하지 않고, 이벤트 방식으로 구독!
            if (_playerMovement != null)
            {
                _playerMovement.OnJumpEvent += TriggerJump;
                _playerMovement.OnDashEvent += TriggerDash;
            }

            if (_playerHealth != null)
            {
                _playerHealth.OnTakeDamageEvent += TriggerTakeDamage;
                _playerHealth.OnDieEvent += TriggerDie;
            }
        }

        private void OnDisable()
        {
            // 메모리 누수 안 나게 이벤트 해제 싹!
            if (_playerMovement != null)
            {
                _playerMovement.OnJumpEvent -= TriggerJump;
                _playerMovement.OnDashEvent -= TriggerDash;
            }

            if (_playerHealth != null)
            {
                _playerHealth.OnTakeDamageEvent -= TriggerTakeDamage;
                _playerHealth.OnDieEvent -= TriggerDie;
            }
        }

        private void Update()
        {
            if (_rb == null || _playerMovement == null) return;

            // [Fix] 모델(내 자신)의 transform을 기준으로 돌리면, 모델이 돌아갈 때마다 입력 축이 꼬여버림!
            // 따라서 '절대 회전 기준점'인 부모(Player 본체)의 transform 기준으로 앞뒤좌우 속도를 판별해야 함.
            Vector3 localVelocity = _playerMovement.transform.InverseTransformDirection(_rb.linearVelocity);

            // Forward 계산: 이동 속도 최대치를 기준으로 비율 0~1 산출
            float moveSpeedLimit = 10f; 
            float forwardAmount = Mathf.Clamp(localVelocity.z / moveSpeedLimit, -1f, 1f);
            float strafeAmount = Mathf.Clamp(localVelocity.x / moveSpeedLimit, -1f, 1f); 

            // 단순 SetFloat이 아니라 마법의 보간 수치(0.1f)를 넣어서,
            // 걷기-뛰기가 로봇처럼 뚝뚝 안 끊기고 엄청 스무스하게 블렌딩되도록 수정!
            _animator.SetFloat(_forwardHash, forwardAmount, 0.1f, Time.deltaTime);
            _animator.SetFloat(_strafeHash, strafeAmount, 0.1f, Time.deltaTime);
        }

        // --- Trigger Functions ---
        private void TriggerJump()
        {
            if (_animator != null) _animator.SetTrigger(_jumpHash);
        }

        private void TriggerDash()
        {
            // [대시 모델 기믹] 오빠 맘대로 SpinAttack 애니메이션 빌려 쓰기! 슝!!
            if (_animator != null) _animator.SetTrigger(_dashHash);
        }

        private void TriggerTakeDamage()
        {
            if (_animator != null) _animator.SetTrigger(_takeDamageHash);
        }

        private void TriggerDie()
        {
            if (_animator != null) _animator.SetTrigger(_dieHash);
        }

        private void LateUpdate()
        {
            // 애니메이터가 뼈대를 다 움직인 직후(LateUpdate)에 강제로 뼈다귀의 Y축을 0으로 짓눌러버림!
            // 로봇 모델의 루트 뼈대(Rig)가 자식으로 있다면, 그 로컬 Y 좌표를 0으로 고정!
            Transform rigTransform = transform.Find("Rig") ?? transform.Find("RigPelvis");
            
            if (rigTransform != null)
            {
                Vector3 fixedPos = rigTransform.localPosition;
                fixedPos.y = 0f; 
                rigTransform.localPosition = fixedPos;
            }
        }
    }
}
