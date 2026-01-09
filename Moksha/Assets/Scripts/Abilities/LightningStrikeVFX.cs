using UnityEngine;

/// <summary>
/// Individual lightning strike VFX controller.
/// Attached to the lightning VFX prefab. Handles auto-return to pool.
/// Positions itself so the lightning visually strikes DOWN onto the target.
/// </summary>
public class LightningStrikeVFX : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("How long the VFX plays before returning to pool")]
    [SerializeField] private float duration = 1f;
    
    [Header("Positioning")]
    [Tooltip("Height above target where the VFX spawns (lightning comes from sky)")]
    [SerializeField] private float skyHeight = 15f;
    
    [Tooltip("If true, VFX rotates to point down at target")]
    [SerializeField] private bool lookAtTarget = true;
    
    [Header("Components")]
    [SerializeField] private ParticleSystem[] particleSystems;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip strikeSound;
    
    //[Header("Audio")]
    //[SerializeField] private float minPitch = 0.9f;
    //[SerializeField] private float maxPitch = 1.1f;

    [Header("Camera Shake")]
    [SerializeField] private float shakeDuration = 0.25f;
    [SerializeField] private float shakeStrength = 0.6f;

    private CameraShake cameraShake;

    // Runtime
    private float timer;
    private bool isActive;
    private Transform cachedTransform;
    private Vector3 targetPosition;
    
    // Target tracking
    private Transform targetTransform;
    private Vector3 headOffset;
    private bool isTracking;

    private void Awake()
    {
        cachedTransform = transform;

        if (particleSystems == null || particleSystems.Length == 0)
            particleSystems = GetComponentsInChildren<ParticleSystem>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        cameraShake = FindObjectOfType<CameraShake>();
    }

    /// <summary>
    /// Activate the lightning strike - VFX spawns at sky height, pointing down at target
    /// </summary>
    /// <param name="targetPos">The exact position to strike (e.g., enemy's head)</param>
    public void ActivateAtTarget(Vector3 targetPos)
    {
        targetPosition = targetPos;
        targetTransform = null;
        isTracking = false;
        
        ActivateInternal();
    }
    
    /// <summary>
    /// Activate the lightning strike and follow a target transform (e.g., enemy head)
    /// </summary>
    /// <param name="target">The transform to follow</param>
    /// <param name="offset">Offset from target position (e.g., head height)</param>
    public void ActivateAndFollow(Transform target, Vector3 offset = default)
    {
        if (target == null)
        {
            Debug.LogWarning("[LightningStrikeVFX] Target is null, using fallback position");
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
        // Position VFX above the target (in the sky)
        UpdatePositionFromTarget();
        
        gameObject.SetActive(true);
        isActive = true;
        timer = 0f;
        
        // Play all particle systems
        for (int i = 0; i < particleSystems.Length; i++)
        {
            if (particleSystems[i] != null)
            {
                particleSystems[i].Clear();
                particleSystems[i].Play();
            }
        }
        
        //// Play sound with slight pitch variation
        //if (audioSource != null && strikeSound != null)
        //{
        //    audioSource.pitch = Random.Range(minPitch, maxPitch);
        //    audioSource.PlayOneShot(strikeSound);
        //}
        if (cameraShake != null)
        {
            cameraShake.Shake(shakeDuration, shakeStrength);
        }
    }
    
    private void UpdatePositionFromTarget()
    {
        // Update target position if tracking
        if (isTracking && targetTransform != null)
        {
            targetPosition = targetTransform.position + headOffset;
        }
        
        // Position VFX above the target
        Vector3 spawnPos = new Vector3(targetPosition.x, targetPosition.y + skyHeight, targetPosition.z);
        cachedTransform.position = spawnPos;
        
        // Rotate to point down at target
        if (lookAtTarget)
        {
            cachedTransform.LookAt(targetPosition);
        }
        else
        {
            // Just point straight down
            cachedTransform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }
    }

    /// <summary>
    /// Legacy activate method - calls new method
    /// </summary>
    public void Activate(Vector3 targetPosition)
    {
        ActivateAtTarget(targetPosition);
    }

    /// <summary>
    /// Deactivate and return to pool
    /// </summary>
    public void Deactivate()
    {
        isActive = false;
        isTracking = false;
        targetTransform = null;
        
        // Stop all particle systems
        for (int i = 0; i < particleSystems.Length; i++)
        {
            if (particleSystems[i] != null)
            {
                particleSystems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
        
        gameObject.SetActive(false);
        
        // Return to pool
        if (LightningStrikeManager.Instance != null)
        {
            LightningStrikeManager.Instance.ReturnToPool(this);
        }
    }

    private void Update()
    {
        if (!isActive) return;
        
        // Follow target if tracking
        if (isTracking)
        {
            // Check if target was destroyed
            if (targetTransform == null)
            {
                isTracking = false;
            }
            else
            {
                UpdatePositionFromTarget();
            }
        }
        
        timer += Time.deltaTime;
        
        if (timer >= duration)
        {
            Deactivate();
        }
    }

    /// <summary>
    /// Set the duration (called by manager if needed)
    /// </summary>
    public void SetDuration(float newDuration)
    {
        duration = newDuration;
    }
    
    /// <summary>
    /// Get the target position this strike is aimed at
    /// </summary>
    public Vector3 GetTargetPosition()
    {
        return targetPosition;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Draw line from VFX to target
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, targetPosition);
        Gizmos.DrawWireSphere(targetPosition, 0.3f);
    }
#endif
}
