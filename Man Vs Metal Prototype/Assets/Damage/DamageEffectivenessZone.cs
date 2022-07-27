using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DamageEffectivenessZone : MonoBehaviour
{
    [SerializeField] private int _damageMultiplier;

    private void Start()
    {
        DamageMultiplier = _damageMultiplier;
    }
    public int DamageMultiplier { get; private set; }
}
