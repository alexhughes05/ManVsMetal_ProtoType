using UnityEngine;
using System;

public class PlayerInputReader : MonoBehaviour
{
    public PlayerInputActions PlayerInputActions { get; private set; }

    //Input Properties
    public event Action JumpInput;
    public event Action ShootInput;
    public event Action Mele;
    public event Action Reload;
    public event Action FireModeToggle;
    public event Action<bool> Scope;
    public event Action<bool> Crouch;
    public event Action<bool> Sprint;

    private void Awake()
    {
        PlayerInputActions = new PlayerInputActions();
        PlayerInputActions.Player.Enable();

        //Jumping
        PlayerInputActions.Player.Jump.performed += _ => JumpInput?.Invoke();
        //Shooting
        PlayerInputActions.Player.Shoot.performed += _ => ShootInput?.Invoke();
        //Mele
        PlayerInputActions.Player.Mele.performed += _ => Mele?.Invoke();
        //Reload
        PlayerInputActions.Player.Reload.performed += _ => Reload?.Invoke();
        //FireModeToggle
        PlayerInputActions.Player.FireModeToggle.performed += _ => FireModeToggle?.Invoke();
        //Scope
        PlayerInputActions.Player.Scope.performed += _ => Scope?.Invoke(true);
        PlayerInputActions.Player.Scope.canceled += _ => Scope?.Invoke(false);
        //Crouch
        PlayerInputActions.Player.Crouch.performed += _ => Crouch?.Invoke(true);
        PlayerInputActions.Player.Crouch.canceled += _ => Crouch?.Invoke(false);
        //Sprint
        PlayerInputActions.Player.Sprint.performed += _ => Sprint?.Invoke(true);
        PlayerInputActions.Player.Sprint.canceled += _ => Sprint?.Invoke(false);
    }
}
