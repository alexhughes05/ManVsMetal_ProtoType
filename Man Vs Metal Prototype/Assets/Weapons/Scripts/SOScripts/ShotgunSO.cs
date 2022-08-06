using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Shotgun", menuName = "Shooter/Shotgun")]
public class ShotgunSO : ShooterSO
{
    [Header("Unique to shotgun")]
    public int numOfPellots;
    private void OnEnable()
    {
        InitializeProperties();
        NumOfProjectiles = numOfPellots;
    }
}
