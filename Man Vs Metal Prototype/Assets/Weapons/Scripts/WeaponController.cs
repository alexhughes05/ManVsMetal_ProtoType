using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class WeaponController : MonoBehaviour
{
    //Inspector
    [SerializeField] private Shooter[] _loadout;
    [SerializeField] private Transform _weaponParent;
    [SerializeField] private AudioSource _shooterAudioSource;
    [SerializeField] private AudioClip _hitMarkerSfx;
    [SerializeField] private Image _hitMarkerImage;
    [SerializeField] private Canvas _crossHair;
    [SerializeField] private bool _visualizeProjectile;
    [SerializeField] private LayerMask _visualProjectileLayerMask;

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
    private Camera _cam;
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
    //Hitmarker
    private float _hitMarkerOpaqueTime;
    //Ammo
    private int _totalProjectilesRemaining = -1;
    private int _projectilesLeftInClip = -1;
    //Recoil
    private bool _computeRecoil;

    //Components/References
    private Recoil _recoil;

    private void Awake()
    {
        _cam = Camera.main;
    }

    private void Start()
    {
        _hitMarkerImage.color = clearWhite;
        Equip(0);
    }
    void Update()
    {
        Debug.Log("Total ammo is: " + _totalProjectilesRemaining);
        Debug.Log("Ammo left in clip: " + _projectilesLeftInClip);

        if (_currentWeapon)
            Debug.Log("Current FireMode is: " + _currentShooterData.fireModes[_currentFireModeIndex]);

        //Always aim weapon holder towards the reticle on the screen (center of camera)
        _weaponParent.rotation = _cam.transform.rotation;

        UpdateTimeUntilNextShot();

        //Will decrement the hitmarker opaque time and make it fade out when the time is 0
        UpdateHitmarkerOpaqueTime();

        if (_computeRecoil)
            _computeRecoil = _recoil.CalculateRecoil(GetComponent<PlayerAim>());
        //_computeRecoil = _recoil.CalculateRecoil(_currentWeapon.transform.GetChild(0));
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
            Projectile visualProjectile = null;

            for (int i = 0; i < _currentShooterData.numOfProjectiles; i++)
            {
                var projectile = SpawnProjectile();

                projectile.DamageableCollision += ProjectileDamageableCollisionHandler;
                projectile.Damage = _currentShooterData.damage;

                //Calculate direction
                var bloomX = Random.Range(-_currentShooterData.bloom, _currentShooterData.bloom);
                var bloomY = Random.Range(-_currentShooterData.bloom, _currentShooterData.bloom);
                var direction = CalculateDirection(_cam.transform.position, bloomX, bloomY);

                projectile.ApplyForceOnProjectile(direction * _currentShooterData.muzzleVelocity, ForceMode.Impulse);

                if (_visualizeProjectile)
                {
                    visualProjectile = SpawnProjectileVisualModel();
                    if (visualProjectile != null)
                    {
                        //Calculate visual projectile direction
                        direction = CalculateDirection(_muzzle.position, bloomX, bloomY);
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

    private Vector3 CalculateDirection(Vector3 initialPos, float bloomX, float bloomY)
    {
        Physics.Raycast(_cam.transform.position, _cam.transform.forward, out RaycastHit hit, Mathf.Infinity, _visualProjectileLayerMask);
        var hitDirection = (hit.point - initialPos).normalized;
        
        Vector3 currentBloom = initialPos + hitDirection * 1000f;
        currentBloom += bloomX * _cam.transform.up;
        currentBloom += bloomY * _cam.transform.right;
        var direction = currentBloom - initialPos;
        return direction.normalized;
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
            PlayShooterSfx(_currentShooterData.reloadSfx, false);
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
    private void PlayShooterSfx(AudioClip audioClip,bool randomizePitch)
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
        var bullet = Instantiate(_currentShooterData.projectile, _cam.transform.position, _cam.transform.rotation);
        bullet.transform.localEulerAngles = new Vector3(bullet.transform.localEulerAngles.x + 90, bullet.transform.localEulerAngles.y, bullet.transform.localEulerAngles.z);
        return bullet.GetComponent<Projectile>();
    }

    private Projectile SpawnProjectileVisualModel()
    {;

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
        _shooterAudioSource.pitch = 1;
        if (_hitMarkerSfx != null)
            _shooterAudioSource.PlayOneShot(_hitMarkerSfx);
        _hitMarkerOpaqueTime = 0.5f;
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

    public bool CalculateRecoil(PlayerAim _playerAimScript)
    {
        _targetRotation = Vector3.Lerp(_targetRotation, Vector3.zero, _returnSpeed / 25f);
        _currentRotation = Vector3.Slerp(_currentRotation, _targetRotation, _snappiness * Time.deltaTime);
        _playerAimScript.RecoilRotation = Quaternion.Euler(_currentRotation);
        //gameObjectTransform.localRotation = Quaternion.Euler(_currentRotation);

        if (_currentRotation != _targetRotation)
            return true;
        else
            return false;
    }
    public void InitiateRecoil() => _targetRotation += new Vector3(_recoilX, Random.Range(-_recoilY, _recoilY), Random.Range(-_recoilZ, _recoilZ));
}
