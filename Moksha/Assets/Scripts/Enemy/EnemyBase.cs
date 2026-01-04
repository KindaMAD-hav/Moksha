using System;
using UnityEngine;

/// <summary>
/// Base class for all enemies. Optimized for high enemy counts.
/// Uses cached values and avoids allocations.
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
    public event Action<float, float> OnHealthChanged;
    
    // Cached values for performance
    protected Transform cachedTransform;
    protected float cachedMaxHealth;
    protected float cachedMoveSpeed;
    protected float cachedRotationSpeed;
    protected float cachedStoppingDistanceSqr; // Squared for fast distance checks
    protected float cachedAttackRangeSqr;      // Squared for fast distance checks
    protected float cachedDamage;
    protected float cachedAttackCooldown;
    protected int cachedXPReward;
    
    // Cached target position (updated by manager)
    protected Vector3 cachedTargetPosition;
    
    // Track if managed by EnemyManager
    protected bool isManagedByManager;
    
    // Properties
    public EnemyStats Stats => stats;
    public float CurrentHealth => currentHealth;
    public float MaxHealth => cachedMaxHealth;
    public bool IsDead { get; protected set; }
    public Transform Target => targetTransform;
    public int Index { get; set; } // For manager tracking

    protected virtual void Awake()
    {
        cachedTransform = transform;
        CacheStats();
        InitializeHealth();
    }

    protected virtual void Start()
    {
        // Auto-find player if not assigned
        if (targetTransform == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                targetTransform = player.transform;
        }
        
        // Check if EnemyManager exists
        isManagedByManager = EnemyManager.Instance != null;
    }

    /// <summary>
    /// Fallback Update for when EnemyManager is not present
    /// </summary>
    protected virtual void Update()
    {
        // Skip if managed by EnemyManager (it calls Tick instead)
        if (isManagedByManager || IsDead || targetTransform == null) return;
        
        cachedTargetPosition = targetTransform.position;
        UpdateBehavior(Time.deltaTime);
    }

    /// <summary>
    /// Cache stats from ScriptableObject to avoid repeated property access
    /// </summary>
    protected virtual void CacheStats()
    {
        if (stats == null) return;
        
        cachedMaxHealth = stats.maxHealth;
        cachedMoveSpeed = stats.moveSpeed;
        cachedRotationSpeed = stats.rotationSpeed;
        cachedStoppingDistanceSqr = stats.stoppingDistance * stats.stoppingDistance;
        cachedAttackRangeSqr = stats.attackRange * stats.attackRange;
        cachedDamage = stats.damage;
        cachedAttackCooldown = stats.attackCooldown;
        cachedXPReward = stats.xpReward;
    }

    /// <summary>
    /// Called by EnemyManager instead of Update() for batched processing
    /// </summary>
    public virtual void Tick(float deltaTime, Vector3 targetPos)
    {
        if (IsDead) return;
        
        cachedTargetPosition = targetPos;
        UpdateBehavior(deltaTime);
    }

    /// <summary>
    /// Mark this enemy as managed by EnemyManager (disables Update)
    /// </summary>
    public void SetManagedByManager(bool managed)
    {
        isManagedByManager = managed;
    }

    /// <summary>
    /// Override this to implement enemy-specific behavior
    /// </summary>
    protected abstract void UpdateBehavior(float deltaTime);

    public virtual void InitializeHealth()
    {
        currentHealth = cachedMaxHealth;
        IsDead = false;
        OnHealthChanged?.Invoke(currentHealth, cachedMaxHealth);
    }

    public virtual void TakeDamage(float damage)
    {
        if (IsDead) return;

        currentHealth -= damage;
        
        if (currentHealth <= 0f)
        {
            currentHealth = 0f;
            Die();
        }
        else
        {
            OnHealthChanged?.Invoke(currentHealth, cachedMaxHealth);
            OnHit();
        }
    }

    protected virtual void OnHit()
    {
        // Override for hit effects - spawn from pool instead of Instantiate
    }

    protected virtual void Die()
    {
        IsDead = true;
        
        // Grant XP
        if (ExperienceManager.Instance != null)
            ExperienceManager.Instance.AddXP(cachedXPReward);

        OnDeath?.Invoke(this);
        gameObject.SetActive(false);
    }

    public void SetTarget(Transform target)
    {
        targetTransform = target;
    }

    public virtual void Initialize(EnemyStats enemyStats, Transform target)
    {
        stats = enemyStats;
        targetTransform = target;
        CacheStats();
        InitializeHealth();
    }

    public virtual void ResetEnemy()
    {
        IsDead = false;
        InitializeHealth();
    }

    /// <summary>
    /// Get squared distance to target (faster than Distance)
    /// </summary>
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    protected float GetSqrDistanceToTarget()
    {
        Vector3 diff = cachedTargetPosition - cachedTransform.position;
        diff.y = 0f;
        return diff.x * diff.x + diff.z * diff.z;
    }

    /// <summary>
    /// Get direction to target (no normalization for speed)
    /// </summary>
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    protected void GetDirectionToTarget(out Vector3 direction)
    {
        direction = cachedTargetPosition - cachedTransform.position;
        direction.y = 0f;
    }

    /// <summary>
    /// Get normalized direction to target
    /// </summary>
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    protected void GetNormalizedDirectionToTarget(out Vector3 direction)
    {
        GetDirectionToTarget(out direction);
        float sqrMag = direction.x * direction.x + direction.z * direction.z;
        if (sqrMag > 0.0001f)
        {
            float invMag = 1f / Mathf.Sqrt(sqrMag);
            direction.x *= invMag;
            direction.z *= invMag;
        }
    }
}
