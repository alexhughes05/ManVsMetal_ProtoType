using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class GenericPool : MonoBehaviour
{
    private ObjectPool<GameObject> _pool;
    private GameObject _objectPrefab;

    private void Awake() => _pool = new ObjectPool<GameObject>(CreateObject, OnTakeObjectFromPool, OnReturnObjectToPool);

    private GameObject CreateObject()
    {
        var prefab = Instantiate(_objectPrefab);
        //prefab.SetPool(_pool);
        return prefab;
    }

    private void OnTakeObjectFromPool(GameObject obj)
    {
        _objectPrefab.gameObject.SetActive(true);

    }

    private void OnReturnObjectToPool(GameObject obj)
    {
        _objectPrefab.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
