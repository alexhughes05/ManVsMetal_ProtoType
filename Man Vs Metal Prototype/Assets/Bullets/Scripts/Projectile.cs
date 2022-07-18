using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Projectile : MonoBehaviour
{
    //Inspector fields
    [SerializeField] private float impactForce;
    [SerializeField] private TrailRenderer _bulletTrail;
    [NonReorderable] public List<BulletImpact> impactEffects;
    public Dictionary<Material, ParticleSystem> _bulletImpactDictionary = new Dictionary<Material, ParticleSystem>();

    //Components/References
    private Rigidbody _rb;

    //Private Fields
    private Vector3 _currentBulletPos;
    private Vector3 _prevBulletPos;
    private bool _collisionDetected;

    //Properties
    public Vector3 BulletTrailStartPos { get; set; }


    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();

        foreach(BulletImpact impact in impactEffects)
        {
            _bulletImpactDictionary.Add(impact.Material, impact.impactEffect);
        }
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

    public bool CheckForProjetileCollision(Vector3 startPoint, Vector3 endPoint)
    {
        bool detectedHit = false;
        Debug.DrawLine(startPoint, endPoint, Color.yellow, 5f);

        if (Physics.Linecast(startPoint, endPoint, out RaycastHit hit))
        {
            TrailRenderer trail = Instantiate(_bulletTrail, BulletTrailStartPos, Quaternion.identity);
            StartCoroutine(SpawnTrail(trail, hit));

            if (_bulletImpactDictionary.Count > 0 && hit.transform.gameObject.layer != LayerMask.NameToLayer("Hidden"))
            {
                if (hit.collider.gameObject.TryGetComponent(out Renderer renderer))
                    CreateImpactEffect(renderer.sharedMaterial, hit);
                else
                    CreateImpactEffect(null, hit);
            }

            var hitRb = hit.rigidbody;

            if (hitRb != null)
                hitRb.AddForce((endPoint - startPoint).normalized * impactForce / hitRb.mass, ForceMode.Impulse);

            detectedHit = true;
        }
        StartCoroutine(DestroyProjectileAfterDelay(gameObject, 2f));

        return detectedHit;
    }

    public void ApplyForceOnProjectile(Vector3 forceVector, ForceMode forceMode)
    {
        _rb.AddForce(forceVector, forceMode);
    }

    private IEnumerator DestroyProjectileAfterDelay(GameObject go, float delayAmount)
    {
        yield return new WaitForSeconds(delayAmount);
        Destroy(go, delayAmount);
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

    private void CreateImpactEffect(Material colliderMat, RaycastHit hit)
    {
        ParticleSystem impactPs;

        if (colliderMat == null || !_bulletImpactDictionary.ContainsKey(colliderMat))
           impactPs = Instantiate(_bulletImpactDictionary[impactEffects[0].Material], hit.point, hit.transform.rotation);
        else
            impactPs = Instantiate(_bulletImpactDictionary[colliderMat], hit.point, hit.transform.rotation);

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
