using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Sniper", menuName = "Weapon/Gun/Sniper")]
public class SniperSO : GunSO
{
    [Header("Unique to Sniper")]
    public GameObject scopeOverlay;
    public float scopedFov;
    private void OnEnable()
    {
        InitializeProperties();
        NumOfProjectiles = 1;
        ScopeOverlay = scopeOverlay;
        ScopedFov = scopedFov;
    }
}
