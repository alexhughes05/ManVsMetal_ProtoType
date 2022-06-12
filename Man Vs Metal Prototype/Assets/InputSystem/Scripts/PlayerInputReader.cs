using UnityEngine;
using System;

public class PlayerInputReader : MonoBehaviour
{
    public PlayerInputActions PlayerInputActions { get; private set; }

    //Input Properties
    public event Action JumpInput;

    private void Awake()
    {
        PlayerInputActions = new PlayerInputActions();
        PlayerInputActions.Player.Enable();

        //Jumping
        PlayerInputActions.Player.Jump.performed += _ => JumpInput?.Invoke();
    }
}
