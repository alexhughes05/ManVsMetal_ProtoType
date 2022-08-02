using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WeaponController : MonoBehaviour
{
    //Inspector
    [SerializeField] private Shooter[] _loadout;
    [SerializeField] private Transform _weaponParent;
    [SerializeField] private Camera _weaponCamera;
    [SerializeField] private AudioSource _shotAudioSource;
    [SerializeField] private AudioSource _reloadAudioSource;
    [SerializeField] private AudioClip _hitMarkerSfx;
    [SerializeField] private Image _hitMarkerImage;
    [SerializeField] private Canvas _crossHair;

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
    //Camera
    private Camera _mainCam;
    //Weapon Info 
    private GameObject _currentWeapon;
    private Shooter _currentShooterData;
    private int _currentFireModeIndex;
    //Shooting Info
    private Coroutine _shootingCoroutine;
    private bool _repeatedShooting;
    private int _remainingProjectilesInBurst = -1;
    private float _timeRemainingUntilNextShot;
    private bool _isReloading;
    //Projectiles
    private List<GameObject> _spawnedProjectiles = new();
    //Hitmarker
    private float _hitMarkerOpaqueTime;
    //Scope
    private GameObject _scopeOverlay;
    private float _normalFov;
    //Ammo
    private int _totalProjectilesRemaining = -1;
    private int _projectilesLeftInClip = -1;
    //Recoil
    private bool _computeRecoil;

    //Components/References
    private Recoil _recoil;
    private Movement _movement;
    private PlayerAim _playerAim;

    //Properties
    //public event Action ZoomedInScope;

    private void Awake()
    {
        _mainCam = Camera.main;
        _movement = GetComponent<Movement>();
        _playerAim = GetComponent<PlayerAim>();
    }

    private void Start()
    {
        _hitMarkerImage.color = clearWhite;
        Equip(0);

        if (_currentShooterData != null && _currentShooterData.scopeOverlay != null)
        {
            _scopeOverlay = Instantiate(_currentShooterData.scopeOverlay);
            _scopeOverlay.SetActive(false);
        }
    }
    void Update()
    {
        //Debug.Log("Total ammo is: " + _totalProjectilesRemaining);
        //Debug.Log("Ammo left in clip: " + _projectilesLeftInClip);

        //if (_currentWeapon)
        //Debug.Log("Current FireMode is: " + _currentShooterData.fireModes[_currentFireModeIndex]);

        //Always aim weapon holder towards the reticle on the screen (center of camera)
        _weaponParent.rotation = _mainCam.transform.rotation;

        UpdateTimeUntilNextShot();

        //Will decrement the hitmarker opaque time and make it fade out when the time is 0
        UpdateHitmarkerOpaqueTime();

        if (_computeRecoil)
            _computeRecoil = _recoil.CalculateRecoil(GetComponent<PlayerAim>());
    }

    private void UpdateHitmarkerOpaqueTime()
    {
        if (_hitMarkerOpaqueTime > 0)
            _hitMarkerOpaqueTime -= Time.deltaTime;
        else if (_hitMarkerImage.color.a > 0)
            _hitMarkerImage.color = Color.Lerp(_hitMarkerImage.color, clearWhite, Time.deltaTime * 1.5f);
    }

    public void ShootingHandler(bool shootingTriggerHeldDown)
    {
        if (_currentWeapon)
        {
            //Initialize burstRounds
            if (_remainingProjectilesInBurst <= 0 && !_repeatedShooting)
                _remainingProjectilesInBurst = _currentShooterData.burstRounds - 1;

            if (shootingTriggerHeldDown)
            {
                if (_totalProjectilesRemaining <= 0 && !_currentShooterData.unlimitedAmmo)
                {
                    PlayShooterSfx(_shotAudioSource, _currentShooterData.dryTrigger, false);
                }
                else
                    _shootingCoroutine = StartCoroutine(AttemptToShoot());
            }
            else
            {
                if (_currentShooterData.fireModes[_currentFireModeIndex] == FireModes.FullAuto)
                    StopCoroutine(_shootingCoroutine);
            }
        }
    }

    private IEnumerator AttemptToShoot()
    {
        do
        {
            if (_timeRemainingUntilNextShot <= 0 && _isReloading == false)
            {
                if (_totalProjectilesRemaining > 0 || _currentShooterData.unlimitedAmmo)
                {
                    _timeRemainingUntilNextShot = 1 / _currentShooterData.fireRate;

                    //Muzzle Flash
                    if (_muzzleFlash != null)
                        _muzzleFlash.Play();

                    //Shot Sound
                    PlayShooterSfx(_shotAudioSource, _currentShooterData.gunFireSfx, true);

                    ShootProjectile();

                    _computeRecoil = true;
                    _recoil.InitiateRecoil();

                    ExecuteFireModesLogic(out _repeatedShooting);

                    UpdateAmmoAndReloadIfNeeded();
                }
            }
            yield return null;

        } while (_repeatedShooting);

        void ShootProjectile()
        {
            Projectile visualProjectile = null;

            for (int i = 0; i < _currentShooterData.numOfProjectiles; i++)
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

        void UpdateAmmoAndReloadIfNeeded()
        {
            if (!_currentShooterData.unlimitedAmmo)
            {
                --_totalProjectilesRemaining;
                --_projectilesLeftInClip;

                if (_projectilesLeftInClip <= 0 && _totalProjectilesRemaining > 0 && !_currentShooterData.unlimitedAmmo)
                    Reload(_currentShooterData.timeToWaitAfterShotToReload);
            }
        }

        void ExecuteFireModesLogic(out bool repeatedShooting)
        {
            if (_currentShooterData.fireModes[_currentFireModeIndex] == FireModes.SemiAuto)
                repeatedShooting = false;
            else if (_currentShooterData.fireModes[_currentFireModeIndex] == FireModes.Burst)
            {
                if (_remainingProjectilesInBurst > 0)
                {
                    repeatedShooting = true;
                    --_remainingProjectilesInBurst;
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
    public void Scope(bool isScoping)
    {
        if (_currentShooterData != null)
        {
            if (isScoping)
            {
                _crossHair.gameObject.SetActive(false);

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
                _crossHair.gameObject.SetActive(true);

                if (_currentShooterData.scopeOverlay)
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
            if (_currentShooterData != null && _currentShooterData.scopeOverlay)
                OnScopeIn();
        }
    }
    private void OnScopeIn()
    {
        _normalFov = _mainCam.fieldOfView;
        _mainCam.fieldOfView = _currentShooterData.scopedFov;
        _scopeOverlay.SetActive(true);
        _weaponCamera.gameObject.SetActive(false);
    }

    private void OnUnscope()
    {
        _mainCam.fieldOfView = _normalFov;
        _scopeOverlay.SetActive(false);
        _weaponCamera.gameObject.SetActive(true);
    }

    public void Reload(float timeToWaitBeforeReload)
    {
        if (_currentShooterData.fireModes[_currentFireModeIndex] == FireModes.Burst)
        {
            _remainingProjectilesInBurst = _currentShooterData.burstRounds - 1;
            StopCoroutine(_shootingCoroutine);
        }

        _isReloading = true;

        if (_totalProjectilesRemaining > _currentShooterData.clipSize)
            _projectilesLeftInClip = _currentShooterData.clipSize;
        else
            _projectilesLeftInClip = _totalProjectilesRemaining;

        StartCoroutine(ReloadAfterDelay(timeToWaitBeforeReload));
    }
    public void ChangeFireMode()
    {
        if (_currentShooterData.fireModes.Length <= 1)
            return;

        _currentFireModeIndex = ++_currentFireModeIndex % _currentShooterData.fireModes.Length;
    }
    private void UpdateTimeUntilNextShot()
    {
        if (_timeRemainingUntilNextShot > 0)
            _timeRemainingUntilNextShot -= Time.deltaTime;
    }
    private IEnumerator ReloadAfterDelay(float timeBeforeReload)
    {
        yield return new WaitForSeconds(timeBeforeReload);

        if (_reloadAudioSource != null)
        {
            PlayShooterSfx(_reloadAudioSource, _currentShooterData.reloadSfx, false);
            yield return new WaitForSeconds(_currentShooterData.reloadSfx.length);
            _isReloading = false;
        }
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
    private void Equip(int weaponIndex)
    {
        if (_currentWeapon != null) Destroy(_currentWeapon);

        GameObject newWeapon = Instantiate(_loadout[weaponIndex]._prefab, _weaponParent.position, _weaponParent.rotation, _weaponParent);
        newWeapon.transform.localPosition = Vector3.zero;
        newWeapon.transform.localEulerAngles = Vector3.zero;

        _currentWeapon = newWeapon;
        _currentShooterData = _loadout[weaponIndex];

        _recoil = new Recoil(_currentShooterData.recoilX, _currentShooterData.recoilY, _currentShooterData.recoilZ, _currentShooterData.snappiness, _currentShooterData.returnSpeed);

        //Setup anchor and resources
        _anchor = _currentWeapon.transform.Find("Anchor");
        _hipState = _currentWeapon.transform.Find("States/Hip");
        _aimState = _currentWeapon.transform.Find("States/Aim");
        _muzzle = _currentWeapon.transform.Find("Anchor/Resources/Muzzle");
        if (_muzzle.childCount > 0)
            _muzzleFlash = _muzzle.GetChild(0).GetComponent<ParticleSystem>();

        //Initialize ammo
        if (_totalProjectilesRemaining == -1)
        {
            _totalProjectilesRemaining = _currentShooterData.maxAmmo;
            _projectilesLeftInClip = _currentShooterData.clipSize;
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
        ;

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

    //Callbacks
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
    public void InitiateRecoil()
    {
        _targetRotation += new Vector3(RecoilX, UnityEngine.Random.Range(-RecoilY, RecoilY), UnityEngine.Random.Range(-RecoilZ, RecoilZ));

        Debug.Log("When scoping, Recoil x is " + RecoilX);
    }
}
