using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Projectile : MonoBehaviour
{
    //Inspector fields
    [SerializeField] private float _impactForce;
    [SerializeField] private TrailRenderer _bulletTrail;
    [SerializeField] private ParticleSystem _defaultImpactEffect;

    //Components/References
    private Rigidbody _rb;

    //Private Fields
    private Vector3 _currentBulletPos;
    private Vector3 _prevBulletPos;
    private bool _collisionDetected;

    //Properties
    public Vector3 BulletTrailStartPos { get; set; }
    public float Damage { get; set; }
    public event Action DamageableCollision;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        _prevBulletPos = _currentBulletPos = transform.position;
    }

    private void Update()
    {
        if (!_collisionDetected)
        {
            _currentBulletPos = transform.position;
            _collisionDetected = CheckForProjetileCollision(_prevBulletPos, _currentBulletPos);
            _prevBulletPos = _currentBulletPos;
        }
    }

    private bool CheckForProjetileCollision(Vector3 startPoint, Vector3 endPoint)
    {
        bool detectedHit = false;
        //Debug.DrawLine(startPoint, endPoint, Color.yellow, 5f);

        if (Physics.Linecast(startPoint, endPoint, out RaycastHit hit))
        {
            detectedHit = true;

            ApplyDamage(hit);

            if (_bulletTrail != null)
            {
                TrailRenderer trail = Instantiate(_bulletTrail, BulletTrailStartPos, Quaternion.identity);
                StartCoroutine(SpawnTrail(trail, hit));
            }

            CreateImpactEffect(hit);

            var hitRb = hit.rigidbody;

            if (hitRb != null)
                hitRb.AddForce((endPoint - startPoint).normalized * _impactForce / hitRb.mass, ForceMode.Impulse);

            var explodable = GetComponent<Explodable>();
            if (explodable != null)
                explodable.TriggerExplosion(transform.position);

            Destroy(gameObject);
        }

        return detectedHit;
    }
    private void ApplyDamage(RaycastHit hit)
    {
        Damageable damageable = hit.collider.transform.root.gameObject.GetComponent<Damageable>();
        if (damageable)
        {
            DamageableCollision.Invoke();
            var damageEffectiveZone = hit.collider.gameObject.GetComponent<DamageEffectivenessZone>();
            var damageMultiplier = damageEffectiveZone ? damageEffectiveZone.DamageMultiplier : 1;
            damageable.TakeDamage(Damage * damageMultiplier);
        }
    }
    public void ApplyForceOnProjectile(Vector3 forceVector, ForceMode forceMode)
    {
        _rb.AddForce(forceVector, forceMode);
    }
    private IEnumerator SpawnTrail(TrailRenderer trail, RaycastHit hit)
    {
        float time = 0;
        Vector3 startPosition = trail.transform.position;

        while (time < 1)
        {
            trail.transform.position = Vector3.Lerp(startPosition, hit.point, time);
            time += Time.deltaTime / trail.time;

            yield return null;
        }
        trail.transform.position = hit.point;
        Destroy(trail.gameObject, trail.time);
        Destroy(gameObject);
    }
    private void CreateImpactEffect(RaycastHit hit)
    {
        ParticleSystem impactPs;

        var impactSpawner = hit.collider.gameObject.GetComponent<ImpactSpawner>();
        if (impactSpawner == null)
            impactSpawner = hit.collider.transform.root.gameObject.GetComponent<ImpactSpawner>();

        if (impactSpawner)
            impactPs = impactSpawner.SpawnImpactEffect(hit.point, hit.transform.rotation);
        else
            impactPs = Instantiate(_defaultImpactEffect, hit.point, hit.transform.rotation);

        impactPs.transform.forward = hit.normal;
        impactPs.Play();
    }
}

[Serializable]
public class BulletImpact
{
    public Material Material;
    public ParticleSystem impactEffect;
}
