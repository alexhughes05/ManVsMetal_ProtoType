using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChildCollision : MonoBehaviour
{
    [SerializeField] ParentCollision _parentCollision;

    void OnCollisionEnter(Collision collision)
    {
        _parentCollision.CollisionDetected(this, collision);
    }
    private void OnCollisionExit(Collision collision)
    {
        _parentCollision.CollisionEnded(this, collision);
    }

    private void OnTriggerEnter(Collider other)
    {
        _parentCollision.TriggerDetected(this, other);
    }

    private void OnTriggerExit(Collider other)
    {
        _parentCollision.TriggerEnded(this, other);
    }
}
