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
    private Mouse _mouseRef;
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

        _mouseRef = Mouse.current;
        _aimRef = _playerInputReader.PlayerInputActions.Player.Aim;
    }
    void Update()
    {
        ReadXAndYInputs();

        if (_inputInitialized)
            RotateCameraAroundXAxis();

        if (_yInput != 0)
            _inputInitialized = true;
    }

    private void FixedUpdate()
    {
        RotateRigidbodyAroundYAxis();
    }
    private void ReadXAndYInputs()
    {
        if (_aimRef.phase == InputActionPhase.Started || _aimRef.phase == InputActionPhase.Performed) //Controller Input
        {
            _xInput = _aimRef.ReadValue<Vector2>().x * _horizontalSensitivity * 35 * Time.deltaTime;
            _yInput = _aimRef.ReadValue<Vector2>().y * _verticalSensitivity * 3 * Time.deltaTime;
        }
        else //Mouse Input
        {
            _xInput = _mouseRef.delta.x.ReadValue() * _horizontalSensitivity * 3 * Time.deltaTime;
            _yInput = _mouseRef.delta.y.ReadValue() * _verticalSensitivity * 0.7f * Time.deltaTime;
        }
    }
    private void RotateCameraAroundXAxis()
    {
        _xAxisRotation -= _yInput;
        _xAxisRotation = Mathf.Clamp(_xAxisRotation, -90, 90);
        transform.localRotation = Quaternion.Euler(_xAxisRotation, 0f, 0f);
    }
    private void RotateRigidbodyAroundYAxis()
    {
        _rbRotationChange.y = _xInput;
        _rb.MoveRotation(_rb.rotation * Quaternion.Euler(_rbRotationChange));
    }
}
