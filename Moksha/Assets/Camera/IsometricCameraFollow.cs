using UnityEngine;

public class IsometricCameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(-10f, 12f, -10f);
    public float smoothTime = 0.15f;

    private CameraShake cameraShake;
    private bool needsHardReset;

    Vector3 vel;

    private void Awake()
    {
        cameraShake = GetComponent<CameraShake>();
        ShatteredPauseMenu.OnPaused += HandlePaused;
        ShatteredPauseMenu.OnResumed += HandleResumed;
    }
    void LateUpdate()
    {
        if (Time.timeScale == 0f)
        {
            if (needsHardReset)
            {
                ResetCameraImmediate();
                needsHardReset = false;
            }
            return;
        }

        if (!target) return;

        Vector3 desired = target.position + offset;

        Vector3 basePos = Vector3.SmoothDamp(
            transform.position,
            desired,
            ref vel,
            smoothTime
        );

        Vector3 shakeOffset = cameraShake != null
            ? cameraShake.CurrentOffset
            : Vector3.zero;

        transform.position = basePos + shakeOffset;
    }
    public void MarkForReset()
    {
        needsHardReset = true;
    }

    public void ResetCameraImmediate()
    {
        vel = Vector3.zero;

        if (cameraShake != null)
            cameraShake.ForceStop();

        if (target != null)
            transform.position = target.position + offset;
    }

    private void HandlePaused()
    {
        ResetCameraImmediate();
    }

    private void HandleResumed()
    {
        ResetCameraImmediate();
    }

  
    private void OnDestroy()
    {
        ShatteredPauseMenu.OnPaused -= HandlePaused;
        ShatteredPauseMenu.OnResumed -= HandleResumed;

    }


}
