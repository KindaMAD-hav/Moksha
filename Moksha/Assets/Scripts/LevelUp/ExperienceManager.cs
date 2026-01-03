using System;
using UnityEngine;

/// <summary>
/// Manages player experience, levels, and triggers level-up events.
/// Attach to a persistent game object (e.g., GameManager or Player).
/// </summary>
public class ExperienceManager : MonoBehaviour
{
    public static ExperienceManager Instance { get; private set; }

    [Header("Experience Settings")]
    [SerializeField] private int baseXPToLevel = 100;
    [SerializeField] private float xpScalingFactor = 1.5f; // Each level requires more XP

    [Header("Current State")]
    [SerializeField] private int currentLevel = 1;
    [SerializeField] private int currentXP = 0;
    [SerializeField] private int xpToNextLevel = 100;

    [Header("Debug")]
    [SerializeField] private bool debugMode = true;

    // Events for other systems to subscribe to
    public event Action<int> OnXPGained;           // Passes amount gained
    public event Action<int> OnLevelUp;            // Passes new level
    public event Action<int, int> OnXPChanged;     // Passes current XP and XP to next level

    public int CurrentLevel => currentLevel;
    public int CurrentXP => currentXP;
    public int XPToNextLevel => xpToNextLevel;
    public float XPProgress => (float)currentXP / xpToNextLevel;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        CalculateXPToNextLevel();
    }

    /// <summary>
    /// Add experience points. Call this when killing enemies.
    /// </summary>
    /// <param name="amount">Amount of XP to add</param>
    public void AddXP(int amount)
    {
        if (amount <= 0) return;

        currentXP += amount;
        OnXPGained?.Invoke(amount);
        OnXPChanged?.Invoke(currentXP, xpToNextLevel);

        if (debugMode)
            Debug.Log($"[XP] Gained {amount} XP. Total: {currentXP}/{xpToNextLevel}");

        // Check for level up (can level up multiple times if enough XP)
        while (currentXP >= xpToNextLevel)
        {
            LevelUp();
        }
    }

    private void LevelUp()
    {
        currentXP -= xpToNextLevel;
        currentLevel++;
        CalculateXPToNextLevel();

        if (debugMode)
            Debug.Log($"[LEVEL UP] Now level {currentLevel}! Next level requires {xpToNextLevel} XP");

        OnLevelUp?.Invoke(currentLevel);
        OnXPChanged?.Invoke(currentXP, xpToNextLevel);
    }

    private void CalculateXPToNextLevel()
    {
        // Formula: baseXP * (scalingFactor ^ (level - 1))
        xpToNextLevel = Mathf.RoundToInt(baseXPToLevel * Mathf.Pow(xpScalingFactor, currentLevel - 1));
    }

    /// <summary>
    /// Reset to level 1 with 0 XP (for new game/restart)
    /// </summary>
    public void ResetProgress()
    {
        currentLevel = 1;
        currentXP = 0;
        CalculateXPToNextLevel();
        OnXPChanged?.Invoke(currentXP, xpToNextLevel);
    }

#if UNITY_EDITOR
    [Header("Inspector Testing")]
    [SerializeField] private int testXPAmount = 50;

    [ContextMenu("Add Test XP")]
    public void AddTestXP()
    {
        AddXP(testXPAmount);
    }

    [ContextMenu("Force Level Up")]
    public void ForceLevelUp()
    {
        AddXP(xpToNextLevel - currentXP);
    }

    [ContextMenu("Reset Progress")]
    public void ResetProgressFromMenu()
    {
        ResetProgress();
        Debug.Log("[XP] Progress reset to Level 1");
    }
#endif
}
