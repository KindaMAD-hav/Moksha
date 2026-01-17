using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages the water veil visual effect and enemy slow interactions.
/// Attach this to a child GameObject of the player with a ParticleSystem.
/// OPTIMIZED: Removed reflection, uses direct property access on EnemyBase.MoveSpeed
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class WaterVeilEffect : MonoBehaviour
{
    private ParticleSystem waterParticles;
    private SphereCollider veilCollider;

    private float slowDuration;
    private float slowMultiplier;
    private float damagePerSecond;

    private Dictionary<BasicEnemy, SlowData> affectedEnemies = new Dictionary<BasicEnemy, SlowData>();
    private float damageTickRate = 0.5f;

    // Reusable list to avoid GC allocations in Update
    private readonly List<BasicEnemy> enemiesToDamageCache = new List<BasicEnemy>(32);

    public bool IsActive { get; private set; }

    private class SlowData
    {
        public float originalSpeed;
        public float nextDamageTime;
        public EnemySlowModifier modifier;
    }

    private void Awake()
    {
        waterParticles = GetComponent<ParticleSystem>();

        veilCollider = GetComponent<SphereCollider>();
        if (veilCollider == null)
        {
            veilCollider = gameObject.AddComponent<SphereCollider>();
        }

        veilCollider.isTrigger = true;
        waterParticles.Stop();
        veilCollider.enabled = false;
        IsActive = false;
    }

    public void Activate(float duration, float speedMultiplier, float radius, float dps)
    {
        slowDuration = duration;
        slowMultiplier = speedMultiplier;
        damagePerSecond = dps;

        veilCollider.radius = radius;
        veilCollider.enabled = true;

        var shape = waterParticles.shape;
        shape.radius = radius;

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
        RestoreAllEnemies();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsActive) return;

        BasicEnemy enemy = other.GetComponent<BasicEnemy>();
        if (enemy != null && !enemy.IsDead)
        {
            ApplySlowEffect(enemy);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        BasicEnemy enemy = other.GetComponent<BasicEnemy>();
        if (enemy != null)
        {
            RemoveSlowEffect(enemy);
        }
    }

    private void Update()
    {
        if (!IsActive) return;

        if (damagePerSecond > 0)
        {
            float currentTime = Time.time;
            
            // Reuse cached list instead of allocating new one each frame
            enemiesToDamageCache.Clear();

            foreach (var kvp in affectedEnemies)
            {
                if (kvp.Key != null && kvp.Value.nextDamageTime <= currentTime)
                {
                    enemiesToDamageCache.Add(kvp.Key);
                    kvp.Value.nextDamageTime = currentTime + damageTickRate;
                }
            }

            for (int i = 0; i < enemiesToDamageCache.Count; i++)
            {
                var enemy = enemiesToDamageCache[i];
                if (enemy != null && !enemy.IsDead)
                {
                    DealDamage(enemy);
                }
            }
        }
    }

    private void ApplySlowEffect(BasicEnemy enemy)
    {
        if (enemy == null || affectedEnemies.ContainsKey(enemy))
            return;

        // Get or add the slow modifier component
        EnemySlowModifier modifier = enemy.GetComponent<EnemySlowModifier>();
        if (modifier == null)
        {
            modifier = enemy.gameObject.AddComponent<EnemySlowModifier>();
        }

        // Use direct property access instead of reflection
        float originalSpeed = enemy.MoveSpeed;

        SlowData data = new SlowData
        {
            originalSpeed = originalSpeed,
            nextDamageTime = Time.time + damageTickRate,
            modifier = modifier
        };

        affectedEnemies.Add(enemy, data);

        // Apply the slow through the modifier (no reflection)
        modifier.ApplySlow(enemy, slowMultiplier, originalSpeed);

        StartCoroutine(RemoveSlowAfterDuration(enemy));
    }

    private void RemoveSlowEffect(BasicEnemy enemy)
    {
        if (enemy == null || !affectedEnemies.ContainsKey(enemy))
            return;

        SlowData data = affectedEnemies[enemy];

        if (data.modifier != null)
        {
            data.modifier.RemoveSlow(data.originalSpeed);
        }

        affectedEnemies.Remove(enemy);
    }

    private void RestoreAllEnemies()
    {
        // Use cached list for iteration to avoid modifying dictionary during enumeration
        enemiesToDamageCache.Clear();
        foreach (var kvp in affectedEnemies)
        {
            enemiesToDamageCache.Add(kvp.Key);
        }
        
        for (int i = 0; i < enemiesToDamageCache.Count; i++)
        {
            RemoveSlowEffect(enemiesToDamageCache[i]);
        }
        affectedEnemies.Clear();
    }

    private void DealDamage(BasicEnemy enemy)
    {
        if (enemy != null && !enemy.IsDead)
        {
            float damageAmount = damagePerSecond * damageTickRate;
            enemy.TakeDamage(damageAmount);
        }
    }

    private System.Collections.IEnumerator RemoveSlowAfterDuration(BasicEnemy enemy)
    {
        yield return new WaitForSeconds(slowDuration);

        if (enemy != null && !veilCollider.bounds.Contains(enemy.transform.position))
        {
            RemoveSlowEffect(enemy);
        }
    }

    private void OnDestroy()
    {
        RestoreAllEnemies();
    }
}

/// <summary>
/// Helper component that continuously applies speed reduction to enemies.
/// Automatically added to enemies when they enter the water veil.
/// OPTIMIZED: Uses direct property access instead of reflection.
/// </summary>
public class EnemySlowModifier : MonoBehaviour
{
    private EnemyBase enemy;
    private float originalSpeed;
    private float slowMultiplier;
    private bool isSlowed;

    public void ApplySlow(EnemyBase targetEnemy, float multiplier, float origSpeed)
    {
        enemy = targetEnemy;
        originalSpeed = origSpeed;
        slowMultiplier = multiplier;
        isSlowed = true;

        // Apply initial slow using direct property access
        enemy.MoveSpeed = originalSpeed * slowMultiplier;
    }

    public void RemoveSlow(float restoreSpeed)
    {
        if (enemy != null)
        {
            enemy.MoveSpeed = restoreSpeed;
        }
        isSlowed = false;
        Destroy(this);
    }

    private void LateUpdate()
    {
        // Continuously reapply the slow in case it gets overwritten
        if (isSlowed && enemy != null && !enemy.IsDead)
        {
            enemy.MoveSpeed = originalSpeed * slowMultiplier;
        }
    }
}
