using UnityEngine;

/// <summary>
/// Projectile fired by ranged enemies.
/// Designed to work with object pooling for performance.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class EnemyProjectile : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private TrailRenderer trail;
    [SerializeField] private ParticleSystem hitEffect;

    [Header("Audio")]
    [SerializeField] private AudioClip hitSFX;

    private Rigidbody rb;
    private float damage;
    private float lifetime;
    private float spawnTime;
    private bool isActive;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    public void Initialize(Vector3 position, Vector3 direction, float speed, float projectileDamage, float projectileLifetime)
    {
        transform.position = position;
        transform.rotation = Quaternion.LookRotation(direction);

        damage = projectileDamage;
        lifetime = projectileLifetime;
        spawnTime = Time.time;
        isActive = true;

        // Set velocity
        rb.linearVelocity = direction * speed;

        // Enable trail if exists
        if (trail != null)
        {
            trail.Clear();
            trail.enabled = true;
        }

        gameObject.SetActive(true);
    }

    private void Update()
    {
        if (!isActive) return;

        // Check lifetime
        if (Time.time - spawnTime >= lifetime)
        {
            ReturnToPool();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isActive) return;

        // Check if hit player
        if (other.CompareTag("Player"))
        {
            IDamageable damageable = other.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(damage);
            }

            OnHit(other.transform.position);
        }
        // Hit environment/obstacles
        else if (!other.CompareTag("Enemy"))
        {
            OnHit(other.ClosestPoint(transform.position));
        }
    }

    private void OnHit(Vector3 hitPosition)
    {
        // Spawn hit effect
        if (hitEffect != null)
        {
            ParticleSystem effect = Instantiate(hitEffect, hitPosition, Quaternion.identity);
            Destroy(effect.gameObject, effect.main.duration);
        }

        // Play hit sound
        if (hitSFX != null && SFXManager.Instance != null)
        {
            SFXManager.Instance.PlayOneShot(hitSFX);
        }

        ReturnToPool();
    }

    private void ReturnToPool()
    {
        isActive = false;
        rb.linearVelocity = Vector3.zero;

        if (trail != null)
        {
            trail.enabled = false;
        }

        if (EnemyProjectilePool.Instance != null)
        {
            EnemyProjectilePool.Instance.ReturnProjectile(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDisable()
    {
        isActive = false;
    }
}