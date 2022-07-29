using System;
using UnityEngine;

[RequireComponent(typeof(PlayerInputReader))]
public class PlayerInputHandler : MonoBehaviour
{
    //Inspector fields
    [Header("Jumping")]
    [SerializeField] private float _initialJumpForce;
    [SerializeField] private float _secondJumpForce;
    [SerializeField] private float _doubleJumpWindow;
    [Header("Crouching")]
    [SerializeField] private float _crouchSpeedMultiplier;
    [Header("Sprinting")]
    [SerializeField] private float _sprintSpeedMultiplier;

    //Private fields
    private float _jumpTimer;
    private bool _isCrouching;

    //Components and References
    private PlayerInputReader _playerInputReader;
    private Movement _movement;
    private GroundChecker _groundChecker;
    private WeaponController _weaponController;

    private void Awake()
    {
        _playerInputReader = GetComponent<PlayerInputReader>();
        _movement = GetComponent<Movement>();
        _groundChecker = GetComponent<GroundChecker>();
        _weaponController = FindObjectOfType<WeaponController>();
    }
    private void Start()
    {
        _jumpTimer = -1;
        _playerInputReader.JumpInput += JumpInputHandler;
        _playerInputReader.ShootInput += _weaponController.ShootingHandler;
        _playerInputReader.Scope += _weaponController.Scope;
        _playerInputReader.Reload += _weaponController.Reload;
        _playerInputReader.FireModeToggle += _weaponController.ChangeFireMode;
        _playerInputReader.Crouch += CrouchHandler;
        _playerInputReader.Sprint += SprintHandler;
    }

    private void Update()
    {
        if (_jumpTimer >= 0)
            _jumpTimer += Time.deltaTime;

        if (_jumpTimer > _doubleJumpWindow)
            ResetJumpTimer();
    }

    private void JumpInputHandler()
    {
        if (_groundChecker.IsGrounded())
        {
            StartJumpTimer();
            _movement.AddToMovementVector(new Vector3(0, _initialJumpForce, 0));
        }

        if (_jumpTimer > 0 && _jumpTimer <= _doubleJumpWindow)
        {
            ResetJumpTimer();
            _movement.AddToMovementVector(new Vector3(0, _secondJumpForce, 0));
        }
    }
    private void CrouchHandler()
    {
        _isCrouching = !_isCrouching;

        if (_isCrouching) 
        {
            _movement.CurrentSpeed *= _crouchSpeedMultiplier;
            transform.localScale = new Vector3(transform.localScale.x, 0.7f, transform.localScale.z);
            transform.position -= new Vector3(0, 0.3f, 0);
        }
        else
        {
            _movement.CurrentSpeed /= _crouchSpeedMultiplier;
            transform.localScale = new Vector3(transform.localScale.x, 1, transform.localScale.z);
            transform.position += new Vector3(0, 0.3f, 0);
        }
    }
    private void SprintHandler(bool shouldSprint)
    {
        if (shouldSprint)
            _movement.CurrentSpeed *= _sprintSpeedMultiplier;
        else
            _movement.CurrentSpeed /= _sprintSpeedMultiplier;
    }
    private void StartJumpTimer()
    {
        _jumpTimer = 0;
    }

    private void ResetJumpTimer()
    {
        _jumpTimer = -1;
    }

}
