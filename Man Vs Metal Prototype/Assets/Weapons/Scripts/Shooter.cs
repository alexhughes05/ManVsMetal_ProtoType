using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "New Shooter", menuName = "Shooter")]
public class Shooter : ScriptableObject
{
    public GameObject _prefab;
    [Header("Projectile")]
    public GameObject projectile;
    [Header("Ammo")]
    public bool unlimitedAmmo;
    public int maxAmmo;
    public int clipSize;
    [Header("Sound Effects")]
    public AudioClip gunFireSfx;
    [Range(0, 1)]
    public float pitchRandomization;
    public AudioClip reloadSfx;
    public AudioClip dryTrigger;
    [Header("Settings")]
    public float damage;
    public float muzzleVelocity;
    public FireModes[] fireModes;
    public int burstRounds;
    public float timeBtwBursts;
    public float fireRate;
    public float aimSpeed;
    public float bloom;
    public float numOfProjectiles;
    [Header("Recoil")]
    public float recoilX;
    public float recoilY;
    public float recoilZ;
    public float snappiness;
    public float returnSpeed;
}
