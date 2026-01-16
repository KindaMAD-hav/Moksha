using System;
using System.Runtime.CompilerServices;
using UnityEngine;

/// <summary>
/// Player heart-based health system with invincibility frames and visual feedback.
/// - Damage is quantized: 1 heart per hit (damage value ignored by design)
/// - Healing is quantized: 1 heart per call
/// - Still implements IDamageable via float bridge (1 heart = 1 health unit)
/// </summary>
public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Heart Health")]
    [SerializeField] private int maxHearts = 10;
    [SerializeField] private int currentHearts;

    [Header("Invincibility Frames")]
    [SerializeField] private float invincibilityDuration = 1f;
    [SerializeField] private float flashInterval = 0.1f;

    [Header("Visual Feedback")]
    [SerializeField] private Renderer[] flashRenderers;
    [SerializeField] private Color damageFlashColor = new Color(1f, 0.3f, 0.3f, 1f);

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip hurtSound;
    [SerializeField] private AudioClip deathSound;

    [Header("Camera Shake")]
    [SerializeField] private float damageShakeDuration = 0.12f;
    [SerializeField] private float damageShakeStrength = 0.25f;
    private CameraShake cameraShake;

    // Events (kept float to avoid breaking existing UI/scripts)
    public event Action<float, float> OnHealthChanged;
    public event Action OnDeath;
    public event Action OnDamaged;

    // State
    private bool isDead;
    private bool isInvincible;
    private float invincibilityTimer;
    private float flashTimer;
    private bool flashState;

    // Cached
    private Transform cachedTransform;
    private Color[] originalColors;
    private MaterialPropertyBlock propertyBlock;
    private static readonly int ColorProperty = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorPropertyAlt = Shader.PropertyToID("_Color");

    // Component flags
    private byte componentFlags;
    private const byte FLAG_AUDIO = 1;
    private const byte FLAG_RENDERERS = 2;

    // IDamageable compatibility (1 heart = 1 health unit)
    public float CurrentHealth => currentHearts;
    public float MaxHealth => maxHearts;

    // Heart API (preferred)
    public int CurrentHearts => currentHearts;
    public int MaxHearts => maxHearts;

    public float HealthPercent => maxHearts > 0 ? (float)currentHearts / maxHearts : 0f;

    public bool IsDead
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => isDead;
    }

    public bool IsInvincible
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => isInvincible;
    }

    private void Awake()
    {
        cachedTransform = transform;
        propertyBlock = new MaterialPropertyBlock();
        cameraShake = FindObjectOfType<CameraShake>();

        // Cache component flags
        componentFlags = 0;
        if (audioSource != null) componentFlags |= FLAG_AUDIO;

        // Auto-find renderers if not assigned
        if (flashRenderers == null || flashRenderers.Length == 0)
        {
            flashRenderers = GetComponentsInChildren<Renderer>();
        }

        if (flashRenderers != null && flashRenderers.Length > 0)
        {
            componentFlags |= FLAG_RENDERERS;
            CacheOriginalColors();
        }

        InitializeHealth();
    }

    private void CacheOriginalColors()
    {
        originalColors = new Color[flashRenderers.Length];

        for (int i = 0; i < flashRenderers.Length; i++)
        {
            if (flashRenderers[i] == null)
            {
                originalColors[i] = Color.white;
                continue;
            }

            // Safely read colors for different shader setups
            flashRenderers[i].GetPropertyBlock(propertyBlock);

            if (propertyBlock.HasColor(ColorProperty))
                originalColors[i] = propertyBlock.GetColor(ColorProperty);
            else if (propertyBlock.HasColor(ColorPropertyAlt))
                originalColors[i] = propertyBlock.GetColor(ColorPropertyAlt);
            else if (flashRenderers[i].material != null)
            {
                if (flashRenderers[i].material.HasProperty(ColorProperty))
                    originalColors[i] = flashRenderers[i].material.GetColor(ColorProperty);
                else if (flashRenderers[i].material.HasProperty(ColorPropertyAlt))
                    originalColors[i] = flashRenderers[i].material.GetColor(ColorPropertyAlt);
                else
                    originalColors[i] = Color.white;
            }
            else
            {
                originalColors[i] = Color.white;
            }
        }
    }

    private void Update()
    {
        if (!isInvincible) return;

        invincibilityTimer -= Time.deltaTime;

        if (invincibilityTimer <= 0f)
        {
            EndInvincibility();
            return;
        }

        // Flash effect
        flashTimer -= Time.deltaTime;
        if (flashTimer <= 0f)
        {
            flashTimer = flashInterval;
            flashState = !flashState;
            UpdateFlashVisual();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void InitializeHealth()
    {
        currentHearts = maxHearts;
        isDead = false;
        isInvincible = false;
        OnHealthChanged?.Invoke(currentHearts, maxHearts);
    }

    /// <summary>
    /// Quantized damage: always removes exactly 1 heart (damage value ignored by design).
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (isDead || isInvincible) return;

        currentHearts -= 1;
        OnDamaged?.Invoke();

        if (cameraShake != null)
            cameraShake.Shake(damageShakeDuration, damageShakeStrength, 35f);

        if ((componentFlags & FLAG_AUDIO) != 0 && hurtSound != null)
            audioSource.PlayOneShot(hurtSound);

        if (currentHearts <= 0)
        {
            currentHearts = 0;
            Die();
        }
        else
        {
            OnHealthChanged?.Invoke(currentHearts, maxHearts);
            StartInvincibility();
        }
    }

    public void HealOneHeart()
    {
        if (isDead) return;
        if (currentHearts >= maxHearts) return;

        currentHearts += 1;
        if (currentHearts > maxHearts) currentHearts = maxHearts;

        OnHealthChanged?.Invoke(currentHearts, maxHearts);
    }

    private void StartInvincibility()
    {
        isInvincible = true;
        invincibilityTimer = invincibilityDuration;
        flashTimer = flashInterval;
        flashState = true;
        UpdateFlashVisual();
    }

    private void EndInvincibility()
    {
        isInvincible = false;
        flashState = false;
        RestoreOriginalColors();
    }

    private void UpdateFlashVisual()
    {
        if ((componentFlags & FLAG_RENDERERS) == 0) return;

        for (int i = 0; i < flashRenderers.Length; i++)
        {
            if (flashRenderers[i] == null) continue;

            flashRenderers[i].GetPropertyBlock(propertyBlock);

            Color targetColor = flashState ? damageFlashColor : originalColors[i];

            if (flashRenderers[i].material != null && flashRenderers[i].material.HasProperty(ColorProperty))
                propertyBlock.SetColor(ColorProperty, targetColor);

            if (flashRenderers[i].material != null && flashRenderers[i].material.HasProperty(ColorPropertyAlt))
                propertyBlock.SetColor(ColorPropertyAlt, targetColor);

            flashRenderers[i].SetPropertyBlock(propertyBlock);
        }
    }

    private void RestoreOriginalColors()
    {
        if ((componentFlags & FLAG_RENDERERS) == 0) return;

        for (int i = 0; i < flashRenderers.Length; i++)
        {
            if (flashRenderers[i] == null) continue;

            flashRenderers[i].GetPropertyBlock(propertyBlock);

            if (flashRenderers[i].material != null && flashRenderers[i].material.HasProperty(ColorProperty))
                propertyBlock.SetColor(ColorProperty, originalColors[i]);

            if (flashRenderers[i].material != null && flashRenderers[i].material.HasProperty(ColorPropertyAlt))
                propertyBlock.SetColor(ColorPropertyAlt, originalColors[i]);

            flashRenderers[i].SetPropertyBlock(propertyBlock);
        }
    }

    private void Die()
    {
        isDead = true;
        isInvincible = false;

        OnHealthChanged?.Invoke(0f, maxHearts);
        OnDeath?.Invoke();

        Debug.Log("Player died!");
    }


#if UNITY_EDITOR
    private void OnValidate()
    {
        if (maxHearts < 1) maxHearts = 1;
        if (invincibilityDuration < 0f) invincibilityDuration = 0f;
        if (flashInterval < 0.01f) flashInterval = 0.01f;
    }
#endif
}
