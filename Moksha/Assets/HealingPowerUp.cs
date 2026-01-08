using UnityEngine;

/// <summary>
/// ScriptableObject for Healing Power-Up configuration.
/// Defines how the healing ability scales with stacks.
/// </summary>
[CreateAssetMenu(fileName = "New Healing PowerUp", menuName = "PowerUps/Healing PowerUp")]
public class HealingPowerUp : PowerUp
{
    [Header("Healing Stats")]
    [Tooltip("Base healing amount per tick")]
    [SerializeField] private float baseHealAmount = 10f;

    [Tooltip("Base cooldown between heals (in seconds)")]
    [SerializeField] private float baseCooldown = 3f;

    [Header("Stack Scaling")]
    [Tooltip("Heal amount added per stack")]
    [SerializeField] private float healAmountPerStack = 5f;

    [Tooltip("Cooldown reduction per stack (in seconds)")]
    [SerializeField] private float cooldownReductionPerStack = 0.2f;

    [Tooltip("Minimum cooldown (prevents cooldown from going too low)")]
    [SerializeField] private float minCooldown = 0.5f;

    /// <summary>
    /// Get the heal amount for a given stack count
    /// </summary>
    public float GetHealAmount(int stacks)
    {
        return baseHealAmount + (healAmountPerStack * (stacks - 1));
    }

    /// <summary>
    /// Get the cooldown for a given stack count
    /// </summary>
    public float GetCooldown(int stacks)
    {
        float cooldown = baseCooldown - (cooldownReductionPerStack * (stacks - 1));
        return Mathf.Max(cooldown, minCooldown);
    }

    /// <summary>
    /// Apply the healing power-up to the player
    /// </summary>
    public override void Apply(GameObject player)
    {
        if (player == null) return;

        HealingAbility healingAbility = player.GetComponent<HealingAbility>();

        if (healingAbility == null)
        {
            // First time acquiring - add and initialize
            healingAbility = player.AddComponent<HealingAbility>();
            healingAbility.Initialize(this);
            Debug.Log($"[HealingPowerUp] Added healing ability to player");
        }
        else
        {
            // Already have it - add a stack
            healingAbility.AddStack();
            Debug.Log($"[HealingPowerUp] Healing ability stack increased to {healingAbility.CurrentStacks}");
        }
    }

    /// <summary>
    /// Get detailed description with current stack stats
    /// </summary>
    public string GetDetailedDescription(int currentStacks = 1)
    {
        float heal = GetHealAmount(currentStacks);
        float cd = GetCooldown(currentStacks);

        return $"<b>Stack {currentStacks}:</b>\n" +
               $"• Heal: {heal} HP\n" +
               $"• Cooldown: {cd:F1}s";
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (baseHealAmount < 0f) baseHealAmount = 0f;
        if (baseCooldown < 0.1f) baseCooldown = 0.1f;
        if (healAmountPerStack < 0f) healAmountPerStack = 0f;
        if (cooldownReductionPerStack < 0f) cooldownReductionPerStack = 0f;
        if (minCooldown < 0.1f) minCooldown = 0.1f;
    }

    [ContextMenu("Log Stack 1 Stats")]
    private void LogStack1() => Debug.Log($"Stack 1: {GetHealAmount(1)} HP, {GetCooldown(1):F1}s cooldown");

    [ContextMenu("Log Stack 5 Stats")]
    private void LogStack5() => Debug.Log($"Stack 5: {GetHealAmount(5)} HP, {GetCooldown(5):F1}s cooldown");

    [ContextMenu("Log Stack 10 Stats")]
    private void LogStack10() => Debug.Log($"Stack 10: {GetHealAmount(10)} HP, {GetCooldown(10):F1}s cooldown");
#endif
}