using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Damageable : MonoBehaviour
{
    [SerializeField] private int startingHealth;

    private float health;

    private void Start()
    {
        health = startingHealth;
    }

    public void TakeDamage(float damageAmount)
    {
        health -= damageAmount;
        if (health < 0)
            health = 0;

        Debug.Log(gameObject.name + " has been damaged. Health is now " + health);

        if (health == 0)
            Die();
    }

    private void Die()
    {
        Destroy(gameObject);
    }
}
