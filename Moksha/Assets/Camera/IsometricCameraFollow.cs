using UnityEngine;

public class IsometricCameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(-10f, 12f, -10f);
    public float smoothTime = 0.15f;

    Vector3 vel;

    void LateUpdate()
    {
        if (!target) return;
        Vector3 desired = target.position + offset;
        transform.position = Vector3.SmoothDamp(transform.position, desired, ref vel, smoothTime);
        // Keep a fixed iso rotation set in the inspector (e.g., X=35, Y=45, Z=0)
    }
}
