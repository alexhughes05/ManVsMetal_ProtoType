using UnityEngine;
using System;

public class PlayerInputReader : MonoBehaviour
{
    public PlayerInputActions PlayerInputActions { get; private set; }

    //Input Properties
    public event Action JumpInput;
    public event Action ShootInput;

    private void Awake()
    {
        PlayerInputActions = new PlayerInputActions();
        PlayerInputActions.Player.Enable();

        //Jumping
        PlayerInputActions.Player.Jump.performed += _ => JumpInput?.Invoke();
        PlayerInputActions.Player.Shoot.performed += _ => ShootInput?.Invoke();
    }
}
