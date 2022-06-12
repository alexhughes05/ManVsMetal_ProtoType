using UnityEngine;

public class GroundChecker : MonoBehaviour
{
    [SerializeField] private Transform groundTransform;
    [SerializeField] private LayerMask groundLayers;
    public bool IsGrounded()
    {
        var isGrounded = Physics.CheckSphere(groundTransform.position, .1f, groundLayers);
        return isGrounded;
    }
}
