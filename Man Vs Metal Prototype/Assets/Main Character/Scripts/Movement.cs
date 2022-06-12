using UnityEngine;

[RequireComponent(typeof(GroundChecker))]
public class Movement : MonoBehaviour
{
    //Inspector fields
    [SerializeField] private float speed;

    //private fields
    private Vector3 _inputVector;
    private Vector3 _movementVector;
    private bool airborneLastFrame;
    private Vector3 playerForward;
    private Vector3 playerRight;

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
                airborneLastFrame = true;
                _movementVector.y += Physics.gravity.y * Time.fixedDeltaTime;
            }
            else
            {
                if (airborneLastFrame)
                {
                    _movementVector.y = 0;
                    airborneLastFrame = false;
                }
            }
        }

        void CalculateRelativeMovement()
        {
            playerForward = transform.forward;
            playerRight = transform.right;
            playerForward = playerForward.normalized;
            playerRight = playerRight.normalized;
            var tempYValue = _movementVector.y;
            _movementVector = (playerForward * _inputVector.y + playerRight * _inputVector.x + Vector3.up * _movementVector.y) * speed;
            _movementVector.y = tempYValue;
        }
    }

    public void AddToMovementVector(Vector3 addedVector)
    {
        _movementVector += addedVector;
    }
}
