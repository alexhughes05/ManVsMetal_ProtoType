using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponPickup : MonoBehaviour
{
    [SerializeField] private ShooterSO _weaponToEquip;
    private void OnTriggerEnter(Collider other)
    {
        var weaponController = other.gameObject.GetComponent<WeaponController>();
        if (weaponController != null)
        {
            //Fill Ammo to max
            _weaponToEquip.RemainingAmmoInClip = _weaponToEquip.clipSize;
            _weaponToEquip.RemainingBurstRounds = _weaponToEquip.burstRounds;
            _weaponToEquip.TotalAmmoRemaining = _weaponToEquip.maxAmmo;

            weaponController.SetActiveWeapon(_weaponToEquip);
        }
    }
}
