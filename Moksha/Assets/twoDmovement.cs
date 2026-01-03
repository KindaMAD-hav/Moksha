using UnityEngine;

public class TwoDMovement : MonoBehaviour
{
    float velocityX = 0.0f;
    float velocityZ = 0.0f;

    public float acceleration = 1.0f;
    public float deceleration = 1.0f;

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

        if (forward && velocityZ < 0.5f)
            velocityZ += acceleration * Time.deltaTime;

        if (backward && velocityZ > -0.5f)
            velocityZ -= acceleration * Time.deltaTime;
        
        if (right && velocityX < 0.5f)
            velocityX += acceleration * Time.deltaTime;

        if (left && velocityX > -0.5f)
            velocityX -= acceleration * Time.deltaTime;

        // Deceleration
        if (!forward && velocityZ > 0.0f)
            velocityZ -= deceleration * Time.deltaTime;

        //if (!forward && velocityZ < 0.0f)
        //    velocityZ = 0.0f;

        if (!backward && velocityZ < 0.0f)
            velocityZ += deceleration * Time.deltaTime;

        //if (!backward && velocityZ > 0.0f)
        //    velocityZ = 0.0f;

        // Clamp to zero (avoid jitter)


        if (!right && velocityX > 0.0f)
            velocityX -= deceleration * Time.deltaTime;

        //if (!right && velocityX < 0.00f)
        //    velocityX = 0.0f;

        if (!left && velocityX < 0.0f)
            velocityX += deceleration * Time.deltaTime;

        //if (!left && velocityX > 0.00f)
        //    velocityX = 0.0f;




        animator.SetFloat("VelocityX", velocityX);
        animator.SetFloat("VelocityZ", velocityZ);
    }
}
