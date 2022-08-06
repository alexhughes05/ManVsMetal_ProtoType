using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Assault Rifle", menuName = "Shooter/Assault Rifle")]
public class AssaultRifleSO : ShooterSO
{
    private void OnEnable()
    {
        InitializeProperties();
        NumOfProjectiles = 1;
    }
}
