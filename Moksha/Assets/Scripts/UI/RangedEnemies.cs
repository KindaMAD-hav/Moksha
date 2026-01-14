using System.Runtime.CompilerServices;
using UnityEngine;

/// <summary>
/// Ranged enemy implementation. 
/// Maintains distance and fires projectiles at the target.
/// </summary>
public class RangedEnemy : EnemyBase
{
    [Header("Ranged Settings")]
    [SerializeField] private EnemyProjectile projectilePrefab;
    [SerializeField] private Transform projectileOrigin; // Where the bullet spawns (e.g., wand tip)
    [SerializeField] private float projectileSpeed = 10f;
    [SerializeField] private float aimPrediction = 0f; // 0 = direct shot, 1 = predict movement

    [Header("Movement Behavior")]
    [SerializeField] private bool fleeIfTooClose = true;
    [SerializeField] private float fleeDistance = 3f;

    [Header("Optional Components")]
    [SerializeField] private Animator animator;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private EnemyDissolve dissolveEffect;

    [Header("Visuals")]
    [SerializeField] private Renderer[] flashRenderers;
    [SerializeField] private Color damageFlashColor = new Color(1f, 0.2f, 0.2f, 1f);

    // Cached Component Flags (Optimization)
    private CharacterController characterController;
    private Rigidbody rb;
    private byte componentFlags;
    private const byte FLAG_ANIMATOR = 1;
    private const byte FLAG_CHAR_CONTROLLER = 2;
    private const byte FLAG_RIGIDBODY = 4;
    private const byte FLAG_AUDIO = 8;
    private const byte FLAG_DISSOLVE = 16;

    // Animation Hashes
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int AttackHash = Animator.StringToHash("Attack");

    // State
    private float attackTimer;
    private MaterialPropertyBlock flashBlock;
    private Color[] originalColors;
    private float flashTimer;
    private bool isFlashing;
    private Vector3 moveDirection;
    private Vector3 movement;

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
                originalColors[i] = flashRenderers[i].sharedMaterial.HasProperty("_BaseColor")
                    ? flashRenderers[i].sharedMaterial.GetColor("_BaseColor")
                    : Color.white;
            }
        }

        // Use center of object if no origin assigned
        if (projectileOrigin == null) projectileOrigin = transform;
    }

    protected override void UpdateBehavior(float deltaTime)
    {
        FaceTargetInstant();
        float sqrDistance = GetSqrDistanceToTarget();
        float fleeDistSqr = fleeDistance * fleeDistance;

        // Update Cooldowns
        if (attackTimer > 0f) attackTimer -= deltaTime;
        if (isFlashing) UpdateFlash(deltaTime);

        // --- BEHAVIOR LOGIC ---

        // 1. Too Close? Flee (Optional)
        if (fleeIfTooClose && sqrDistance < fleeDistSqr)
        {
            Move(deltaTime, -1f); // Move backwards
        }
        // 2. In Attack Range? Stop and Shoot
        else if (sqrDistance <= cachedAttackRangeSqr)
        {
            SetAnimSpeed(0f);
            TryAttack();
        }
        // 3. Too Far? Chase
        else
        {
            Move(deltaTime, 1f); // Move forwards
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Move(float deltaTime, float directionMultiplier)
    {
        // Calculate direction
        GetNormalizedDirectionToTarget(out moveDirection);

        // Apply Multiplier (1 for chase, -1 for flee)
        moveDirection *= directionMultiplier;

        // Apply Movement
        if ((componentFlags & FLAG_CHAR_CONTROLLER) != 0)
        {
            movement.x = moveDirection.x * cachedMoveSpeed * deltaTime;
            movement.z = moveDirection.z * cachedMoveSpeed * deltaTime;
            movement.y = -9.81f * deltaTime; // Simple gravity
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
            // Fallback translation
            cachedTransform.position += moveDirection * (cachedMoveSpeed * deltaTime);
        }

        SetAnimSpeed(cachedMoveSpeed * (directionMultiplier > 0 ? 1 : -1));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void TryAttack()
    {
        if (attackTimer > 0f) return;

        attackTimer = cachedAttackCooldown;

        // Visuals
        if ((componentFlags & FLAG_ANIMATOR) != 0)
            animator.SetTrigger(AttackHash);

        if ((componentFlags & FLAG_AUDIO) != 0 && stats.attackSound != null)
            audioSource.PlayOneShot(stats.attackSound);

        // Logic (Spawn Projectile)
        ShootProjectile();
    }

    private void ShootProjectile()
    {
        if (projectilePrefab == null) return;

        // Calculate direction
        Vector3 fireDir = (cachedTargetPosition - projectileOrigin.position).normalized;
        fireDir.y = 0; // Keep flat plane for top-down games, remove if 3D FPS

        // Instantiate (Or use PoolManager here)
        var proj = Instantiate(projectilePrefab, projectileOrigin.position, Quaternion.LookRotation(fireDir));
        proj.Initialize(cachedDamage, projectileSpeed, fireDir);
    }

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
            animator.SetFloat(SpeedHash, Mathf.Abs(speed)); // Use Abs so fleeing plays walk anim
    }

    // --- DAMAGE FLASH LOGIC (Copied & Simplified from BasicEnemy) ---
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
            // Restore originals
            for (int i = 0; i < flashRenderers.Length; i++)
            {
                if (flashRenderers[i] == null) continue;
                flashRenderers[i].GetPropertyBlock(flashBlock);
                flashBlock.SetColor("_BaseColor", originalColors[i]);
                flashRenderers[i].SetPropertyBlock(flashBlock);
            }
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
}