using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Move Settings")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float jumpForce = 15f;
    [SerializeField] private float jumpCutMultiplier = 0.5f;

    [Header("Feel Settings")]
    [SerializeField] private float coyoteTime = 0.15f;
    [SerializeField] private float jumpBufferTime = 0.2f;

    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 15f;

    [Header("Drop Settings")]
    [SerializeField] private float dropDisableTime = 0.5f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheckPos;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    private Rigidbody _rb;
    private Vector2 _moveInput;
    private float _coyoteTimeCounter;
    private float _jumpBufferCounter;
    private bool _isGrounded;
    private bool _isJumpPressed;
    private Collider _playerCollider;
    private PlatformFunction _currentFunctionPlatform;

    private void Awake()
    {
        TryGetComponent(out _rb);
        TryGetComponent(out _playerCollider);
    }

    private void Update()
    {
        UpdateJumpTimers();
    }

    private void FixedUpdate()
    {
        CheckGround();
        ApplyMovement();
        HandleJump();
        ApplyRotation();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        _moveInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            _jumpBufferCounter = jumpBufferTime;
            _isJumpPressed = true;
        }
        else if (context.canceled)
        {
            _isJumpPressed = false;

            if (_rb.linearVelocity.y > 0f)
            {
                CutJumpVelocity();
            }
        }
    }

    public void OnDrop(InputAction.CallbackContext context)
    {
        if (context.started && _currentFunctionPlatform != null)
        {
            StartCoroutine(DisableCollisionRoutine(_currentFunctionPlatform));
        }
    }

    private IEnumerator DisableCollisionRoutine(PlatformFunction platform)
    {
        Collider platformCollider = platform.platformCollider;

        Physics.IgnoreCollision(_playerCollider, platformCollider, true);

        yield return new WaitForSeconds(dropDisableTime);

        Physics.IgnoreCollision(_playerCollider, platformCollider, false);

    }



    private void CutJumpVelocity()
    {
        _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, _rb.linearVelocity.y * jumpCutMultiplier, _rb.linearVelocity.z);
    }

    private void UpdateJumpTimers()
    {
        if (_isGrounded)
        {
            _coyoteTimeCounter = coyoteTime;
        }
        else
        {
            _coyoteTimeCounter -= Time.deltaTime;
        }

        if (_jumpBufferCounter > 0)
        {
            _jumpBufferCounter -= Time.deltaTime;
        }
    }

    private void ApplyMovement()
    {
        float targetSpeedZ = _moveInput.x * moveSpeed;
        _rb.linearVelocity = new Vector3(0f, _rb.linearVelocity.y, targetSpeedZ);
    }

    private void ApplyRotation()
    {
        if (_moveInput.x != 0)
        {
            Vector3 lookDirection = new Vector3(0, 0, _moveInput.x);
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private void HandleJump()
    {
        if (_jumpBufferCounter > 0f && _coyoteTimeCounter > 0f)
        {
            _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
            _rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);

            _jumpBufferCounter = 0f;
            _coyoteTimeCounter = 0f;

            if (!_isJumpPressed)
            {
                CutJumpVelocity();
            }
        }
    }

    private void CheckGround()
    {
        //_isGrounded = Physics.CheckSphere(groundCheckPos.position, groundCheckRadius, groundLayer);

        _isGrounded = false;
        _currentFunctionPlatform = null;

        Collider[] colliders = Physics.OverlapSphere(groundCheckPos.position, groundCheckRadius, groundLayer);

        if (colliders.Length > 0)
        {
            _isGrounded = true;

            foreach (var col in colliders)
            {
                if (col.TryGetComponent(out PlatformFunction platform))
                {
                    _currentFunctionPlatform = platform;
                    break;
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (groundCheckPos != null)
        {
            Gizmos.color = _isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheckPos.position, groundCheckRadius);
        }
    }
}