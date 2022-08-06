using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Sniper", menuName = "Shooter/Sniper")]
public class SniperSO : ShooterSO
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
