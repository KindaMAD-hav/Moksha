using UnityEngine;

/// <summary>
/// Individual lightning strike VFX controller.
/// Attached to the lightning VFX prefab. Handles auto-return to pool.
/// </summary>
public class LightningStrikeVFX : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("How long the VFX plays before returning to pool")]
    [SerializeField] private float duration = 1f;
    
    [Tooltip("Offset from target position (usually up for sky-to-ground effect)")]
    [SerializeField] private Vector3 spawnOffset = new Vector3(0f, 5f, 0f);
    
    [Header("Components")]
    [SerializeField] private ParticleSystem[] particleSystems;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip strikeSound;
    
    // Runtime
    private float timer;
    private bool isActive;
    private Transform cachedTransform;

    private void Awake()
    {
        cachedTransform = transform;
        
        // Auto-find particle systems if not assigned
        if (particleSystems == null || particleSystems.Length == 0)
        {
            particleSystems = GetComponentsInChildren<ParticleSystem>();
        }
        
        // Auto-find audio source
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    /// <summary>
    /// Activate the lightning strike at target position
    /// </summary>
    public void Activate(Vector3 targetPosition)
    {
        cachedTransform.position = targetPosition + spawnOffset;
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
        
        // Play sound
        if (audioSource != null && strikeSound != null)
        {
            audioSource.PlayOneShot(strikeSound);
        }
    }

    /// <summary>
    /// Deactivate and return to pool
    /// </summary>
    public void Deactivate()
    {
        isActive = false;
        
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
}
