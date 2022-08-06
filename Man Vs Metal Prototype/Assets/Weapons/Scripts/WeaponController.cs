using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WeaponController : MonoBehaviour
{
    #region Inspector Fields
    [SerializeField] private ShooterSO[] _startingWeapons = new ShooterSO[2];
    [SerializeField] private Transform _weaponParent;
    [SerializeField] private Camera _weaponCamera;
    [SerializeField] private AudioSource _shotAudioSource;
    [SerializeField] private AudioSource _reloadAudioSource;
    [SerializeField] private AudioClip _hitMarkerSfx;
    [SerializeField] private Image _hitMarkerImage;
    #endregion

    #region LocalFields
    //Colors
    private Color clearWhite = new(1, 1, 1, 0);

    //Prefab Transforms
    private Transform _anchor;
    private Transform _hipState;
    private Transform _aimState;
    private Transform _muzzle;
    //Particle Systems
    private ParticleSystem _muzzleFlash;
    //Coroutines
    private Coroutine _scopeCoroutine;
    private Coroutine _shootingCoroutine;
    private Coroutine _reloadCoroutine;
    //Camera
    private Camera _mainCam;
    //Weapon Info 
    private GameObject _activeWeapon;
    private ShooterSO[] _equippedWeapons = new ShooterSO[2];
    private ShooterSO _currentShooterData;
    private int _activeWeaponIndex;
    //Shooting Info
    private bool _repeatedShooting;
    private float _timeRemainingUntilNextShot;
    //Reloading
    private bool _manualReload;
    //Projectiles
    private List<GameObject> _spawnedProjectiles = new();
    //Hitmarker
    private float _hitMarkerOpaqueTime;
    //Scope
    private GameObject _scopeOverlay;
    private GameObject _crossHair;
    private float _normalFov;
    //Recoil
    private bool _computeRecoil;

    //Components/References
    private Recoil _recoil;
    private Movement _movement;
    private PlayerAim _playerAim;
    #endregion

    #region MonoBehaviour Methods
    private void Awake()
    {
        _mainCam = Camera.main;
        _movement = GetComponent<Movement>();
        _playerAim = GetComponent<PlayerAim>();
    }

    private void Start()
    {
        _hitMarkerImage.color = clearWhite;

        if (_startingWeapons.Length > 0)
        {
            for (int i = 0; i < 2; i++)
                _equippedWeapons[i] = _startingWeapons[i];

            SetActiveWeapon(_equippedWeapons[0]);
        }
    }
    void Update()
    {
        if (_currentShooterData != null)
        {
            //Debug.Log($"IsRefillingAmmo: {_currentShooterData.IsRefillingAmmo}");
            //Debug.Log($"IsCocking: {_currentShooterData.IsCocking}");
            //Debug.Log($"AmmoLeftInMag: {_currentShooterData.RemainingAmmoInClip}");
        }
        //Debug.Log("Total ammo is: " + _totalProjectilesRemaining);
        //if (_currentShooterData != null)
        //Debug.Log("Ammo left in clip: " + _currentShooterData.RemainingAmmoInClip);

        //if (_currentWeapon)
        //Debug.Log("Current FireMode is: " + _currentShooterData.fireModes[_currentFireModeIndex]);

        //Always aim weapon holder towards the reticle on the screen (center of camera)
        _weaponParent.rotation = _mainCam.transform.rotation;

        UpdateTimeUntilNextShot();

        //Will decrement the hitmarker opaque time and make it fade out when the time is 0
        UpdateHitmarkerOpaqueTime();

        if (_recoil != null && _computeRecoil)
            _computeRecoil = _recoil.CalculateRecoil(GetComponent<PlayerAim>());
    }
    #endregion

    #region Public Methods
    public void SetActiveWeapon(ShooterSO shooter)
    {
        //Cancel current reload if you swap weapons
        StopAllCoroutines();
        _repeatedShooting = false;

        if (_currentShooterData != null)
        {
            _currentShooterData.IsRefillingAmmo = false;
        }

        if (_reloadAudioSource != null && _reloadAudioSource.isPlaying)
        {
            _reloadAudioSource.Stop();
        }

        if (_activeWeapon != null) Destroy(_activeWeapon);
        if (_scopeOverlay != null) Destroy(_scopeOverlay);
        if (_crossHair != null) Destroy(_crossHair);


        if (shooter != null)
        {

            if (_activeWeapon == null)
                _recoil = new Recoil(shooter.recoilX, shooter.recoilY, shooter.recoilZ, shooter.snappiness, shooter.returnSpeed);
            else
            {
                var nextWeaponIndex = (_activeWeaponIndex + 1) % _equippedWeapons.Length;
                if (_equippedWeapons[nextWeaponIndex] == null)
                    ++_activeWeaponIndex;

                _recoil.RecoilX = shooter.recoilX;
                _recoil.RecoilY = shooter.recoilY;
                _recoil.RecoilZ = shooter.recoilZ;
                _recoil.Snappiness = shooter.snappiness;
                _recoil.ReturnSpeed = shooter.returnSpeed;
            }

            GameObject newWeapon = Instantiate(shooter._prefab, _weaponParent.position, _weaponParent.rotation, _weaponParent);
            newWeapon.transform.localPosition = Vector3.zero;
            newWeapon.transform.localEulerAngles = Vector3.zero;
            _activeWeapon = newWeapon;
            _currentShooterData = shooter;

            _equippedWeapons[_activeWeaponIndex] = _currentShooterData;
        }
        else
        {
            _activeWeapon = null;
            _currentShooterData = null;
        }


        if (_currentShooterData != null)
        {
            if (_currentShooterData.IsCocking)
            {
                _currentShooterData.IsRefillingAmmo = true;
                StartCoroutine(CockAfterDelay(_currentShooterData.timeToWaitAfterAmmoRefillToCock));
            }

            if (_currentShooterData.ScopeOverlay != null)
            {
                _scopeOverlay = Instantiate(_currentShooterData.ScopeOverlay);
                _scopeOverlay.SetActive(false);
            }

            if (_currentShooterData.crossHair != null)
                _crossHair = Instantiate(_currentShooterData.crossHair);

            //Setup anchor and resources
            _anchor = _activeWeapon.transform.Find("Anchor");
            _hipState = _activeWeapon.transform.Find("States/Hip");
            _aimState = _activeWeapon.transform.Find("States/Aim");
            _muzzle = _activeWeapon.transform.Find("Anchor/Resources/Muzzle");
            if (_muzzle.childCount > 0)
                _muzzleFlash = _muzzle.GetChild(0).GetComponent<ParticleSystem>();
        }
    }

    #region Event Handlers

    public void ReloadHandler()
    {
        if (!IsCurrentlyReloading() && _currentShooterData.RemainingAmmoInClip < _currentShooterData.clipSize && _currentShooterData.TotalAmmoRemaining > 0)
        {
            _manualReload = true;
            StartCoroutine(AttemptToReload());
        }
    }
    public IEnumerator AttemptToReload()
    {
        //Refilling the weapon ammo
        if (_currentShooterData.RemainingAmmoInClip <= 0 || _manualReload)
        {
            _manualReload = false;
            yield return StartCoroutine(RefillAmmoAfterDelay(_currentShooterData.timeToWaitAfterShotToReload));
        }

        //Cocking the weapon
        if (_currentShooterData.IsRefillingAmmo || _currentShooterData.fireModes[_currentShooterData.CurrentFireModeIndex] == FireModes.SingleFire)
        {
            yield return StartCoroutine(CockAfterDelay(_currentShooterData.timeToWaitAfterAmmoRefillToCock));
        }

        _currentShooterData.IsRefillingAmmo = false;
    }
    public void ShootingHandler(bool shootingTriggerHeldDown)
    {
        if (_activeWeapon)
        {
            //Initialize burstRounds
            if (_currentShooterData.RemainingBurstRounds <= 0 && !_repeatedShooting)
                _currentShooterData.RemainingBurstRounds = _currentShooterData.burstRounds - 1;

            if (shootingTriggerHeldDown)
            {

                _shootingCoroutine = StartCoroutine(AttemptToShoot());
            }
            else
            {
                if (_currentShooterData.fireModes[_currentShooterData.CurrentFireModeIndex] == FireModes.FullAuto)
                    StopCoroutine(_shootingCoroutine);
            }
        }
    }
    public void CycleWeapon()
    {
        _activeWeaponIndex = ++_activeWeaponIndex % _equippedWeapons.Length;

        //If you only have one weapon, dont allow to cycle
        if (_equippedWeapons[_activeWeaponIndex] == null)
            return;

        SetActiveWeapon(_equippedWeapons[_activeWeaponIndex]);
    }
    public void ChangeFireMode()
    {
        if (_currentShooterData.fireModes.Length <= 1)
            return;

        _currentShooterData.CurrentFireModeIndex = ++_currentShooterData.CurrentFireModeIndex % _currentShooterData.fireModes.Length;
    }
    public void Scope(bool isScoping)
    {
        if (_currentShooterData != null)
        {
            if (isScoping)
            {
                _crossHair.SetActive(false);

                if (_scopeCoroutine != null)
                    StopCoroutine(_scopeCoroutine);
                _scopeCoroutine = StartCoroutine(ExecuteScopeEffect(_aimState.localPosition, 1 / _currentShooterData.scopeSpeed));

                if (_movement != null)
                    _movement.CurrentSpeed /= 3;

                if (_playerAim != null)
                {
                    _playerAim.VerticalSensitivity /= _currentShooterData.scopedAimSensitivityReductionFactor;
                    _playerAim.HorizontalSensitivity /= _currentShooterData.scopedAimSensitivityReductionFactor;
                }

                if (_recoil != null)
                {
                    _recoil.RecoilX /= _currentShooterData.scopedRecoilReductionFactor;
                    _recoil.RecoilY /= _currentShooterData.scopedRecoilReductionFactor;
                    _recoil.RecoilZ /= _currentShooterData.scopedRecoilReductionFactor;
                }
            }
            else
            {
                if (_scopeCoroutine != null)
                    StopCoroutine(_scopeCoroutine);
                _scopeCoroutine = StartCoroutine(ExecuteScopeEffect(_hipState.localPosition, 1 / _currentShooterData.scopeSpeed));
                _crossHair.SetActive(true);

                if (_currentShooterData.ScopeOverlay)
                    OnUnscope();

                if (_movement != null)
                    _movement.CurrentSpeed *= 3;

                if (_playerAim != null)
                {
                    _playerAim.VerticalSensitivity *= _currentShooterData.scopedAimSensitivityReductionFactor;
                    _playerAim.HorizontalSensitivity *= _currentShooterData.scopedAimSensitivityReductionFactor;
                }

                if (_recoil != null)
                {
                    _recoil.RecoilX *= _currentShooterData.scopedRecoilReductionFactor;
                    _recoil.RecoilY *= _currentShooterData.scopedRecoilReductionFactor;
                    _recoil.RecoilZ *= _currentShooterData.scopedRecoilReductionFactor;
                }
            }
        }
    }
    #endregion

    #endregion

    #region Private Methods
    private IEnumerator ExecuteScopeEffect(Vector3 endValue, float duration)
    {
        var startValue = _anchor.localPosition;

        var time = 0f;

        while (time < duration)
        {
            _anchor.localPosition = Vector3.Lerp(startValue, endValue, time / duration);
            time += Time.deltaTime;
            yield return null;
        }
        _anchor.localPosition = endValue;

        //If scoping in
        if (endValue == _aimState.localPosition)
        {
            if (_currentShooterData != null && _currentShooterData.ScopeOverlay)
                OnScopeIn();
        }
    }
    private void OnScopeIn()
    {
        _normalFov = _mainCam.fieldOfView;
        _mainCam.fieldOfView = _currentShooterData.ScopedFov;
        _scopeOverlay.SetActive(true);
        _weaponCamera.gameObject.SetActive(false);
    }
    private void OnUnscope()
    {
        _mainCam.fieldOfView = _normalFov;
        _scopeOverlay.SetActive(false);
        _weaponCamera.gameObject.SetActive(true);
    }
    private IEnumerator AttemptToShoot()
    {
        do
        {
            if (_currentShooterData != null)
            {
                if (_currentShooterData.RemainingAmmoInClip <= 0 && !_currentShooterData.unlimitedAmmo && !IsCurrentlyReloading())
                {
                    PlayShooterSfx(_shotAudioSource, _currentShooterData.dryTrigger, false);
                }

                if (_timeRemainingUntilNextShot <= 0 && !IsCurrentlyReloading())
                {
                    if (_currentShooterData.TotalAmmoRemaining > 0 && _currentShooterData.RemainingAmmoInClip > 0 || _currentShooterData.unlimitedAmmo)
                    {
                        _timeRemainingUntilNextShot = 1 / _currentShooterData.fireRate;

                        //Muzzle Flash
                        if (_muzzleFlash != null)
                            _muzzleFlash.Play();

                        //Shot Sound
                        if (_currentShooterData.gunFireSfx != null)
                            PlayShooterSfx(_shotAudioSource, _currentShooterData.gunFireSfx, true);

                        ShootProjectile();

                        _computeRecoil = true;
                        _recoil.InitiateRecoil();

                        ExecuteFireModesLogic(out _repeatedShooting);

                        if (!_currentShooterData.unlimitedAmmo)
                        {
                            UpdateAmmo();
                            if (_currentShooterData != null && _currentShooterData.TotalAmmoRemaining > 0)
                                yield return _reloadCoroutine = StartCoroutine(AttemptToReload());
                        }
                    }
                }
            }
            yield return null;

        } while (_repeatedShooting);

        void ShootProjectile()
        {
            Projectile visualProjectile = null;

            for (int i = 0; i < _currentShooterData.NumOfProjectiles; i++)
            {
                var projectile = SpawnProjectile();
                _spawnedProjectiles.Add(projectile.gameObject);
                projectile.DamageableProjectileCollision += ProjectileDamageableCollisionHandler;
                projectile.ProjectileCollision += ProjectileCollisionHandler;
                projectile.Damage = _currentShooterData.damage;

                //Calculate direction
                var bloomX = UnityEngine.Random.Range(-_currentShooterData.bloom, _currentShooterData.bloom);
                var bloomY = UnityEngine.Random.Range(-_currentShooterData.bloom, _currentShooterData.bloom);
                var direction = projectile.CalculateProjectileDirection(_mainCam.transform.position, _mainCam, bloomX, bloomY);

                projectile.ApplyForceOnProjectile(direction * _currentShooterData.muzzleVelocity, ForceMode.Impulse);

                if (projectile.VisualizeProjectile)
                {
                    visualProjectile = SpawnProjectileVisualModel();
                    if (visualProjectile != null)
                    {
                        _spawnedProjectiles.Add(visualProjectile.gameObject);

                        //Calculate visual projectile direction
                        direction = visualProjectile.CalculateProjectileDirection(_muzzle.position, _mainCam, bloomX, bloomY);
                        visualProjectile.ApplyForceOnProjectile(direction * _currentShooterData.muzzleVelocity, ForceMode.Impulse);
                    }
                }

                projectile.BulletTrailStartPos = _muzzle.position;
            }
        }

        void UpdateAmmo()
        {
            --_currentShooterData.TotalAmmoRemaining;
            --_currentShooterData.RemainingAmmoInClip;
        }

        void ExecuteFireModesLogic(out bool repeatedShooting)
        {
            var currentFireModeIndex = _currentShooterData.CurrentFireModeIndex;
            if (_currentShooterData.fireModes[currentFireModeIndex] == FireModes.SemiAuto || _currentShooterData.fireModes[currentFireModeIndex] == FireModes.SingleFire)
                repeatedShooting = false;
            else if (_currentShooterData.fireModes[currentFireModeIndex] == FireModes.Burst)
            {
                if (_currentShooterData.RemainingBurstRounds > 0)
                {
                    repeatedShooting = true;
                    --_currentShooterData.RemainingBurstRounds;
                }
                else
                {
                    repeatedShooting = false;
                    _timeRemainingUntilNextShot += _currentShooterData.timeBtwBursts;
                }
            }
            else
                repeatedShooting = true;
        }
    }
    private void UpdateHitmarkerOpaqueTime()
    {
        if (_hitMarkerOpaqueTime > 0)
            _hitMarkerOpaqueTime -= Time.deltaTime;
        else if (_hitMarkerImage.color.a > 0)
            _hitMarkerImage.color = Color.Lerp(_hitMarkerImage.color, clearWhite, Time.deltaTime * 1.5f);
    }
    private void UpdateTimeUntilNextShot()
    {
        if (_timeRemainingUntilNextShot > 0)
            _timeRemainingUntilNextShot -= Time.deltaTime;
    }
    private bool IsCurrentlyReloading()
    {
        var isReloading = false;

        if (_currentShooterData != null)
        {
            if (_currentShooterData.IsRefillingAmmo || _currentShooterData.IsCocking)
                isReloading = true;
        }

        return isReloading;
    }
    private IEnumerator RefillAmmoAfterDelay(float timeBeforeReload)
    {
        _currentShooterData.IsRefillingAmmo = true;

        yield return new WaitForSeconds(timeBeforeReload);

        if (_reloadAudioSource != null)
        {
            if (_currentShooterData.refillAmmoSfx != null)
                PlayShooterSfx(_reloadAudioSource, _currentShooterData.refillAmmoSfx, false);
            yield return new WaitForSeconds(_currentShooterData.refillAmmoSfx.length);
        }

        //Update Ammo Count
        if (_currentShooterData.fireModes[_currentShooterData.CurrentFireModeIndex] == FireModes.Burst)
        {
            _currentShooterData.RemainingBurstRounds = _currentShooterData.burstRounds - 1;
            StopCoroutine(_shootingCoroutine);
        }

        if (_currentShooterData.TotalAmmoRemaining > _currentShooterData.clipSize)
            _currentShooterData.RemainingAmmoInClip = _currentShooterData.clipSize;
        else
            _currentShooterData.RemainingAmmoInClip = _currentShooterData.TotalAmmoRemaining;
    }

    private IEnumerator CockAfterDelay(float timeBeforeCock)
    {
        _currentShooterData.IsCocking = true;

        yield return new WaitForSeconds(timeBeforeCock);

        if (_reloadAudioSource != null)
        {
            if (_currentShooterData.cockSfx != null)
                PlayShooterSfx(_reloadAudioSource, _currentShooterData.cockSfx, false);
            yield return new WaitForSeconds(_currentShooterData.cockSfx.length);
        }

        _currentShooterData.IsRefillingAmmo = false;
        _currentShooterData.IsCocking = false;

    }
    private void PlayShooterSfx(AudioSource audioSource, AudioClip audioClip, bool randomizePitch)
    {
        if (_shotAudioSource != null)
        {
            if (randomizePitch)
                audioSource.pitch = 1 - _currentShooterData.pitchRandomization + UnityEngine.Random.Range(-_currentShooterData.pitchRandomization, _currentShooterData.pitchRandomization);
            else
                audioSource.pitch = 1;

            audioSource.clip = audioClip;
            audioSource.Play();
        }
    }
    private Projectile SpawnProjectile()
    {
        var bullet = Instantiate(_currentShooterData.projectile, _mainCam.transform.position, _mainCam.transform.rotation);
        bullet.transform.localEulerAngles = new Vector3(bullet.transform.localEulerAngles.x + 90, bullet.transform.localEulerAngles.y, bullet.transform.localEulerAngles.z);
        return bullet.GetComponent<Projectile>();
    }
    private Projectile SpawnProjectileVisualModel()
    {
        var visualBullet = Instantiate(_currentShooterData.projectile, _muzzle.position, _muzzle.rotation);

        //Turn on the mesh renderer so you can see the projectile
        visualBullet.GetComponent<MeshRenderer>().enabled = true;
        foreach (var component in visualBullet.GetComponents<Component>())
        {
            if (component is Transform || component is MeshRenderer || component is MeshFilter || component is Rigidbody)
                continue;

            Destroy(component);
        }
        visualBullet.transform.localEulerAngles = new Vector3(visualBullet.transform.localEulerAngles.x + 90, visualBullet.transform.localEulerAngles.y, visualBullet.transform.localEulerAngles.z);
        return visualBullet.GetComponent<Projectile>();
    }
    #endregion

    #region Callbacks
    private void ProjectileDamageableCollisionHandler()
    {
        //Show hitmarker
        _hitMarkerImage.color = Color.white;
        _shotAudioSource.pitch = 1;
        if (_hitMarkerSfx != null)
            _shotAudioSource.PlayOneShot(_hitMarkerSfx);
        _hitMarkerOpaqueTime = 0.5f;
    }
    private void ProjectileCollisionHandler()
    {
        foreach (GameObject projectile in _spawnedProjectiles)
            Destroy(obj: projectile, 0.5f);

        _spawnedProjectiles.Clear();
    }
    #endregion
}

public class Recoil
{
    private Vector3 _currentRotation;
    private Vector3 _targetRotation;

    public float RecoilX { get; set; }
    public float RecoilY { get; set; }
    public float RecoilZ { get; set; }
    public float Snappiness { get; set; }
    public float ReturnSpeed { get; set; }

    public Recoil(float recoilX, float recoilY, float recoilZ, float snappiness, float returnSpeed)
    {
        RecoilX = recoilX;
        RecoilY = recoilY;
        RecoilZ = recoilZ;
        Snappiness = snappiness;
        ReturnSpeed = returnSpeed;
    }

    public bool CalculateRecoil(PlayerAim _playerAimScript)
    {
        _targetRotation = Vector3.Lerp(_targetRotation, Vector3.zero, ReturnSpeed / 25f);
        _currentRotation = Vector3.Slerp(_currentRotation, _targetRotation, Snappiness * Time.deltaTime);
        _playerAimScript.RecoilRotation = Quaternion.Euler(_currentRotation);
        //gameObjectTransform.localRotation = Quaternion.Euler(_currentRotation);

        if (_currentRotation != _targetRotation)
            return true;
        else
            return false;
    }
    public void InitiateRecoil() => _targetRotation += new Vector3(RecoilX, UnityEngine.Random.Range(-RecoilY, RecoilY), UnityEngine.Random.Range(-RecoilZ, RecoilZ));
}
