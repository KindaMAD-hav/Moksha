using UnityEngine;

/// <summary>
/// ScriptableObject for player base stats.
/// Can be modified at runtime for difficulty scaling or power-ups.
/// </summary>
[CreateAssetMenu(fileName = "NewPlayerStats", menuName = "Moksha/Player Stats")]
public class PlayerStats : ScriptableObject
{
    [Header("Health")]
    [Tooltip("Base maximum health")]
    public float maxHealth = 100f;
    
    [Tooltip("Health regeneration per second (0 = disabled)")]
    public float healthRegen = 0f;
    
    [Tooltip("Duration of invincibility after taking damage")]
    public float invincibilityDuration = 1f;
    
    [Header("Movement")]
    [Tooltip("Base movement speed")]
    public float moveSpeed = 6f;
    
    [Tooltip("Rotation speed in degrees per second")]
    public float rotationSpeed = 720f;
    
    [Header("Combat")]
    [Tooltip("Global damage multiplier")]
    public float damageMultiplier = 1f;
    
    [Tooltip("Global attack speed multiplier")]
    public float attackSpeedMultiplier = 1f;
    
    [Tooltip("Pickup range for XP orbs and items")]
    public float pickupRange = 2f;
    
    [Header("Luck & Progression")]
    [Tooltip("Affects rare drops and power-up quality")]
    [Range(0f, 100f)]
    public float luck = 0f;
    
    [Tooltip("XP gain multiplier")]
    public float xpMultiplier = 1f;
}
