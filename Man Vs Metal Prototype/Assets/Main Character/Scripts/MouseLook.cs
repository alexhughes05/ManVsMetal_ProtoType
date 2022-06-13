using UnityEngine;
using UnityEngine.InputSystem;

public class MouseLook : MonoBehaviour
{
    //Inspector Fields
    [SerializeField] private Transform _playerBody;
    [SerializeField] private float _horizontalSensitivity;
    [SerializeField] private float _verticalSensitivity;

    //Private Fields
    private float _xInput;
    private float _yInput;
    private bool _inputInitialized;
    private float _xAxisRotation;
    private Vector3 _rbRotationChange;
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
    void Update()
    {
        _yInput = _aimRef.ReadValue<Vector2>().y;

        if (_inputInitialized)
            RotateCameraAroundXAxis();

        if (_yInput != 0)
            _inputInitialized = true;
    }

    private void FixedUpdate()
    {
        RotateRigidbodyAroundYAxis();
    }
    private void RotateCameraAroundXAxis()
    {
        _xAxisRotation -= _yInput * _verticalSensitivity * Time.deltaTime;
        _xAxisRotation = Mathf.Clamp(_xAxisRotation, -90, 90);
        transform.localRotation = Quaternion.Euler(_xAxisRotation, 0f, 0f);
    }
    private void RotateRigidbodyAroundYAxis()
    {
        _xInput = _aimRef.ReadValue<Vector2>().x;
        _rbRotationChange.y = _xInput * _horizontalSensitivity * Time.fixedDeltaTime;
        _rb.MoveRotation(_rb.rotation * Quaternion.Euler(_rbRotationChange));
    }
}
