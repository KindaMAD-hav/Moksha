using System.Runtime.CompilerServices;
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
    [SerializeField] private EnemyDissolve dissolveEffect;

    [Header("Damage Flash")]
    [SerializeField] private Renderer[] flashRenderers;
    [SerializeField] private Color damageFlashColor = new Color(1f, 0.2f, 0.2f, 1f);
    [SerializeField] private float flashDuration = 0.12f;


    // Animator parameter hashes (static for all instances)
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int DieHash = Animator.StringToHash("Die");
    // Shader color property IDs (local to enemy)
    private static readonly int ColorProperty = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorPropertyAlt = Shader.PropertyToID("_Color");


    // Cached components
    private CharacterController characterController;
    private Rigidbody rb;
    private byte componentFlags; // Bit flags for component presence
    private MaterialPropertyBlock flashBlock;
    private Color[] originalColors;
    private float flashTimer;
    private bool isFlashing;


    // Component flag bits
    private const byte FLAG_ANIMATOR = 1;
    private const byte FLAG_CHAR_CONTROLLER = 2;
    private const byte FLAG_RIGIDBODY = 4;
    private const byte FLAG_AUDIO = 8;
    private const byte FLAG_DISSOLVE = 16;

    // State
    private float attackTimer;
    private float noiseX, noiseZ;
    private float lastAnimSpeed;

    // Cached vectors (avoid allocations)
    private Vector3 moveDirection;
    private Vector3 movement;

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
        dissolveEffect = GetComponent<EnemyDissolve>();
        // Auto-find renderers if not assigned
        if (flashRenderers == null || flashRenderers.Length == 0)
        {
            flashRenderers = GetComponentsInChildren<Renderer>();
        }

        if (flashRenderers != null && flashRenderers.Length > 0)
        {
            flashBlock = new MaterialPropertyBlock();
            originalColors = new Color[flashRenderers.Length];

            for (int i = 0; i < flashRenderers.Length; i++)
            {
                if (flashRenderers[i] == null) continue;

                flashRenderers[i].GetPropertyBlock(flashBlock);

                if (flashBlock.HasColor(ColorProperty))
                    originalColors[i] = flashBlock.GetColor(ColorProperty);
                else
                    originalColors[i] = Color.white;
            }
        }

        // Set component flags (faster than null checks)
        componentFlags = 0;
        if (animator != null) componentFlags |= FLAG_ANIMATOR;
        if (characterController != null) componentFlags |= FLAG_CHAR_CONTROLLER;
        if (rb != null) componentFlags |= FLAG_RIGIDBODY;
        if (audioSource != null) componentFlags |= FLAG_AUDIO;
        if (dissolveEffect != null) componentFlags |= FLAG_DISSOLVE;

        GenerateNoiseOffset();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void GenerateNoiseOffset()
    {
        float x = Random.value * 2f - 1f;
        float z = Random.value * 2f - 1f;
        float invMag = movementNoise / Mathf.Sqrt(x * x + z * z + 0.0001f);
        noiseX = x * invMag;
        noiseZ = z * invMag;
    }

    protected override void UpdateBehavior(float deltaTime)
    {
        FaceTargetInstant(); // 🔥 always face player
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
        // Damage flash update
        if (isFlashing)
        {
            flashTimer -= deltaTime;
            if (flashTimer <= 0f)
            {
                EndFlash();
            }
        }

    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void FaceTargetInstant()
    {
        if (targetTransform == null) return;

        Vector3 dir = targetTransform.position - cachedTransform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.0001f) return;

        cachedTransform.rotation = Quaternion.LookRotation(dir);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ChaseTarget(float deltaTime)
    {
        GetNormalizedDirectionToTarget(out moveDirection);
        
        // Early exit if no direction
        if (moveDirection.x == 0f & moveDirection.z == 0f) return;

        // Add noise (small contribution) - inline multiplication
        const float noiseScale = 0.1f;
        float dirX = moveDirection.x + noiseX * noiseScale;
        float dirZ = moveDirection.z + noiseZ * noiseScale;
        
        // Re-normalize (fast)
        float sqrMag = dirX * dirX + dirZ * dirZ;
        if (sqrMag > 0.0001f)
        {
            float invMag = 1f / Mathf.Sqrt(sqrMag);
            dirX *= invMag;
            dirZ *= invMag;
        }
        
        moveDirection.x = dirX;
        moveDirection.z = dirZ;

        // Rotate towards target
        RotateTowards(deltaTime);

        // Calculate movement
        float moveAmount = cachedMoveSpeed * deltaTime;
        movement.x = dirX * moveAmount;
        movement.z = dirZ * moveAmount;

        // Apply movement based on available component
        if ((componentFlags & FLAG_CHAR_CONTROLLER) != 0)
        {
            movement.y = -2f * deltaTime; // Gravity
            characterController.Move(movement);
        }
        else if ((componentFlags & FLAG_RIGIDBODY) != 0)
        {
            Vector3 vel = rb.linearVelocity;
            vel.x = dirX * cachedMoveSpeed;
            vel.z = dirZ * cachedMoveSpeed;
            rb.linearVelocity = vel;
        }
        else
        {
            movement.y = 0f;
            cachedTransform.position += movement;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RotateTowards(float deltaTime)
    {
        if (targetTransform == null) return;

        Vector3 dir = targetTransform.position - cachedTransform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.0001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(dir);
        cachedTransform.rotation = Quaternion.RotateTowards(
            cachedTransform.rotation,
            targetRotation,
            cachedRotationSpeed * deltaTime
        );
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetAnimSpeed(float speed)
    {
        // Only update animator if value changed (reduces animator overhead)
        if ((componentFlags & FLAG_ANIMATOR) != 0 && speed != lastAnimSpeed)
        {
            animator.SetFloat(SpeedHash, speed);
            lastAnimSpeed = speed;
        }
    }

    protected override void Die()
    {
        if ((componentFlags & FLAG_AUDIO) != 0 && stats.deathSound != null)
            audioSource.PlayOneShot(stats.deathSound);

        // Start dissolve effect but keep moving
        if ((componentFlags & FLAG_DISSOLVE) != 0)
        {
            // Grant XP immediately
            if (ExperienceManager.Instance != null)
                ExperienceManager.Instance.AddXP(cachedXPReward);
            
            // Mark as dissolving - enemy keeps moving
            IsDissolving = true;
            
            // Start dissolve - enemy keeps moving until this completes
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
        // Only now fully disable the enemy
        IsDissolving = false;
        IsDead = true;
        DisableMovement();
        gameObject.SetActive(false);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void DisableMovement()
    {
        if ((componentFlags & FLAG_CHAR_CONTROLLER) != 0)
            characterController.enabled = false;
        if ((componentFlags & FLAG_RIGIDBODY) != 0)
            rb.isKinematic = true;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnableMovement()
    {
        if ((componentFlags & FLAG_CHAR_CONTROLLER) != 0)
            characterController.enabled = true;
        if ((componentFlags & FLAG_RIGIDBODY) != 0)
            rb.isKinematic = false;
    }

    public override void ResetEnemy()
    {
        base.ResetEnemy();
        
        attackTimer = 0f;
        lastAnimSpeed = -1f;
        checkedDamageable = false;
        targetDamageable = null;
        
        EnableMovement();
        
        if ((componentFlags & FLAG_DISSOLVE) != 0)
            dissolveEffect.ResetDissolve();

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

    public override void TakeDamage(float damage)
    {
        base.TakeDamage(damage);

        if (!IsDead)
            StartFlash();
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void StartFlash()
    {
        if (flashRenderers == null || flashRenderers.Length == 0) return;

        isFlashing = true;
        flashTimer = flashDuration;

        for (int i = 0; i < flashRenderers.Length; i++)
        {
            if (flashRenderers[i] == null) continue;

            flashRenderers[i].GetPropertyBlock(flashBlock);

            if (flashRenderers[i].material.HasProperty("_BaseColor"))
                flashBlock.SetColor("_BaseColor", damageFlashColor);
            if (flashRenderers[i].material.HasProperty("_Color"))
                flashBlock.SetColor("_Color", damageFlashColor);

            flashRenderers[i].SetPropertyBlock(flashBlock);
        }
    }

    private void EndFlash()
    {
        isFlashing = false;

        for (int i = 0; i < flashRenderers.Length; i++)
        {
            if (flashRenderers[i] == null) continue;

            flashRenderers[i].GetPropertyBlock(flashBlock);

            if (flashRenderers[i].material.HasProperty("_BaseColor"))
                flashBlock.SetColor("_BaseColor", originalColors[i]);
            if (flashRenderers[i].material.HasProperty("_Color"))
                flashBlock.SetColor("_Color", originalColors[i]);

            flashRenderers[i].SetPropertyBlock(flashBlock);
        }
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

    


    [ContextMenu("Kill This Enemy")]
    public void DebugKill()
    {
        if (IsDead) return;
        TakeDamage(cachedMaxHealth + 1f);
    }
#endif
}
