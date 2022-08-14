using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class WeaponController : MonoBehaviour
{
    #region Inspector Fields
    [SerializeField] private WeaponSO[] _startingWeapons = new WeaponSO[2];
    [SerializeField] private Transform _weaponHolder;
    [SerializeField] private Camera _weaponCamera;
    #endregion

    #region LocalFields
    private Weapon _activeWeapon;
    private WeaponSO[] _equippedWeapons = new WeaponSO[2];
    private int _activeWeaponIndex;
    #endregion

    #region MonoBehaviour Methods
    private void Start()
    {
        if (_startingWeapons.Length > 0)
        {
            for (int i = 0; i < 2; i++)
            {
                if (_startingWeapons[i] != null)
                {
                    _startingWeapons[i].ResetData();
                    _equippedWeapons[i] = _startingWeapons[i];
                }
            }

            SetActiveWeapon(_equippedWeapons[0]);
        }
    }
    #endregion

    #region Public Methods
    public void SetActiveWeapon(WeaponSO weaponSO)
    {
        var weapon = weaponSO.prefab.GetComponent<Weapon>();

        if (_activeWeapon != null) Destroy(_activeWeapon.gameObject);

        if (weaponSO != null)
        {
            if (_activeWeapon != null)
            {
                var nextWeaponIndex = (_activeWeaponIndex + 1) % _equippedWeapons.Length;
                if (_equippedWeapons[nextWeaponIndex] == null)
                    _activeWeaponIndex = nextWeaponIndex;
            }

            GameObject newWeaponGo = Instantiate(weapon.gameObject, _weaponHolder.position, _weaponHolder.rotation, _weaponHolder);
            var newWeapon = newWeaponGo.GetComponent<Weapon>();
            if (newWeapon != null)
            {
                newWeapon.GetComponent<Weapon>().WeaponCamera = _weaponCamera;
                newWeapon.GetComponent<Weapon>().WeaponHolder = _weaponHolder;
                newWeapon.transform.localPosition = Vector3.zero;
                newWeapon.transform.localEulerAngles = Vector3.zero;
                _activeWeapon = newWeapon.GetComponent<Weapon>();
                _equippedWeapons[_activeWeaponIndex] = weaponSO;
            }
        }
        else
        {
            _activeWeapon = null;
        }
    }
    public void CycleWeapon()
    {
        if (_equippedWeapons[_activeWeaponIndex] == null || _equippedWeapons[1] == null)
            return;

        _activeWeaponIndex = ++_activeWeaponIndex % _equippedWeapons.Length;

        SetActiveWeapon(_equippedWeapons[_activeWeaponIndex]);
    }
    #endregion

    #region Event Handlers
    public void UseWeaponHandler(bool use)
    {
        if (_activeWeapon != null)
        {
            if (use)
                _activeWeapon.StartUsing();
            else
                _activeWeapon.StopUsing();
        }
    }
    #endregion
}
