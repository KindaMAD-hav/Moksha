using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

/// <summary>
/// Lightning Strike Ability - Attached to player when they acquire the power-up.
/// Periodically finds nearby enemies and strikes them with lightning.
/// </summary>
public class LightningStrikeAbility : MonoBehaviour
{
    [Header("Runtime Stats (from PowerUp)")]
    [SerializeField] private float damage;
    [SerializeField] private float cooldown;
    [SerializeField] private float range;
    [SerializeField] private int targetCount;
    [SerializeField] private int currentStacks;

    [Header("Targeting")]
    [Tooltip("Height offset to target enemy's head (added to enemy position)")]
    [SerializeField] private float headHeightOffset = 1.5f;
    
    [Tooltip("If true, tries to find a 'Head' child transform on enemy")]
    [SerializeField] private bool useHeadBone = true;
    
    [Tooltip("Names to search for head bone (checked in order)")]
    [SerializeField] private string[] headBoneNames = { "Head", "head", "Bip001 Head", "mixamorig:Head" };

    [Header("Debug")]
    [SerializeField] private float cooldownTimer;

    // Reference to power-up data
    private LightningStrikePowerUp powerUpData;
    
    // Cached
    private Transform cachedTransform;
    private List<EnemyBase> nearbyEnemies;
    private float rangeSqr;
    
    // Cache for head transforms (avoid repeated FindChild calls)
    private Dictionary<int, Transform> headTransformCache;

    public int CurrentStacks => currentStacks;

    /// <summary>
    /// Initialize with power-up data (called when first acquired)
    /// </summary>
    public void Initialize(LightningStrikePowerUp data)
    {
        powerUpData = data;
        currentStacks = 1;
        cachedTransform = transform;
        nearbyEnemies = new List<EnemyBase>(32);
        headTransformCache = new Dictionary<int, Transform>(64);
        
        UpdateStats();
        cooldownTimer = cooldown; // Start ready to strike
    }

    /// <summary>
    /// Add a stack (called when power-up is acquired again)
    /// </summary>
    public void AddStack()
    {
        currentStacks++;
        UpdateStats();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateStats()
    {
        if (powerUpData == null) return;
        
        damage = powerUpData.GetDamage(currentStacks);
        cooldown = powerUpData.GetCooldown(currentStacks);
        range = powerUpData.GetRange(currentStacks);
        targetCount = powerUpData.GetTargetCount(currentStacks);
        rangeSqr = range * range;
    }

    private void Update()
    {
        if (powerUpData == null) return;
        
        cooldownTimer += Time.deltaTime;
        
        if (cooldownTimer >= cooldown)
        {
            TryStrike();
        }
    }

    private void TryStrike()
    {
        // Find nearby enemies
        FindNearbyEnemies();
        
        if (nearbyEnemies.Count == 0) return;
        
        // Reset cooldown
        cooldownTimer = 0f;
        
        // Strike up to targetCount enemies
        int strikes = Mathf.Min(targetCount, nearbyEnemies.Count);
        
        for (int i = 0; i < strikes; i++)
        {
            EnemyBase enemy = nearbyEnemies[i];
            if (enemy == null || enemy.IsDead) continue;
            
            // Get precise head position
            Vector3 strikePosition = GetEnemyHeadPosition(enemy);
            
            // Spawn lightning VFX at head position
            if (LightningStrikeManager.Instance != null)
            {
                LightningStrikeManager.Instance.SpawnLightningAtTarget(strikePosition);
            }
            
            // Deal damage
            enemy.TakeDamage(damage);
        }
    }

    /// <summary>
    /// Get the enemy's head position for precise lightning targeting
    /// </summary>
    private Vector3 GetEnemyHeadPosition(EnemyBase enemy)
    {
        Transform enemyTransform = enemy.transform;
        int instanceId = enemy.GetInstanceID();
        
        // Try to get cached head transform
        if (useHeadBone)
        {
            if (headTransformCache.TryGetValue(instanceId, out Transform cachedHead))
            {
                if (cachedHead != null)
                {
                    return cachedHead.position;
                }
            }
            else
            {
                // Search for head bone
                Transform headBone = FindHeadBone(enemyTransform);
                headTransformCache[instanceId] = headBone; // Cache even if null
                
                if (headBone != null)
                {
                    return headBone.position;
                }
            }
        }
        
        // Fallback: Use collider bounds if available
        Collider col = enemy.GetComponent<Collider>();
        if (col != null)
        {
            Bounds bounds = col.bounds;
            return new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
        }
        
        // Fallback: Use CharacterController bounds
        CharacterController cc = enemy.GetComponent<CharacterController>();
        if (cc != null)
        {
            Vector3 pos = enemyTransform.position;
            return new Vector3(pos.x, pos.y + cc.height, pos.z);
        }
        
        // Final fallback: enemy position + height offset
        Vector3 basePos = enemyTransform.position;
        return new Vector3(basePos.x, basePos.y + headHeightOffset, basePos.z);
    }

    /// <summary>
    /// Search for head bone in enemy hierarchy
    /// </summary>
    private Transform FindHeadBone(Transform root)
    {
        // Check immediate children first (faster)
        for (int i = 0; i < headBoneNames.Length; i++)
        {
            Transform found = root.Find(headBoneNames[i]);
            if (found != null) return found;
        }
        
        // Deep search through all children
        return FindHeadBoneRecursive(root);
    }

    private Transform FindHeadBoneRecursive(Transform parent)
    {
        foreach (Transform child in parent)
        {
            // Check if this child matches any head bone name
            string childName = child.name;
            for (int i = 0; i < headBoneNames.Length; i++)
            {
                if (childName.Contains(headBoneNames[i]))
                {
                    return child;
                }
            }
            
            // Recurse into children
            Transform found = FindHeadBoneRecursive(child);
            if (found != null) return found;
        }
        
        return null;
    }

    private void FindNearbyEnemies()
    {
        nearbyEnemies.Clear();
        
        // Use EnemyManager for efficient enemy lookup
        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.GetEnemiesInRadius(cachedTransform.position, range, nearbyEnemies);
            
            // Sort by distance (closest first)
            SortByDistance();
        }
    }

    private void SortByDistance()
    {
        if (nearbyEnemies.Count <= 1) return;
        
        Vector3 myPos = cachedTransform.position;
        int count = nearbyEnemies.Count;
        
        // Simple selection sort (efficient for small lists, no allocations)
        for (int i = 0; i < count - 1 && i < targetCount; i++)
        {
            int minIndex = i;
            float minDistSqr = GetSqrDistance(nearbyEnemies[i].transform.position, myPos);
            
            for (int j = i + 1; j < count; j++)
            {
                float distSqr = GetSqrDistance(nearbyEnemies[j].transform.position, myPos);
                if (distSqr < minDistSqr)
                {
                    minDistSqr = distSqr;
                    minIndex = j;
                }
            }
            
            if (minIndex != i)
            {
                EnemyBase temp = nearbyEnemies[i];
                nearbyEnemies[i] = nearbyEnemies[minIndex];
                nearbyEnemies[minIndex] = temp;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float GetSqrDistance(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dy = a.y - b.y;
        float dz = a.z - b.z;
        return dx * dx + dy * dy + dz * dz;
    }

    /// <summary>
    /// Force a strike (for testing)
    /// </summary>
    public void ForceStrike()
    {
        cooldownTimer = cooldown;
        TryStrike();
    }
    
    /// <summary>
    /// Clear head transform cache (call if enemies are destroyed/recycled)
    /// </summary>
    public void ClearHeadCache()
    {
        headTransformCache?.Clear();
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, range > 0 ? range : 10f);
    }
#endif
}
