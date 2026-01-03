using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Database of all available power-ups in the game.
/// Create this as a ScriptableObject asset and assign your power-ups to it.
/// </summary>
[CreateAssetMenu(fileName = "PowerUpDatabase", menuName = "Power-Ups/Power-Up Database")]
public class PowerUpDatabase : ScriptableObject
{
    [Header("All Available Power-Ups")]
    public List<PowerUp> allPowerUps = new List<PowerUp>();

    [Header("Rarity Weights")]
    [Tooltip("Higher weight = more common")]
    public float commonWeight = 60f;
    public float uncommonWeight = 25f;
    public float rareWeight = 10f;
    public float epicWeight = 4f;
    public float legendaryWeight = 1f;

    /// <summary>
    /// Get random power-ups for level-up selection.
    /// </summary>
    /// <param name="count">Number of power-ups to return</param>
    /// <param name="acquiredPowerUps">Dictionary of already acquired power-ups and their stack counts</param>
    /// <returns>List of randomly selected power-ups</returns>
    public List<PowerUp> GetRandomPowerUps(int count, Dictionary<PowerUp, int> acquiredPowerUps = null)
    {
        List<PowerUp> available = GetAvailablePowerUps(acquiredPowerUps);
        List<PowerUp> selected = new List<PowerUp>();

        if (available.Count == 0)
        {
            Debug.LogWarning("[PowerUpDatabase] No available power-ups!");
            return selected;
        }

        // Build weighted list
        List<(PowerUp powerUp, float weight)> weightedList = new List<(PowerUp, float)>();
        foreach (var powerUp in available)
        {
            float weight = GetRarityWeight(powerUp.rarity);
            weightedList.Add((powerUp, weight));
        }

        // Select random power-ups without duplicates
        for (int i = 0; i < count && weightedList.Count > 0; i++)
        {
            PowerUp chosen = SelectWeightedRandom(weightedList);
            selected.Add(chosen);
            
            // Remove from pool to avoid duplicates in same selection
            weightedList.RemoveAll(x => x.powerUp == chosen);
        }

        return selected;
    }

    private List<PowerUp> GetAvailablePowerUps(Dictionary<PowerUp, int> acquiredPowerUps)
    {
        List<PowerUp> available = new List<PowerUp>();

        foreach (var powerUp in allPowerUps)
        {
            if (powerUp == null) continue;

            // Check if can still be acquired
            if (!powerUp.canStack)
            {
                // Non-stackable: only available if not yet acquired
                if (acquiredPowerUps != null && acquiredPowerUps.ContainsKey(powerUp))
                    continue;
            }
            else if (powerUp.maxStacks > 0)
            {
                // Has max stacks: check if limit reached
                if (acquiredPowerUps != null && 
                    acquiredPowerUps.TryGetValue(powerUp, out int stacks) && 
                    stacks >= powerUp.maxStacks)
                    continue;
            }

            available.Add(powerUp);
        }

        return available;
    }

    private float GetRarityWeight(PowerUpRarity rarity)
    {
        return rarity switch
        {
            PowerUpRarity.Common => commonWeight,
            PowerUpRarity.Uncommon => uncommonWeight,
            PowerUpRarity.Rare => rareWeight,
            PowerUpRarity.Epic => epicWeight,
            PowerUpRarity.Legendary => legendaryWeight,
            _ => commonWeight
        };
    }

    private PowerUp SelectWeightedRandom(List<(PowerUp powerUp, float weight)> weightedList)
    {
        float totalWeight = 0f;
        foreach (var item in weightedList)
            totalWeight += item.weight;

        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;

        foreach (var item in weightedList)
        {
            currentWeight += item.weight;
            if (randomValue <= currentWeight)
                return item.powerUp;
        }

        return weightedList[weightedList.Count - 1].powerUp;
    }
}
