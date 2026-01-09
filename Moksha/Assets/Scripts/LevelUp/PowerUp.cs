using UnityEngine;

/// <summary>
/// Base class for all power-ups. Create new power-ups by inheriting from this class.
/// </summary>
public abstract class PowerUp : ScriptableObject
{
    [Header("Power-Up Info")]
    public string powerUpName = "New Power-Up";
    [TextArea(2, 4)]
    public string description = "Description of what this power-up does.";
    public Sprite icon;
    
    [Header("Rarity")]
    public PowerUpRarity rarity = PowerUpRarity.Common;
    
    [Header("Stacking")]
    [Tooltip("Can this power-up be selected multiple times?")]
    public bool canStack = true;
    [Tooltip("Maximum times this can be stacked. 0 = unlimited")]
    public int maxStacks = 0;

    /// <summary>
    /// Called when the player selects this power-up.
    /// Override this to implement the power-up effect.
    /// </summary>
    /// <param name="player">The player GameObject receiving the power-up</param>
    public abstract void Apply(GameObject player);

    /// <summary>
    /// Optional: Called when the power-up is removed (if your game supports that)
    /// </summary>
    /// <param name="player">The player GameObject</param>
    public virtual void Remove(GameObject player) { }

    /// <summary>
    /// Get the color associated with this power-up's rarity
    /// </summary>
    public Color GetRarityColor()
    {
        return rarity switch
        {
            PowerUpRarity.Common => new Color(0.7f, 0.7f, 0.7f),     // Gray
            PowerUpRarity.Uncommon => new Color(0.2f, 0.8f, 0.2f),   // Green
            PowerUpRarity.Rare => new Color(0.2f, 0.4f, 1f),         // Blue
            PowerUpRarity.Epic => new Color(0.6f, 0.2f, 0.8f),       // Purple
            PowerUpRarity.Legendary => new Color(1f, 0.8f, 0.2f),    // Gold
            _ => Color.white
        };
    }
}

public enum PowerUpRarity
{
    Common = 0,
    Uncommon = 1,
    Rare = 2,
    Epic = 3,
    Legendary = 4
}
