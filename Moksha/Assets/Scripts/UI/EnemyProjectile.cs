using UnityEngine;

/// <summary>
/// Simple projectile logic. 
/// NOTE: For a roguelike with many enemies, integrate this with an ObjectPool system.
/// </summary>
public class EnemyProjectile : MonoBehaviour
{
    private float damage;
    private float speed;
    private float lifeTime = 5f;
    private Vector3 direction;
    private bool isInitialized = false;

    public void Initialize(float damageAmount, float projSpeed, Vector3 moveDir)
    {
        damage = damageAmount;
        speed = projSpeed;
        direction = moveDir.normalized;
        isInitialized = true;

        // Auto-destroy if it hits nothing
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        if (!isInitialized) return;

        // Move forward relative to world space based on direction
        transform.position += direction * (speed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Don't hit the enemy that shot this
        if (other.CompareTag("Enemy")) return;

        // Check for player/damageable
        IDamageable target = other.GetComponent<IDamageable>();
        if (target != null)
        {
            target.TakeDamage(damage);
            Destroy(gameObject); // Return to pool here if using pooling
        }
        else if (other.gameObject.layer == LayerMask.NameToLayer("Ground") || other.gameObject.layer == LayerMask.NameToLayer("Obstacle"))
        {
            // Destroy on wall hit
            Destroy(gameObject);
        }
    }
}