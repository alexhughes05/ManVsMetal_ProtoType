using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class MouseLook : MonoBehaviour
{
    //Inspector Fields
    [SerializeField] private Transform _playerBody;
    [SerializeField] private float _horizontalSensitivity;
    [SerializeField] private float _verticalSensitivity;

    //Private Fields
    private float _mouseX;
    private float _mouseY;
    private bool _mouseInitialized;
    private float _xAxisRotation;
    private Vector3 _rbDeltaRotation;

    //Components/References
    private Rigidbody _rb;

    private void Awake()
    {
        _rb = _playerBody.GetComponent<Rigidbody>();
    }
    private void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
    void Update()
    {
        _mouseX = Mouse.current.delta.x.ReadValue() * _horizontalSensitivity * 4 * Time.deltaTime;
        _mouseY = Mouse.current.delta.y.ReadValue() * _verticalSensitivity * Time.deltaTime;
        
        if (_mouseInitialized)
        {
            _xAxisRotation -= _mouseY;
            _xAxisRotation = Mathf.Clamp(_xAxisRotation, -90, 90);
            transform.localRotation = Quaternion.Euler(_xAxisRotation, 0f, 0f);
        }

        if (_mouseY != 0)
            _mouseInitialized = true;
    }

    private void FixedUpdate()
    {
        _rbDeltaRotation.y = _mouseX;
        _rb.MoveRotation(_rb.rotation * Quaternion.Euler(_rbDeltaRotation));
    }
}
