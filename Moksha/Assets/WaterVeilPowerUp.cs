using UnityEngine;

/// <summary>
/// Water Veil power-up that creates a water particle effect around the player
/// and slows enemies that come into contact with it.
/// </summary>
[CreateAssetMenu(fileName = "WaterVeil", menuName = "Power-Ups/Water Veil")]
public class WaterVeilPowerUp : PowerUp
{
    [Header("Water Veil Settings")]
    [Tooltip("Duration of the slow effect on enemies (in seconds)")]
    public float slowDuration = 3f;

    [Tooltip("Movement speed multiplier for slowed enemies (0.5 = 50% speed)")]
    [Range(0.1f, 0.9f)]
    public float slowMultiplier = 0.5f;

    [Tooltip("Radius of the water veil effect")]
    public float veilRadius = 3f;

    [Tooltip("Damage per second (optional, set to 0 for no damage)")]
    public float damagePerSecond = 0f;

    public override void Apply(GameObject player)
    {
        WaterVeilEffect effect = player.GetComponentInChildren<WaterVeilEffect>();

        if (effect == null)
        {
            Debug.LogWarning("WaterVeilEffect component not found as child of player!");
            return;
        }

        // Activate or upgrade the water veil
        if (!effect.IsActive)
        {
            effect.Activate(slowDuration, slowMultiplier, veilRadius, damagePerSecond);
        }
        else
        {
            // Stack upgrade: increase duration and radius
            effect.UpgradeEffect(slowDuration * 0.5f, veilRadius * 0.2f);
        }
    }

    public string GetDescription(int currentStacks)
    {
        if (currentStacks == 0)
        {
            return $"Surrounds you with a water veil that slows enemies to {slowMultiplier * 100}% speed for {slowDuration}s.";
        }
        else
        {
            return $"Water Veil Lv.{currentStacks + 1}: Increased duration and radius.";
        }
    }
}