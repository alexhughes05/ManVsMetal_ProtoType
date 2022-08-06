using UnityEngine;
using System;
using System.Collections;

public class PlayerInputReader : MonoBehaviour
{
    public PlayerInputActions PlayerInputActions { get; private set; }

    //Input Properties
    public event Action JumpInput;
    public event Action Mele;
    public event Action Crouch;
    public event Action FireModeToggle;
    public event Action CycleWeapon;
    public event Action Reload;
    public event Action<bool> ShootInput;
    public event Action<bool> Scope;
    public event Action<bool> Sprint;

    private void Awake()
    {
        PlayerInputActions = new PlayerInputActions();
        PlayerInputActions.Player.Enable();

        //Jumping
        PlayerInputActions.Player.Jump.performed += _ => JumpInput?.Invoke();
        //Mele
        PlayerInputActions.Player.Mele.performed += _ => Mele?.Invoke();
        //Crouch
        PlayerInputActions.Player.Crouch.performed += _ => Crouch?.Invoke();
        //FireModeToggle
        PlayerInputActions.Player.FireModeToggle.performed += _ => FireModeToggle?.Invoke();
        //Swap Weapon
        PlayerInputActions.Player.CycleWeapon.performed += _ => CycleWeapon?.Invoke();
        //Reload
        PlayerInputActions.Player.Reload.performed += _ => Reload?.Invoke();
        //Shooting
        PlayerInputActions.Player.Shoot.performed += _ => ShootInput?.Invoke(true);
        PlayerInputActions.Player.Shoot.canceled += _ => ShootInput?.Invoke(false);
        //Scope
        PlayerInputActions.Player.Scope.performed += _ => Scope?.Invoke(true);
        PlayerInputActions.Player.Scope.canceled += _ => Scope?.Invoke(false);
        //Sprint
        PlayerInputActions.Player.Sprint.performed += _ => Sprint?.Invoke(true);
        PlayerInputActions.Player.Sprint.canceled += _ => Sprint?.Invoke(false);
    }
}
