using UnityEngine;

[RequireComponent(typeof(GroundChecker))]
public class Movement : MonoBehaviour
{
    //Inspector fields
    [SerializeField] private float _speed;
    [SerializeField] private float _gravityMultiplier;

    //private fields
    private Vector3 _inputVector;
    private Vector3 _movementVector;
    private bool _airborneLastFrame;
    private Vector3 _playerForward;
    private Vector3 _playerRight;

    //Components and References
    private Rigidbody _rb;
    private GroundChecker _groundChecker;
    private PlayerInputReader _playerInputReader;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _groundChecker = GetComponent<GroundChecker>();
        _playerInputReader = GetComponent<PlayerInputReader>();
    }
    private void Update()
    {
        _inputVector = _playerInputReader.PlayerInputActions.Player.Movement.ReadValue<Vector2>();
    }

    private void FixedUpdate()
    {
        UpdateMovementVector();
        _rb.MovePosition(transform.position + Time.fixedDeltaTime * _movementVector);
    }

    private void UpdateMovementVector()
    {
        CalculateGravity();
        CalculateRelativeMovement();

        void CalculateGravity()
        {
            if (_groundChecker.IsGrounded() == false)
            {
                _airborneLastFrame = true;
                _movementVector.y += Physics.gravity.y * _gravityMultiplier * Time.fixedDeltaTime;
            }
            else
            {
                if (_airborneLastFrame)
                {
                    _movementVector.y = 0;
                    _airborneLastFrame = false;
                }
            }
        }

        void CalculateRelativeMovement()
        {
            _playerForward = transform.forward;
            _playerRight = transform.right;
            _playerForward = _playerForward.normalized;
            _playerRight = _playerRight.normalized;
            var tempYValue = _movementVector.y;
            _movementVector = (_playerForward * _inputVector.y + _playerRight * _inputVector.x + Vector3.up * _movementVector.y) * _speed;
            _movementVector.y = tempYValue;
        }
    }

    public void AddToMovementVector(Vector3 addedVector)
    {
        _movementVector += addedVector;
    }
}
