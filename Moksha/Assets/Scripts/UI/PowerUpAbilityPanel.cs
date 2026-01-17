using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Main controller for the powerup ability panel.
/// Displays all powerups with grayscale icons when not owned,
/// and colored icons with stack counts when owned.
/// </summary>
public class PowerUpAbilityPanel : MonoBehaviour
{
    [System.Serializable]
    public class PowerUpDisplayData
    {
        [Tooltip("Reference to the PowerUp ScriptableObject")]
        public PowerUp powerUp;

        [Tooltip("Black & white sprite shown when player doesn't have this powerup")]
        public Sprite grayscaleSprite;

        [Tooltip("Colored sprite shown when player has this powerup")]
        public Sprite coloredSprite;
    }

    [Header("Display Configuration")]
    [Tooltip("Array of powerups to display in the panel. Assign sprites for each.")]
    [SerializeField] private PowerUpDisplayData[] powerUpDisplays;

    [Header("UI References")]
    [Tooltip("Prefab for individual powerup slots")]
    [SerializeField] private PowerUpSlotUI slotPrefab;

    [Tooltip("Parent transform for slots (should have HorizontalLayoutGroup)")]
    [SerializeField] private Transform slotContainer;

    // Runtime mapping: PowerUp -> Slot
    private Dictionary<PowerUp, PowerUpSlotUI> powerUpToSlot;

    // Cached reference
    private LevelUpUI levelUpUI;

    private void Awake()
    {
        powerUpToSlot = new Dictionary<PowerUp, PowerUpSlotUI>(powerUpDisplays?.Length ?? 8);
    }

    private void Start()
    {
        // Cache LevelUpUI reference
        levelUpUI = LevelUpUI.Instance;

        // Create slots for all configured powerups
        CreateSlots();

        // Subscribe to powerup acquisition events
        if (levelUpUI != null)
        {
            levelUpUI.OnPowerUpAcquired += HandlePowerUpAcquired;
        }

        // Initial sync in case player already has powerups
        SyncAllSlots();
    }

    private void OnDestroy()
    {
        if (levelUpUI != null)
        {
            levelUpUI.OnPowerUpAcquired -= HandlePowerUpAcquired;
        }
    }

    /// <summary>
    /// Creates UI slots for all configured powerups.
    /// </summary>
    private void CreateSlots()
    {
        if (powerUpDisplays == null || slotPrefab == null || slotContainer == null)
        {
            Debug.LogWarning("[PowerUpAbilityPanel] Missing required references!");
            return;
        }

        for (int i = 0; i < powerUpDisplays.Length; i++)
        {
            PowerUpDisplayData data = powerUpDisplays[i];

            if (data.powerUp == null)
            {
                Debug.LogWarning($"[PowerUpAbilityPanel] PowerUp at index {i} is null, skipping.");
                continue;
            }

            // Instantiate slot
            PowerUpSlotUI slot = Instantiate(slotPrefab, slotContainer);
            slot.name = $"Slot_{data.powerUp.powerUpName}";

            // Initialize with sprites
            slot.Initialize(data.grayscaleSprite, data.coloredSprite);

            // Map for quick lookup
            powerUpToSlot[data.powerUp] = slot;
        }
    }

    /// <summary>
    /// Called when player acquires a powerup.
    /// </summary>
    private void HandlePowerUpAcquired(PowerUp powerUp, int newStackCount)
    {
        if (powerUp == null)
            return;

        if (powerUpToSlot.TryGetValue(powerUp, out PowerUpSlotUI slot))
        {
            slot.SetStacks(newStackCount);
        }
    }

    /// <summary>
    /// Syncs all slots with current powerup stacks from LevelUpUI.
    /// Call this on start or when needing a full refresh.
    /// </summary>
    public void SyncAllSlots()
    {
        if (levelUpUI == null)
        {
            levelUpUI = LevelUpUI.Instance;
            if (levelUpUI == null)
                return;
        }

        foreach (var kvp in powerUpToSlot)
        {
            PowerUp powerUp = kvp.Key;
            PowerUpSlotUI slot = kvp.Value;

            int stacks = levelUpUI.GetPowerUpStacks(powerUp);
            slot.SetStacks(stacks);
        }
    }

    /// <summary>
    /// Resets all slots to 0 stacks (grayscale).
    /// </summary>
    public void ResetAllSlots()
    {
        foreach (var slot in powerUpToSlot.Values)
        {
            slot.ResetSlot();
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Sync Slots Now")]
    private void EditorSyncSlots()
    {
        if (Application.isPlaying)
            SyncAllSlots();
    }

    [ContextMenu("Reset All Slots")]
    private void EditorResetSlots()
    {
        if (Application.isPlaying)
            ResetAllSlots();
    }
#endif
}
