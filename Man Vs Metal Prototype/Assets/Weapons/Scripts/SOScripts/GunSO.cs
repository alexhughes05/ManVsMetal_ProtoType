using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "New Gun", menuName = "Weapon/Gun")]
public abstract class GunSO : WeaponSO
{
    #region Inspector
    [Header("Crosshair")]
    public GameObject crossHair;
    [Header("Projectile")]
    public GameObject projectile;
    [Header("Ammo")]
    public bool unlimitedAmmo;
    public int maxAmmo;
    public int clipSize;
    [Header("Scope")]
    public float scopeSpeed;
    public float scopedRecoilReductionFactor;
    public float scopedAimSensitivityReductionFactor;
    [Header("Sound Effects")]
    public AudioClip gunFireSfx;
    [Range(0, 1)]
    public float pitchRandomization;
    public float timeToWaitAfterShotToReload;
    public float timeDelayBeforeCocking;
    public AudioClip refillAmmoSfx;
    public AudioClip cockSfx;
    public AudioClip dryTrigger;
    [Header("Gun Settings")]
    public float muzzleVelocity;
    public FireModes[] fireModes;
    public int burstRounds;
    public float timeBtwBursts;
    public float fireRate;
    public float bloom;
    [Header("Recoil")]
    public float recoilX;
    public float recoilY;
    public float recoilZ;
    public float snappiness;
    public float returnSpeed;
    #endregion

    protected void InitializeProperties()
    {
        RemainingAmmoInClip = -1;
        RemainingBurstRounds = -1;
        TotalAmmoRemaining = -1;
        CurrentFireModeIndex = 0;
        IsRefillingAmmo = false;
        IsCocking = false;
    }

    #region Properties
    public int NumOfProjectiles { get; protected set; }
    public int CurrentFireModeIndex { get; set; }
    public GameObject ScopeOverlay { get; protected set; }
    public float ScopedFov { get; protected set; }
    public bool IsRefillingAmmo { get; set; }
    public bool IsCocking { get; set; }
    public int RemainingAmmoInClip { get; set; }
    public int RemainingBurstRounds { get; set; }
    public int TotalAmmoRemaining { get; set; }
    #endregion

    public override void ResetData()
    {
        RemainingAmmoInClip = clipSize;
        RemainingBurstRounds = burstRounds;
        TotalAmmoRemaining = maxAmmo;
    }
}
