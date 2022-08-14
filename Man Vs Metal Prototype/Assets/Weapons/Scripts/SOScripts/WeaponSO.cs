using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon", menuName = "Weapon")]
public abstract class WeaponSO : ScriptableObject
{
    [Header("Prefab")]
    public GameObject prefab;
    [Header("Damage")]
    public float damage;

    public abstract void ResetData();

}
