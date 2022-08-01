using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Explosion : MonoBehaviour
{
    [SerializeField] private ParticleSystem _explosionVfx;
    [NonReorderable] [SerializeField] private ExplosionZones[] _explosionZones;

    private ParentCollision _parentCollision;
    private AudioSource _audioSource;
    private IDictionary<Transform, List<Collider>> _rootDamageablesInExplosion = new Dictionary<Transform, List<Collider>>();
    private float scaleFactor;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        _parentCollision = GetComponent<ParentCollision>();
    }
    private void Start()
    {
        //Sort Explosion Zones based on radius (smallest is first in the list)
        _explosionZones.OrderBy(x => x._blastZoneCollider.radius);

        var scaleArray = new float[] { transform.localScale.x, transform.localScale.y, transform.localScale.z };
        scaleFactor = scaleArray.Max();

        _audioSource.Play();

        if (_parentCollision != null)
            Invoke(nameof(GroupCollidersByRootTransforms), 0.1f);
    }
    private void GroupCollidersByRootTransforms()
    {
        //Group Colliders by root transforms
        if (_parentCollision.CurrentColliders != null && _parentCollision.CurrentColliders.Count > 0)
        {
            foreach (KeyValuePair<ChildCollision, List<Collider>> entry in _parentCollision.CurrentColliders)
            {
                for (int i = 0; i < entry.Value.Count; i++)
                {
                    var colliderRootTransform = entry.Value[i].transform.root;

                    //Add the root gameobject to the dictionary only if it's Damageable
                    if (colliderRootTransform.GetComponent<Damageable>() != null)
                    {
                        if (!_rootDamageablesInExplosion.ContainsKey(colliderRootTransform))
                            _rootDamageablesInExplosion.Add(new KeyValuePair<Transform, List<Collider>>(colliderRootTransform, new List<Collider> { entry.Value[i] }));
                        else if (!_rootDamageablesInExplosion[colliderRootTransform].Contains(entry.Value[i]))
                            _rootDamageablesInExplosion[colliderRootTransform].Add(entry.Value[i]);
                    }
                }
            }
        }
        //OutputColliderNames();

        CalculateDamagesFromExplosion();
    }
    private void CalculateDamagesFromExplosion()
    {
        //Get the damageables of all the root transforms and their associated collider that is closest to the center of the explosion
        if (_rootDamageablesInExplosion != null && _rootDamageablesInExplosion.Count > 0)
        {
            var closestColliderDistanceFromExplosionCenter = _explosionZones[_explosionZones.Length - 1]._blastZoneCollider.radius * scaleFactor;

            foreach (KeyValuePair<Transform, List<Collider>> rootTransform in _rootDamageablesInExplosion)
            {
                for (int i = 0; i < rootTransform.Value.Count; i++)
                {
                    float currentColliderDistanceFromExplosionCenter = Vector3.Distance(transform.position, rootTransform.Value[i].ClosestPoint(transform.position));
                    //Debug.Log("The center of the explosion is at " + transform.position + ". The point of the collider " + rootTransform.Value[i].name + " is at " + rootTransform.Value[i].ClosestPoint(transform.position));
                    //Debug.Log("The colliderDistance from center is " + currentColliderDistanceFromExplosionCenter);

                    if (currentColliderDistanceFromExplosionCenter < closestColliderDistanceFromExplosionCenter)
                    {
                        closestColliderDistanceFromExplosionCenter = currentColliderDistanceFromExplosionCenter;
                    }
                }

                //Update damageables dictionary
                var currentDamageable = rootTransform.Key.GetComponent<Damageable>();
                //Debug.Log("Closest point to center of explosion has a distance of: " + closestColliderDistanceFromExplosionCenter);
                ApplyDamageToDamageable(currentDamageable, closestColliderDistanceFromExplosionCenter);
            }
        }

        Destroy(gameObject, _explosionVfx.main.duration);
    }

    private void ApplyDamageToDamageable(Damageable damageable, float distanceFromExplosionCenter)
    {
        //Determine collider radius
        float smallestInnerRadius = float.MaxValue;
        ExplosionZones nearestEnclosingExplosionZone = new(null, 0, 0);


        for (int i = 0; i < _explosionZones.Length; i++)
        {
            if (_explosionZones[i]._blastZoneCollider.radius * scaleFactor >= distanceFromExplosionCenter)
            {
                if (_explosionZones[i]._blastZoneCollider.radius * scaleFactor < smallestInnerRadius)
                {
                    if (i > 0)
                        smallestInnerRadius = _explosionZones[i - 1]._blastZoneCollider.radius * scaleFactor;
                    else
                        smallestInnerRadius = 0f;

                    nearestEnclosingExplosionZone = _explosionZones[i];
                }
            }
        }

        if (smallestInnerRadius == float.MaxValue)
            return;

        var t = (1 / (nearestEnclosingExplosionZone._blastZoneCollider.radius * scaleFactor - smallestInnerRadius)) * (distanceFromExplosionCenter - smallestInnerRadius);
        var damageAmount = Mathf.Lerp(nearestEnclosingExplosionZone._upperDamage, nearestEnclosingExplosionZone._lowerDamage, t);
        Debug.Log("The calculated damage is " + damageAmount);
        damageable.TakeDamage(damageAmount);
    }
    private void OutputColliderNames()
    {
        if (_rootDamageablesInExplosion != null && _rootDamageablesInExplosion.Count > 0)
        {
            string colliderNames = "";
            foreach (KeyValuePair<Transform, List<Collider>> entry in _rootDamageablesInExplosion)
            {
                for (int i = 0; i < entry.Value.Count; i++)
                {
                    colliderNames += entry.Value[i].name + "\n";
                }

                Debug.Log("Root transform: " + entry.Key.name + " has the following colliders: " + colliderNames);
            }
        }
    }
}

[Serializable]
public struct ExplosionZones
{
    public SphereCollider _blastZoneCollider;
    public float _upperDamage;
    public float _lowerDamage;
    public ExplosionZones(SphereCollider blastZone, float lowerDamage, float upperDamage)
    {
        _blastZoneCollider = blastZone;
        _lowerDamage = lowerDamage;
        _upperDamage = upperDamage;
    }
}
