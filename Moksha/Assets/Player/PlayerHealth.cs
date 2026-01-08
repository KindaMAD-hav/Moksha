using System;
using System.Runtime.CompilerServices;
using UnityEngine;

/// <summary>
/// Player health system with invincibility frames and visual feedback.
/// Optimized for performance with minimal allocations.
/// </summary>
public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;
    
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
    
    // Events
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
    private static readonly int EmissionColorProperty = Shader.PropertyToID("_EmissionColor");
    
    // Component flags
    private byte componentFlags;
    private const byte FLAG_AUDIO = 1;
    private const byte FLAG_RENDERERS = 2;

    // Properties
    public float CurrentHealth
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => currentHealth;
    }
    
    public float MaxHealth
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => maxHealth;
    }
    
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
    
    public float HealthPercent
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => maxHealth > 0f ? currentHealth / maxHealth : 0f;
    }

    private void Awake()
    {
        cachedTransform = transform;
        propertyBlock = new MaterialPropertyBlock();
        
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
            if (flashRenderers[i] != null && flashRenderers[i].material != null)
            {
                originalColors[i] = flashRenderers[i].material.color;
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
        currentHealth = maxHealth;
        isDead = false;
        isInvincible = false;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;
        if (isInvincible) return;
        if (damage <= 0f) return;

        currentHealth -= damage;
        OnDamaged?.Invoke();
        
        // Play hurt sound
        if ((componentFlags & FLAG_AUDIO) != 0 && hurtSound != null)
        {
            audioSource.PlayOneShot(hurtSound);
        }

        if (currentHealth <= 0f)
        {
            currentHealth = 0f;
            Die();
        }
        else
        {
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            StartInvincibility();
        }
    }

    public void Heal(float amount)
    {
        if (isDead) return;
        if (amount <= 0f) return;

        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void SetMaxHealth(float newMax, bool healToFull = false)
    {
        float healthPercent = maxHealth > 0f ? currentHealth / maxHealth : 1f;
        maxHealth = newMax;
        
        if (healToFull)
        {
            currentHealth = maxHealth;
        }
        else
        {
            // Maintain health percentage
            currentHealth = Mathf.Min(maxHealth * healthPercent, maxHealth);
        }
        
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void AddMaxHealth(float amount, bool alsoHeal = true)
    {
        maxHealth += amount;
        if (alsoHeal)
        {
            currentHealth += amount;
        }
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
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
            
            if (flashState)
            {
                propertyBlock.SetColor(ColorProperty, damageFlashColor);
            }
            else
            {
                propertyBlock.SetColor(ColorProperty, originalColors[i]);
            }
            
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
            propertyBlock.SetColor(ColorProperty, originalColors[i]);
            flashRenderers[i].SetPropertyBlock(propertyBlock);
        }
    }

    private void Die()
    {
        isDead = true;
        isInvincible = false;
        RestoreOriginalColors();
        
        // Play death sound
        if ((componentFlags & FLAG_AUDIO) != 0 && deathSound != null)
        {
            audioSource.PlayOneShot(deathSound);
        }

        OnHealthChanged?.Invoke(0f, maxHealth);
        OnDeath?.Invoke();
        
        // Don't destroy - let GameManager handle death state
        Debug.Log("Player died!");
    }

    public void Revive(float healthPercent = 1f)
    {
        if (!isDead) return;
        
        isDead = false;
        currentHealth = maxHealth * Mathf.Clamp01(healthPercent);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (maxHealth < 1f) maxHealth = 1f;
        if (invincibilityDuration < 0f) invincibilityDuration = 0f;
        if (flashInterval < 0.01f) flashInterval = 0.01f;
    }

    [ContextMenu("Test Take 10 Damage")]
    private void TestDamage()
    {
        TakeDamage(10f);
    }

    [ContextMenu("Test Heal 10")]
    private void TestHeal()
    {
        Heal(10f);
    }

    [ContextMenu("Test Kill Player")]
    private void TestKill()
    {
        TakeDamage(maxHealth + 1f);
    }
#endif
}
