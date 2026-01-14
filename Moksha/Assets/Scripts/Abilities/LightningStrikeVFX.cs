using UnityEngine;

public class LightningStrikeVFX : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float duration = 1f;
    [SerializeField] private float skyHeight = 15f;
    [SerializeField] private bool lookAtTarget = true;

    [Header("Components")]
    [SerializeField] private ParticleSystem[] particleSystems;

    // Removed local AudioSource -> Manager handles it

    [Header("Camera Shake")]
    [SerializeField] private float shakeDuration = 0.25f;
    [SerializeField] private float shakeStrength = 0.6f;

    private CameraShake cameraShake;

    // Runtime
    private float timer;
    private bool isActive;
    private Transform cachedTransform;
    private Vector3 targetPosition;

    private Transform targetTransform;
    private Vector3 headOffset;
    private bool isTracking;

    private void Awake()
    {
        cachedTransform = transform;

        if (particleSystems == null || particleSystems.Length == 0)
            particleSystems = GetComponentsInChildren<ParticleSystem>();

        cameraShake = FindObjectOfType<CameraShake>(); // Ideally Cache this in Manager too
    }

    public void ActivateAtTarget(Vector3 targetPos)
    {
        targetPosition = targetPos;
        targetTransform = null;
        isTracking = false;
        ActivateInternal();
    }

    public void ActivateAndFollow(Transform target, Vector3 offset = default)
    {
        if (target == null)
        {
            ActivateAtTarget(transform.position);
            return;
        }

        targetTransform = target;
        headOffset = offset;
        isTracking = true;
        targetPosition = target.position + offset;

        ActivateInternal();
    }

    private void ActivateInternal()
    {
        UpdatePositionFromTarget();

        gameObject.SetActive(true);
        isActive = true;
        timer = 0f;

        // Play Particles
        for (int i = 0; i < particleSystems.Length; i++)
        {
            // Null check removed if we assume array is clean, but safer to keep
            if (particleSystems[i] != null)
            {
                particleSystems[i].Clear();
                particleSystems[i].Play();
            }
        }

        // Audio handled by Manager now

        if (cameraShake != null)
        {
            cameraShake.Shake(shakeDuration, shakeStrength);
        }
    }

    private void UpdatePositionFromTarget()
    {
        if (isTracking && targetTransform != null)
        {
            targetPosition = targetTransform.position + headOffset;
        }

        // Set Position
        Vector3 spawnPos = targetPosition;
        spawnPos.y += skyHeight;
        cachedTransform.position = spawnPos;

        // Set Rotation
        if (lookAtTarget)
        {
            cachedTransform.LookAt(targetPosition);
        }
        else
        {
            cachedTransform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }
    }

    public void Deactivate()
    {
        isActive = false;
        isTracking = false;
        targetTransform = null;

        // Stop Particles
        for (int i = 0; i < particleSystems.Length; i++)
        {
            if (particleSystems[i] != null)
                particleSystems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        gameObject.SetActive(false);

        if (LightningStrikeManager.Instance != null)
        {
            LightningStrikeManager.Instance.ReturnToPool(this);
        }
    }

    private void Update()
    {
        if (!isActive) return;

        if (isTracking)
        {
            if (targetTransform == null) isTracking = false;
            else UpdatePositionFromTarget();
        }

        timer += Time.deltaTime;
        if (timer >= duration)
        {
            Deactivate();
        }
    }
}