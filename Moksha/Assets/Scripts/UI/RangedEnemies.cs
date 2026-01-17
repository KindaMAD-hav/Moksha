using System.Runtime.CompilerServices;
using UnityEngine;

/// <summary>
/// Optimized Ranged Enemy with Levitation and Attack Animation.
/// Supports both Pooled (Global) and Instantiated (Unique) projectiles.
/// </summary>
public class RangedEnemy : EnemyBase
{
    [Header("Ranged Setup")]
    [Tooltip("Leave empty to use the global ProjectileManager pool. Assign to override.")]
    [SerializeField] private GameObject projectilePrefab; // <--- ADDED BACK
    [SerializeField] private Transform projectileOrigin;
    [SerializeField] private float fleeDistance = 4f;

    [Header("Fire Rate")]
    [Tooltip("Override attack cooldown. Set to -1 to use EnemyStats value. Higher = slower fire rate.")]
    [SerializeField] private float attackCooldownOverride = -1f;

    [Header("Levitation Settings")]
    [Tooltip("How high off the ground the enemy floats")]
    [SerializeField] private float hoverHeight = 1.5f;
    [Tooltip("Speed of the up/down bobbing")]
    [SerializeField] private float bobFrequency = 2f;
    [Tooltip("Distance of the up/down bobbing")]
    [SerializeField] private float bobAmplitude = 0.2f;
    [Tooltip("Layers to consider as ground for levitation")]
    [SerializeField] private LayerMask groundLayer;

    [Header("Visuals/Audio")]
    [SerializeField] private Animator animator;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private Renderer[] flashRenderers;
    [SerializeField] private Color damageFlashColor = new Color(1f, 0.2f, 0.2f, 1f);

    // Dissolve Effect
    [SerializeField] private EnemyDissolve dissolveEffect;

    // --- Optimization Caches ---
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int AttackHash = Animator.StringToHash("Attack");

    private CharacterController characterController;
    private Rigidbody rb;
    private byte componentFlags;

    // Component Flags for fast checks
    private const byte FLAG_ANIMATOR = 1;
    private const byte FLAG_CHAR_CONTROLLER = 2;
    private const byte FLAG_RIGIDBODY = 4;
    private const byte FLAG_AUDIO = 8;
    private const byte FLAG_DISSOLVE = 16;

    // Runtime State
    private float attackTimer;
    private MaterialPropertyBlock flashBlock;
    private Color[] originalColors;
    private float flashTimer;
    private bool isFlashing;
    private Vector3 moveDirection;
    private Vector3 movement;

    // Cached Stats
    private float cachedFleeDistSqr;
    private float cachedProjSpeed;
    private float cachedProjLifetime;

    protected override void Awake()
    {
        base.Awake();

        // 1. Cache Components
        animator = GetComponentInChildren<Animator>();
        audioSource = GetComponent<AudioSource>();
        characterController = GetComponent<CharacterController>();
        rb = GetComponent<Rigidbody>();
        dissolveEffect = GetComponent<EnemyDissolve>();

        // 2. Set Flags
        componentFlags = 0;
        if (animator != null) componentFlags |= FLAG_ANIMATOR;
        if (characterController != null) componentFlags |= FLAG_CHAR_CONTROLLER;
        if (rb != null) componentFlags |= FLAG_RIGIDBODY;
        if (audioSource != null) componentFlags |= FLAG_AUDIO;
        if (dissolveEffect != null) componentFlags |= FLAG_DISSOLVE;

        // Default ground layer if not set
        if (groundLayer == 0) groundLayer = LayerMask.GetMask("Default", "Ground", "Terrain");

        // 3. Setup Flash Renderers
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

        if (projectileOrigin == null) projectileOrigin = transform;
    }

    protected override void CacheStats()
    {
        base.CacheStats();
        cachedFleeDistSqr = fleeDistance * fleeDistance;

        if (stats != null)
        {
            cachedProjSpeed = stats.projectileSpeed;
            cachedProjLifetime = stats.projectileLifetime;
        }
        else
        {
            cachedProjSpeed = 10f;
            cachedProjLifetime = 5f;
        }

        // Apply fire rate override if set
        if (attackCooldownOverride > 0f)
        {
            cachedAttackCooldown = attackCooldownOverride;
        }
    }

    protected override void UpdateBehavior(float deltaTime)
    {
        if (IsDissolving) return;

        FaceTargetInstant();
        float sqrDistance = GetSqrDistanceToTarget();

        if (attackTimer > 0f) attackTimer -= deltaTime;
        if (isFlashing) UpdateFlash(deltaTime);

        // --- AI LOGIC ---

        // 1. Flee if too close
        if (sqrDistance < cachedFleeDistSqr)
        {
            Move(deltaTime, -1f); // Move backwards
        }
        // 2. Attack if in range
        else if (sqrDistance <= cachedAttackRangeSqr)
        {
            SetAnimSpeed(0f);
            ApplyLevitation(deltaTime, Vector3.zero);
            TryAttack();
        }
        // 3. Chase if too far
        else
        {
            Move(deltaTime, 1f); // Move forwards
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Move(float deltaTime, float dirMult)
    {
        GetNormalizedDirectionToTarget(out moveDirection);
        moveDirection *= dirMult;

        // Calculate Horizontal Movement
        Vector3 horizontalMove = moveDirection * cachedMoveSpeed;

        // Apply Levitation & Horizontal Movement combined
        ApplyLevitation(deltaTime, horizontalMove);

        // Optional: Update animation speed if you add a Run/Fly animation later
        SetAnimSpeed(cachedMoveSpeed * (dirMult > 0 ? 1 : -1));
    }

    private void ApplyLevitation(float deltaTime, Vector3 desiredHorizontalVelocity)
    {
        float targetY = cachedTransform.position.y;

        if (Physics.Raycast(cachedTransform.position + Vector3.up, Vector3.down, out RaycastHit hit, 10f, groundLayer))
        {
            targetY = hit.point.y + hoverHeight + (Mathf.Sin(Time.time * bobFrequency) * bobAmplitude);
        }

        if ((componentFlags & FLAG_CHAR_CONTROLLER) != 0)
        {
            float verticalDiff = (targetY - cachedTransform.position.y) * 5f;
            movement.x = desiredHorizontalVelocity.x * deltaTime;
            movement.z = desiredHorizontalVelocity.z * deltaTime;
            movement.y = verticalDiff * deltaTime;
            characterController.Move(movement);
        }
        else if ((componentFlags & FLAG_RIGIDBODY) != 0)
        {
            float verticalDiff = (targetY - cachedTransform.position.y) * 5f;
            Vector3 vel = rb.linearVelocity;
            vel.x = desiredHorizontalVelocity.x;
            vel.z = desiredHorizontalVelocity.z;
            vel.y = verticalDiff;
            rb.linearVelocity = vel;
        }
        else
        {
            Vector3 newPos = cachedTransform.position;
            newPos += desiredHorizontalVelocity * deltaTime;
            newPos.y = Mathf.Lerp(newPos.y, targetY, deltaTime * 5f);
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

        ShootProjectile();
    }

    // --- UPDATED SHOOT LOGIC ---
    private void ShootProjectile()
    {
        // 1. Calculate direction to target
        Vector3 fireDir = (cachedTargetPosition - projectileOrigin.position).normalized;
        fireDir.y = 0; // Keep flat for top-down logic

        // 2. CHECK: Do we have a specific prefab assigned?
        if (projectilePrefab != null)
        {
            // --- MANUAL INSTANTIATION (Unique Projectile) ---
            GameObject projObj = Instantiate(projectilePrefab, projectileOrigin.position, Quaternion.LookRotation(fireDir));

            // Try to initialize it if it has the component
            EnemyProjectile projScript = projObj.GetComponent<EnemyProjectile>();
            if (projScript != null)
            {
                projScript.Initialize(cachedDamage, cachedProjSpeed, fireDir);

                // CRITICAL FIX: Since this isn't in the pool, the pool won't destroy it.
                // We must ensure it destroys itself if it tries to return to a pool that doesn't own it.
                // NOTE: This relies on you updating EnemyProjectilePool OR simply relying on Destroy fallback.
                // If EnemyProjectile.cs purely relies on ReturnToPool, this object might persist.
                // Safest quick fix for non-pooled objects:
                Destroy(projObj, cachedProjLifetime); // Hard backup destruction
            }
        }
        else
        {
            // --- POOLED INSTANTIATION (Optimization) ---
            if (ProjectileManager.Instance == null) return;

            ProjectileManager.Instance.FireProjectile(
                projectileOrigin.position,
                fireDir,
                cachedProjSpeed,
                cachedDamage,
                cachedProjLifetime,
                stats.hitEffect,
                stats.hitSound
            );
        }
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetAnimSpeed(float speed)
    {
        if ((componentFlags & FLAG_ANIMATOR) != 0)
        {
            animator.SetFloat(SpeedHash, Mathf.Abs(speed));
        }
    }

    // --- Death & Dissolve ---
    protected override void Die()
    {
        if ((componentFlags & FLAG_AUDIO) != 0 && stats.deathSound != null)
            audioSource.PlayOneShot(stats.deathSound);

        if ((componentFlags & FLAG_DISSOLVE) != 0)
        {
            GrantXPOnce();
            IsDissolving = true;
            if (GetComponent<Collider>()) GetComponent<Collider>().enabled = false;
            DisableMovement();
            dissolveEffect.StartDissolve(OnDissolveComplete);
        }
        else
        {
            DisableMovement();
            base.Die();
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
        isFlashing = false;

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
}