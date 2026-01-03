using UnityEngine;

/// <summary>
/// Generic stat modifier power-up. Use this as a template for simple stat changes.
/// </summary>
[CreateAssetMenu(fileName = "StatModifier", menuName = "Power-Ups/Stat Modifier")]
public class StatModifierPowerUp : PowerUp
{
    [Header("Stat Modifier Settings")]
    public StatType statType;
    public float value = 10f;
    public ModifierType modifierType = ModifierType.Flat;

    public override void Apply(GameObject player)
    {
        // This is a placeholder implementation.
        // When you have a proper stats system, modify it here.
        Debug.Log($"[PowerUp] {powerUpName}: {modifierType} {statType} by {value}");
        
        // Example of how you might implement this later:
        // var stats = player.GetComponent<PlayerStats>();
        // if (stats != null)
        // {
        //     stats.ModifyStat(statType, value, modifierType);
        // }
    }
}

public enum StatType
{
    MaxHealth,
    Damage,
    AttackSpeed,
    CritChance,
    CritDamage,
    Armor,
    DodgeChance,
    PickupRadius,
    XPMultiplier
}

public enum ModifierType
{
    Flat,       // +10
    Percentage  // +10%
}
