using UnityEngine;

/// <summary>
/// ScriptableObject for Healing Power-Up configuration.
/// Heals the player in HEARTS per tick.
/// </summary>
[CreateAssetMenu(fileName = "New Healing PowerUp", menuName = "PowerUps/Healing PowerUp")]
public class HealingPowerUp : PowerUp
{
    [Header("Heart Healing")]
    [SerializeField] private int baseHeartsPerTick = 1;
    [SerializeField] private int extraHeartsPerStack = 0;

    [Header("Cooldown")]
    [Tooltip("Base cooldown between heals (in seconds)")]
    [SerializeField] private float baseCooldown = 3f;

    [Tooltip("Cooldown reduction per stack (in seconds)")]
    [SerializeField] private float cooldownReductionPerStack = 0.2f;

    [Tooltip("Minimum cooldown (prevents cooldown from going too low)")]
    [SerializeField] private float minCooldown = 0.5f;

    public int GetHeartsPerTick(int stacks)
    {
        if (stacks < 1) stacks = 1;
        return baseHeartsPerTick + extraHeartsPerStack * (stacks - 1);
    }

    public float GetCooldown(int stacks)
    {
        if (stacks < 1) stacks = 1;
        float cd = baseCooldown - (cooldownReductionPerStack * (stacks - 1));
        return Mathf.Max(cd, minCooldown);
    }

    public override void Apply(GameObject player)
    {
        if (player == null)
        {
            Debug.LogError("[HealingPowerUp] Cannot apply - player is null!");
            return;
        }

        HealingAbility healingAbility = player.GetComponent<HealingAbility>();

        if (healingAbility == null)
        {
            healingAbility = player.AddComponent<HealingAbility>();
            healingAbility.Initialize(this);
        }
        else
        {
            healingAbility.EnsureInitialized(this);
            healingAbility.AddStack();
        }
    }

    public string GetDetailedDescription(int currentStacks = 1)
    {
        int hearts = GetHeartsPerTick(currentStacks);
        float cd = GetCooldown(currentStacks);

        return $"<b>Stack {currentStacks}:</b>\n" +
               $"• Heal: {hearts} heart(s)\n" +
               $"• Cooldown: {cd:F1}s";
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (baseHeartsPerTick < 0) baseHeartsPerTick = 0;
        if (extraHeartsPerStack < 0) extraHeartsPerStack = 0;

        if (baseCooldown < 0.1f) baseCooldown = 0.1f;
        if (cooldownReductionPerStack < 0f) cooldownReductionPerStack = 0f;
        if (minCooldown < 0.1f) minCooldown = 0.1f;
    }

    [ContextMenu("Log Stack 1 Stats")]
    private void LogStack1() => Debug.Log($"Stack 1: {GetHeartsPerTick(1)} hearts, {GetCooldown(1):F1}s cooldown");

    [ContextMenu("Log Stack 5 Stats")]
    private void LogStack5() => Debug.Log($"Stack 5: {GetHeartsPerTick(5)} hearts, {GetCooldown(5):F1}s cooldown");

    [ContextMenu("Log Stack 10 Stats")]
    private void LogStack10() => Debug.Log($"Stack 10: {GetHeartsPerTick(10)} hearts, {GetCooldown(10):F1}s cooldown");
#endif
}
