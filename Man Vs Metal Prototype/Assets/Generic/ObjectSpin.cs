using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectSpin : MonoBehaviour
{
    [SerializeField] private Vector3 _spinVector;
    [SerializeField] private bool _randomizeSpin;

    private void Start()
    {
        if (_randomizeSpin)
        {
            _spinVector.x = Random.Range(-_spinVector.x, _spinVector.x);
            _spinVector.y = Random.Range(-_spinVector.y, _spinVector.y);
            _spinVector.z = Random.Range(-_spinVector.z, _spinVector.z);
        }
    }
    private void Update()
    {
        transform.Rotate(_spinVector * Time.deltaTime);
    }
}

