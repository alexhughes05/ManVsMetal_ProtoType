using System.Collections;
using UnityEngine;

public class Shooter : MonoBehaviour
{
    //Inspector
    [Header("Projectile")]
    [SerializeField] private int _damage;
    [SerializeField] private Transform _endOfWeaponBarrelPos;
    [SerializeField] private GameObject _projectile;
    [Header("Visual Effects")]
    [SerializeField] private ParticleSystem _muzzleFlash;
    [Header("Sound Effects")]
    [SerializeField] private AudioSource _gunFire;
    [Header("Settings")]
    [SerializeField] private float _muzzleVelocity;
    [SerializeField] private float _fireRate;
    [Header("Recoil")]
    [SerializeField] private float _recoilX;
    [SerializeField] private float _recoilY;
    [SerializeField] private float _recoilZ;
    [SerializeField] private float _snappiness;
    [SerializeField] private float _returnSpeed;

    //Private Fields
    private float _timeRemainingUntilNextShot;
    private bool _computeRecoil;
    private Camera cam;

    //Components/References
    private Projectile _projectileScript;
    private Recoil _recoil;

    private void Awake()
    {
        cam = Camera.main;
        _recoil = new Recoil(_recoilX, _recoilY, _recoilZ, _snappiness, _returnSpeed);
    }

    void Update()
    {
        if (_timeRemainingUntilNextShot > 0) 
            _timeRemainingUntilNextShot -= Time.deltaTime;

        if (_computeRecoil)
            _computeRecoil = _recoil.CalculateRecoil(transform);
    }

    public void Shoot()
    {
        if (_timeRemainingUntilNextShot <= 0)
        {
            _timeRemainingUntilNextShot = 1/_fireRate;

            if (_muzzleFlash != null)
                _muzzleFlash.Play();

            if (_gunFire != null)
                _gunFire.Play();

            var bullet = SpawnBullet();

            _projectileScript = bullet.GetComponent<Projectile>();
            _projectileScript.Damage = _damage;
            _projectileScript.ApplyForceOnProjectile(bullet.transform.up.normalized * _muzzleVelocity, ForceMode.Impulse);
            _projectileScript.BulletTrailStartPos = _endOfWeaponBarrelPos.position;

            _computeRecoil = true;
            _recoil.InitiateRecoil();
        }
    }

    private GameObject SpawnBullet()
    {
        var bullet = Instantiate(_projectile, cam.transform.position, cam.transform.rotation);
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
