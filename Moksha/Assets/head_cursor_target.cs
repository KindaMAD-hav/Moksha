using UnityEngine;

public class FollowCursorSimple : MonoBehaviour
{
    public Camera targetCamera;
    public float depth = 2f;

    void Start()
    {
        if (!targetCamera)
            targetCamera = Camera.main;
    }

    void Update()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = depth;

        transform.position = targetCamera.ScreenToWorldPoint(mousePos);
    }
}
