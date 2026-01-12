using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Optimized water veil effect for high enemy counts.
/// Uses a single Update loop and pooled objects.
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class WaterVeilEffect : MonoBehaviour
{
    private ParticleSystem waterParticles;
    private SphereCollider veilCollider;

    private float slowDuration;
    private float slowMultiplier;
    private float damagePerSecond;

    // Use arrays for better cache performance with many enemies
    private List<BasicEnemy> affectedEnemies = new List<BasicEnemy>(32);
    private List<float> originalSpeeds = new List<float>(32);
    private List<float> slowEndTimes = new List<float>(32);
    private List<float> nextDamageTimes = new List<float>(32);

    private System.Reflection.FieldInfo cachedSpeedField;
    private float damageTickRate = 0.5f;

    public bool IsActive { get; private set; }

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

        // Cache the field info once for all enemies
        cachedSpeedField = typeof(EnemyBase).GetField("cachedMoveSpeed",
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.Instance);
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
        if (!IsActive || cachedSpeedField == null) return;

        float currentTime = Time.time;

        // Process all affected enemies in a single loop
        for (int i = affectedEnemies.Count - 1; i >= 0; i--)
        {
            BasicEnemy enemy = affectedEnemies[i];

            // Remove dead or null enemies
            if (enemy == null || enemy.IsDead)
            {
                RemoveAtIndex(i);
                continue;
            }

            // Reapply slow each frame (keeps it consistent)
            float targetSpeed = originalSpeeds[i] * slowMultiplier;
            cachedSpeedField.SetValue(enemy, targetSpeed);

            // Check for duration expiry
            if (currentTime >= slowEndTimes[i])
            {
                // Check if still in collider bounds
                if (!veilCollider.bounds.Contains(enemy.transform.position))
                {
                    RestoreAndRemoveAtIndex(i);
                    continue;
                }
                else
                {
                    // Refresh duration if still inside
                    slowEndTimes[i] = currentTime + slowDuration;
                }
            }

            // Apply damage if enabled
            if (damagePerSecond > 0 && currentTime >= nextDamageTimes[i])
            {
                enemy.TakeDamage(damagePerSecond * damageTickRate);
                nextDamageTimes[i] = currentTime + damageTickRate;
            }
        }
    }

    private void ApplySlowEffect(BasicEnemy enemy)
    {
        if (cachedSpeedField == null) return;

        // Check if already affected
        int existingIndex = affectedEnemies.IndexOf(enemy);
        if (existingIndex >= 0)
        {
            // Refresh duration
            slowEndTimes[existingIndex] = Time.time + slowDuration;
            return;
        }

        float originalSpeed = (float)cachedSpeedField.GetValue(enemy);

        // Add to lists
        affectedEnemies.Add(enemy);
        originalSpeeds.Add(originalSpeed);
        slowEndTimes.Add(Time.time + slowDuration);
        nextDamageTimes.Add(Time.time + damageTickRate);

        // Apply initial slow
        cachedSpeedField.SetValue(enemy, originalSpeed * slowMultiplier);
    }

    private void RemoveSlowEffect(BasicEnemy enemy)
    {
        int index = affectedEnemies.IndexOf(enemy);
        if (index >= 0)
        {
            RestoreAndRemoveAtIndex(index);
        }
    }

    private void RestoreAndRemoveAtIndex(int index)
    {
        BasicEnemy enemy = affectedEnemies[index];
        if (enemy != null && cachedSpeedField != null)
        {
            cachedSpeedField.SetValue(enemy, originalSpeeds[index]);
        }
        RemoveAtIndex(index);
    }

    private void RemoveAtIndex(int index)
    {
        affectedEnemies.RemoveAt(index);
        originalSpeeds.RemoveAt(index);
        slowEndTimes.RemoveAt(index);
        nextDamageTimes.RemoveAt(index);
    }

    private void RestoreAllEnemies()
    {
        if (cachedSpeedField == null) return;

        for (int i = 0; i < affectedEnemies.Count; i++)
        {
            BasicEnemy enemy = affectedEnemies[i];
            if (enemy != null)
            {
                cachedSpeedField.SetValue(enemy, originalSpeeds[i]);
            }
        }

        affectedEnemies.Clear();
        originalSpeeds.Clear();
        slowEndTimes.Clear();
        nextDamageTimes.Clear();
    }

    private void OnDestroy()
    {
        RestoreAllEnemies();
    }
}