using UnityEngine;

/// <summary>
/// Lightning Strike Power-Up - When acquired, periodically strikes nearby enemies with lightning.
/// Stacks increase damage, strike frequency, or chain count.
/// </summary>
[CreateAssetMenu(fileName = "LightningStrikePowerUp", menuName = "PowerUps/Lightning Strike")]
public class LightningStrikePowerUp : PowerUp
{
    [Header("Lightning Settings")]
    [Tooltip("Base damage per lightning strike")]
    public float baseDamage = 25f;
    
    [Tooltip("Damage increase per stack")]
    public float damagePerStack = 10f;
    
    [Tooltip("Base cooldown between strikes (seconds)")]
    public float baseCooldown = 3f;
    
    [Tooltip("Cooldown reduction per stack (seconds)")]
    public float cooldownReductionPerStack = 0.3f;
    
    [Tooltip("Minimum cooldown (can't go below this)")]
    public float minCooldown = 0.5f;
    
    [Tooltip("Base range to detect enemies")]
    public float baseRange = 10f;
    
    [Tooltip("Range increase per stack")]
    public float rangePerStack = 2f;
    
    [Tooltip("Number of enemies to strike at once (base)")]
    public int baseTargetCount = 1;
    
    [Tooltip("Extra targets per N stacks (0 = no extra targets)")]
    public int stacksPerExtraTarget = 3;

    [Header("AOE Settings")]
    public bool enableAOE = true;
    public float aoeRadius = 3f;
    public float aoeDamageMultiplier = 0.6f;


    public override void Apply(GameObject player)
    {
        // Get or add the lightning ability component
        LightningStrikeAbility ability = player.GetComponent<LightningStrikeAbility>();
        
        if (ability == null)
        {
            ability = player.AddComponent<LightningStrikeAbility>();
            ability.Initialize(this);
        }
        else
        {
            ability.AddStack();
        }
        
        Debug.Log($"[PowerUp] Lightning Strike applied! Stacks: {ability.CurrentStacks}");
    }
    
    /// <summary>
    /// Calculate damage for given stack count
    /// </summary>
    public float GetDamage(int stacks)
    {
        return baseDamage + damagePerStack * (stacks - 1);
    }
    
    /// <summary>
    /// Calculate cooldown for given stack count
    /// </summary>
    public float GetCooldown(int stacks)
    {
        float cooldown = baseCooldown - cooldownReductionPerStack * (stacks - 1);
        return Mathf.Max(cooldown, minCooldown);
    }
    
    /// <summary>
    /// Calculate range for given stack count
    /// </summary>
    public float GetRange(int stacks)
    {
        return baseRange + rangePerStack * (stacks - 1);
    }
    
    /// <summary>
    /// Calculate target count for given stack count
    /// </summary>
    public int GetTargetCount(int stacks)
    {
        if (stacksPerExtraTarget <= 0) return baseTargetCount;
        return baseTargetCount + (stacks - 1) / stacksPerExtraTarget;
    }
}
