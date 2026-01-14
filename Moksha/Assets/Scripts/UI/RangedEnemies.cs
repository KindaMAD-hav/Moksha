using System.Runtime.CompilerServices;
using UnityEngine;

public class RangedEnemy : EnemyBase
{
    [Header("Ranged Settings")]
    [SerializeField] private Transform projectileOrigin;
    [SerializeField] private float projectileSpeed = 12f;

    [Header("Movement")]
    [SerializeField] private float fleeDistance = 4f;

    [Header("Visuals/Audio")]
    [SerializeField] private Animator animator;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private Renderer[] flashRenderers;
    [SerializeField] private Color damageFlashColor = new Color(1f, 0.2f, 0.2f, 1f);

    // Dissolve
    [SerializeField] private EnemyDissolve dissolveEffect;

    // --- Optimization Caches ---
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private CharacterController characterController;
    private Rigidbody rb;
    private byte componentFlags;
    private const byte FLAG_ANIMATOR = 1;
    private const byte FLAG_CHAR_CONTROLLER = 2;
    private const byte FLAG_RIGIDBODY = 4;
    private const byte FLAG_AUDIO = 8;
    private const byte FLAG_DISSOLVE = 16;

    private float attackTimer;
    private MaterialPropertyBlock flashBlock;
    private Color[] originalColors;
    private float flashTimer;
    private bool isFlashing;
    private Vector3 moveDirection;
    private Vector3 movement;

    // Pre-calculate squared distances to avoid math in Update
    private float cachedFleeDistSqr;

    protected override void Awake()
    {
        base.Awake();

        // Cache Components
        animator = GetComponentInChildren<Animator>();
        audioSource = GetComponent<AudioSource>();
        characterController = GetComponent<CharacterController>();
        rb = GetComponent<Rigidbody>();
        dissolveEffect = GetComponent<EnemyDissolve>();

        // Flags
        componentFlags = 0;
        if (animator != null) componentFlags |= FLAG_ANIMATOR;
        if (characterController != null) componentFlags |= FLAG_CHAR_CONTROLLER;
        if (rb != null) componentFlags |= FLAG_RIGIDBODY;
        if (audioSource != null) componentFlags |= FLAG_AUDIO;
        if (dissolveEffect != null) componentFlags |= FLAG_DISSOLVE;

        // Flash Setup
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
    }

    protected override void UpdateBehavior(float deltaTime)
    {
        if (IsDissolving) return;

        FaceTargetInstant();
        float sqrDistance = GetSqrDistanceToTarget();

        if (attackTimer > 0f) attackTimer -= deltaTime;
        if (isFlashing) UpdateFlash(deltaTime);

        // --- Logic ---

        // 1. Flee
        if (sqrDistance < cachedFleeDistSqr)
        {
            Move(deltaTime, -1f);
        }
        // 2. Attack
        else if (sqrDistance <= cachedAttackRangeSqr)
        {
            SetAnimSpeed(0f);
            TryAttack();
        }
        // 3. Chase
        else
        {
            Move(deltaTime, 1f);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Move(float deltaTime, float dirMult)
    {
        GetNormalizedDirectionToTarget(out moveDirection);
        moveDirection *= dirMult;

        // Manual gravity because we don't use RB gravity for cleaner control
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
        // No Instantiate! Use the Manager.
        if (ProjectileManager.Instance == null) return;

        Vector3 fireDir = (cachedTargetPosition - projectileOrigin.position).normalized;
        fireDir.y = 0;

        ProjectileManager.Instance.FireProjectile(
            projectileOrigin.position,
            fireDir,
            projectileSpeed,
            cachedDamage,
            stats.attackRange,
            stats.hitEffect, // Passing effects from SO
            stats.hitSound
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

    // --- Death / Dissolve ---
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
        if ((componentFlags & FLAG_CHAR_CONTROLLER) != 0) characterController.enabled = false;
        if ((componentFlags & FLAG_RIGIDBODY) != 0) { rb.linearVelocity = Vector3.zero; rb.isKinematic = true; }
    }

    public override void ResetEnemy()
    {
        base.ResetEnemy();
        attackTimer = 0f;
        isFlashing = false;
        if ((componentFlags & FLAG_CHAR_CONTROLLER) != 0) characterController.enabled = true;
        if ((componentFlags & FLAG_RIGIDBODY) != 0) { rb.isKinematic = false; rb.useGravity = true; }
        if ((componentFlags & FLAG_DISSOLVE) != 0) dissolveEffect.ResetDissolve();
    }

    // --- Flash Logic (Same as before) ---
    public override void TakeDamage(float damage) { base.TakeDamage(damage); if (!IsDead) StartFlash(); }
    private void StartFlash() { isFlashing = true; flashTimer = 0.15f; ApplyFlashColor(damageFlashColor); }
    private void UpdateFlash(float dt) { flashTimer -= dt; if (flashTimer <= 0f) { isFlashing = false; RestoreFlashColor(); } }
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