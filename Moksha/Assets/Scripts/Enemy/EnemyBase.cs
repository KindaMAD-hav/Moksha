using System;
using System.Runtime.CompilerServices;
using UnityEngine;

/// <summary>
/// Base class for all enemies. Optimized for high enemy counts.
/// Uses cached values, aggressive inlining, and avoids allocations.
/// </summary>
public abstract class EnemyBase : MonoBehaviour, IDamageable
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
    protected float cachedStoppingDistanceSqr;
    protected float cachedAttackRangeSqr;
    protected float cachedDamage;
    protected float cachedAttackCooldown;
    protected int cachedXPReward;
    protected EnemyPurifyBridge purifyBridge;


    // Cached target position (updated by manager)
    protected Vector3 cachedTargetPosition;

    // Track if managed by EnemyManager
    protected bool isManagedByManager;
    // XP guard (prevents multi-awards during dissolve / repeated hits)
    protected bool hasGrantedXP;


    // Properties
    public EnemyStats Stats
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => stats;
    }

    public float CurrentHealth
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => currentHealth;
    }

    public float MaxHealth
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => cachedMaxHealth;
    }

    public bool IsDead { get; protected set; }
    public bool IsDissolving { get; protected set; }

    public Transform Target
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => targetTransform;
    }

    public int Index { get; set; } = -1;

    protected virtual void Awake()
    {
        cachedTransform = transform;
        purifyBridge = GetComponent<EnemyPurifyBridge>();
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
        if (isManagedByManager | targetTransform == null) return;

        // Allow movement while dissolving, only stop when fully dead
        if (IsDead & !IsDissolving) return;

        cachedTargetPosition = targetTransform.position;
        UpdateBehavior(Time.deltaTime);
    }

    /// <summary>
    /// Cache stats from ScriptableObject to avoid repeated property access
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual void Tick(float deltaTime, Vector3 targetPos)
    {
        // Allow movement while dissolving, only stop when fully dead
        // Use bitwise OR for branchless check
        if (IsDead & !IsDissolving) return;

        cachedTargetPosition = targetPos;
        UpdateBehavior(deltaTime);
    }

    /// <summary>
    /// Mark this enemy as managed by EnemyManager (disables Update)
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetManagedByManager(bool managed)
    {
        isManagedByManager = managed;
    }

    /// <summary>
    /// Override this to implement enemy-specific behavior
    /// </summary>
    protected abstract void UpdateBehavior(float deltaTime);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual void InitializeHealth()
    {
        currentHealth = cachedMaxHealth;
        IsDead = false;
        if (currentHealth > 0f)
        {
            OnHealthChanged?.Invoke(currentHealth, cachedMaxHealth);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual void TakeDamage(float damage)
    {
        if (IsDead) return;

        currentHealth -= damage;

        // 🔥 DAMAGE NUMBER HERE
        if (EnemyDamageNumberManager.Instance != null)
        {
            EnemyDamageNumberManager.Instance.ShowDamage(
                cachedTransform,
                Mathf.RoundToInt(damage),
                Color.white // or override later
            );
        }

        if (currentHealth <= 0f)
        {
            currentHealth = 0f;
            Die();
        }
        else
        {
            if (currentHealth > 0f)
            {
                OnHealthChanged?.Invoke(currentHealth, cachedMaxHealth);
            }
            OnHit();
        }
    }


    protected virtual void OnHit()
    {
        // Override for hit effects - spawn from pool instead of Instantiate
    }

    protected virtual void Die()
    {
        if (IsDead) return;

        IsDead = true;
        GrantXPOnce();

        OnDeath?.Invoke(this);
        gameObject.SetActive(false);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetTarget(Transform target)
    {
        targetTransform = target;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void GrantXPOnce()
    {
        if (hasGrantedXP) return;

        hasGrantedXP = true;

        if (ExperienceManager.Instance != null)
            ExperienceManager.Instance.AddXP(cachedXPReward);
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
        IsDissolving = false;
        hasGrantedXP = false;

        // Restore collider
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.enabled = true;

        InitializeHealth();
    }


    /// <summary>
    /// Get squared distance to target (faster than Distance)
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected float GetSqrDistanceToTarget()
    {
        float dx = cachedTargetPosition.x - cachedTransform.position.x;
        float dz = cachedTargetPosition.z - cachedTransform.position.z;
        return dx * dx + dz * dz;
    }

    /// <summary>
    /// Get direction to target (no normalization for speed)
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void GetDirectionToTarget(out float dirX, out float dirZ)
    {
        Vector3 pos = cachedTransform.position;
        dirX = cachedTargetPosition.x - pos.x;
        dirZ = cachedTargetPosition.z - pos.z;
    }

    /// <summary>
    /// Get normalized direction to target
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void GetNormalizedDirectionToTarget(out Vector3 direction)
    {
        Vector3 pos = cachedTransform.position;
        direction.x = cachedTargetPosition.x - pos.x;
        direction.y = 0f;
        direction.z = cachedTargetPosition.z - pos.z;

        float sqrMag = direction.x * direction.x + direction.z * direction.z;
        if (sqrMag > 0.0001f)
        {
            float invMag = 1f / Mathf.Sqrt(sqrMag); // Use Mathf for Unity
            direction.x *= invMag;
            direction.z *= invMag;
        }
    }
}
