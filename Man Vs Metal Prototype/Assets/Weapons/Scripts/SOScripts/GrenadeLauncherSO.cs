using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Grenade Launcher", menuName = "Weapon/Gun/GrenadeLauncher")]
public class GrenadeLauncherSO : GunSO
{
    private void OnEnable()
    {
        InitializeProperties();
        NumOfProjectiles = 1;
    }
}
