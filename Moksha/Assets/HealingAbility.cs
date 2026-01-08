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
        else
        {
            Debug.Log($"[HealingAbility] Found HealingVFX on: {healingVFX.gameObject.name}");
            // Make sure VFX GameObject is active
            if (!healingVFX.gameObject.activeSelf)
            {
                healingVFX.gameObject.SetActive(true);
            }
        }

        UpdateStats();
        cooldownTimer = cooldown; // Start ready to heal

        Debug.Log($"[HealingAbility] Initialized - Heal: {healAmount}, Cooldown: {cooldown}s, PowerUpData: {powerUpData != null}");
    }

    /// <summary>
    /// Ensure the ability has a valid power-up data reference
    /// Called before AddStack to prevent null reference issues
    /// </summary>
    public void EnsureInitialized(HealingPowerUp data)
    {
        if (powerUpData == null)
        {
            Debug.LogWarning("[HealingAbility] PowerUpData was null, reinitializing...");
            powerUpData = data;

            // Cache components if needed
            if (playerHealth == null)
            {
                playerHealth = GetComponent<PlayerHealth>();
            }

            if (healingVFX == null)
            {
                healingVFX = GetComponentInChildren<HealingVFX>(true);
                if (healingVFX != null && !healingVFX.gameObject.activeSelf)
                {
                    healingVFX.gameObject.SetActive(true);
                }
            }

            // If stacks were never set, default to 0 (will become 1 when AddStack is called)
            if (currentStacks == 0)
            {
                currentStacks = 0;
            }

            UpdateStats();
        }
    }

    /// <summary>
    /// Add a stack (called when power-up is acquired again)
    /// </summary>
    public void AddStack()
    {
        // Ensure we have cached components if Initialize wasn't called
        if (playerHealth == null)
        {
            playerHealth = GetComponent<PlayerHealth>();
        }

        if (healingVFX == null)
        {
            healingVFX = GetComponentInChildren<HealingVFX>(true);
            if (healingVFX != null && !healingVFX.gameObject.activeSelf)
            {
                healingVFX.gameObject.SetActive(true);
            }
        }

        currentStacks++;
        UpdateStats();

        Debug.Log($"[HealingAbility] Stack added - Total: {currentStacks}, Heal: {healAmount}, Cooldown: {cooldown}s");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateStats()
    {
        if (powerUpData == null)
        {
            Debug.LogError("[HealingAbility] UpdateStats called but powerUpData is NULL!");
            return;
        }

        healAmount = powerUpData.GetHealAmount(currentStacks);
        cooldown = powerUpData.GetCooldown(currentStacks);

        Debug.Log($"[HealingAbility] Stats updated - Stacks: {currentStacks}, Heal: {healAmount}, Cooldown: {cooldown}s");
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
        Debug.Log($"[HealingAbility] TryHeal called - Health: {playerHealth.CurrentHealth}/{playerHealth.MaxHealth}, Dead: {playerHealth.IsDead}, HealAmount: {healAmount}");

        // Don't heal if already at max health or dead
        if (playerHealth.IsDead || playerHealth.CurrentHealth >= playerHealth.MaxHealth)
        {
            Debug.Log("[HealingAbility] Skipping heal - player at max health or dead");
            return;
        }

        // Check if heal amount is valid
        if (healAmount <= 0f)
        {
            Debug.LogError($"[HealingAbility] HealAmount is {healAmount}! PowerUpData is {(powerUpData == null ? "NULL" : "valid")}");
            return;
        }

        // Reset cooldown
        cooldownTimer = 0f;

        // Heal the player
        playerHealth.Heal(healAmount);

        // Play healing VFX
        if (healingVFX != null)
        {
            Debug.Log("[HealingAbility] Playing healing VFX");
            healingVFX.PlayEffect();
        }
        else
        {
            Debug.LogWarning("[HealingAbility] HealingVFX is null!");
        }

        Debug.Log($"[HealingAbility] Healed player for {healAmount} HP - New Health: {playerHealth.CurrentHealth}");
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
    
    [ContextMenu("Debug Current State")]
    private void DebugState()
    {
        Debug.Log($"=== HealingAbility Debug ===");
        Debug.Log($"PowerUpData: {(powerUpData == null ? "NULL" : powerUpData.name)}");
        Debug.Log($"Current Stacks: {currentStacks}");
        Debug.Log($"Heal Amount: {healAmount}");
        Debug.Log($"Cooldown: {cooldown}s");
        Debug.Log($"PlayerHealth: {(playerHealth == null ? "NULL" : "Found")}");
        Debug.Log($"HealingVFX: {(healingVFX == null ? "NULL" : healingVFX.gameObject.name)}");
    }
#endif
}