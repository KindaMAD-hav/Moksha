using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Optimized level-up UI with card pooling.
/// Uses object pooling instead of Instantiate/Destroy.
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

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip levelUpSound;
    [SerializeField] private AudioClip selectSound;

    // Object pool for cards
    private PowerUpCard[] cardPool;
    private int activeCardCount;
    
    // Track acquired power-ups
    private Dictionary<PowerUp, int> acquiredPowerUps;
    private int pendingLevelUps;

    // Cached
    private bool hasAudioSource;

    public bool IsShowing => panel != null && panel.activeSelf;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Pre-allocate
        acquiredPowerUps = new Dictionary<PowerUp, int>(32);
        
        // Create card pool
        cardPool = new PowerUpCard[cardsToShow];
        for (int i = 0; i < cardsToShow; i++)
        {
            PowerUpCard card = Instantiate(cardPrefab, cardContainer);
            card.gameObject.SetActive(false);
            cardPool[i] = card;
        }

        if (panel != null)
            panel.SetActive(false);
            
        hasAudioSource = audioSource != null;
    }

    private void Start()
    {
        if (playerObject == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerObject = player;
        }

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
        if (!IsShowing)
            ShowLevelUpPanel();
    }

    public void ShowLevelUpPanel()
    {
        if (powerUpDatabase == null) return;

        // Hide all cards first
        HideAllCards();

        // Get random power-ups
        List<PowerUp> choices = powerUpDatabase.GetRandomPowerUps(cardsToShow, acquiredPowerUps);
        if (choices.Count == 0)
        {
            pendingLevelUps = 0;
            return;
        }

        // Setup cards from pool
        activeCardCount = choices.Count;
        for (int i = 0; i < activeCardCount; i++)
        {
            cardPool[i].Setup(choices[i], OnCardSelected);
            cardPool[i].gameObject.SetActive(true);
        }

        panel.SetActive(true);
        Time.timeScale = 0f;

        if (hasAudioSource && levelUpSound != null)
            audioSource.PlayOneShot(levelUpSound);
    }

    private void OnCardSelected(PowerUp selectedPowerUp)
    {
        if (selectedPowerUp == null) return;

        if (playerObject != null)
            selectedPowerUp.Apply(playerObject);

        // Track acquisition
        if (acquiredPowerUps.TryGetValue(selectedPowerUp, out int count))
            acquiredPowerUps[selectedPowerUp] = count + 1;
        else
            acquiredPowerUps[selectedPowerUp] = 1;

        if (hasAudioSource && selectSound != null)
            audioSource.PlayOneShot(selectSound);

        pendingLevelUps--;

        if (pendingLevelUps > 0)
            ShowLevelUpPanel();
        else
            HidePanel();
    }

    private void HidePanel()
    {
        HideAllCards();
        panel.SetActive(false);
        Time.timeScale = 1f;
    }

    private void HideAllCards()
    {
        for (int i = 0; i < cardPool.Length; i++)
        {
            if (cardPool[i] != null)
                cardPool[i].gameObject.SetActive(false);
        }
        activeCardCount = 0;
    }

    public int GetPowerUpStacks(PowerUp powerUp)
    {
        return acquiredPowerUps.TryGetValue(powerUp, out int stacks) ? stacks : 0;
    }

    public void ResetPowerUps()
    {
        acquiredPowerUps.Clear();
        pendingLevelUps = 0;
    }
}
