using System.Runtime.CompilerServices;
using UnityEngine;

/// <summary>
/// Ranged enemy that shoots projectiles at the player.
/// Optimized for high enemy counts with object pooling.
/// </summary>
public class RangedEnemy : EnemyBase
{
    [Header("Ranged Enemy Settings")]
    [SerializeField] private float projectileSpeed = 10f;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint; // Where projectile spawns
    [SerializeField] private float projectileLifetime = 5f;

    [Header("Behavior")]
    [SerializeField] private float preferredDistance = 8f; // Keep distance from player
    [SerializeField] private float retreatDistance = 5f; // Start backing away if closer
    [SerializeField] private float attackWindupTime = 0.5f; // Animation time before shot

    [Header("Optional Components")]
    [SerializeField] private Animator animator;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private EnemyDissolve dissolveEffect;

    [Header("Damage Flash")]
    [SerializeField] private bool enableDamageFlash = true;
    [SerializeField] private Renderer[] flashRenderers;
    [SerializeField] private Color damageFlashColor = new Color(1f, 0.2f, 0.2f, 1f);
    [SerializeField] private float flashDuration = 0.12f;

    [Header("Audio")]
    [SerializeField] private AudioClip shootSFX;
    [SerializeField] private float shootPitchMin = 0.9f;
    [SerializeField] private float shootPitchMax = 1.1f;

    // Animator hashes
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int DieHash = Animator.StringToHash("Die");
    private static readonly int ColorProperty = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorPropertyAlt = Shader.PropertyToID("_Color");

    // Cached components
    private CharacterController characterController;
    private Rigidbody rb;
    private byte componentFlags;
    private MaterialPropertyBlock flashBlock;
    private Color[] originalColors;
    private float flashTimer;
    private bool isFlashing;

    // Component flags
    private const byte FLAG_ANIMATOR = 1;
    private const byte FLAG_CHAR_CONTROLLER = 2;
    private const byte FLAG_RIGIDBODY = 4;
    private const byte FLAG_AUDIO = 8;
    private const byte FLAG_DISSOLVE = 16;

    // State
    private float attackTimer;
    private float windupTimer;
    private bool isPreparingShot;
    private float lastAnimSpeed;

    // Cached distances
    private float preferredDistanceSqr;
    private float retreatDistanceSqr;

    // Cached vectors
    private Vector3 moveDirection;
    private Vector3 movement;

    protected override void Awake()
    {
        base.Awake();

        // Auto-create fire point if not assigned
        if (firePoint == null)
        {
            GameObject fp = new GameObject("FirePoint");
            fp.transform.SetParent(transform);
            fp.transform.localPosition = new Vector3(0, 1f, 0.5f); // Front of enemy
            firePoint = fp.transform;
        }

        // Cache components
        animator = GetComponentInChildren<Animator>();
        audioSource = GetComponent<AudioSource>();
        characterController = GetComponent<CharacterController>();
        rb = GetComponent<Rigidbody>();
        dissolveEffect = GetComponent<EnemyDissolve>();

        // Setup damage flash
        if (!enableDamageFlash)
        {
            flashRenderers = null;
        }
        else if (flashRenderers == null || flashRenderers.Length == 0)
        {
            flashRenderers = GetComponentsInChildren<Renderer>();
        }

        if (enableDamageFlash && flashRenderers != null && flashRenderers.Length > 0)
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

        // Set component flags
        componentFlags = 0;
        if (animator != null) componentFlags |= FLAG_ANIMATOR;
        if (characterController != null) componentFlags |= FLAG_CHAR_CONTROLLER;
        if (rb != null) componentFlags |= FLAG_RIGIDBODY;
        if (audioSource != null) componentFlags |= FLAG_AUDIO;
        if (dissolveEffect != null) componentFlags |= FLAG_DISSOLVE;

        // Cache squared distances
        preferredDistanceSqr = preferredDistance * preferredDistance;
        retreatDistanceSqr = retreatDistance * retreatDistance;
    }

    protected override void UpdateBehavior(float deltaTime)
    {
        FaceTarget(deltaTime);
        float sqrDistance = GetSqrDistanceToTarget();

        // Update timers
        if (attackTimer > 0f)
            attackTimer -= deltaTime;

        if (isPreparingShot)
        {
            windupTimer -= deltaTime;
            if (windupTimer <= 0f)
            {
                ShootProjectile();
                isPreparingShot = false;
            }
        }

        // Behavior based on distance
        if (sqrDistance <= cachedAttackRangeSqr && attackTimer <= 0f && !isPreparingShot)
        {
            // In attack range - shoot
            PrepareShot();
            SetAnimSpeed(0f);
        }
        else if (sqrDistance < retreatDistanceSqr)
        {
            // Too close - retreat
            RetreatFromTarget(deltaTime);
            SetAnimSpeed(cachedMoveSpeed);
        }
        else if (sqrDistance > preferredDistanceSqr)
        {
            // Too far - approach
            ApproachTarget(deltaTime);
            SetAnimSpeed(cachedMoveSpeed);
        }
        else
        {
            // At good distance - strafe/idle
            StrafeAroundTarget(deltaTime);
            SetAnimSpeed(cachedMoveSpeed * 0.5f);
        }

        // Damage flash update
        if (enableDamageFlash && isFlashing)
        {
            flashTimer -= deltaTime;
            if (flashTimer <= 0f)
            {
                EndFlash();
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void FaceTarget(float deltaTime)
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
    private void ApproachTarget(float deltaTime)
    {
        GetNormalizedDirectionToTarget(out moveDirection);
        MoveInDirection(moveDirection, deltaTime);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RetreatFromTarget(float deltaTime)
    {
        GetNormalizedDirectionToTarget(out moveDirection);
        // Move in opposite direction
        moveDirection.x = -moveDirection.x;
        moveDirection.z = -moveDirection.z;
        MoveInDirection(moveDirection, deltaTime);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void StrafeAroundTarget(float deltaTime)
    {
        GetNormalizedDirectionToTarget(out moveDirection);
        // Strafe perpendicular to target direction
        float temp = moveDirection.x;
        moveDirection.x = -moveDirection.z;
        moveDirection.z = temp;
        MoveInDirection(moveDirection, deltaTime);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void MoveInDirection(Vector3 direction, float deltaTime)
    {
        if (direction.x == 0f & direction.z == 0f) return;

        float moveAmount = cachedMoveSpeed * deltaTime;
        movement.x = direction.x * moveAmount;
        movement.z = direction.z * moveAmount;

        // Apply movement
        if ((componentFlags & FLAG_CHAR_CONTROLLER) != 0)
        {
            movement.y = -2f * deltaTime; // Gravity
            characterController.Move(movement);
        }
        else if ((componentFlags & FLAG_RIGIDBODY) != 0)
        {
            Vector3 vel = rb.linearVelocity;
            vel.x = direction.x * cachedMoveSpeed;
            vel.z = direction.z * cachedMoveSpeed;
            rb.linearVelocity = vel;
        }
        else
        {
            movement.y = 0f;
            cachedTransform.position += movement;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void PrepareShot()
    {
        isPreparingShot = true;
        windupTimer = attackWindupTime;
        attackTimer = cachedAttackCooldown;

        if ((componentFlags & FLAG_ANIMATOR) != 0)
            animator.SetTrigger(AttackHash);
    }

    private void ShootProjectile()
    {
        if (targetTransform == null || projectilePrefab == null) return;

        // Calculate direction to target
        Vector3 direction = (targetTransform.position - firePoint.position).normalized;

        // Try to get from pool first
        EnemyProjectile projectile = null;

        if (EnemyProjectilePool.Instance != null)
        {
            projectile = EnemyProjectilePool.Instance.GetProjectile();
        }
        else
        {
            // Fallback: Instantiate if no pool
            GameObject projObj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
            projectile = projObj.GetComponent<EnemyProjectile>();
        }

        if (projectile != null)
        {
            projectile.Initialize(firePoint.position, direction, projectileSpeed, cachedDamage, projectileLifetime);
        }

        // Play sound
        if (shootSFX != null)
        {
            if (SFXManager.Instance != null)
            {
                float pitch = Random.Range(shootPitchMin, shootPitchMax);
                SFXManager.Instance.PlayOneShot(shootSFX, pitch);
            }
            else if ((componentFlags & FLAG_AUDIO) != 0)
            {
                audioSource.pitch = Random.Range(shootPitchMin, shootPitchMax);
                audioSource.PlayOneShot(shootSFX);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetAnimSpeed(float speed)
    {
        if ((componentFlags & FLAG_ANIMATOR) != 0 && speed != lastAnimSpeed)
        {
            animator.SetFloat(SpeedHash, speed);
            lastAnimSpeed = speed;
        }
    }

    protected override void Die()
    {
        if ((componentFlags & FLAG_DISSOLVE) != 0)
        {
            GrantXPOnce();
            IsDissolving = true;

            Collider col = GetComponent<Collider>();
            if (col != null)
                col.enabled = false;

            if ((componentFlags & FLAG_RIGIDBODY) != 0)
            {
                rb.linearVelocity = Vector3.zero;
                rb.useGravity = false;
                rb.isKinematic = true;
            }

            if ((componentFlags & FLAG_CHAR_CONTROLLER) != 0)
            {
                characterController.enabled = false;
            }

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
        windupTimer = 0f;
        isPreparingShot = false;
        lastAnimSpeed = -1f;

        if ((componentFlags & FLAG_RIGIDBODY) != 0)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }

        if ((componentFlags & FLAG_CHAR_CONTROLLER) != 0)
        {
            characterController.enabled = true;
        }

        EnableMovement();

        if ((componentFlags & FLAG_DISSOLVE) != 0)
            dissolveEffect.ResetDissolve();
    }

    public override void TakeDamage(float damage)
    {
        base.TakeDamage(damage);

        if (IsDead) return;

        if (enableDamageFlash)
            StartFlash();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void StartFlash()
    {
        if (!enableDamageFlash || flashRenderers == null || flashRenderers.Length == 0) return;

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
        if (!enableDamageFlash) return;
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

    // Animation event callback (optional)
    public void OnShootAnimationEvent()
    {
        if (isPreparingShot)
        {
            ShootProjectile();
            isPreparingShot = false;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, Mathf.Sqrt(cachedAttackRangeSqr));
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, preferredDistance);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, retreatDistance);
    }
#endif
}