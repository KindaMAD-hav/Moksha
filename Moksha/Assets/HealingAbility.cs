using System.Runtime.CompilerServices;
using UnityEngine;

/// <summary>
/// Healing Ability - Attached to player when they acquire the power-up.
/// Periodically heals the player over time.
/// </summary>
public class HealingAbility : MonoBehaviour
{
    [Header("Runtime Stats (from PowerUp)")]
    [SerializeField] private float healAmount;
    [SerializeField] private float cooldown;
    [SerializeField] private int currentStacks;

    [Header("Debug")]
    [SerializeField] private float cooldownTimer;

    // Reference to power-up data
    private HealingPowerUp powerUpData;

    // Cached components
    private PlayerHealth playerHealth;
    private HealingVFX healingVFX;

    public int CurrentStacks => currentStacks;

    /// <summary>
    /// Initialize with power-up data (called when first acquired)
    /// </summary>
    public void Initialize(HealingPowerUp data)
    {
        powerUpData = data;
        currentStacks = 1;

        // Get player health component
        playerHealth = GetComponent<PlayerHealth>();
        if (playerHealth == null)
        {
            Debug.LogError("[HealingAbility] No PlayerHealth component found!");
            enabled = false;
            return;
        }

        // Find the healing VFX (should be a child of the player)
        healingVFX = GetComponentInChildren<HealingVFX>(true);
        if (healingVFX == null)
        {
            Debug.LogWarning("[HealingAbility] No HealingVFX found in children!");
        }

        UpdateStats();
        cooldownTimer = cooldown; // Start ready to heal
    }

    /// <summary>
    /// Add a stack (called when power-up is acquired again)
    /// </summary>
    public void AddStack()
    {
        currentStacks++;
        UpdateStats();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateStats()
    {
        if (powerUpData == null) return;

        healAmount = powerUpData.GetHealAmount(currentStacks);
        cooldown = powerUpData.GetCooldown(currentStacks);
    }

    private void Update()
    {
        if (powerUpData == null || playerHealth == null) return;

        cooldownTimer += Time.deltaTime;

        if (cooldownTimer >= cooldown)
        {
            TryHeal();
        }
    }

    private void TryHeal()
    {
        // Don't heal if already at max health or dead
        if (playerHealth.IsDead || playerHealth.CurrentHealth >= playerHealth.MaxHealth)
        {
            return;
        }

        // Reset cooldown
        cooldownTimer = 0f;

        // Heal the player
        playerHealth.Heal(healAmount);

        // Play healing VFX
        if (healingVFX != null)
        {
            healingVFX.PlayEffect();
        }

        Debug.Log($"[HealingAbility] Healed player for {healAmount} HP");
    }

    /// <summary>
    /// Force a heal (for testing)
    /// </summary>
    public void ForceHeal()
    {
        cooldownTimer = cooldown;
        TryHeal();
    }

#if UNITY_EDITOR
    [ContextMenu("Force Heal Now")]
    private void ForceHealTest()
    {
        ForceHeal();
    }
#endif
}