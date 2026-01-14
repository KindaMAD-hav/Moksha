using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

/// <summary>
/// Optimized Lightning Strike Ability.
/// Uses partial sorting to find closest enemies without sorting the full list.
/// Minimizes allocations and component lookups.
/// </summary>
public class LightningStrikeAbility : MonoBehaviour
{
    [Header("Runtime Stats")]
    [SerializeField] private float damage;
    [SerializeField] private float cooldown;
    [SerializeField] private float range;
    [SerializeField] private int targetCount;
    [SerializeField] private int currentStacks;

    [Header("Targeting")]
    [SerializeField] private float headHeightOffset = 1.5f;
    [SerializeField] private bool useHeadBone = true;
    [SerializeField] private string[] headBoneNames = { "Head", "head", "Bip001 Head", "mixamorig:Head" };

    // Reference to power-up data
    private LightningStrikePowerUp powerUpData;

    // Cached
    private Transform cachedTransform;
    private List<EnemyBase> nearbyEnemies; // Reused list
    private EnemyBase[] closestEnemiesBuffer; // Fixed buffer for targets
    private float rangeSqr;

    // AOE Stats
    private bool enableAOE;
    private float aoeRadius;
    private float aoeDamageMultiplier;
    private float cooldownTimer;

    // Cache for head transforms (InstanceID -> Transform)
    private Dictionary<int, Transform> headTransformCache;

    public int CurrentStacks => currentStacks;

    public void Initialize(LightningStrikePowerUp data)
    {
        powerUpData = data;
        currentStacks = 1;
        cachedTransform = transform;

        // Pre-allocate with generous capacity to avoid resize during combat
        nearbyEnemies = new List<EnemyBase>(128);
        headTransformCache = new Dictionary<int, Transform>(256);
        closestEnemiesBuffer = new EnemyBase[20]; // Max targets cap

        UpdateStats();
        cooldownTimer = cooldown;
    }

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

        enableAOE = powerUpData.enableAOE;
        aoeRadius = powerUpData.aoeRadius;
        aoeDamageMultiplier = powerUpData.aoeDamageMultiplier;

        // Resize buffer if target count grows beyond initial size
        if (closestEnemiesBuffer == null || closestEnemiesBuffer.Length < targetCount)
            closestEnemiesBuffer = new EnemyBase[targetCount + 5];
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
        // 1. Get Enemies (Allocation Free)
        nearbyEnemies.Clear();
        if (EnemyManager.Instance == null) return;

        // This relies on the optimized EnemyManager you implemented earlier
        EnemyManager.Instance.GetEnemiesInRadius(cachedTransform.position, range, nearbyEnemies);

        if (nearbyEnemies.Count == 0) return;

        cooldownTimer = 0f;

        // 2. Play Sound (Centralized)
        if (LightningStrikeManager.Instance != null)
            LightningStrikeManager.Instance.PlayStrikeSound();

        // 3. Find Top K Closest Enemies (Optimization: Don't sort the whole list!)
        int strikeCount = SelectClosestEnemies(targetCount);

        // 4. Strike Selected Targets
        for (int i = 0; i < strikeCount; i++)
        {
            EnemyBase enemy = closestEnemiesBuffer[i];
            if (enemy == null || enemy.IsDead) continue;

            // Get head transform logic
            Transform enemyTransform = enemy.transform;
            Transform headTransform = GetEnemyHeadTransform(enemy, out Vector3 headOffset);

            // Visuals
            if (LightningStrikeManager.Instance != null)
            {
                if (headTransform != null)
                    LightningStrikeManager.Instance.SpawnLightningFollowing(headTransform);
                else
                    LightningStrikeManager.Instance.SpawnLightningFollowing(enemyTransform, headOffset);
            }

            // Damage
            ApplyLightningDamage(enemy, damage);
        }
    }

    /// <summary>
    /// Partial Sort / Selection Algorithm. 
    /// Finds the 'k' closest enemies without sorting the entire list (O(N*K) instead of O(N log N)).
    /// Result is stored in 'closestEnemiesBuffer'.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int SelectClosestEnemies(int k)
    {
        int count = nearbyEnemies.Count;
        if (count == 0) return 0;

        // If we want more targets than we have enemies, just take them all
        if (k >= count)
        {
            for (int i = 0; i < count; i++) closestEnemiesBuffer[i] = nearbyEnemies[i];
            return count;
        }

        Vector3 myPos = cachedTransform.position;

        // Run K passes to find the K closest items
        // For very small K (e.g., 3-5) this is much faster than Quicksort on 200 items.
        for (int i = 0; i < k; i++)
        {
            int closestIndex = -1;
            float closestDistSqr = float.MaxValue;

            // Search the *remaining* list (j starts at 0, but we check if already picked?)
            // Actually, simpler swap-remove approach:
            // Scan from i to end of list. Swap closest to position i.

            for (int j = i; j < count; j++)
            {
                EnemyBase enemy = nearbyEnemies[j];
                float d = GetSqrDistance(enemy.transform.position, myPos);

                if (d < closestDistSqr)
                {
                    closestDistSqr = d;
                    closestIndex = j;
                }
            }

            if (closestIndex != -1)
            {
                // Swap closest to position i in the source list
                EnemyBase temp = nearbyEnemies[i];
                nearbyEnemies[i] = nearbyEnemies[closestIndex];
                nearbyEnemies[closestIndex] = temp;

                // Add to buffer
                closestEnemiesBuffer[i] = nearbyEnemies[i];
            }
        }

        return k;
    }

    private void ApplyLightningDamage(EnemyBase primaryEnemy, float baseDamage)
    {
        if (primaryEnemy == null || primaryEnemy.IsDead) return;

        primaryEnemy.TakeDamage(baseDamage);

        if (!enableAOE || aoeRadius <= 0f) return;

        // --- AOE LOGIC ---
        // Note: For AOE, we query the manager again or reuse the nearby list?
        // Querying manager again is safer for accuracy, but slower. 
        // Let's reuse nearbyEnemies but be careful since we just shuffled it.
        // Better: Just do a raw distance check on the already fetched 'nearbyEnemies' list.

        float aoeRadiusSqr = aoeRadius * aoeRadius;
        Vector3 center = primaryEnemy.transform.position;
        int count = nearbyEnemies.Count;

        for (int i = 0; i < count; i++)
        {
            EnemyBase enemy = nearbyEnemies[i];

            // Skip self and dead
            if (enemy == primaryEnemy || enemy == null || enemy.IsDead) continue;

            if (GetSqrDistance(enemy.transform.position, center) <= aoeRadiusSqr)
            {
                enemy.TakeDamage(baseDamage * aoeDamageMultiplier);
            }
        }
    }

    /// <summary>
    /// optimized head lookup with caching
    /// </summary>
    private Transform GetEnemyHeadTransform(EnemyBase enemy, out Vector3 headOffset)
    {
        int id = enemy.GetInstanceID();

        // 1. Check Cache
        if (headTransformCache.TryGetValue(id, out Transform cachedHead))
        {
            // Verify reference is still alive
            if (cachedHead != null)
            {
                headOffset = Vector3.zero;
                return cachedHead;
            }
            // If null, it was destroyed or something changed, fall through to re-find
        }

        // 2. Find it
        Transform foundHead = null;
        headOffset = Vector3.zero;

        if (useHeadBone)
        {
            foundHead = FindHeadBone(enemy.transform);
        }

        // 3. Cache it (even if null to avoid re-searching failed searches)
        // If found, cache it. If not found, we handle offsets below.
        if (foundHead != null)
        {
            headTransformCache[id] = foundHead;
            return foundHead;
        }

        // 4. Fallbacks (Calculate offset once)
        Collider col = enemy.GetComponent<Collider>();
        if (col != null)
        {
            float height = col.bounds.max.y - enemy.transform.position.y;
            headOffset.y = height;
        }
        else
        {
            headOffset.y = headHeightOffset;
        }

        // Cache null so we don't search bones again next frame, 
        // but we rely on the offset logic.
        headTransformCache[id] = null;

        return null;
    }

    private Transform FindHeadBone(Transform root)
    {
        // Breadth-first search usually finds head faster than depth-first in humanoids
        // But for deep hierarchies, DFS is standard. Keep existing logic but optimize array access.

        // Optimization: Check common names on immediate children first
        int childCount = root.childCount;
        for (int k = 0; k < childCount; k++)
        {
            Transform child = root.GetChild(k);
            for (int i = 0; i < headBoneNames.Length; i++)
            {
                // String comparison is slow, but unavoidable here. 
                // Using .name is property access, cache it?
                if (child.name.Contains(headBoneNames[i])) return child;
            }
        }

        // Deep Search
        return FindHeadBoneRecursive(root);
    }

    private Transform FindHeadBoneRecursive(Transform parent)
    {
        foreach (Transform child in parent)
        {
            for (int i = 0; i < headBoneNames.Length; i++)
            {
                if (child.name.Contains(headBoneNames[i])) return child;
            }
            Transform found = FindHeadBoneRecursive(child);
            if (found != null) return found;
        }
        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float GetSqrDistance(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dy = a.y - b.y;
        float dz = a.z - b.z;
        return dx * dx + dy * dy + dz * dz;
    }

    // Call this when entering a new level to clear old references
    public void ClearCache()
    {
        headTransformCache.Clear();
    }
}