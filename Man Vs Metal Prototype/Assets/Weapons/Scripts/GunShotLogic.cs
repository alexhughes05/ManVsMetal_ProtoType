using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunShotLogic
{
    private GunSO _gunData;
    private AudioSource _shotAudioSource;
    private Transform _shotLocation;
    private Transform _initialVisualProjectileSpawn;
    private ParticleSystem _muzzleFlash;

    public float TimeRemainingUntilNextShot { get; set; }
    public bool RepeatedShooting { get; set; }
    public Transform VisualShotLocation { get; set; }
    public int? NumOfShotsToFire { get; set; }
    public int ShotsFiredCount { get; set; }
    public GunShotLogic(GunSO gunData, AudioSource shotAudioSource, ParticleSystem muzzleFlash, Transform shotLocation)
    {
        _gunData = gunData;
        _shotAudioSource = shotAudioSource;
        _muzzleFlash = muzzleFlash;
        _shotLocation = shotLocation;
    }
    public void Shoot(Projectile spawnedProjectile)
    {
        TimeRemainingUntilNextShot = 1 / _gunData.fireRate;
        spawnedProjectile.Damage = _gunData.damage;

        //Muzzle Flash
        if (_muzzleFlash != null)
            _muzzleFlash.Play();

        //Shot Sound
        if (_gunData.gunFireSfx != null)
            PlayShooterSfx(_shotAudioSource, _gunData.gunFireSfx, true);

        AddForceOnProjectile(spawnedProjectile);

        #region Shoot Local Methods
        void AddForceOnProjectile(Projectile spawnedProjectile)
        {
            //Randomize Bloom
            var bloomX = UnityEngine.Random.Range(-_gunData.bloom, _gunData.bloom);
            var bloomY = UnityEngine.Random.Range(-_gunData.bloom, _gunData.bloom);

            Vector3 direction;

            if (_initialVisualProjectileSpawn != null)
            {
                //Calculate visual projectile direction
                direction = spawnedProjectile.CalculateVisualProjectileDirection(_shotLocation, _initialVisualProjectileSpawn, bloomX, bloomY);
            }
            else
            {
                direction = spawnedProjectile.CalculateProjectileDirection(_shotLocation, bloomX, bloomY);
            }

            spawnedProjectile.ApplyForceOnProjectile(direction * _gunData.muzzleVelocity, ForceMode.Impulse);
            spawnedProjectile.BulletTrailStartPos = _shotLocation.position;
        }
        #endregion
    }
    public void PlayShooterSfx(AudioSource audioSource, AudioClip audioClip, bool randomizePitch)
    {
        if (audioSource != null)
        {
            if (randomizePitch)
                audioSource.pitch = 1 - _gunData.pitchRandomization + UnityEngine.Random.Range(-_gunData.pitchRandomization, _gunData.pitchRandomization);
            else
                audioSource.pitch = 1;

            audioSource.clip = audioClip;
            audioSource.Play();
        }
    }
    public bool GunHasAmmoInClip()
    {
        //Able to shoot
        if (_gunData.RemainingAmmoInClip > 0 || _gunData.unlimitedAmmo)
            return true;
        else
            return false;
    }
    public bool NoAmmoRemaining()
    {
        if (_gunData.TotalAmmoRemaining <= 0 && !_gunData.unlimitedAmmo)
            return true;
        else
            return false;
    }
    public bool GunReadyToFire()
    {
        if (TimeRemainingUntilNextShot <= 0)
            return true;
        else
            return false;
    }
}

