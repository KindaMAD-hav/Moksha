using UnityEngine;

/// <summary>
/// Basic melee enemy - optimized for high enemy counts.
/// No NavMesh required - uses simple direct movement.
/// </summary>
public class BasicEnemy : EnemyBase
{
    [Header("Basic Enemy Settings")]
    [SerializeField] private float movementNoise = 0.5f;
    
    [Header("Optional Components")]
    [SerializeField] private Animator animator;
    [SerializeField] private AudioSource audioSource;

    // Animator parameter hashes (static for all instances)
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int DieHash = Animator.StringToHash("Die");

    // Cached components
    private CharacterController characterController;
    private Rigidbody rb;
    private bool hasAnimator;
    private bool hasCharacterController;
    private bool hasRigidbody;
    private bool hasAudioSource;

    // State
    private float attackTimer;
    private Vector3 noiseOffset;
    private float lastAnimSpeed;

    // Cached vectors (avoid allocations)
    private Vector3 moveDirection;
    private Vector3 movement;
    private Quaternion targetRotation;

    // Cached IDamageable on target (avoid GetComponent every attack)
    private IDamageable targetDamageable;
    private bool checkedDamageable;

    protected override void Awake()
    {
        base.Awake();
        
        // Cache all component references once
        animator = GetComponentInChildren<Animator>();
        audioSource = GetComponent<AudioSource>();
        characterController = GetComponent<CharacterController>();
        rb = GetComponent<Rigidbody>();

        // Cache booleans for fast null checks
        hasAnimator = animator != null;
        hasCharacterController = characterController != null;
        hasRigidbody = rb != null;
        hasAudioSource = audioSource != null;

        GenerateNoiseOffset();
    }

    private void GenerateNoiseOffset()
    {
        // Use faster random generation
        float x = Random.value * 2f - 1f;
        float z = Random.value * 2f - 1f;
        float invMag = movementNoise / Mathf.Sqrt(x * x + z * z + 0.0001f);
        noiseOffset.x = x * invMag;
        noiseOffset.y = 0f;
        noiseOffset.z = z * invMag;
    }

    protected override void UpdateBehavior(float deltaTime)
    {
        float sqrDistance = GetSqrDistanceToTarget();
        
        // Update attack cooldown
        if (attackTimer > 0f)
            attackTimer -= deltaTime;

        // Behavior based on squared distance (faster than sqrt)
        if (sqrDistance <= cachedAttackRangeSqr)
        {
            TryAttack();
            SetAnimSpeed(0f);
        }
        else if (sqrDistance > cachedStoppingDistanceSqr)
        {
            ChaseTarget(deltaTime);
            SetAnimSpeed(cachedMoveSpeed);
        }
        else
        {
            SetAnimSpeed(0f);
        }
    }

    private void ChaseTarget(float deltaTime)
    {
        GetNormalizedDirectionToTarget(out moveDirection);
        if (moveDirection.x == 0f && moveDirection.z == 0f) return;

        // Add noise (small contribution)
        float noiseScale = 0.1f;
        moveDirection.x += noiseOffset.x * noiseScale;
        moveDirection.z += noiseOffset.z * noiseScale;
        
        // Re-normalize (fast approximation)
        float sqrMag = moveDirection.x * moveDirection.x + moveDirection.z * moveDirection.z;
        if (sqrMag > 0.0001f)
        {
            float invMag = 1f / Mathf.Sqrt(sqrMag);
            moveDirection.x *= invMag;
            moveDirection.z *= invMag;
        }

        // Rotate towards target
        RotateTowards(deltaTime);

        // Calculate movement
        float moveAmount = cachedMoveSpeed * deltaTime;
        movement.x = moveDirection.x * moveAmount;
        movement.z = moveDirection.z * moveAmount;

        if (hasCharacterController)
        {
            movement.y = -2f * deltaTime; // Gravity
            characterController.Move(movement);
        }
        else if (hasRigidbody)
        {
            Vector3 vel = rb.linearVelocity;
            vel.x = moveDirection.x * cachedMoveSpeed;
            vel.z = moveDirection.z * cachedMoveSpeed;
            rb.linearVelocity = vel;
        }
        else
        {
            movement.y = 0f;
            cachedTransform.position += movement;
        }
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    private void RotateTowards(float deltaTime)
    {
        targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
        cachedTransform.rotation = Quaternion.RotateTowards(
            cachedTransform.rotation,
            targetRotation,
            cachedRotationSpeed * deltaTime
        );
    }

    private void TryAttack()
    {
        if (attackTimer > 0f) return;

        attackTimer = cachedAttackCooldown;

        if (hasAnimator)
            animator.SetTrigger(AttackHash);

        if (hasAudioSource && stats.attackSound != null)
            audioSource.PlayOneShot(stats.attackSound);

        DealDamage();
    }

    private void DealDamage()
    {
        if (targetTransform == null) return;
        if (GetSqrDistanceToTarget() > cachedAttackRangeSqr) return;

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

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    private void SetAnimSpeed(float speed)
    {
        // Only update animator if value changed (reduces animator overhead)
        if (hasAnimator && speed != lastAnimSpeed)
        {
            animator.SetFloat(SpeedHash, speed);
            lastAnimSpeed = speed;
        }
    }

    protected override void Die()
    {
        if (hasAnimator)
            animator.SetTrigger(DieHash);

        if (hasAudioSource && stats.deathSound != null)
            audioSource.PlayOneShot(stats.deathSound);

        if (hasCharacterController)
            characterController.enabled = false;
        if (hasRigidbody)
            rb.isKinematic = true;

        base.Die();
    }

    public override void ResetEnemy()
    {
        base.ResetEnemy();
        
        attackTimer = 0f;
        lastAnimSpeed = -1f; // Force animator update on next tick
        checkedDamageable = false;
        targetDamageable = null;
        
        if (hasCharacterController)
            characterController.enabled = true;
        if (hasRigidbody)
            rb.isKinematic = false;

        GenerateNoiseOffset();
    }

    public override void Initialize(EnemyStats enemyStats, Transform target)
    {
        base.Initialize(enemyStats, target);
        checkedDamageable = false;
        targetDamageable = null;
    }

    // Called from animation event (optional)
    public void OnAttackHit()
    {
        DealDamage();
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (stats == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, stats.attackRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, stats.stoppingDistance);
    }
#endif
}

/// <summary>
/// Interface for anything that can take damage.
/// </summary>
public interface IDamageable
{
    void TakeDamage(float damage);
}
