using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float rotationSpeed = 720f;
    public float gravityDown = -2f;

    [Header("Input (assign from Game.inputactions)")]
    public InputActionReference move;
    public InputActionReference aimStick;
    public InputActionReference pointerPosition;

    [Header("References")]
    public Transform aimPivot;

    CharacterController cc;
    Camera cam;
    Vector3 verticalVel;

    // NEW: For Speed Management
    private float baseSpeed;
    private Coroutine slowCoroutine;

    public bool HasManualAimInput { get; private set; }

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        cam = Camera.main;

        if (!aimPivot) aimPivot = transform;

        // Store the original speed
        baseSpeed = moveSpeed;
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

    public void RotateTowardsAutoAim(Vector3 worldDir)
    {
        worldDir.y = 0f;
        if (worldDir.sqrMagnitude < 0.001f) return;

        Quaternion targetRot = Quaternion.LookRotation(worldDir, Vector3.up);
        aimPivot.rotation = Quaternion.Slerp(aimPivot.rotation, targetRot, 20f * Time.deltaTime);
    }

    void HandleMovement()
    {
        Vector2 moveInput = move?.action.ReadValue<Vector2>() ?? Vector2.zero;

        Vector3 camF = cam.transform.forward; camF.y = 0; camF.Normalize();
        Vector3 camR = cam.transform.right; camR.y = 0; camR.Normalize();

        Vector3 moveWorld = camF * moveInput.y + camR * moveInput.x;
        if (moveWorld.sqrMagnitude > 1f) moveWorld.Normalize();

        verticalVel.y += gravityDown * Time.deltaTime;
        cc.Move((moveWorld * moveSpeed + verticalVel) * Time.deltaTime);

        if (cc.isGrounded && verticalVel.y < 0f)
            verticalVel.y = -0.1f;
    }

    void HandleAiming()
    {
        Vector2 stick = aimStick?.action.ReadValue<Vector2>() ?? Vector2.zero;
        Vector3 aimDir = Vector3.zero;

        if (stick.sqrMagnitude > 0.05f)
        {
            HasManualAimInput = true;
            Vector3 camF = cam.transform.forward; camF.y = 0; camF.Normalize();
            Vector3 camR = cam.transform.right; camR.y = 0; camR.Normalize();
            aimDir = (camF * stick.y + camR * stick.x).normalized;
        }
        else
        {
            Vector2 screenPos = pointerPosition?.action.ReadValue<Vector2>() ?? Vector2.zero;
            Ray ray = cam.ScreenPointToRay(screenPos);
            Plane plane = new Plane(Vector3.up, Vector3.zero);

            if (plane.Raycast(ray, out float dist))
            {
                Vector3 hit = ray.GetPoint(dist);
                Vector3 flat = hit - transform.position;
                flat.y = 0;
                if (flat.sqrMagnitude > 0.001f)
                {
                    aimDir = flat.normalized;
                }
            }
        }

        HasManualAimInput = stick.sqrMagnitude > 0.05f || aimDir != Vector3.zero;

        if (aimDir != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(aimDir, Vector3.up);
            aimPivot.rotation = Quaternion.RotateTowards(aimPivot.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }
    }

    // --- NEW: SLOW SYSTEM ---

    /// <summary>
    /// Slows the player by a percentage (0.0 to 1.0) for a duration.
    /// </summary>
    public void ApplySlow(float slowPercentage, float duration)
    {
        if (slowCoroutine != null) StopCoroutine(slowCoroutine);
        slowCoroutine = StartCoroutine(SlowRoutine(slowPercentage, duration));
    }

    private IEnumerator SlowRoutine(float slowPercentage, float duration)
    {
        // Clamp to ensure we don't go negative or increase speed
        slowPercentage = Mathf.Clamp01(slowPercentage);

        // Calculate new speed (e.g., 6 * (1 - 0.3) = 4.2)
        moveSpeed = baseSpeed * (1f - slowPercentage);

        yield return new WaitForSeconds(duration);

        // Reset
        moveSpeed = baseSpeed;
        slowCoroutine = null;
    }
}