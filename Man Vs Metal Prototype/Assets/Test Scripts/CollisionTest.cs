using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionTest : MonoBehaviour
{

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log("collision detected on gameObject " + collision.gameObject.name);
    }
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Trigger detected on gameobject " + other.gameObject.name);
    }
}
