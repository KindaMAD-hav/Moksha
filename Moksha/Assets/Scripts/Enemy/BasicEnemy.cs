using UnityEngine;

/// <summary>
/// Basic melee enemy that chases and attacks the player.
/// No NavMesh required - uses simple direct movement.
/// </summary>
public class BasicEnemy : EnemyBase
{
    [Header("Basic Enemy Settings")]
    [SerializeField] private float attackTimer;
    [SerializeField] private bool isAttacking;

    [Header("Movement Variation")]
    [Tooltip("Random offset to prevent enemies stacking perfectly")]
    [SerializeField] private float movementNoise = 0.5f;
    
    [Header("Optional Components")]
    [SerializeField] private Animator animator;
    [SerializeField] private AudioSource audioSource;

    // Animator parameter hashes (for performance)
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int DieHash = Animator.StringToHash("Die");

    private Vector3 noiseOffset;
    private CharacterController characterController;
    private Rigidbody rb;

    protected override void Awake()
    {
        base.Awake();
        
        // Get optional components
        animator = GetComponentInChildren<Animator>();
        audioSource = GetComponent<AudioSource>();
        characterController = GetComponent<CharacterController>();
        rb = GetComponent<Rigidbody>();

        // Random noise offset for movement variation
        noiseOffset = new Vector3(
            Random.Range(-1f, 1f),
            0,
            Random.Range(-1f, 1f)
        ).normalized * movementNoise;
    }

    protected override void UpdateBehavior()
    {
        float distanceToTarget = GetDistanceToTarget();
        
        // Update attack cooldown
        if (attackTimer > 0)
            attackTimer -= Time.deltaTime;

        // Behavior based on distance
        if (distanceToTarget <= stats.attackRange)
        {
            // In attack range - stop and attack
            TryAttack();
            UpdateAnimation(0f);
        }
        else if (distanceToTarget > stats.stoppingDistance)
        {
            // Chase player
            ChaseTarget();
            UpdateAnimation(stats.moveSpeed);
        }
        else
        {
            UpdateAnimation(0f);
        }
    }

    private void ChaseTarget()
    {
        Vector3 direction = GetDirectionToTarget();
        if (direction == Vector3.zero) return;

        // Add slight noise to prevent perfect stacking
        Vector3 moveDir = (direction + noiseOffset * 0.1f).normalized;

        // Rotate towards target
        RotateTowards(direction);

        // Move towards target
        Vector3 movement = moveDir * stats.moveSpeed * Time.deltaTime;

        if (characterController != null)
        {
            // Add gravity if using CharacterController
            movement.y = -2f * Time.deltaTime;
            characterController.Move(movement);
        }
        else if (rb != null)
        {
            // Use Rigidbody velocity
            Vector3 vel = moveDir * stats.moveSpeed;
            vel.y = rb.linearVelocity.y;
            rb.linearVelocity = vel;
        }
        else
        {
            // Simple transform movement
            transform.position += movement;
        }
    }

    private void RotateTowards(Vector3 direction)
    {
        if (direction == Vector3.zero) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            stats.rotationSpeed * Time.deltaTime
        );
    }

    private void TryAttack()
    {
        if (attackTimer > 0 || isAttacking) return;

        isAttacking = true;
        attackTimer = stats.attackCooldown;

        // Trigger attack animation
        if (animator != null)
            animator.SetTrigger(AttackHash);

        // Play attack sound
        if (audioSource != null && stats.attackSound != null)
            audioSource.PlayOneShot(stats.attackSound);

        // Deal damage (can be called from animation event instead)
        DealDamage();

        isAttacking = false;
    }

    private void DealDamage()
    {
        if (targetTransform == null) return;

        // Check if still in range
        if (GetDistanceToTarget() > stats.attackRange) return;

        // Try to damage player
        // You'll need to implement IDamageable or similar on your player
        var damageable = targetTransform.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(stats.damage);
        }
        else
        {
            // Fallback: Send message (less performant but works without interface)
            targetTransform.SendMessage("TakeDamage", stats.damage, SendMessageOptions.DontRequireReceiver);
        }
    }

    private void UpdateAnimation(float speed)
    {
        if (animator != null)
            animator.SetFloat(SpeedHash, speed);
    }

    protected override void Die()
    {
        // Trigger death animation
        if (animator != null)
            animator.SetTrigger(DieHash);

        // Play death sound
        if (audioSource != null && stats.deathSound != null)
            audioSource.PlayOneShot(stats.deathSound);

        // Disable movement
        if (characterController != null)
            characterController.enabled = false;
        if (rb != null)
            rb.isKinematic = true;

        base.Die();
    }

    public override void ResetEnemy()
    {
        base.ResetEnemy();
        
        attackTimer = 0f;
        isAttacking = false;
        
        if (characterController != null)
            characterController.enabled = true;
        if (rb != null)
            rb.isKinematic = false;

        // Generate new noise offset
        noiseOffset = new Vector3(
            Random.Range(-1f, 1f),
            0,
            Random.Range(-1f, 1f)
        ).normalized * movementNoise;
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

        // Attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, stats.attackRange);

        // Stopping distance
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, stats.stoppingDistance);
    }
#endif
}

/// <summary>
/// Interface for anything that can take damage.
/// Implement this on your player, destructibles, etc.
/// </summary>
public interface IDamageable
{
    void TakeDamage(float damage);
}
