using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParentCollision : MonoBehaviour
{
    private IDictionary<ChildCollision, List<Collision>> _currentCollisions;
    private IDictionary<ChildCollision, List<Collider>> _currentColliders;
    private void Awake()
    {
        _currentCollisions = new Dictionary<ChildCollision, List<Collision>>();
        _currentColliders = new Dictionary<ChildCollision, List<Collider>>();
    }
    public IDictionary<ChildCollision, List<Collision>> CurrentCollisions
    {
        get { return _currentCollisions; }
        private set { _currentCollisions = value; }
    }
    public IDictionary<ChildCollision, List<Collider>> CurrentColliders
    {
        get { return _currentColliders; }
        private set { _currentColliders = value; }
    }

    public void CollisionDetected(ChildCollision childGo, Collision collision) 
    {
        if (!CurrentCollisions.ContainsKey(childGo))
            CurrentCollisions.Add(new KeyValuePair<ChildCollision, List<Collision>>(childGo, new List<Collision> { collision}));
        else
            CurrentCollisions[childGo].Add(collision);
    }
    public void CollisionEnded(ChildCollision childGo, Collision collision)
    {
        if (CurrentCollisions.ContainsKey(childGo))
        {
            if (CurrentCollisions[childGo].Count == 1)
                CurrentCollisions.Remove(childGo);
            else
                CurrentCollisions[childGo].Remove(collision);
        }
    }
    public void TriggerDetected(ChildCollision childGo, Collider collider)
    {
        if (!CurrentColliders.ContainsKey(childGo))
            CurrentColliders.Add(new KeyValuePair<ChildCollision, List<Collider>>(childGo, new List<Collider> { collider }));
        else
            CurrentColliders[childGo].Add(collider);
    }
    public void TriggerEnded(ChildCollision childGo, Collider collider)
    {
        if (CurrentColliders.ContainsKey(childGo))
        {
            if (CurrentColliders[childGo].Count == 1)
                CurrentColliders.Remove(childGo);
            else
                CurrentColliders[childGo].Remove(collider);
        }
    }
}
