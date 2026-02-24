using UnityEngine;

namespace Player
{
    public class PlayerAnimator : MonoBehaviour
    {
        private Animator _animator;
        private PlayerMovement _playerMovement;
        private PlayerHealth _playerHealth;
        private PlayerHook _playerHook; // [New] 스윙 애니메이션용 캐싱
        private Rigidbody _rb;

        // 2D 전용 심플 해시 변환!
        private readonly int _isWalkingHash = Animator.StringToHash("IsWalking");
        private readonly int _isJumpingHash = Animator.StringToHash("IsJumping"); 
        private readonly int _isSwingingHash = Animator.StringToHash("IsSwinging"); // [New]
        private readonly int _dashHash = Animator.StringToHash("Dash"); // [Fix] 깔끔하게 이름 변경
        private readonly int _dieHash = Animator.StringToHash("Die");
        private readonly int _takeDamageHash = Animator.StringToHash("Take Damage");

        private void Awake()
        {
            // 부모/자식 어디에 달려있든 유연하게 캐싱!
            _animator = GetComponentInChildren<Animator>();
            _playerMovement = GetComponentInParent<PlayerMovement>();
            _playerHealth = GetComponentInParent<PlayerHealth>();
            _playerHook = GetComponentInParent<PlayerHook>();
            _rb = GetComponentInParent<Rigidbody>();
        }

        private void OnEnable()
        {
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
            if (_animator == null || _playerMovement == null) return;

            // 1. 걷기 로직: 방향키(x축 입력)가 들어오면 걷기 ON!!
            bool isWalking = Mathf.Abs(_playerMovement.MoveInput.x) > 0.05f;
            _animator.SetBool(_isWalkingHash, isWalking);

            // 2. 공중 로직 (진짜 중요!⭐)
            bool isJumping = !_playerMovement.IsGrounded || (_rb != null && _rb.linearVelocity.y > 0.1f);
            _animator.SetBool(_isJumpingHash, isJumping);

            // 3. 스윙 로직 (훅을 걸고 있는지 공용으로 체크)
            // (주의: PlayerMovement 안의 내부 변수를 외부 접근자를 만들거나 Hook 스크립트의 상태를 가져옵니다)
            // [New] PlayerHook 안쪽의 "_isHooking" 상태를 직접 알 수 있게 리플렉션 없이도 편하게 짤 수 있음!
            // 하지만 제일 간단하게 공중에서 줄을 매달고 있는지를 판단하려면 RigidBody의 제약 조건이나 이동 스피드를 쓸 수도 있음.
            // 일단은 외부로 상태를 넘겨서 처리하는 게 정석! (PlayerMovement에서 제어 금지 상태인지 확인)
            bool isSwinging = !_playerMovement.CanMove && !_playerMovement.IsDashing && !_playerMovement.IsGrounded;
            _animator.SetBool(_isSwingingHash, isSwinging);
        }

        // --- Trigger Functions ---
        private void TriggerJump()
        {
            if (_animator != null) _animator.SetTrigger("Jump");
        }

        private void TriggerDash()
        {
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
    }
}
