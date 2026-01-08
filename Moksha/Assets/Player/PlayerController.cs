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

    [Header("Debug")]
    public bool debugAiming = true;
    public bool debugMovement = false;

    CharacterController cc;
    Camera cam;
    Vector3 verticalVel;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        cam = Camera.main;

        if (!aimPivot) aimPivot = transform;

        if (cam == null)
        {
            Debug.LogError("[PlayerController] ❌ Camera.main is NULL!");
        }
        else
        {
            Debug.Log("[PlayerController] ✅ Camera found: " + cam.name);
        }
    }

    void OnEnable()
    {
        move?.action.Enable();
        aimStick?.action.Enable();
        pointerPosition?.action.Enable();

        Debug.Log("[PlayerController] Inputs enabled");
    }

    void OnDisable()
    {
        move?.action.Disable();
        aimStick?.action.Disable();
        pointerPosition?.action.Disable();

        Debug.Log("[PlayerController] Inputs disabled");
    }

    void Update()
    {
        HandleMovement();
        HandleAiming();
    }

    void HandleMovement()
    {
        Vector2 moveInput = move?.action.ReadValue<Vector2>() ?? Vector2.zero;

        if (debugMovement && moveInput != Vector2.zero)
        {
            Debug.Log($"[Movement] Input: {moveInput}");
        }

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

        // ───────────────────────── STICK AIM ─────────────────────────
        if (stick.sqrMagnitude > 0.05f)
        {
            Vector3 camF = cam.transform.forward; camF.y = 0; camF.Normalize();
            Vector3 camR = cam.transform.right; camR.y = 0; camR.Normalize();

            aimDir = (camF * stick.y + camR * stick.x).normalized;

            if (debugAiming)
            {
                Debug.Log($"[AIM] 🎮 Stick used | Stick: {stick} | AimDir: {aimDir}");
            }
        }
        // ───────────────────────── MOUSE AIM ─────────────────────────
        else
        {
            Vector2 screenPos = pointerPosition?.action.ReadValue<Vector2>() ?? Vector2.zero;

            if (debugAiming)
            {
                Debug.Log($"[AIM] 🖱 Mouse ScreenPos: {screenPos}");
            }

            Ray ray = cam.ScreenPointToRay(screenPos);
            Plane plane = new Plane(Vector3.up, Vector3.zero);

            if (plane.Raycast(ray, out float dist))
            {
                Vector3 hit = ray.GetPoint(dist);
                Vector3 flat = hit - transform.position;
                flat.y = 0;

                if (debugAiming)
                {
                    Debug.Log($"[AIM] 🧭 Ray hit at: {hit}");
                    Debug.Log($"[AIM] Flat dir before normalize: {flat}");
                }

                if (flat.sqrMagnitude > 0.001f)
                {
                    aimDir = flat.normalized;

                    if (debugAiming)
                    {
                        Debug.Log($"[AIM] ✅ Mouse AimDir: {aimDir}");
                    }
                }
                else if (debugAiming)
                {
                    Debug.LogWarning("[AIM] ⚠ Flat direction too small");
                }
            }
            else if (debugAiming)
            {
                Debug.LogWarning("[AIM] ❌ Ray did NOT hit ground plane");
            }
        }

        // ───────────────────────── ROTATION ─────────────────────────
        if (aimDir != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(aimDir, Vector3.up);
            aimPivot.rotation = Quaternion.RotateTowards(
                aimPivot.rotation,
                targetRot,
                rotationSpeed * Time.deltaTime
            );

            if (debugAiming)
            {
                Debug.Log($"[AIM] 🔄 Rotating toward: {targetRot.eulerAngles}");
            }
        }
        else if (debugAiming)
        {
            Debug.LogWarning("[AIM] ❌ aimDir is ZERO — no rotation applied");
        }
    }
}
