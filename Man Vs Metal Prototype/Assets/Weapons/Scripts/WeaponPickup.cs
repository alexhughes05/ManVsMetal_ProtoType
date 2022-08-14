using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponPickup : MonoBehaviour
{
    [SerializeField] private WeaponSO _weaponToEquip;

    private void OnTriggerEnter(Collider other)
    {
        var weaponController = other.gameObject.GetComponent<WeaponController>();
        if (weaponController != null)
        {
            _weaponToEquip.ResetData();
            weaponController.SetActiveWeapon(_weaponToEquip);
        }
    }
}
