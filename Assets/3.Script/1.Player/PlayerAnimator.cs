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

            // 1. 걷기 로직: 방향키(x축 입력)가 들어오고, 땅에 닿아있을 때만 걷기 ON!! (벽에 매달려서 걷는 애니메이션 방지)
            bool isWalking = Mathf.Abs(_playerMovement.MoveInput.x) > 0.05f && _playerMovement.IsGrounded;
            _animator.SetBool(_isWalkingHash, isWalking);

            // 2. 공중 로직 (진짜 중요!⭐)
            bool isJumping = !_playerMovement.IsGrounded || (_rb != null && _rb.linearVelocity.y > 0.1f);
            _animator.SetBool(_isJumpingHash, isJumping);

            // 3. 스윙 로직 (진짜 훅을 걸고 있는지 명시적으로 체크!)
            // [Fix] 기존엔 !CanMove로 퉁쳐서, 벽 점프할 때 잠깐 CanMove=false 되는 구간에 스윙 애니가 오발동했음
            bool isSwinging = _playerMovement.IsHookingState;
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
