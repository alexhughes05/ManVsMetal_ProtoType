using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Gun : Weapon, IReloadable, IScopeable
{
    #region Inspector Fields
    [SerializeField] private bool isControlledByAI;
    [Header("Hitmarker")]
    [SerializeField] private AudioClip _hitMarkerSfx;
    [SerializeField] private GameObject _hitMarkerPrefab;
    #endregion

    #region LocalFields
    private PlayerInputReader _playerInputReader;
    private GunSO _gunData;
    //Colors
    private Color clearWhite = new(1, 1, 1, 0);
    //Coroutines
    private Coroutine _scopeCoroutine;
    private Coroutine _shootingCoroutine;
    //Camera
    private Camera _mainCam;
    //Projectiles
    private List<GameObject> _spawnedProjectiles = new();
    //Hitmarker
    private Image _hitmarkerImage;
    private GameObject _hitmarker;
    private float _hitMarkerOpaqueTime;
    //Scope
    private GameObject _scopeOverlay;
    //Crosshair
    private GameObject _crossHair;
    //Recoil
    private bool _computeRecoil;
    //Gun Transforms
    private Transform _anchor;
    private Transform _hipState;
    private Transform _aimState;
    private Transform _muzzle;
    private ParticleSystem _muzzleFlash;
    //Audio
    private AudioSource _shotAudioSource;
    private AudioSource _reloadAudioSource;
    //Components/References
    private GunRecoil _recoil;
    private GunReloadLogic _gunReloadLogic;
    private GunShotLogic _gunShotLogic;
    private GunScopeLogic _gunScopeLogic;
    private Movement _movement;
    private PlayerAim _playerAim;
    #endregion

    #region MonoBehaviour Methods
    private void Awake()
    {
        _gunData = (GunSO)_weaponData;
        _movement = FindObjectOfType<Movement>(); //MAKE MORE EFFICIENT BY HAVING A REF TO PROTAGONIST AND GET PLAYERS MONOBEHAVIOUR TO DISABLE WEAPON CAM
        _playerAim = FindObjectOfType<PlayerAim>();
        _playerInputReader = FindObjectOfType<PlayerInputReader>();
        _reloadAudioSource = gameObject.AddComponent<AudioSource>();
    }
    private void Start()
    {
        _mainCam = Camera.main;

        _shotAudioSource = _playerAim.gameObject.GetComponent<AudioSource>();
        if (_shotAudioSource == null)
            _shotAudioSource = _playerAim.gameObject.AddComponent<AudioSource>();

        //Instantiate hitmarker and set to clear
        _hitmarker = Instantiate(_hitMarkerPrefab, gameObject.transform);
        _hitmarkerImage = _hitmarker.GetComponentInChildren<Image>();
        _hitmarkerImage.color = clearWhite;

        //Instantiate scope overlay and set active to false
        if (_gunData.ScopeOverlay != null)
        {
            _scopeOverlay = Instantiate(_gunData.ScopeOverlay, gameObject.transform);
            _scopeOverlay.SetActive(false);
        }

        //Instantiate crosshair
        if (_gunData.crossHair != null)
            _crossHair = Instantiate(_gunData.crossHair, gameObject.transform);

        //Seteup weapon transforms
        InitializeWeaponTransforms();

        //Instantiate classes

        if (isControlledByAI)
            _gunShotLogic = new GunShotLogic(_gunData, _shotAudioSource, _muzzleFlash, _muzzle);
        else
            _gunShotLogic = new GunShotLogic(_gunData, _shotAudioSource, _muzzleFlash, Camera.main.transform);

        _gunReloadLogic = new GunReloadLogic(_gunData, _reloadAudioSource);
        _gunScopeLogic = new GunScopeLogic(_gunData, _mainCam, WeaponCamera, _scopeOverlay, _anchor);
        _recoil = new GunRecoil(_gunData.recoilX, _gunData.recoilY, _gunData.recoilZ, _gunData.snappiness, _gunData.returnSpeed);

        //Subscribe to events
        _playerInputReader.ScopeIn += ScopeIn;
        _playerInputReader.ScopeOut += ScopeOut;
        _playerInputReader.Reload += Reload;
        _playerInputReader.FireModeToggle += FireModeHandler;

        if (_gunData.IsCocking)
        {
            _gunData.IsRefillingAmmo = true;
            StartCoroutine(_gunReloadLogic.CockGunAfterDelay(_gunData.timeDelayBeforeCocking));
        }
    }
    void Update()
    {
        //Always aim weapon holder towards the reticle on the screen (center of camera)
        if (WeaponHolder != null) WeaponHolder.rotation = _mainCam.transform.rotation;

        if (_gunShotLogic != null) UpdateTimeUntilNextShot();

        //Will decrement the hitmarker opaque time and make it fade out when the time is 0
        if (_hitmarkerImage != null) UpdateHitmarkerOpaqueTime();

        if (_recoil != null && _computeRecoil)
            _computeRecoil = _recoil.CalculateRecoil(_playerAim);
    }
    private void OnDestroy()
    {
        if (_gunShotLogic != null) _gunShotLogic.RepeatedShooting = false;

        if (_gunScopeLogic != null)
        {
            if (_gunScopeLogic.IsScopedIn)
                ScopeOut();
        }

        if (_playerInputReader != null)
        {
            _playerInputReader.ScopeIn -= ScopeIn;
            _playerInputReader.ScopeOut -= ScopeOut;
            _playerInputReader.Reload -= Reload;
            _playerInputReader.FireModeToggle -= FireModeHandler;
        }

        if (_gunData != null)
        {
            _gunData.IsRefillingAmmo = false;
        }
    }
    #endregion

    #region Interface Implementations
    public override void Use(int numOfShots)
    {
        _gunShotLogic.NumOfShotsToFire = numOfShots;
        StartCoroutine(AttemptToShoot());
    }
    public override Coroutine StartUsing()
    {
        _gunShotLogic.RepeatedShooting = true;
        return StartCoroutine(AttemptToShoot());
    }
    public override void StopUsing()
    {
        _gunShotLogic.RepeatedShooting = false;
        _gunShotLogic.NumOfShotsToFire = null;
    }
    public bool Reload()
    {
        if (_gunReloadLogic != null && _gunReloadLogic.CanReload())
        {
            StartCoroutine(RefillAmmoAndCockWeapon());
            return true;
        }

        //Might not need
        if (_gunData.fireModes[_gunData.CurrentFireModeIndex] == FireModes.Burst)
            StopCoroutine(_shootingCoroutine);

        return false;
    }
    public bool ScopeIn()
    {
        //CAN'T SCOPE IN WHILE RELOADING
        if (_gunReloadLogic != null && _gunReloadLogic.IsCurrentlyReloading()) return false;

        if (_gunScopeLogic != null)
        {
            ApplyScopeModifiers(true);

            _gunScopeLogic.IsScopedIn = true;

            _crossHair.SetActive(false);

            if (_scopeCoroutine != null)
                StopCoroutine(_scopeCoroutine);

            _scopeCoroutine = StartCoroutine(_gunScopeLogic.ExecuteScopeEffect(true, _aimState.localPosition, 1 / _gunData.scopeSpeed));

            return true;
        }
        return false;
    }
    public bool ScopeOut()
    {
        if (_gunReloadLogic != null && _gunScopeLogic != null)
        {
            if (_gunScopeLogic.IsScopedIn)
            {
                if (_scopeCoroutine != null)
                    StopCoroutine(_scopeCoroutine);

                if (gameObject.activeInHierarchy)
                {
                    _scopeCoroutine = StartCoroutine(_gunScopeLogic.ExecuteScopeEffect(false, _hipState.localPosition, 1 / _gunData.scopeSpeed));
                    _crossHair.SetActive(true);
                }

                if (_gunData.ScopeOverlay)
                    _gunScopeLogic.ZoomOutAndRemoveScope();

                ApplyScopeModifiers(false);
                _gunScopeLogic.IsScopedIn = false;

                return true;
            }
        }
        return false;
    }
    #endregion

    #region Private Methods
    private IEnumerator AttemptToShoot()
    {
        //Initialize burstRounds
        if (_gunData.RemainingBurstRounds <= 0 && !_gunShotLogic.RepeatedShooting)
            _gunData.RemainingBurstRounds = _gunData.burstRounds - 1;

        do
        {
            var successfulShot = ShootIfValid();

            if (successfulShot)
            {
                ++_gunShotLogic.ShotsFiredCount;
                _computeRecoil = true;
                _recoil.InitiateRecoil();

                if (!isControlledByAI)
                    ExecuteFireModesLogic();

                //COCK GUN AFTER SHOT
                if (_gunReloadLogic.ShouldCockGunAfterShot()) StartCoroutine(_gunReloadLogic.CockGunAfterDelay(_gunData.timeDelayBeforeCocking));

                //TRY TO AUTO RELOAD WHEN EMPTY
                else if (_gunData.RemainingAmmoInClip <= 0) Reload();
            }

            yield return null;

        } while (_gunShotLogic.RepeatedShooting || _gunShotLogic.ShotsFiredCount < _gunShotLogic.NumOfShotsToFire);

        _gunShotLogic.ShotsFiredCount = 0;
        _gunShotLogic.NumOfShotsToFire = null;

        #region Local AttemptToShoot Methods
        void ExecuteFireModesLogic()
        {
            var currentFireModeIndex = _gunData.CurrentFireModeIndex;

            if (_gunData.fireModes[currentFireModeIndex] == FireModes.SemiAuto || _gunData.fireModes[currentFireModeIndex] == FireModes.SingleFire)
            {
                _gunShotLogic.RepeatedShooting = false;
            }
            else if (_gunData.fireModes[currentFireModeIndex] == FireModes.Burst)
            {
                if (_gunData.RemainingBurstRounds > 0)
                {
                    _gunShotLogic.RepeatedShooting = true;
                }
                else
                {
                    _gunShotLogic.RepeatedShooting = false;
                    _gunShotLogic.TimeRemainingUntilNextShot += _gunData.timeBtwBursts;
                }
            }
            else
                _gunShotLogic.RepeatedShooting = true;
        }
        #endregion
    }
    private bool ShootIfValid()
    {
        if (_gunData != null && _gunShotLogic != null)
        {
            if (_gunShotLogic.GunReadyToFire() && !_gunReloadLogic.IsCurrentlyReloading())
            {
                if (_gunShotLogic.GunHasAmmoInClip())
                {
                    for (int i = 0; i < _gunData.NumOfProjectiles; i++)
                    {
                        var projectile = SpawnProjectile();

                        //SET VISUAL SHOT LOCATION IF YOU WANT TO VISUALZE PROJECTILE
                        if (projectile.VisualizeProjectile)
                            _gunShotLogic.VisualShotLocation = _muzzle;

                        //SETUP CALLBACKS FOR WHEN PROJECTILE COLLIDES
                        _spawnedProjectiles.Add(projectile.gameObject);
                        projectile.DamageableProjectileCollision += ProjectileDamageableCollisionHandler;
                        projectile.ProjectileCollision += ProjectileCollisionHandler;

                        //SHOOT ACTUAL PROJECTILE
                        _gunShotLogic.Shoot(projectile);

                        if (projectile.VisualizeProjectile)
                        {
                            Projectile visualProjectile = SpawnProjectileVisualModel();
                            _spawnedProjectiles.Add(visualProjectile.gameObject);

                            //SHOOT THE VISUAL PROJECTILE
                            _gunShotLogic.Shoot(visualProjectile);
                        }
                    }

                    if (!_gunData.unlimitedAmmo) UpdateAmmo();

                    return true;
                }
                else if (!_gunShotLogic.GunHasAmmoInClip())
                    _gunShotLogic.PlayShooterSfx(_shotAudioSource, _gunData.dryTrigger, false);
            }
        }
        return false;

        #region Shoot Local Methods
        Projectile SpawnProjectile()
        {
            var bullet = Instantiate(_gunData.projectile, _mainCam.transform.position, _mainCam.transform.rotation);
            bullet.transform.localEulerAngles = new Vector3(bullet.transform.localEulerAngles.x + 90, bullet.transform.localEulerAngles.y, bullet.transform.localEulerAngles.z);
            return bullet.GetComponent<Projectile>();
        }
        Projectile SpawnProjectileVisualModel()
        {
            var visualBullet = Instantiate(_gunData.projectile, _muzzle.position, _muzzle.rotation);

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
        void UpdateAmmo()
        {
            if (HasRemainingBurstRounds()) --_gunData.RemainingBurstRounds;

            --_gunData.TotalAmmoRemaining;
            --_gunData.RemainingAmmoInClip;

            #region UpdateAmmo Local Methods
            bool HasRemainingBurstRounds() => _gunData.fireModes[_gunData.CurrentFireModeIndex] == FireModes.Burst && _gunData.RemainingBurstRounds > 0;
            #endregion
        }
        #endregion
    }
    private void InitializeWeaponTransforms()
    {
        _anchor = transform.Find("Anchor");
        _hipState = transform.Find("States/Hip");
        _aimState = transform.Find("States/Aim");
        _muzzle = transform.Find("Anchor/Resources/Muzzle");
        if (_muzzle.childCount > 0)
            _muzzleFlash = _muzzle.GetChild(0).GetComponent<ParticleSystem>();
    }
    private void UpdateHitmarkerOpaqueTime()
    {
        if (_hitMarkerOpaqueTime > 0)
            _hitMarkerOpaqueTime -= Time.deltaTime;
        else if (_hitmarkerImage.color.a > 0)
            _hitmarkerImage.color = Color.Lerp(_hitmarkerImage.color, clearWhite, Time.deltaTime * 1.5f);
    }
    private void UpdateTimeUntilNextShot()
    {
        if (_gunShotLogic.TimeRemainingUntilNextShot > 0)
            _gunShotLogic.TimeRemainingUntilNextShot -= Time.deltaTime;
    }
    private IEnumerator RefillAmmoAndCockWeapon()
    {
        if (_gunReloadLogic != null)
        {
            //Refilling Ammo
            yield return StartCoroutine(_gunReloadLogic.RefillAmmoAfterDelay(_gunData.timeToWaitAfterShotToReload));

            //Cocking Weapon
            yield return StartCoroutine(_gunReloadLogic.CockGunAfterDelay(_gunData.timeDelayBeforeCocking));
        }
    }
    private void ApplyScopeModifiers(bool isScopingIn)
    {
        if (isScopingIn)
        {
            if (_movement != null)
                _movement.CurrentSpeed /= 3;

            if (_playerAim != null)
            {
                _playerAim.VerticalSensitivity /= _gunData.scopedAimSensitivityReductionFactor;
                _playerAim.HorizontalSensitivity /= _gunData.scopedAimSensitivityReductionFactor;
            }

            if (_recoil != null)
            {
                _recoil.RecoilX /= _gunData.scopedRecoilReductionFactor;
                _recoil.RecoilY /= _gunData.scopedRecoilReductionFactor;
                _recoil.RecoilZ /= _gunData.scopedRecoilReductionFactor;
            }
        }
        else
        {
            if (_movement != null)
                _movement.CurrentSpeed *= 3;

            if (_playerAim != null)
            {
                _playerAim.VerticalSensitivity *= _gunData.scopedAimSensitivityReductionFactor;
                _playerAim.HorizontalSensitivity *= _gunData.scopedAimSensitivityReductionFactor;
            }

            if (_recoil != null)
            {
                _recoil.RecoilX *= _gunData.scopedRecoilReductionFactor;
                _recoil.RecoilY *= _gunData.scopedRecoilReductionFactor;
                _recoil.RecoilZ *= _gunData.scopedRecoilReductionFactor;
            }

            _gunScopeLogic.IsScopedIn = false;
        }
    }
    #endregion

    #region Event Handlers
    public void FireModeHandler()
    {
        if (_gunData.fireModes.Length <= 1)
            return;

        _gunData.CurrentFireModeIndex = ++_gunData.CurrentFireModeIndex % _gunData.fireModes.Length;
    }
    #endregion

    #region Callbacks
    private void ProjectileDamageableCollisionHandler()
    {
        //Show hitmarker
        _hitmarkerImage.color = Color.white;
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
