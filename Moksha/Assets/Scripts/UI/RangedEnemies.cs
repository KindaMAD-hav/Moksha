using System.Runtime.CompilerServices;
using UnityEngine;

/// <summary>
/// Optimized Ranged Enemy.
/// Uses ProjectileManager for bullets and EnemyStats for configuration.
/// </summary>
public class RangedEnemy : EnemyBase
{
    [Header("Ranged Setup")]
    [SerializeField] private Transform projectileOrigin;
    [SerializeField] private float fleeDistance = 4f;

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

    // Cached Stats (Pre-calculated to avoid SO access in Update)
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

        // 2. Set Flags (Bitwise operations are faster than null checks in Update)
        componentFlags = 0;
        if (animator != null) componentFlags |= FLAG_ANIMATOR;
        if (characterController != null) componentFlags |= FLAG_CHAR_CONTROLLER;
        if (rb != null) componentFlags |= FLAG_RIGIDBODY;
        if (audioSource != null) componentFlags |= FLAG_AUDIO;
        if (dissolveEffect != null) componentFlags |= FLAG_DISSOLVE;

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

    /// <summary>
    /// Override to cache specific Ranged stats from the SO
    /// </summary>
    protected override void CacheStats()
    {
        base.CacheStats(); // Cache generic stats (Health, MoveSpeed, etc.)

        cachedFleeDistSqr = fleeDistance * fleeDistance;

        // Cache Projectile Settings from the new EnemyStats fields
        if (stats != null)
        {
            cachedProjSpeed = stats.projectileSpeed;
            cachedProjLifetime = stats.projectileLifetime;
        }
        else
        {
            // Fallbacks if stats are missing
            cachedProjSpeed = 10f;
            cachedProjLifetime = 5f;
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

        // Simple manual gravity
        float gravity = -9.81f;

        if ((componentFlags & FLAG_CHAR_CONTROLLER) != 0)
        {
            movement.x = moveDirection.x * cachedMoveSpeed * deltaTime;
            movement.z = moveDirection.z * cachedMoveSpeed * deltaTime;
            movement.y = gravity * deltaTime;
            characterController.Move(movement);
        }
        else if ((componentFlags & FLAG_RIGIDBODY) != 0)
        {
            Vector3 vel = rb.linearVelocity;
            vel.x = moveDirection.x * cachedMoveSpeed;
            vel.z = moveDirection.z * cachedMoveSpeed;
            rb.linearVelocity = vel;
        }
        else
        {
            cachedTransform.position += moveDirection * (cachedMoveSpeed * deltaTime);
        }

        SetAnimSpeed(cachedMoveSpeed * (dirMult > 0 ? 1 : -1));
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

    private void ShootProjectile()
    {
        if (ProjectileManager.Instance == null) return;

        // Calculate direction to target
        Vector3 fireDir = (cachedTargetPosition - projectileOrigin.position).normalized;
        fireDir.y = 0; // Keep flat for top-down logic

        // Fire using the Manager
        ProjectileManager.Instance.FireProjectile(
            projectileOrigin.position,
            fireDir,
            cachedProjSpeed,       // Cached from Stats
            cachedDamage,          // Cached from Stats
            cachedProjLifetime,    // Cached from Stats
            stats.hitEffect,       // Effect from Stats
            stats.hitSound         // Sound from Stats
        );
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
            animator.SetFloat(SpeedHash, Mathf.Abs(speed));
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

            // Disable physics immediately to prevent corpse blocking
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

        // Restore movement components
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