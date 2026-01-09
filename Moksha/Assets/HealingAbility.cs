using System.Runtime.CompilerServices;
using UnityEngine;

/// <summary>
/// Healing Ability - attached to player when they acquire the power-up.
/// Enables HealingVFX and heals the player every cooldown in HEART ticks.
/// </summary>
public class HealingAbility : MonoBehaviour
{
    [Header("Runtime Stats (from PowerUp)")]
    [SerializeField] private int heartsPerTick = 1;
    [SerializeField] private float cooldown;
    [SerializeField] private int currentStacks;

    [Header("Debug")]
    [SerializeField] private float cooldownTimer;

    private HealingPowerUp powerUpData;

    private PlayerHealth playerHealth;
    private HealingVFX healingVFX;

    public int CurrentStacks => currentStacks;

    public void Initialize(HealingPowerUp data)
    {
        powerUpData = data;
        currentStacks = 1;

        playerHealth = GetComponent<PlayerHealth>();
        if (playerHealth == null)
        {
            Debug.LogError("[HealingAbility] No PlayerHealth component found!");
            enabled = false;
            return;
        }

        // Enable the VFX object around player when ability is acquired
        healingVFX = GetComponentInChildren<HealingVFX>(true);
        if (healingVFX != null && !healingVFX.gameObject.activeSelf)
            healingVFX.gameObject.SetActive(true);

        UpdateStats();

        // Start ready (first heal happens after cooldown, unless you want immediate heal)
        cooldownTimer = 0f;
    }

    public void EnsureInitialized(HealingPowerUp data)
    {
        if (powerUpData != null) return;

        Debug.LogWarning("[HealingAbility] PowerUpData was null, reinitializing...");
        powerUpData = data;

        if (playerHealth == null)
            playerHealth = GetComponent<PlayerHealth>();

        if (healingVFX == null)
        {
            healingVFX = GetComponentInChildren<HealingVFX>(true);
            if (healingVFX != null && !healingVFX.gameObject.activeSelf)
                healingVFX.gameObject.SetActive(true);
        }

        // If this component existed without init, stacks may still be 0
        if (currentStacks < 0) currentStacks = 0;

        UpdateStats();
    }

    public void AddStack()
    {
        if (playerHealth == null) playerHealth = GetComponent<PlayerHealth>();

        if (healingVFX == null)
        {
            healingVFX = GetComponentInChildren<HealingVFX>(true);
            if (healingVFX != null && !healingVFX.gameObject.activeSelf)
                healingVFX.gameObject.SetActive(true);
        }

        currentStacks++;
        UpdateStats();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateStats()
    {
        if (powerUpData == null)
        {
            Debug.LogError("[HealingAbility] UpdateStats called but powerUpData is NULL!");
            return;
        }

        heartsPerTick = powerUpData.GetHeartsPerTick(currentStacks);
        cooldown = powerUpData.GetCooldown(currentStacks);
    }

    private void Update()
    {
        if (powerUpData == null || playerHealth == null) return;

        cooldownTimer += Time.deltaTime;

        if (cooldownTimer >= cooldown)
            TryHeal();
    }

    private void TryHeal()
    {
        // Don't heal if already at max hearts or dead
        if (playerHealth.IsDead || playerHealth.CurrentHearts >= playerHealth.MaxHearts)
            return;

        cooldownTimer = 0f;

        for (int i = 0; i < heartsPerTick; i++)
            playerHealth.HealOneHeart();

        if (healingVFX != null)
            healingVFX.PlayEffect();
    }

    public void ForceHeal()
    {
        cooldownTimer = cooldown;
        TryHeal();
    }

#if UNITY_EDITOR
    [ContextMenu("Force Heal Now")]
    private void ForceHealTest() => ForceHeal();
#endif
}
