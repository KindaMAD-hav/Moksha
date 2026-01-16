using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Individual orbiting fireball that handles collision detection and damage dealing.
/// Attach to a fireball prefab with a trigger collider.
/// </summary>
[RequireComponent(typeof(Collider))]
public class OrbitingFireball : MonoBehaviour
{
    private float damage;
    private float hitCooldown;
    private int orbitIndex;
    private float angleOffset;

    // Track hit cooldowns per enemy to prevent rapid damage
    private Dictionary<int, float> enemyHitTimers = new Dictionary<int, float>(32);

    // Cached list for cleanup
    private List<int> expiredTimers = new List<int>(16);

    private void Awake()
    {
        // Ensure collider is set as trigger
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    public void Initialize(float damageAmount, float cooldown)
    {
        damage = damageAmount;
        hitCooldown = cooldown;
        enemyHitTimers.Clear();
    }

    public void SetDamage(float newDamage)
    {
        damage = newDamage;
    }

    public void SetOrbitIndex(int index, float angleStep)
    {
        orbitIndex = index;
        angleOffset = index * angleStep;
    }

    private void Update()
    {
        // Clean up expired hit timers periodically
        CleanupExpiredTimers();
    }

    private void CleanupExpiredTimers()
    {
        if (enemyHitTimers.Count == 0) return;

        float currentTime = Time.time;
        expiredTimers.Clear();

        foreach (var kvp in enemyHitTimers)
        {
            if (currentTime >= kvp.Value)
            {
                expiredTimers.Add(kvp.Key);
            }
        }

        for (int i = 0; i < expiredTimers.Count; i++)
        {
            enemyHitTimers.Remove(expiredTimers[i]);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        TryDamageEnemy(other);
    }

    private void OnTriggerStay(Collider other)
    {
        // Also check on stay for enemies that stay in contact
        TryDamageEnemy(other);
    }

    private void TryDamageEnemy(Collider other)
    {
        // Try to get EnemyBase component
        EnemyBase enemy = other.GetComponent<EnemyBase>();
        if (enemy == null)
        {
            // Check parent in case collider is on a child object
            enemy = other.GetComponentInParent<EnemyBase>();
        }

        if (enemy == null || enemy.IsDead) return;

        int enemyId = enemy.GetInstanceID();
        float currentTime = Time.time;

        // Check if this enemy is on cooldown
        if (enemyHitTimers.TryGetValue(enemyId, out float nextHitTime))
        {
            if (currentTime < nextHitTime)
            {
                return; // Still on cooldown
            }
        }

        // Deal damage
        enemy.TakeDamage(damage);

        // Set cooldown for this enemy
        enemyHitTimers[enemyId] = currentTime + hitCooldown;

        // Optional: Spawn hit VFX here if you have one
        // FireballHitVFX.Spawn(other.ClosestPoint(transform.position));
    }

    private void OnDestroy()
    {
        enemyHitTimers.Clear();
    }
}