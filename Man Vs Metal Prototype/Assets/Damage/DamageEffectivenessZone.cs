using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageEffectivenessZone : MonoBehaviour
{
    [SerializeField] private int _damageMultiplier;

    private void Start()
    {
        DamageMultiplier = _damageMultiplier;
    }
    public int DamageMultiplier { get; private set; }
}
