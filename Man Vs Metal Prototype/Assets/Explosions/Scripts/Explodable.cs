using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Explodable : MonoBehaviour
{
    [SerializeField] private Explosion _explosion;

    public void TriggerExplosion(Vector3 explosionCenter)
    {
        Instantiate(_explosion.gameObject, explosionCenter, Quaternion.identity);
    }
}
