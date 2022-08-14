using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Shotgun", menuName = "Weapon/Gun/Shotgun")]
public class ShotgunSO : GunSO
{
    [Header("Unique to shotgun")]
    public int numOfPellots;
    private void OnEnable()
    {
        InitializeProperties();
        NumOfProjectiles = numOfPellots;
    }
}
