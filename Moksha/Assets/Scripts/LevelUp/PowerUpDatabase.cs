using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Optimized power-up database with cached weighted lists.
/// Minimizes allocations during random selection.
/// </summary>
[CreateAssetMenu(fileName = "PowerUpDatabase", menuName = "Power-Ups/Power-Up Database")]
public class PowerUpDatabase : ScriptableObject
{
    [Header("All Available Power-Ups")]
    public PowerUp[] allPowerUps; // Array instead of List for faster iteration

    [Header("Rarity Weights")]
    public float commonWeight = 60f;
    public float uncommonWeight = 25f;
    public float rareWeight = 10f;
    public float epicWeight = 4f;
    public float legendaryWeight = 1f;

    // Cached weights array (indexed by PowerUpRarity enum)
    private float[] rarityWeights;
    
    // Pre-allocated lists to avoid allocations
    private List<PowerUp> availableCache;
    private List<float> weightsCache;
    private List<PowerUp> selectedCache;

    private void OnEnable()
    {
        // Cache rarity weights in array for O(1) lookup
        rarityWeights = new float[5];
        rarityWeights[(int)PowerUpRarity.Common] = commonWeight;
        rarityWeights[(int)PowerUpRarity.Uncommon] = uncommonWeight;
        rarityWeights[(int)PowerUpRarity.Rare] = rareWeight;
        rarityWeights[(int)PowerUpRarity.Epic] = epicWeight;
        rarityWeights[(int)PowerUpRarity.Legendary] = legendaryWeight;

        // Pre-allocate caches
        int capacity = allPowerUps != null ? allPowerUps.Length : 16;
        availableCache = new List<PowerUp>(capacity);
        weightsCache = new List<float>(capacity);
        selectedCache = new List<PowerUp>(4);
    }

    public List<PowerUp> GetRandomPowerUps(int count, Dictionary<PowerUp, int> acquiredPowerUps = null)
    {
        // Initialize caches if needed (for runtime ScriptableObject access)
        if (availableCache == null)
            OnEnable();

        // Clear and reuse cached lists
        availableCache.Clear();
        weightsCache.Clear();
        selectedCache.Clear();

        if (allPowerUps == null || allPowerUps.Length == 0)
            return selectedCache;

        // Build available list with weights
        float totalWeight = 0f;
        
        for (int i = 0; i < allPowerUps.Length; i++)
        {
            PowerUp powerUp = allPowerUps[i];
            if (powerUp == null) continue;

            // Check availability
            if (!IsPowerUpAvailable(powerUp, acquiredPowerUps))
                continue;

            float weight = rarityWeights[(int)powerUp.rarity];
            availableCache.Add(powerUp);
            totalWeight += weight;
            weightsCache.Add(totalWeight); // Store cumulative weight
        }

        if (availableCache.Count == 0)
            return selectedCache;

        // Select random power-ups
        int toSelect = count < availableCache.Count ? count : availableCache.Count;
        
        for (int i = 0; i < toSelect; i++)
        {
            int selectedIndex = SelectWeightedIndex(totalWeight);
            if (selectedIndex >= 0)
            {
                PowerUp chosen = availableCache[selectedIndex];
                selectedCache.Add(chosen);

                // Remove from available pool
                float removedWeight = selectedIndex > 0 
                    ? weightsCache[selectedIndex] - weightsCache[selectedIndex - 1]
                    : weightsCache[0];
                
                RemoveAtIndex(selectedIndex, ref totalWeight, removedWeight);
            }
        }

        return selectedCache;
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    private bool IsPowerUpAvailable(PowerUp powerUp, Dictionary<PowerUp, int> acquired)
    {
        if (acquired == null) return true;
        
        if (!powerUp.canStack)
        {
            return !acquired.ContainsKey(powerUp);
        }
        
        if (powerUp.maxStacks > 0)
        {
            if (acquired.TryGetValue(powerUp, out int stacks))
                return stacks < powerUp.maxStacks;
        }
        
        return true;
    }

    private int SelectWeightedIndex(float totalWeight)
    {
        if (availableCache.Count == 0) return -1;
        
        float random = Random.value * totalWeight;
        
        // Binary search for performance with large lists
        int low = 0;
        int high = weightsCache.Count - 1;
        
        while (low < high)
        {
            int mid = (low + high) >> 1;
            if (weightsCache[mid] < random)
                low = mid + 1;
            else
                high = mid;
        }
        
        return low;
    }

    private void RemoveAtIndex(int index, ref float totalWeight, float removedWeight)
    {
        // Update cumulative weights for remaining items
        for (int i = index; i < weightsCache.Count; i++)
        {
            weightsCache[i] -= removedWeight;
        }
        
        totalWeight -= removedWeight;
        
        // Remove from lists
        availableCache.RemoveAt(index);
        weightsCache.RemoveAt(index);
    }

    /// <summary>
    /// Rebuild weight cache if rarity weights change at runtime
    /// </summary>
    public void RefreshWeights()
    {
        rarityWeights[(int)PowerUpRarity.Common] = commonWeight;
        rarityWeights[(int)PowerUpRarity.Uncommon] = uncommonWeight;
        rarityWeights[(int)PowerUpRarity.Rare] = rareWeight;
        rarityWeights[(int)PowerUpRarity.Epic] = epicWeight;
        rarityWeights[(int)PowerUpRarity.Legendary] = legendaryWeight;
    }
}
