using System.Runtime.CompilerServices;
using UnityEngine;

/// <summary>
/// Mortar-style ranged enemy that lobs ice projectiles in a parabolic arc.
/// The projectile lands at the player's predicted position with an AOE indicator.
/// </summary>
public class MortarEnemy : EnemyBase
{
    [Header("Mortar Setup")]
    [Tooltip("The ice mortar projectile prefab (must have IceMortarProjectile component)")]
    [SerializeField] private GameObject mortarProjectilePrefab;
    [SerializeField] private Transform projectileOrigin;

    [Header("Fire Rate")]
    [Tooltip("Override attack cooldown. Set to -1 to use EnemyStats value. Higher = slower fire rate.")]
    [SerializeField] private float attackCooldownOverride = -1f;

    [Header("Arc Settings")]
    [Tooltip("Peak height of the arc above the launch point")]
    [SerializeField] private float arcHeight = 8f;
    [Tooltip("Time for the projectile to reach its target")]
    [SerializeField] private float flightDuration = 1.5f;
    [Tooltip("How far ahead to predict player position (0 = aim at current position)")]
    [SerializeField] private float leadTime = 0.5f;

    [Header("Flee Behavior")]
    [SerializeField] private float fleeDistance = 5f;

    [Header("Visuals/Audio")]
    [SerializeField] private Animator animator;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private Renderer[] flashRenderers;
    [SerializeField] private Color damageFlashColor = new Color(0.4f, 0.8f, 1f, 1f);

    [Header("Dissolve Effect")]
    [SerializeField] private EnemyDissolve dissolveEffect;

    // Animator Hashes
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int AttackHash = Animator.StringToHash("Attack");

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
    private MaterialPropertyBlock flashBlock;
    private Color[] originalColors;
    private float flashTimer;
    private bool isFlashing;
    private Vector3 moveDirection;
    private Vector3 movement;

    // Cached Stats
    private float cachedFleeDistSqr;

    // Player velocity tracking for prediction
    private Vector3 lastTargetPosition;
    private Vector3 estimatedTargetVelocity;

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

        if (projectileOrigin == null) projectileOrigin = transform;
    }

    protected override void CacheStats()
    {
        base.CacheStats();
        cachedFleeDistSqr = fleeDistance * fleeDistance;

        // Apply fire rate override if set
        if (attackCooldownOverride > 0f)
        {
            cachedAttackCooldown = attackCooldownOverride;
        }
    }

    protected override void UpdateBehavior(float deltaTime)
    {
        if (IsDissolving) return;

        // Track target velocity for prediction
        UpdateTargetVelocityEstimate(deltaTime);

        FaceTargetInstant();
        float sqrDistance = GetSqrDistanceToTarget();

        if (attackTimer > 0f) attackTimer -= deltaTime;
        if (isFlashing) UpdateFlash(deltaTime);

        // --- AI LOGIC ---

        // 1. Flee if too close
        if (sqrDistance < cachedFleeDistSqr)
        {
            Move(deltaTime, -1f);
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
            Move(deltaTime, 1f);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateTargetVelocityEstimate(float deltaTime)
    {
        if (deltaTime > 0.001f)
        {
            estimatedTargetVelocity = (cachedTargetPosition - lastTargetPosition) / deltaTime;
        }
        lastTargetPosition = cachedTargetPosition;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Move(float deltaTime, float dirMult)
    {
        GetNormalizedDirectionToTarget(out moveDirection);
        moveDirection *= dirMult;

        Vector3 horizontalMove = moveDirection * cachedMoveSpeed;

        if ((componentFlags & FLAG_CHAR_CONTROLLER) != 0)
        {
            movement.x = horizontalMove.x * deltaTime;
            movement.z = horizontalMove.z * deltaTime;
            movement.y = -9.81f * deltaTime; // Simple gravity
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

        FireMortarProjectile();
    }

    private void FireMortarProjectile()
    {
        if (mortarProjectilePrefab == null)
        {
            Debug.LogWarning($"[MortarEnemy] No mortar projectile prefab assigned on {gameObject.name}");
            return;
        }

        // Predict where the player will be
        Vector3 predictedTargetPosition = cachedTargetPosition + (estimatedTargetVelocity * leadTime);
        predictedTargetPosition.y = 0f; // Keep on ground level

        // Spawn projectile
        Vector3 spawnPos = projectileOrigin.position;
        GameObject projObj = Instantiate(mortarProjectilePrefab, spawnPos, Quaternion.identity);

        IceMortarProjectile mortarScript = projObj.GetComponent<IceMortarProjectile>();
        if (mortarScript != null)
        {
            mortarScript.Initialize(
                spawnPos,
                predictedTargetPosition,
                arcHeight,
                flightDuration,
                cachedDamage
            );
        }
        else
        {
            Debug.LogError($"[MortarEnemy] Projectile prefab missing IceMortarProjectile component!");
            Destroy(projObj);
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
            if (cachedCollider != null) cachedCollider.enabled = false;
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
        lastTargetPosition = Vector3.zero;
        estimatedTargetVelocity = Vector3.zero;

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
