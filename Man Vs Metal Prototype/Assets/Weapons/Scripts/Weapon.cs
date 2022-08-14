using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Weapon : MonoBehaviour
{
    #region Inspector Fields
    [SerializeField] protected WeaponSO _weaponData;
    #endregion

    #region Properties
    public Camera WeaponCamera { get; set; }
    public Transform WeaponHolder { get; set; }
    #endregion

    #region Abstract Methods
    public abstract void Use(int numOfUses);
    public abstract Coroutine StartUsing();
    public abstract void StopUsing();
    #endregion
}
