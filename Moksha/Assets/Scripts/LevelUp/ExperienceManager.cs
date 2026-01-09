using System;
using UnityEngine;

/// <summary>
/// Optimized experience manager. Minimal overhead, no allocations during gameplay.
/// </summary>
public class ExperienceManager : MonoBehaviour
{
    public static ExperienceManager Instance { get; private set; }

    [Header("Experience Settings")]
    [SerializeField] private int baseXPToLevel = 100;
    [SerializeField] private float xpScalingFactor = 1.5f;

    [Header("Current State")]
    [SerializeField] private int currentLevel = 1;
    [SerializeField] private int currentXP = 0;
    [SerializeField] private int xpToNextLevel = 100;

    // Events
    public event Action<int> OnXPGained;
    public event Action<int> OnLevelUp;
    public event Action<int, int> OnXPChanged;

    // Cached values
    private float cachedXPProgress;

    public int CurrentLevel => currentLevel;
    public int CurrentXP => currentXP;
    public int XPToNextLevel => xpToNextLevel;
    public float XPProgress => cachedXPProgress;

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

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public void AddXP(int amount)
    {
        if (amount <= 0) return;

        currentXP += amount;
        
        OnXPGained?.Invoke(amount);

        // Check for level up
        while (currentXP >= xpToNextLevel)
        {
            LevelUp();
        }
        
        UpdateProgress();
        OnXPChanged?.Invoke(currentXP, xpToNextLevel);
    }

    private void LevelUp()
    {
        currentXP -= xpToNextLevel;
        currentLevel++;
        CalculateXPToNextLevel();
        OnLevelUp?.Invoke(currentLevel);
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    private void CalculateXPToNextLevel()
    {
        // Faster than Mathf.Pow for small exponents
        float multiplier = 1f;
        for (int i = 1; i < currentLevel; i++)
            multiplier *= xpScalingFactor;
        
        xpToNextLevel = (int)(baseXPToLevel * multiplier + 0.5f);
        UpdateProgress();
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    private void UpdateProgress()
    {
        cachedXPProgress = (float)currentXP / xpToNextLevel;
    }

    public void ResetProgress()
    {
        currentLevel = 1;
        currentXP = 0;
        CalculateXPToNextLevel();
        OnXPChanged?.Invoke(currentXP, xpToNextLevel);
    }

#if UNITY_EDITOR
    [Header("Testing")]
    [SerializeField] private int testXPAmount = 50;

    [ContextMenu("Add Test XP")]
    public void AddTestXP() => AddXP(testXPAmount);

    [ContextMenu("Force Level Up")]
    public void ForceLevelUp() => AddXP(xpToNextLevel - currentXP);

    [ContextMenu("Reset Progress")]
    public void ResetProgressFromMenu()
    {
        ResetProgress();
        Debug.Log("[XP] Progress reset");
    }
#endif
}
