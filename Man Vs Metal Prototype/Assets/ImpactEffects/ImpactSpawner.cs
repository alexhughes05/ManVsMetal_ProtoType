using UnityEngine;

public class ImpactSpawner : MonoBehaviour
{
    [SerializeField] private ParticleSystem impactEffect;

    public ParticleSystem SpawnImpactEffect(Vector3 impactSpawnPoint, Quaternion impactSpawnRotation)
    {
        return Instantiate(impactEffect, impactSpawnPoint, impactSpawnRotation);
    }
}
