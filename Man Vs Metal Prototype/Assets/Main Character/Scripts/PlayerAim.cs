using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAim : MonoBehaviour
{
    //Inspector Fields
    [SerializeField] private Transform _playerBody;
    [SerializeField] private Vector3 _viewpointOffset;
    [SerializeField][Range(0.01f, 1f)] private float cameraSmoothSpeed;
    [SerializeField] private bool enableLerpedRotation;
    [SerializeField] private float rotationDampening;
    [SerializeField] private float _horizontalSensitivity;
    [SerializeField] private float _verticalSensitivity;

    //Private Fields
    private Vector2 _inputVector;
    private bool _inputInitialized;
    private float _cameraPitch;
    private float _cameraYaw;
    private Vector3 _cameraVelocity;
    private InputAction _aimRef;

    //Components/References
    private Rigidbody _rb;
    private PlayerInputReader _playerInputReader;

    private void Awake()
    {
        _rb = _playerBody.GetComponent<Rigidbody>();
        _playerInputReader = _playerBody.GetComponent<PlayerInputReader>();
    }
    private void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        _aimRef = _playerInputReader.PlayerInputActions.Player.Aim;
    }
    private void FixedUpdate()
    {
        SyncRigidbodyYawWithCamera();
    }
    void Update()
    {
        _inputVector = _aimRef.ReadValue<Vector2>();

        if (_inputInitialized)
            Rotate();

        if (_inputVector.y != 0)
            _inputInitialized = true;
    }
    private void LateUpdate()
    {
        var desiredPosition = _playerBody.position + _viewpointOffset;
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref _cameraVelocity, cameraSmoothSpeed);
    }

    private void SyncRigidbodyYawWithCamera()
    {
        _rb.MoveRotation(Quaternion.Euler(0, _cameraYaw, 0f));
    }

    private void Rotate()
    {
        _cameraPitch -= _inputVector.y * _verticalSensitivity * Time.deltaTime;
        _cameraPitch = Mathf.Clamp(_cameraPitch, -90, 90);
        _cameraYaw += _inputVector.x * _horizontalSensitivity * Time.deltaTime;
        var qRot = Quaternion.Euler(_cameraPitch, _cameraYaw, 0f);
        if (enableLerpedRotation)
            transform.rotation = Quaternion.Lerp(transform.rotation, qRot, Time.deltaTime * rotationDampening);
        else
            transform.rotation = qRot;
    }
}
