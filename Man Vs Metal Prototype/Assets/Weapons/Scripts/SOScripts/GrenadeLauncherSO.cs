using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Grenade Launcher", menuName = "Shooter/Grenade Launcher")]
public class GrenadeLauncherSO : ShooterSO
{
    private void OnEnable()
    {
        InitializeProperties();
        NumOfProjectiles = 1;
    }
}
