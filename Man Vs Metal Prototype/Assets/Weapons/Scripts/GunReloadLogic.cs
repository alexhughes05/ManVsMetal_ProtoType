using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunReloadLogic
{
    private GunSO _gunData;
    private AudioSource _reloadAudioSource;
    public GunReloadLogic(GunSO gunData, AudioSource reloadAudioSource)
    {
        _gunData = gunData;
        _reloadAudioSource = reloadAudioSource;
    }

    public bool IsCurrentlyReloading()
    {
        var isReloading = false;

        if (_gunData != null)
        {
            if (_gunData.IsRefillingAmmo || _gunData.IsCocking)
                isReloading = true;
        }

        return isReloading;
    }
    public IEnumerator RefillAmmoAfterDelay(float timeBeforeReload)
    {
        _gunData.IsRefillingAmmo = true;

        yield return new WaitForSeconds(timeBeforeReload);

        if (_reloadAudioSource != null)
        {
            if (_gunData.refillAmmoSfx != null)
                PlayReloadSfx(_reloadAudioSource, _gunData.refillAmmoSfx);

            yield return new WaitForSeconds(_gunData.refillAmmoSfx.length);
        }

        //Update Ammo Count
        if (_gunData.fireModes[_gunData.CurrentFireModeIndex] == FireModes.Burst)
        {
            _gunData.RemainingBurstRounds = _gunData.burstRounds - 1;
            //StopCoroutine(_shootingCoroutine);
        }

        if (_gunData.TotalAmmoRemaining > _gunData.clipSize)
            _gunData.RemainingAmmoInClip = _gunData.clipSize;
        else
            _gunData.RemainingAmmoInClip = _gunData.TotalAmmoRemaining;
    }
    public IEnumerator CockGunAfterDelay(float timeBeforeCock)
    {
        _gunData.IsCocking = true;

        yield return new WaitForSeconds(timeBeforeCock);

        if (_reloadAudioSource != null)
        {
            if (_gunData.cockSfx != null)
                PlayReloadSfx(_reloadAudioSource, _gunData.cockSfx);
            yield return new WaitForSeconds(_gunData.cockSfx.length);
        }

        _gunData.IsRefillingAmmo = false;
        _gunData.IsCocking = false;

    }
    public bool CanReload()
    {
        return !IsCurrentlyReloading() && _gunData.RemainingAmmoInClip < _gunData.clipSize && _gunData.TotalAmmoRemaining > 0;
    }
    public bool ShouldCockGunAfterShot()
    {
        return _gunData.fireModes[_gunData.CurrentFireModeIndex] == FireModes.SingleFire && _gunData.RemainingAmmoInClip > 0;
    }
    private void PlayReloadSfx(AudioSource audioSource, AudioClip audioClip)
    {
        if (audioSource != null)
        {
            audioSource.clip = audioClip;
            audioSource.Play();
        }
    }
}
