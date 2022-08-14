using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Handgun", menuName = "Weapon/Gun/Handgun")]
public class HandgunSO : GunSO
{
    private void OnEnable()
    {
        InitializeProperties();
        NumOfProjectiles = 1;
    }
}
