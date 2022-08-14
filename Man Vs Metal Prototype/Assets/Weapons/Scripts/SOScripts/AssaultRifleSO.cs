using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Assault Rifle", menuName = "Weapon/Gun/Assault Rifle")]
public class AssaultRifleSO : GunSO
{
    private void OnEnable()
    {
        InitializeProperties();
        NumOfProjectiles = 1;
    }
}
