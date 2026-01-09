using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Optimized level-up UI with card pooling and animations.
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
    [SerializeField] private float staggerDelay = 0.1f;

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
    private LayoutGroup layoutGroup;

    public bool IsShowing => panel != null && panel.activeSelf;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        acquiredPowerUps = new Dictionary<PowerUp, int>(32);
        
        // Get layout group if present
        if (cardContainer != null)
            layoutGroup = cardContainer.GetComponent<LayoutGroup>();
        
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

        HideAllCards();

        List<PowerUp> choices = powerUpDatabase.GetRandomPowerUps(cardsToShow, acquiredPowerUps);
        if (choices.Count == 0)
        {
            pendingLevelUps = 0;
            return;
        }

        // Show panel first so layout can work
        panel.SetActive(true);

        var cam = Camera.main.GetComponent<IsometricCameraFollow>();
        if (cam != null)
            cam.ResetCameraImmediate();

        Time.timeScale = 0f;

        // Activate cards and set them up (hidden initially)
        activeCardCount = choices.Count;
        for (int i = 0; i < activeCardCount; i++)
        {
            cardPool[i].ResetCard();
            cardPool[i].gameObject.SetActive(true);
            cardPool[i].Setup(choices[i], OnCardSelected);
            cardPool[i].PrepareForAnimation(); // Hide until animation starts
        }

        // Start coroutine to animate after layout
        StartCoroutine(AnimateCardsAfterLayout());

        if (hasAudioSource && levelUpSound != null)
            audioSource.PlayOneShot(levelUpSound);
    }

    private IEnumerator AnimateCardsAfterLayout()
    {
        // Force layout rebuild
        if (layoutGroup != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(cardContainer as RectTransform);
        }
        
        // Wait one frame for layout to fully complete
        yield return null;

        // Now capture each card's position and start animations
        for (int i = 0; i < activeCardCount; i++)
        {
            PowerUpCard card = cardPool[i];
            if (card != null && card.RectTransform != null)
            {
                // Capture the position layout gave this card
                Vector3 targetPos = card.RectTransform.localPosition;
                
                // Start animation with stagger delay
                float delay = i * staggerDelay;
                card.PlayEntryAnimation(targetPos, delay);
            }
        }
    }

    private void OnCardSelected(PowerUp selectedPowerUp)
    {
        if (selectedPowerUp == null) return;

        if (playerObject != null)
            selectedPowerUp.Apply(playerObject);

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
        var cam = Camera.main.GetComponent<IsometricCameraFollow>();
        if (cam != null)
            cam.MarkForReset();


        Time.timeScale = 1f;
    }

    private void HideAllCards()
    {
        for (int i = 0; i < cardPool.Length; i++)
        {
            if (cardPool[i] != null)
            {
                cardPool[i].ResetCard();
                cardPool[i].gameObject.SetActive(false);
            }
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
