using UnityEngine;

/// <summary>
/// Healing VFX controller - Attached to the healing particle effect GameObject.
/// Handles playing the healing particle effect and optional audio.
/// </summary>
public class HealingVFX : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private ParticleSystem[] particleSystems;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip healSound;

    [Header("Audio")]
    [SerializeField] private float minPitch = 0.95f;
    [SerializeField] private float maxPitch = 1.05f;

    [Header("Auto-Deactivate")]
    [Tooltip("If true, deactivates the GameObject after particles finish")]
    [SerializeField] private bool autoDeactivate = false;

    [Tooltip("Duration before auto-deactivating (if autoDeactivate is true)")]
    [SerializeField] private float duration = 2f;

    // Runtime
    private float timer;
    private bool isPlaying;

    private void Awake()
    {
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

        // Start inactive if autoDeactivate is enabled
        if (autoDeactivate)
        {
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Play the healing effect
    /// </summary>
    public void PlayEffect()
    {
        // Activate GameObject if it was inactive
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        isPlaying = true;
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

        // Play sound with slight pitch variation
        if (audioSource != null && healSound != null)
        {
            audioSource.pitch = Random.Range(minPitch, maxPitch);
            audioSource.PlayOneShot(healSound);
        }
    }

    /// <summary>
    /// Stop the healing effect
    /// </summary>
    public void StopEffect()
    {
        isPlaying = false;

        // Stop all particle systems
        for (int i = 0; i < particleSystems.Length; i++)
        {
            if (particleSystems[i] != null)
            {
                particleSystems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        if (autoDeactivate)
        {
            gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (!isPlaying || !autoDeactivate) return;

        timer += Time.deltaTime;

        if (timer >= duration)
        {
            StopEffect();
        }
    }

    /// <summary>
    /// Set the duration (if needed)
    /// </summary>
    public void SetDuration(float newDuration)
    {
        duration = newDuration;
    }

#if UNITY_EDITOR
    [ContextMenu("Test Play Effect")]
    private void TestPlay()
    {
        PlayEffect();
    }
    
    [ContextMenu("Test Stop Effect")]
    private void TestStop()
    {
        StopEffect();
    }
#endif
}