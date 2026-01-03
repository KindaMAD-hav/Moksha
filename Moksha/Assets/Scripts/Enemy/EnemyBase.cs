using System;
using UnityEngine;

/// <summary>
/// Base class for all enemies. Handles common functionality like
/// health, damage, death, and XP drops.
/// </summary>
public abstract class EnemyBase : MonoBehaviour
{
    [Header("Stats (Override with EnemyStats SO)")]
    [SerializeField] protected EnemyStats stats;
    
    [Header("Runtime State")]
    [SerializeField] protected float currentHealth;
    
    [Header("References")]
    [SerializeField] protected Transform targetTransform;
    
    // Events
    public event Action<EnemyBase> OnDeath;
    public event Action<float, float> OnHealthChanged; // current, max
    
    // Properties
    public EnemyStats Stats => stats;
    public float CurrentHealth => currentHealth;
    public float MaxHealth => stats != null ? stats.maxHealth : 100f;
    public bool IsDead => currentHealth <= 0;
    public Transform Target => targetTransform;
    
    protected virtual void Awake()
    {
        InitializeHealth();
    }

    protected virtual void Start()
    {
        // Auto-find player if not assigned
        if (targetTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                targetTransform = player.transform;
        }
    }

    protected virtual void Update()
    {
        if (IsDead || targetTransform == null) return;
        
        UpdateBehavior();
    }

    /// <summary>
    /// Override this to implement enemy-specific behavior (movement, attacks, etc.)
    /// </summary>
    protected abstract void UpdateBehavior();

    /// <summary>
    /// Initialize or reset health to max
    /// </summary>
    public virtual void InitializeHealth()
    {
        currentHealth = MaxHealth;
        OnHealthChanged?.Invoke(currentHealth, MaxHealth);
    }

    /// <summary>
    /// Apply damage to this enemy
    /// </summary>
    public virtual void TakeDamage(float damage)
    {
        if (IsDead) return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);
        
        OnHealthChanged?.Invoke(currentHealth, MaxHealth);

        if (stats != null && stats.hitEffect != null)
            PlayHitEffect();

        if (currentHealth <= 0)
            Die();
    }

    /// <summary>
    /// Called when health reaches 0
    /// </summary>
    protected virtual void Die()
    {
        // Grant XP to player
        if (ExperienceManager.Instance != null && stats != null)
            ExperienceManager.Instance.AddXP(stats.xpReward);

        // Play death effect
        if (stats != null && stats.deathEffect != null)
            Instantiate(stats.deathEffect, transform.position, Quaternion.identity);

        OnDeath?.Invoke(this);
        
        // Destroy or return to pool
        gameObject.SetActive(false);
    }

    protected virtual void PlayHitEffect()
    {
        if (stats.hitEffect != null)
            Instantiate(stats.hitEffect, transform.position, Quaternion.identity);
    }

    /// <summary>
    /// Set a new target for this enemy
    /// </summary>
    public void SetTarget(Transform target)
    {
        targetTransform = target;
    }

    /// <summary>
    /// Initialize enemy with stats (used by spawner/pool)
    /// </summary>
    public virtual void Initialize(EnemyStats enemyStats, Transform target)
    {
        stats = enemyStats;
        targetTransform = target;
        InitializeHealth();
    }

    /// <summary>
    /// Reset enemy state (for object pooling)
    /// </summary>
    public virtual void ResetEnemy()
    {
        InitializeHealth();
    }

    protected float GetDistanceToTarget()
    {
        if (targetTransform == null) return float.MaxValue;
        return Vector3.Distance(transform.position, targetTransform.position);
    }

    protected Vector3 GetDirectionToTarget()
    {
        if (targetTransform == null) return Vector3.zero;
        Vector3 dir = targetTransform.position - transform.position;
        dir.y = 0; // Keep on ground plane
        return dir.normalized;
    }
}
