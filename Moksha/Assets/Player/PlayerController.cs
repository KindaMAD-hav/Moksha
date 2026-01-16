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

#if UNITY_EDITOR
    [Header("Debug (Editor Only)")]
    public bool debugAiming = false;
    public bool debugMovement = false;
#endif

    CharacterController cc;
    Camera cam;
    Vector3 verticalVel;

    public bool HasManualAimInput { get; private set; }

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        cam = Camera.main;

        if (!aimPivot) aimPivot = transform;

#if UNITY_EDITOR
        if (cam == null)
        {
            Debug.LogError("[PlayerController] Camera.main is NULL!");
        }
#endif
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

        // Strong smoothing (locks in ~2-3 frames)
        aimPivot.rotation = Quaternion.Slerp(
            aimPivot.rotation,
            targetRot,
            20f * Time.deltaTime
        );
    }

    void HandleMovement()
    {
        Vector2 moveInput = move?.action.ReadValue<Vector2>() ?? Vector2.zero;

#if UNITY_EDITOR
        if (debugMovement && moveInput != Vector2.zero)
        {
            Debug.Log($"[Movement] Input: {moveInput}");
        }
#endif

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
        HasManualAimInput = false;

        Vector2 stick = aimStick?.action.ReadValue<Vector2>() ?? Vector2.zero;
        Vector3 aimDir = Vector3.zero;

        // STICK AIM
        if (stick.sqrMagnitude > 0.05f)
        {
            HasManualAimInput = true;
            Vector3 camF = cam.transform.forward; camF.y = 0; camF.Normalize();
            Vector3 camR = cam.transform.right; camR.y = 0; camR.Normalize();

            aimDir = (camF * stick.y + camR * stick.x).normalized;

#if UNITY_EDITOR
            if (debugAiming)
            {
                Debug.Log($"[AIM] Stick used | Stick: {stick} | AimDir: {aimDir}");
            }
#endif
        }
        // MOUSE AIM
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
                    HasManualAimInput = true;
                    aimDir = flat.normalized;
                }
            }
        }

        HasManualAimInput = stick.sqrMagnitude > 0.05f;

        // ROTATION
        if (aimDir != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(aimDir, Vector3.up);
            aimPivot.rotation = Quaternion.RotateTowards(
                aimPivot.rotation,
                targetRot,
                rotationSpeed * Time.deltaTime
            );

#if UNITY_EDITOR
            if (debugAiming)
            {
                Debug.Log($"[AIM] Rotating toward: {targetRot.eulerAngles}");
            }
#endif
        }
    }
}
