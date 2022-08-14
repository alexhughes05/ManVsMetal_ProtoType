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
    public event Action<bool> Sprint;
    public event Action<bool> AttackInput;
    public event Func<bool> Reload;
    public event Func<bool> ScopeIn;
    public event Func<bool> ScopeOut;

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
        //Shooting
        PlayerInputActions.Player.Attack.performed += _ => AttackInput?.Invoke(true);
        PlayerInputActions.Player.Attack.canceled += _ => AttackInput?.Invoke(false);
        //Scope
        PlayerInputActions.Player.Scope.performed += _ => ScopeIn?.Invoke();
        PlayerInputActions.Player.Scope.canceled += _ => ScopeOut?.Invoke();
        //Sprint
        PlayerInputActions.Player.Sprint.performed += _ => Sprint?.Invoke(true);
        PlayerInputActions.Player.Sprint.canceled += _ => Sprint?.Invoke(false);
        //Reload
        PlayerInputActions.Player.Reload.performed += _ => Reload?.Invoke();
    }
}
