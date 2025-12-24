using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float rotationSpeed = 720f; // deg/sec
    public float gravityDown = -2f;    // tiny push to stay grounded

    [Header("Input (assign from Game.inputactions)")]
    public InputActionReference move;            // Vector2
    public InputActionReference aimStick;        // Vector2
    public InputActionReference pointerPosition; // Vector2


    [Header("References")]
    public Transform aimPivot; // optional; default = transform

    CharacterController cc;
    Camera cam;
    Vector3 verticalVel;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        cam = Camera.main;
        if (!aimPivot) aimPivot = transform;
    }

    void OnEnable()
    {
        move?.action.Enable();
        aimStick?.action.Enable();
        pointerPosition?.action.Enable();
    }

    void OnDisable()
    {
        move?.action.Disable();
        aimStick?.action.Disable();
        pointerPosition?.action.Disable();
    }

    void Update()
    {
        HandleMovement();
        HandleAiming();
    }

    void HandleMovement()
    {
        Vector2 moveInput = move?.action.ReadValue<Vector2>() ?? Vector2.zero;

        // Camera-relative movement on XZ plane
        Vector3 camF = cam.transform.forward; camF.y = 0; camF.Normalize();
        Vector3 camR = cam.transform.right; camR.y = 0; camR.Normalize();

        Vector3 moveWorld = (camF * moveInput.y + camR * moveInput.x);
        if (moveWorld.sqrMagnitude > 1f) moveWorld.Normalize();

        // Add a tiny constant downward force so CharacterController stays grounded
        verticalVel.y += gravityDown * Time.deltaTime;

        cc.Move((moveWorld * moveSpeed + verticalVel) * Time.deltaTime);

        // Reset vertical when grounded
        if (cc.isGrounded && verticalVel.y < 0f) verticalVel.y = -0.1f;
    }

    void HandleAiming()
    {
        // Prefer right-stick aim if present, else aim at pointer on ground plane
        Vector2 stick = aimStick?.action.ReadValue<Vector2>() ?? Vector2.zero;
        Vector3 aimDir = Vector3.zero;

        if (stick.sqrMagnitude > 0.05f)
        {
            Vector3 camF = cam.transform.forward; camF.y = 0; camF.Normalize();
            Vector3 camR = cam.transform.right; camR.y = 0; camR.Normalize();
            aimDir = (camF * stick.y + camR * stick.x).normalized;
        }
        else
        {
            Vector2 screenPos = pointerPosition?.action.ReadValue<Vector2>() ?? Vector2.zero;
            Ray ray = cam.ScreenPointToRay(screenPos);
            // Ground plane at y = 0
            Plane plane = new Plane(Vector3.up, Vector3.zero);
            if (plane.Raycast(ray, out float dist))
            {
                Vector3 hit = ray.GetPoint(dist);
                Vector3 flat = hit - transform.position; flat.y = 0;
                if (flat.sqrMagnitude > 0.001f) aimDir = flat.normalized;
            }
        }

        if (aimDir != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(aimDir, Vector3.up);
            aimPivot.rotation = Quaternion.RotateTowards(aimPivot.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }
    }
}
