using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages orbiting fireballs around the player.
/// Each stack adds one additional fireball that orbits and damages enemies on contact.
/// </summary>
public class FireballOrbitAbility : MonoBehaviour
{
    [Header("Runtime Stats")]
    [SerializeField] private float damage;
    [SerializeField] private float orbitSpeed;
    [SerializeField] private int currentStacks;

    // Reference to power-up data
    private FireballPowerUp powerUpData;

    // Cached
    private Transform cachedTransform;
    private List<OrbitingFireball> activeFireballs = new List<OrbitingFireball>();
    
    // Current orbit angle in degrees
    private float currentAngle;

    public int CurrentStacks => currentStacks;

    public void Initialize(FireballPowerUp data)
    {
        powerUpData = data;
        currentStacks = 1;
        cachedTransform = transform;
        currentAngle = 0f;

        UpdateStats();
        SpawnFireballs();
    }

    public void AddStack()
    {
        currentStacks++;
        UpdateStats();
        SpawnFireballs();
    }

    private void UpdateStats()
    {
        if (powerUpData == null) return;

        damage = powerUpData.GetDamage(currentStacks);
        orbitSpeed = powerUpData.GetOrbitSpeed(currentStacks);

        // Update damage on existing fireballs
        for (int i = 0; i < activeFireballs.Count; i++)
        {
            if (activeFireballs[i] != null)
            {
                activeFireballs[i].SetDamage(damage);
            }
        }
    }

    private void SpawnFireballs()
    {
        if (powerUpData == null || powerUpData.fireballPrefab == null)
        {
            Debug.LogWarning("[FireballOrbitAbility] No fireball prefab assigned!");
            return;
        }

        int targetCount = powerUpData.GetFireballCount(currentStacks);
        int currentCount = activeFireballs.Count;

        // Spawn additional fireballs if needed
        while (activeFireballs.Count < targetCount)
        {
            SpawnSingleFireball();
        }

        // Reposition all fireballs evenly around the orbit
        RepositionFireballs();
    }

    private void SpawnSingleFireball()
    {
        GameObject fireballObj = Instantiate(powerUpData.fireballPrefab, cachedTransform.position, Quaternion.identity);
        fireballObj.transform.localScale = Vector3.one * powerUpData.fireballScale;

        // Get or add the OrbitingFireball component
        OrbitingFireball fireball = fireballObj.GetComponent<OrbitingFireball>();
        if (fireball == null)
        {
            fireball = fireballObj.AddComponent<OrbitingFireball>();
        }

        fireball.Initialize(damage, powerUpData.hitCooldown);
        activeFireballs.Add(fireball);
    }

    private void RepositionFireballs()
    {
        int count = activeFireballs.Count;
        if (count == 0) return;

        float angleStep = 360f / count;

        for (int i = 0; i < count; i++)
        {
            if (activeFireballs[i] != null)
            {
                activeFireballs[i].SetOrbitIndex(i, angleStep);
            }
        }
    }

    private void Update()
    {
        if (powerUpData == null) return;

        // Update orbit angle
        currentAngle += orbitSpeed * Time.deltaTime;
        if (currentAngle >= 360f) currentAngle -= 360f;

        // Update fireball positions
        UpdateFireballPositions();
    }

    private void UpdateFireballPositions()
    {
        int count = activeFireballs.Count;
        if (count == 0) return;

        Vector3 center = cachedTransform.position + Vector3.up * powerUpData.heightOffset;
        float angleStep = 360f / count;

        for (int i = 0; i < count; i++)
        {
            OrbitingFireball fireball = activeFireballs[i];
            if (fireball == null)
            {
                // Remove null entries (destroyed fireballs)
                activeFireballs.RemoveAt(i);
                i--;
                count--;
                continue;
            }

            // Calculate position for this fireball
            float angle = (currentAngle + i * angleStep) * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(
                Mathf.Cos(angle) * powerUpData.orbitRadius,
                0f,
                Mathf.Sin(angle) * powerUpData.orbitRadius
            );

            fireball.transform.position = center + offset;

            // Optional: Make fireballs face their movement direction
            float nextAngle = (currentAngle + i * angleStep + 10f) * Mathf.Deg2Rad;
            Vector3 nextOffset = new Vector3(
                Mathf.Cos(nextAngle) * powerUpData.orbitRadius,
                0f,
                Mathf.Sin(nextAngle) * powerUpData.orbitRadius
            );
            Vector3 direction = (center + nextOffset) - fireball.transform.position;
            if (direction.sqrMagnitude > 0.001f)
            {
                fireball.transform.rotation = Quaternion.LookRotation(direction);
            }
        }
    }

    private void OnDestroy()
    {
        // Clean up all fireballs when ability is removed
        for (int i = 0; i < activeFireballs.Count; i++)
        {
            if (activeFireballs[i] != null)
            {
                Destroy(activeFireballs[i].gameObject);
            }
        }
        activeFireballs.Clear();
    }

    /// <summary>
    /// Call this when entering a new level to respawn fireballs
    /// </summary>
    public void ResetFireballs()
    {
        // Destroy existing
        for (int i = 0; i < activeFireballs.Count; i++)
        {
            if (activeFireballs[i] != null)
            {
                Destroy(activeFireballs[i].gameObject);
            }
        }
        activeFireballs.Clear();

        // Respawn
        SpawnFireballs();
    }
}
