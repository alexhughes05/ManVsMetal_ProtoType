using System;
using UnityEngine;

[RequireComponent(typeof(PlayerInputReader))]
public class PlayerInputHandler : MonoBehaviour
{
    //Inspector fields
    [SerializeField] private float _initialJumpForce;
    [SerializeField] private float _secondJumpForce;
    [SerializeField] private float _doubleJumpWindow;

    //Private fields
    private float _jumpTimer;

    //Components and References
    private PlayerInputReader _playerInputReader;
    private Movement _movement;
    private GroundChecker _groundChecker;
    private Shooter _shooter;

    private void Awake()
    {
        _playerInputReader = GetComponent<PlayerInputReader>();
        _movement = GetComponent<Movement>();
        _groundChecker = GetComponent<GroundChecker>();
        _shooter = FindObjectOfType<Shooter>();
    }
    private void Start()
    {
        _jumpTimer = -1;
        _playerInputReader.JumpInput += JumpInputHandler;
        _playerInputReader.ShootInput += _shooter.Shoot;
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
    private void StartJumpTimer()
    {
        _jumpTimer = 0;
    }

    private void ResetJumpTimer()
    {
        _jumpTimer = -1;
    }

}
