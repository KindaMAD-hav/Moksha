using UnityEngine;
using System.Runtime.CompilerServices;

/// <summary>
/// Enemy projectile with object pooling support.
/// OPTIMIZED: Uses EnemyProjectilePool instead of Destroy(), cached transform & layer lookups.
/// </summary>
public class EnemyProjectile : MonoBehaviour
{
    private float damage;
    private float speed;
    private float lifeTime = 5f;
    private float aliveTime;
    private Vector3 direction;
    private bool isInitialized;
    
    // Cached references
    private Transform cachedTransform;
    private static int groundLayer = -1;
    private static int obstacleLayer = -1;

    private void Awake()
    {
        cachedTransform = transform;
        
        // Cache layer lookups once (static, shared across all projectiles)
        if (groundLayer < 0)
        {
            groundLayer = LayerMask.NameToLayer("Ground");
            obstacleLayer = LayerMask.NameToLayer("Obstacle");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Initialize(float damageAmount, float projSpeed, Vector3 moveDir)
    {
        damage = damageAmount;
        speed = projSpeed;
        direction = moveDir.normalized;
        aliveTime = 0f;
        isInitialized = true;
    }

    private void Update()
    {
        if (!isInitialized) return;

        // Move forward relative to world space based on direction
        cachedTransform.position += direction * (speed * Time.deltaTime);

        // Check lifetime and return to pool
        aliveTime += Time.deltaTime;
        if (aliveTime >= lifeTime)
        {
            ReturnToPool();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Don't hit the enemy that shot this
        if (other.CompareTag("Enemy")) return;

        int layer = other.gameObject.layer;

        // Check for player/damageable
        IDamageable target = other.GetComponent<IDamageable>();
        if (target != null)
        {
            target.TakeDamage(damage);
            ReturnToPool();
        }
        else if (layer == groundLayer || layer == obstacleLayer)
        {
            // Return to pool on wall hit
            ReturnToPool();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ReturnToPool()
    {
        isInitialized = false;
        
        if (EnemyProjectilePool.Instance != null)
        {
            EnemyProjectilePool.Instance.ReturnProjectile(this);
        }
        else
        {
            // Fallback if pool doesn't exist
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Called when retrieved from pool to reset state
    /// </summary>
    public void ResetProjectile()
    {
        aliveTime = 0f;
        isInitialized = false;
    }
}
