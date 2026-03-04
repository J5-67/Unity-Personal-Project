using UnityEngine;

namespace Player
{
    public class PlayerAnimator : MonoBehaviour
    {
        private Animator _animator;
        private PlayerMovement _playerMovement;
        private PlayerHealth _playerHealth;
        private PlayerHook _playerHook;
        private Rigidbody _rb;

        private readonly int _isWalkingHash = Animator.StringToHash("IsWalking");
        private readonly int _isJumpingHash = Animator.StringToHash("IsJumping");
        private readonly int _isSwingingHash = Animator.StringToHash("IsSwinging");
        private readonly int _dashHash = Animator.StringToHash("Dash");
        private readonly int _dieHash = Animator.StringToHash("Die");
        private readonly int _takeDamageHash = Animator.StringToHash("Take Damage");

        private void Awake()
        {

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

            bool isWalking = Mathf.Abs(_playerMovement.MoveInput.x) > 0.05f && _playerMovement.IsGrounded;
            _animator.SetBool(_isWalkingHash, isWalking);

            bool isJumping = !_playerMovement.IsGrounded || (_rb != null && _rb.linearVelocity.y > 0.1f);
            _animator.SetBool(_isJumpingHash, isJumping);

            bool isSwinging = _playerMovement.IsHookingState;
            _animator.SetBool(_isSwingingHash, isSwinging);
        }

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
