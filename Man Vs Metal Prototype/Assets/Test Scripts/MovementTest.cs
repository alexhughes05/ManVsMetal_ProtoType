using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovementTest : MonoBehaviour
{
    [SerializeField] private float speed;
    [SerializeField] private Transform otherCube;
    [SerializeField] private Rigidbody otherRb;
    private Rigidbody _rb;



    // Start is called before the first frame update
    void Start()
    {
        _rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        transform.Translate(speed * Time.deltaTime * Vector3.right);
        //otherCube.transform.Translate(speed * Time.deltaTime * Vector3.left);
        //Debug.Log("Speed is " + speed);
        //_rb.AddForce(Vector3.forward * speed);
        //_rb.MovePosition(transform.position + Vector3.right * speed * Time.fixedDeltaTime);
        //_rb.velocity = new Vector3(speed, _rb.velocity.y, _rb.velocity.z);
        //otherRb.velocity = new Vector3(-speed, _rb.velocity.y, _rb.velocity.z);
        //Debug.Log("Time is " + Time.time + ". The velocity is currently " + _rb.velocity);
    }
}
