using UnityEngine;

public class WeaponDataAssigner : MonoBehaviour
{
    [SerializeField] private GunSO _weaponData;

    public GunSO WeaponData 
    { 
        get { return _weaponData; } 
    }
}
