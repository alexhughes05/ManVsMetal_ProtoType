using System.Collections;
using UnityEngine;

public class WeaponController : MonoBehaviour
{
    //Inspector
    [SerializeField] private Shooter[] _loadout;
    [SerializeField] private AudioSource _shooterAudioSource;
    [SerializeField] private Canvas _crossHair;
    [SerializeField] private Transform _weaponParent;

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
    private Camera cam;
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
    //Ammo
    private int _totalProjectilesRemaining = -1;
    private int _projectilesLeftInClip = -1;
    //Recoil
    private bool _computeRecoil;

    //Components/References
    private Projectile _projectileScript;
    private Recoil _recoil;

    private void Awake()
    {
        cam = Camera.main;
    }

    private void Start()
    {
        Equip(0);
    }
    void Update()
    {
        Debug.Log("Total ammo is: " + _totalProjectilesRemaining);
        Debug.Log("Ammo left in clip: " + _projectilesLeftInClip);

        if (_currentWeapon)
            Debug.Log("Current FireMode is: " + _currentShooterData.fireModes[_currentFireModeIndex]);

        //Always aim weapon holder towards the reticle on the screen (center of camera)
        _weaponParent.rotation = cam.transform.rotation;

        UpdateTimeUntilNextShot();

        if (_computeRecoil)
            _computeRecoil = _recoil.CalculateRecoil(_currentWeapon.transform.GetChild(0));
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
                    PlayShooterSfx(_currentShooterData.dryTrigger, false);
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
                    PlayShooterSfx(_currentShooterData.gunFireSfx, true);

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
            for (int i = 0; i < _currentShooterData.numOfProjectiles; i++)
            {
                var bullet = SpawnBullet();

                _projectileScript = bullet.GetComponent<Projectile>();
                _projectileScript.Damage = _currentShooterData.damage;

                Vector3 currentBloom = cam.transform.position + cam.transform.forward * 1000;
                currentBloom += Random.Range(-_currentShooterData.bloom, _currentShooterData.bloom) * cam.transform.up;
                currentBloom += Random.Range(-_currentShooterData.bloom, _currentShooterData.bloom) * cam.transform.right;
                currentBloom -= cam.transform.position;
                currentBloom.Normalize();

                _projectileScript.ApplyForceOnProjectile(currentBloom * _currentShooterData.muzzleVelocity, ForceMode.Impulse);
                _projectileScript.BulletTrailStartPos = _muzzle.position;
            }
        }

        void UpdateAmmoAndReloadIfNeeded()
        {
            if (!_currentShooterData.unlimitedAmmo)
            {
                --_totalProjectilesRemaining;
                --_projectilesLeftInClip;

                if (_projectilesLeftInClip <= 0 && _totalProjectilesRemaining > 0 && !_currentShooterData.unlimitedAmmo)
                    Reload();
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
        if (isScoping)
        {
            _crossHair.gameObject.SetActive(false);

            if (_scopeCoroutine != null)
                StopCoroutine(_scopeCoroutine);
            _scopeCoroutine = StartCoroutine(ExecuteScopeEffect(_aimState.localPosition, 1 / _currentShooterData.aimSpeed));
        }
        else
        {
            if (_scopeCoroutine != null)
                StopCoroutine(_scopeCoroutine);
            _scopeCoroutine = StartCoroutine(ExecuteScopeEffect(_hipState.localPosition, 1 / _currentShooterData.aimSpeed));
            _crossHair.gameObject.SetActive(true);
        }
    }

    public void Reload()
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

        StartCoroutine(ReloadAfterAudioClipIsDonePlaying());
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
    private IEnumerator ReloadAfterAudioClipIsDonePlaying()
    {
        while (_shooterAudioSource.isPlaying)
        {
            yield return null;
        }

        if (_shooterAudioSource != null)
        {
            _shooterAudioSource.pitch = 1;
            _shooterAudioSource.clip = _currentShooterData.reloadSfx;
            _shooterAudioSource.Play();
            yield return new WaitForSeconds(_currentShooterData.reloadSfx.length);
            _isReloading = false;
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
    }
    private void PlayShooterSfx(AudioClip audioClip, bool randomizePitch)
    {
        if (_shooterAudioSource != null)
        {
            if (randomizePitch)
                _shooterAudioSource.pitch = 1 - _currentShooterData.pitchRandomization + Random.Range(-_currentShooterData.pitchRandomization, _currentShooterData.pitchRandomization);
            else
                _shooterAudioSource.pitch = 1;

            _shooterAudioSource.clip = audioClip;
            _shooterAudioSource.Play();
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
        _muzzleFlash = _muzzle.GetChild(0).GetComponent<ParticleSystem>();

        //Initialize ammo
        if (_totalProjectilesRemaining == -1)
        {
            _totalProjectilesRemaining = _currentShooterData.maxAmmo;
            _projectilesLeftInClip = _currentShooterData.clipSize;
        }
    }
    private GameObject SpawnBullet()
    {
        var bullet = Instantiate(_currentShooterData.projectile, cam.transform.position, cam.transform.rotation);
        bullet.transform.localEulerAngles = new Vector3(bullet.transform.localEulerAngles.x + 90, bullet.transform.localEulerAngles.y, bullet.transform.localEulerAngles.z);
        return bullet;
    }
}

public class Recoil
{
    private readonly float _recoilX;
    private readonly float _recoilY;
    private readonly float _recoilZ;
    private readonly float _snappiness;
    private readonly float _returnSpeed;
    private Vector3 _currentRotation;
    private Vector3 _targetRotation;

    public Recoil(float recoilX, float recoilY, float recoilZ, float snappiness, float returnSpeed)
    {
        _recoilX = recoilX;
        _recoilY = recoilY;
        _recoilZ = recoilZ;
        _snappiness = snappiness;
        _returnSpeed = returnSpeed;
    }

    public bool CalculateRecoil(Transform gameObjectTransform)
    {
        _targetRotation = Vector3.Lerp(_targetRotation, Vector3.zero, _returnSpeed / 25f);
        _currentRotation = Vector3.Slerp(_currentRotation, _targetRotation, _snappiness * Time.deltaTime);
        gameObjectTransform.localRotation = Quaternion.Euler(_currentRotation);

        if (_currentRotation != _targetRotation)
            return true;
        else
            return false;
    }
    public void InitiateRecoil() => _targetRotation += new Vector3(_recoilX, Random.Range(-_recoilY, _recoilY), Random.Range(-_recoilZ, _recoilZ));
}
