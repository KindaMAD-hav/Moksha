using UnityEngine;

public class TwoDMovement : MonoBehaviour
{
    float velocityX = 0.0f;
    float velocityZ = 0.0f;

    public float acceleration = 5.0f;
    public float deceleration = 2.0f;
    public float maxSpeed = 0.5f;

    Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        bool forward = Input.GetKey(KeyCode.W);
        bool backward = Input.GetKey(KeyCode.S);
        bool right = Input.GetKey(KeyCode.D);
        bool left = Input.GetKey(KeyCode.A);

        // -------- ACCELERATION (WORLD INPUT) --------
        if (forward && velocityZ < maxSpeed)
            velocityZ += acceleration * Time.deltaTime;

        if (backward && velocityZ > -maxSpeed)
            velocityZ -= acceleration * Time.deltaTime;

        if (right && velocityX < maxSpeed)
            velocityX += acceleration * Time.deltaTime;

        if (left && velocityX > -maxSpeed)
            velocityX -= acceleration * Time.deltaTime;

        // -------- DECELERATION --------
        if (!forward && velocityZ > 0f)
            velocityZ -= deceleration * Time.deltaTime;

        if (!backward && velocityZ < 0f)
            velocityZ += deceleration * Time.deltaTime;

        if (!right && velocityX > 0f)
            velocityX -= deceleration * Time.deltaTime;

        if (!left && velocityX < 0f)
            velocityX += deceleration * Time.deltaTime;

        // -------- CLEAN SMALL VALUES --------
        if (Mathf.Abs(velocityX) < 0.01f) velocityX = 0f;
        if (Mathf.Abs(velocityZ) < 0.01f) velocityZ = 0f;

        // -------- WORLD → LOCAL --------
        Vector3 worldMove = new Vector3(velocityX, 0f, velocityZ);
        Vector3 localMove = transform.InverseTransformDirection(worldMove);

        float speed = Mathf.Max(0f, localMove.z);
        speed = Mathf.Clamp01(speed / maxSpeed);


        // -------- ANIMATOR --------
        animator.SetFloat("VelocityX", localMove.x, 0.1f, Time.deltaTime);
        animator.SetFloat("VelocityZ", localMove.z, 0.1f, Time.deltaTime);
        animator.SetFloat("Speed", speed, 0.1f, Time.deltaTime);
    }
}
