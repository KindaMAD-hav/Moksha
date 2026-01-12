using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages the water veil visual effect and enemy slow interactions.
/// Attach this to a child GameObject of the player with a ParticleSystem.
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class WaterVeilEffect : MonoBehaviour
{
    private ParticleSystem waterParticles;
    private SphereCollider veilCollider;

    private float slowDuration;
    private float slowMultiplier;
    private float damagePerSecond;

    private Dictionary<GameObject, SlowData> affectedEnemies = new Dictionary<GameObject, SlowData>();
    private float damageTickRate = 0.5f; // Apply damage every 0.5 seconds

    public bool IsActive { get; private set; }

    private class SlowData
    {
        public float originalSpeed;
        public float nextDamageTime;
    }

    private void Awake()
    {
        waterParticles = GetComponent<ParticleSystem>();

        // Create or get sphere collider for detection
        veilCollider = GetComponent<SphereCollider>();
        if (veilCollider == null)
        {
            veilCollider = gameObject.AddComponent<SphereCollider>();
        }

        veilCollider.isTrigger = true;

        // Start inactive
        waterParticles.Stop();
        veilCollider.enabled = false;
        IsActive = false;
    }

    public void Activate(float duration, float speedMultiplier, float radius, float dps)
    {
        slowDuration = duration;
        slowMultiplier = speedMultiplier;
        damagePerSecond = dps;

        // Set collider radius
        veilCollider.radius = radius;
        veilCollider.enabled = true;

        // Configure particle system size to match radius
        var shape = waterParticles.shape;
        shape.radius = radius;

        // Start particle effect
        waterParticles.Play();
        IsActive = true;
    }

    public void UpgradeEffect(float additionalDuration, float additionalRadius)
    {
        slowDuration += additionalDuration;

        float newRadius = veilCollider.radius + additionalRadius;
        veilCollider.radius = newRadius;

        var shape = waterParticles.shape;
        shape.radius = newRadius;
    }

    public void Deactivate()
    {
        waterParticles.Stop();
        veilCollider.enabled = false;
        IsActive = false;

        // Restore all affected enemies
        RestoreAllEnemies();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsActive) return;

        // Check if it's an enemy (adjust tag/layer as needed)
        if (other.CompareTag("Enemy"))
        {
            ApplySlowEffect(other.gameObject);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            RemoveSlowEffect(other.gameObject);
        }
    }

    private void Update()
    {
        if (!IsActive) return;

        // Apply damage over time to enemies in range
        if (damagePerSecond > 0)
        {
            float currentTime = Time.time;
            List<GameObject> enemiesToDamage = new List<GameObject>();

            foreach (var kvp in affectedEnemies)
            {
                if (kvp.Value.nextDamageTime <= currentTime)
                {
                    enemiesToDamage.Add(kvp.Key);
                    kvp.Value.nextDamageTime = currentTime + damageTickRate;
                }
            }

            foreach (var enemy in enemiesToDamage)
            {
                DealDamage(enemy);
            }
        }
    }

    private void ApplySlowEffect(GameObject enemy)
    {
        if (affectedEnemies.ContainsKey(enemy))
            return;

        // Try to get enemy movement component (adjust to your enemy system)
        var enemyMovement = enemy.GetComponent<EnemyMovement>();
        if (enemyMovement != null)
        {
            SlowData data = new SlowData
            {
                originalSpeed = enemyMovement.moveSpeed,
                nextDamageTime = Time.time + damageTickRate
            };

            affectedEnemies.Add(enemy, data);
            enemyMovement.moveSpeed *= slowMultiplier;

            // Optional: Start slow duration timer
            StartCoroutine(RemoveSlowAfterDuration(enemy));
        }
    }

    private void RemoveSlowEffect(GameObject enemy)
    {
        if (!affectedEnemies.ContainsKey(enemy))
            return;

        var enemyMovement = enemy.GetComponent<EnemyMovement>();
        if (enemyMovement != null)
        {
            enemyMovement.moveSpeed = affectedEnemies[enemy].originalSpeed;
        }

        affectedEnemies.Remove(enemy);
    }

    private void RestoreAllEnemies()
    {
        List<GameObject> enemies = new List<GameObject>(affectedEnemies.Keys);
        foreach (var enemy in enemies)
        {
            RemoveSlowEffect(enemy);
        }
        affectedEnemies.Clear();
    }

    private void DealDamage(GameObject enemy)
    {
        var health = enemy.GetComponent<EnemyHealth>();
        if (health != null)
        {
            float damageAmount = damagePerSecond * damageTickRate;
            health.TakeDamage(damageAmount);
        }
    }

    private System.Collections.IEnumerator RemoveSlowAfterDuration(GameObject enemy)
    {
        yield return new WaitForSeconds(slowDuration);

        // Only remove if enemy is no longer in trigger
        if (!veilCollider.bounds.Contains(enemy.transform.position))
        {
            RemoveSlowEffect(enemy);
        }
    }

    private void OnDestroy()
    {
        RestoreAllEnemies();
    }
}