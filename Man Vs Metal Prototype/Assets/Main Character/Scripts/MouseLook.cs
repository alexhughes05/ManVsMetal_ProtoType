using UnityEngine;
using UnityEngine.InputSystem;

public class MouseLook : MonoBehaviour
{
    //Inspector Fields
    [SerializeField] private Transform _playerBody;
    [SerializeField] private float _horizontalSensitivity;
    [SerializeField] private float _verticalSensitivity;

    //Private Fields
    private Vector2 _inputVector;
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
        _inputVector = _aimRef.ReadValue<Vector2>();
        Debug.Log("update value read. Y input is currently " + _inputVector.y + " at frame " + Time.frameCount);

        if (_inputInitialized)
            RotateCameraAroundXAxis();

        if (_inputVector.y != 0)
            _inputInitialized = true;
    }

    private void FixedUpdate()
    {
        RotateRigidbodyAroundYAxis();
    }
    private void RotateCameraAroundXAxis()
    {
        _xAxisRotation -= _inputVector.y * _verticalSensitivity * Time.deltaTime;
        _xAxisRotation = Mathf.Clamp(_xAxisRotation, -90, 90);
        transform.localRotation = Quaternion.Euler(_xAxisRotation, 0f, 0f);
    }
    private void RotateRigidbodyAroundYAxis()
    {
        Debug.Log("fixedUpdate value read. x input is currently " + _inputVector.x + " at frame " + Time.frameCount);
        _rbRotationChange.y = _inputVector.x * _horizontalSensitivity * Time.fixedDeltaTime;
        _rb.MoveRotation(_rb.rotation * Quaternion.Euler(_rbRotationChange));
    }
}
