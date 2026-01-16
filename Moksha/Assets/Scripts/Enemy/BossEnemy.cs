using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

/// <summary>
/// Boss enemy - a powerful melee combatant with the ability to spawn minions.
/// Features:
/// - Melee attacks similar to BasicEnemy
/// - Periodic minion spawning
/// - Walk, Attack, and Spawn animations
/// - Single-instance restriction via spawner integration
/// </summary>
public class BossEnemy : EnemyBase
{
    [Header("Boss Settings")]
    [SerializeField] private float bossScale = 2f;
    [Tooltip("How close the boss needs to be to attack")]
    [SerializeField] private float meleeAttackRange = 3f;

    [Header("Minion Spawning")]
    [Tooltip("Prefabs of enemies to spawn as minions")]
    [SerializeField] private GameObject[] minionPrefabs;
    [Tooltip("Stats for spawned minions")]
    [SerializeField] private EnemyStats[] minionStats;
    [Tooltip("Time between spawn attempts")]
    [SerializeField] private float spawnCooldown = 10f;
    [Tooltip("Number of minions to spawn each time")]
    [SerializeField] private int minionsPerSpawn = 3;
    [Tooltip("Distance from boss to spawn minions")]
    [SerializeField] private float spawnRadius = 5f;
    [Tooltip("Maximum number of minions allowed at once")]
    [SerializeField] private int maxActiveMinions = 10;

    [Header("Visuals/Audio")]
    [SerializeField] private Animator animator;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private Renderer[] flashRenderers;
    [SerializeField] private Color damageFlashColor = new Color(1f, 0.2f, 0.2f, 1f);
    [SerializeField] private GameObject spawnEffect;
    [SerializeField] private AudioClip spawnSound;

    [Header("Dissolve Effect")]
    [SerializeField] private EnemyDissolve dissolveEffect;

    // Animator Hashes
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int SpawnHash = Animator.StringToHash("Spawn");

    // Component Caching
    private CharacterController characterController;
    private Rigidbody rb;
    private byte componentFlags;

    private const byte FLAG_ANIMATOR = 1;
    private const byte FLAG_CHAR_CONTROLLER = 2;
    private const byte FLAG_RIGIDBODY = 4;
    private const byte FLAG_AUDIO = 8;
    private const byte FLAG_DISSOLVE = 16;

    // Runtime State
    private float attackTimer;
    private float spawnTimer;
    private MaterialPropertyBlock flashBlock;
    private Color[] originalColors;
    private float flashTimer;
    private bool isFlashing;
    private Vector3 moveDirection;
    private Vector3 movement;

    // Track spawned minions
    private List<EnemyBase> activeMinions = new List<EnemyBase>();

    // Cached target damageable
    private IDamageable targetDamageable;
    private bool checkedDamageable;

    // Cached values
    private float cachedMeleeRangeSqr;

    protected override void Awake()
    {
        base.Awake();

        // Cache Components
        animator = GetComponentInChildren<Animator>();
        audioSource = GetComponent<AudioSource>();
        characterController = GetComponent<CharacterController>();
        rb = GetComponent<Rigidbody>();
        dissolveEffect = GetComponent<EnemyDissolve>();

        // Set Flags
        componentFlags = 0;
        if (animator != null) componentFlags |= FLAG_ANIMATOR;
        if (characterController != null) componentFlags |= FLAG_CHAR_CONTROLLER;
        if (rb != null) componentFlags |= FLAG_RIGIDBODY;
        if (audioSource != null) componentFlags |= FLAG_AUDIO;
        if (dissolveEffect != null) componentFlags |= FLAG_DISSOLVE;

        // Setup Flash Renderers
        if (flashRenderers == null || flashRenderers.Length == 0)
            flashRenderers = GetComponentsInChildren<Renderer>();

        if (flashRenderers.Length > 0)
        {
            flashBlock = new MaterialPropertyBlock();
            originalColors = new Color[flashRenderers.Length];
            for (int i = 0; i < flashRenderers.Length; i++)
            {
                if (flashRenderers[i] == null) continue;
                flashRenderers[i].GetPropertyBlock(flashBlock);
                originalColors[i] = flashRenderers[i].sharedMaterial.HasProperty("_BaseColor") ?
                    flashRenderers[i].sharedMaterial.GetColor("_BaseColor") : Color.white;
            }
        }

        // Apply boss scale
        transform.localScale = Vector3.one * bossScale;
    }

    protected override void CacheStats()
    {
        base.CacheStats();
        cachedMeleeRangeSqr = meleeAttackRange * meleeAttackRange;
    }

    protected override void UpdateBehavior(float deltaTime)
    {
        if (IsDissolving) return;

        // Clean up dead minions from tracking list
        activeMinions.RemoveAll(m => m == null || m.IsDead);

        FaceTargetInstant();
        float sqrDistance = GetSqrDistanceToTarget();

        // Update timers
        if (attackTimer > 0f) attackTimer -= deltaTime;
        if (spawnTimer > 0f) spawnTimer -= deltaTime;
        if (isFlashing) UpdateFlash(deltaTime);

        // --- AI LOGIC ---

        // Try to spawn minions periodically
        if (spawnTimer <= 0f && activeMinions.Count < maxActiveMinions)
        {
            TrySpawnMinions();
        }

        // Melee attack if in range
        if (sqrDistance <= cachedMeleeRangeSqr)
        {
            TryAttack();
        }
        // Chase if too far - movement happens but animation stays in default run state
        else
        {
            Move(deltaTime);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Move(float deltaTime)
    {
        GetNormalizedDirectionToTarget(out moveDirection);

        if (moveDirection.x == 0f & moveDirection.z == 0f)
            return;

        Vector3 horizontalMove = moveDirection * cachedMoveSpeed;

        if ((componentFlags & FLAG_CHAR_CONTROLLER) != 0)
        {
            movement.x = horizontalMove.x * deltaTime;
            movement.z = horizontalMove.z * deltaTime;
            movement.y = -9.81f * deltaTime; // Gravity
            characterController.Move(movement);
        }
        else if ((componentFlags & FLAG_RIGIDBODY) != 0)
        {
            Vector3 vel = rb.linearVelocity;
            vel.x = horizontalMove.x;
            vel.z = horizontalMove.z;
            rb.linearVelocity = vel;
        }
        else
        {
            Vector3 newPos = cachedTransform.position + horizontalMove * deltaTime;
            cachedTransform.position = newPos;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void TryAttack()
    {
        if (attackTimer > 0f) return;
        attackTimer = cachedAttackCooldown;

        if ((componentFlags & FLAG_ANIMATOR) != 0)
            animator.SetTrigger(AttackHash);

        if ((componentFlags & FLAG_AUDIO) != 0 && stats.attackSound != null)
            audioSource.PlayOneShot(stats.attackSound);

        DealDamage();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void DealDamage()
    {
        if (targetTransform == null) return;
        if (GetSqrDistanceToTarget() > cachedMeleeRangeSqr) return;

        // Cache the damageable interface lookup
        if (!checkedDamageable)
        {
            targetDamageable = targetTransform.GetComponent<IDamageable>();
            checkedDamageable = true;
        }

        if (targetDamageable != null)
        {
            targetDamageable.TakeDamage(cachedDamage);
        }
    }

    private void TrySpawnMinions()
    {
        if (minionPrefabs == null || minionPrefabs.Length == 0) return;

        spawnTimer = spawnCooldown;

        // Trigger spawn animation
        if ((componentFlags & FLAG_ANIMATOR) != 0)
            animator.SetTrigger(SpawnHash);

        // Play spawn sound
        if ((componentFlags & FLAG_AUDIO) != 0 && spawnSound != null)
            audioSource.PlayOneShot(spawnSound);

        // Spawn effect
        if (spawnEffect != null)
        {
            GameObject effect = Instantiate(spawnEffect, cachedTransform.position, Quaternion.identity);
            Destroy(effect, 3f);
        }

        // Spawn minions
        int toSpawn = Mathf.Min(minionsPerSpawn, maxActiveMinions - activeMinions.Count);
        for (int i = 0; i < toSpawn; i++)
        {
            SpawnMinion();
        }
    }

    private void SpawnMinion()
    {
        if (minionPrefabs == null || minionPrefabs.Length == 0) return;

        // Select random minion type
        int randomIndex = Random.Range(0, minionPrefabs.Length);
        GameObject prefab = minionPrefabs[randomIndex];

        if (prefab == null) return;

        // Calculate spawn position around boss
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float distance = Random.Range(spawnRadius * 0.5f, spawnRadius);
        Vector3 offset = new Vector3(Mathf.Cos(angle) * distance, 0f, Mathf.Sin(angle) * distance);
        Vector3 spawnPos = cachedTransform.position + offset;

        // Instantiate minion - ensure it's active from the start
        GameObject minionObj = Instantiate(prefab, spawnPos, Quaternion.identity);
        
        // Make sure it's active (in case prefab was inactive)
        if (!minionObj.activeSelf)
            minionObj.SetActive(true);

        EnemyBase minion = minionObj.GetComponent<EnemyBase>();

        if (minion == null)
        {
            Debug.LogError($"[BossEnemy] Minion prefab {prefab.name} is missing EnemyBase component!");
            Destroy(minionObj);
            return;
        }

        // Initialize with stats
        EnemyStats statsToUse = null;
        if (minionStats != null && randomIndex < minionStats.Length && minionStats[randomIndex] != null)
        {
            statsToUse = minionStats[randomIndex];
        }
        else if (minion.Stats != null)
        {
            statsToUse = minion.Stats;
        }

        if (statsToUse != null)
        {
            minion.Initialize(statsToUse, targetTransform);
        }
        else
        {
            Debug.LogWarning($"[BossEnemy] No stats found for minion {prefab.name}, using SetTarget only");
            minion.SetTarget(targetTransform);
        }

        // Register with EnemyManager FIRST before tracking
        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.RegisterEnemy(minion);
            minion.SetManagedByManager(true);
        }
        else
        {
            // If no manager, minion will use its own Update loop
            minion.SetManagedByManager(false);
        }

        // TEMPORARY FIX: Force minions to use their own Update loop for testing
        // Remove this line once you verify animations work
        minion.SetManagedByManager(false);

        // Listen to minion death to remove from tracking
        minion.OnDeath += OnMinionDeath;

        // Track minion
        activeMinions.Add(minion);

        Debug.Log($"[BossEnemy] Spawned minion {prefab.name} at {spawnPos}");
    }

    private void OnMinionDeath(EnemyBase minion)
    {
        activeMinions.Remove(minion);
        minion.OnDeath -= OnMinionDeath;
    }

    // --- Helpers ---
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void FaceTargetInstant()
    {
        if (targetTransform == null) return;
        Vector3 dir = cachedTargetPosition - cachedTransform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
            cachedTransform.rotation = Quaternion.LookRotation(dir);
    }

    // --- Death & Dissolve ---
    protected override void Die()
    {
        // Kill all spawned minions when boss dies
        foreach (var minion in activeMinions)
        {
            if (minion != null && !minion.IsDead)
            {
                minion.TakeDamage(float.MaxValue);
            }
        }
        activeMinions.Clear();

        if ((componentFlags & FLAG_AUDIO) != 0 && stats.deathSound != null)
            audioSource.PlayOneShot(stats.deathSound);

        if ((componentFlags & FLAG_DISSOLVE) != 0)
        {
            GrantXPOnce();
            IsDissolving = true;
            if (cachedCollider != null) cachedCollider.enabled = false;
            DisableMovement();
            dissolveEffect.StartDissolve(OnDissolveComplete);
        }
        else
        {
            DisableMovement();
            base.Die();
        }

        // Notify spawner that boss is dead
        if (EnemySpawner.Instance != null)
        {
            EnemySpawner.Instance.OnBossDeath();
        }
    }

    private void OnDissolveComplete()
    {
        IsDissolving = false;
        IsDead = true;
        DisableMovement();
        gameObject.SetActive(false);
    }

    private void DisableMovement()
    {
        if ((componentFlags & FLAG_CHAR_CONTROLLER) != 0)
            characterController.enabled = false;

        if ((componentFlags & FLAG_RIGIDBODY) != 0)
        {
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
    }

    public override void ResetEnemy()
    {
        base.ResetEnemy();
        attackTimer = 0f;
        spawnTimer = 0f;
        isFlashing = false;
        checkedDamageable = false;
        targetDamageable = null;
        activeMinions.Clear();

        if ((componentFlags & FLAG_CHAR_CONTROLLER) != 0)
            characterController.enabled = true;

        if ((componentFlags & FLAG_RIGIDBODY) != 0)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }

        if ((componentFlags & FLAG_DISSOLVE) != 0)
            dissolveEffect.ResetDissolve();
    }

    // --- Flash Logic ---
    public override void TakeDamage(float damage)
    {
        base.TakeDamage(damage);
        if (!IsDead) StartFlash();
    }

    private void StartFlash()
    {
        isFlashing = true;
        flashTimer = 0.15f;
        ApplyFlashColor(damageFlashColor);
    }

    private void UpdateFlash(float dt)
    {
        flashTimer -= dt;
        if (flashTimer <= 0f)
        {
            isFlashing = false;
            RestoreFlashColor();
        }
    }

    private void ApplyFlashColor(Color col)
    {
        for (int i = 0; i < flashRenderers.Length; i++)
        {
            if (flashRenderers[i] == null) continue;
            flashRenderers[i].GetPropertyBlock(flashBlock);
            flashBlock.SetColor("_BaseColor", col);
            flashRenderers[i].SetPropertyBlock(flashBlock);
        }
    }

    private void RestoreFlashColor()
    {
        for (int i = 0; i < flashRenderers.Length; i++)
        {
            if (flashRenderers[i] == null) continue;
            flashRenderers[i].GetPropertyBlock(flashBlock);
            flashBlock.SetColor("_BaseColor", originalColors[i]);
            flashRenderers[i].SetPropertyBlock(flashBlock);
        }
    }

    // Animation event callback (called from attack animation)
    public void OnAttackHit()
    {
        DealDamage();
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Draw melee attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, meleeAttackRange);

        // Draw spawn radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
#endif
}
