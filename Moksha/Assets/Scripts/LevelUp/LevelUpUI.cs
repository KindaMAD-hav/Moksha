using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the level-up UI panel that displays power-up cards.
/// Pauses the game when shown and resumes when a selection is made.
/// </summary>
public class LevelUpUI : MonoBehaviour
{
    public static LevelUpUI Instance { get; private set; }

    [Header("References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Transform cardContainer;
    [SerializeField] private PowerUpCard cardPrefab;
    [SerializeField] private PowerUpDatabase powerUpDatabase;

    [Header("Settings")]
    [SerializeField] private int cardsToShow = 3;
    [SerializeField] private GameObject playerObject;

    [Header("Audio (Optional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip levelUpSound;
    [SerializeField] private AudioClip selectSound;

    // Track acquired power-ups and their stack counts
    private Dictionary<PowerUp, int> acquiredPowerUps = new Dictionary<PowerUp, int>();
    private List<PowerUpCard> activeCards = new List<PowerUpCard>();
    private int pendingLevelUps = 0;

    public bool IsShowing => panel != null && panel.activeSelf;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (panel != null)
            panel.SetActive(false);
    }

    private void Start()
    {
        // Auto-find player if not assigned
        if (playerObject == null)
            playerObject = GameObject.FindGameObjectWithTag("Player");

        // Subscribe to level up events
        if (ExperienceManager.Instance != null)
            ExperienceManager.Instance.OnLevelUp += HandleLevelUp;
    }

    private void OnDestroy()
    {
        if (ExperienceManager.Instance != null)
            ExperienceManager.Instance.OnLevelUp -= HandleLevelUp;
    }

    private void HandleLevelUp(int newLevel)
    {
        pendingLevelUps++;
        
        // Only show if not already showing
        if (!IsShowing)
            ShowLevelUpPanel();
    }

    /// <summary>
    /// Shows the level-up panel with random power-up choices.
    /// </summary>
    public void ShowLevelUpPanel()
    {
        if (powerUpDatabase == null)
        {
            Debug.LogError("[LevelUpUI] PowerUpDatabase not assigned!");
            return;
        }

        // Clear existing cards
        ClearCards();

        // Get random power-ups
        List<PowerUp> choices = powerUpDatabase.GetRandomPowerUps(cardsToShow, acquiredPowerUps);

        if (choices.Count == 0)
        {
            Debug.LogWarning("[LevelUpUI] No power-ups available to show!");
            pendingLevelUps = 0;
            return;
        }

        // Create cards
        foreach (var powerUp in choices)
        {
            PowerUpCard card = Instantiate(cardPrefab, cardContainer);
            card.Setup(powerUp, OnCardSelected);
            activeCards.Add(card);
        }

        // Show panel and pause game
        panel.SetActive(true);
        Time.timeScale = 0f;

        // Play sound
        if (audioSource != null && levelUpSound != null)
            audioSource.PlayOneShot(levelUpSound);
    }

    private void OnCardSelected(PowerUp selectedPowerUp)
    {
        if (selectedPowerUp == null) return;

        // Apply the power-up
        if (playerObject != null)
            selectedPowerUp.Apply(playerObject);

        // Track acquisition
        if (acquiredPowerUps.ContainsKey(selectedPowerUp))
            acquiredPowerUps[selectedPowerUp]++;
        else
            acquiredPowerUps[selectedPowerUp] = 1;

        Debug.Log($"[LevelUpUI] Selected: {selectedPowerUp.powerUpName}");

        // Play sound
        if (audioSource != null && selectSound != null)
            audioSource.PlayOneShot(selectSound);

        // Handle pending level ups
        pendingLevelUps--;

        if (pendingLevelUps > 0)
        {
            // More level ups pending, show again
            ShowLevelUpPanel();
        }
        else
        {
            // Hide panel and resume game
            HidePanel();
        }
    }

    private void HidePanel()
    {
        ClearCards();
        panel.SetActive(false);
        Time.timeScale = 1f;
    }

    private void ClearCards()
    {
        foreach (var card in activeCards)
        {
            if (card != null)
                Destroy(card.gameObject);
        }
        activeCards.Clear();
    }

    /// <summary>
    /// Get the current stack count for a power-up
    /// </summary>
    public int GetPowerUpStacks(PowerUp powerUp)
    {
        return acquiredPowerUps.TryGetValue(powerUp, out int stacks) ? stacks : 0;
    }

    /// <summary>
    /// Get all acquired power-ups
    /// </summary>
    public Dictionary<PowerUp, int> GetAcquiredPowerUps()
    {
        return new Dictionary<PowerUp, int>(acquiredPowerUps);
    }

    /// <summary>
    /// Reset all acquired power-ups (for new game)
    /// </summary>
    public void ResetPowerUps()
    {
        acquiredPowerUps.Clear();
        pendingLevelUps = 0;
    }

#if UNITY_EDITOR
    [ContextMenu("Test Show Panel")]
    public void TestShowPanel()
    {
        ShowLevelUpPanel();
    }
#endif
}
