using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Minecraft-style hearts UI (no half hearts).
/// Built on top of PlayerHealth.OnHealthChanged.
/// </summary>
public class PlayerHeartsUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Transform heartsContainer;
    [SerializeField] private Image heartPrefab; // prefab must be an Image

    [Header("Sprites")]
    [SerializeField] private Sprite fullHeartSprite;
    [SerializeField] private Sprite emptyHeartSprite;

    [Header("Hearts Mapping")]
    [Tooltip("If ON: max hearts are derived from maxHealth / healthPerHeart. If OFF: uses fixedMaxHearts.")]
    [SerializeField] private bool deriveMaxHeartsFromMaxHealth = false;

    [Tooltip("Used when deriveMaxHeartsFromMaxHealth is OFF.")]
    [SerializeField] private int fixedMaxHearts = 10;

    [Tooltip("Used when deriveMaxHeartsFromMaxHealth is ON. Example: 10 health = 1 heart.")]
    [SerializeField] private float healthPerHeart = 10f;

    // runtime
    private readonly List<Image> hearts = new List<Image>(32);
    private int lastMaxHearts = -1;
    private int lastCurrentHearts = -1;

    void Awake()
    {
        // Auto-find PlayerHealth like your health bar does. :contentReference[oaicite:1]{index=1}
        if (playerHealth == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerHealth = player.GetComponent<PlayerHealth>();
        }

        if (heartsContainer == null) heartsContainer = transform;
    }

    void OnEnable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged += OnHealthChanged;
            OnHealthChanged(playerHealth.CurrentHealth, playerHealth.MaxHealth);
        }
    }

    void OnDisable()
    {
        if (playerHealth != null)
            playerHealth.OnHealthChanged -= OnHealthChanged;
    }

    void OnHealthChanged(float current, float max)
    {
        int maxHearts = GetMaxHearts(max);
        float valuePerHeart = GetValuePerHeart(max, maxHearts);

        int currentHearts = ToFullHearts(current, valuePerHeart, maxHearts);

        // Rebuild if max hearts changed
        if (maxHearts != lastMaxHearts)
        {
            RebuildHearts(maxHearts);
            lastMaxHearts = maxHearts;
            lastCurrentHearts = -1; // force refresh
        }

        // Only repaint if current hearts changed
        if (currentHearts != lastCurrentHearts)
        {
            UpdateHeartSprites(currentHearts, maxHearts);
            lastCurrentHearts = currentHearts;
        }
    }

    int GetMaxHearts(float maxHealth)
    {
        if (deriveMaxHeartsFromMaxHealth)
        {
            float hph = Mathf.Max(0.001f, healthPerHeart);
            return Mathf.Max(1, Mathf.CeilToInt(maxHealth / hph));
        }

        return Mathf.Max(1, fixedMaxHearts);
    }

    float GetValuePerHeart(float maxHealth, int maxHearts)
    {
        if (deriveMaxHeartsFromMaxHealth)
            return Mathf.Max(0.001f, healthPerHeart);

        // Fixed number of hearts represents maxHealth evenly
        return maxHearts > 0 ? Mathf.Max(0.001f, maxHealth / maxHearts) : 1f;
    }

    // No half hearts: full-or-empty.
    // Rule: floor() to avoid "free" extra heart; but if alive (>0) show at least 1.
    int ToFullHearts(float currentHealth, float valuePerHeart, int maxHearts)
    {
        if (currentHealth <= 0f) return 0;

        int heartsFull = Mathf.FloorToInt(currentHealth / valuePerHeart + 1e-6f);
        heartsFull = Mathf.Clamp(heartsFull, 0, maxHearts);

        // If alive, show at least 1 heart.
        return Mathf.Max(1, heartsFull);
    }

    void RebuildHearts(int maxHearts)
    {
        // Ensure list size
        while (hearts.Count < maxHearts)
        {
            var img = Instantiate(heartPrefab, heartsContainer);
            img.sprite = emptyHeartSprite;
            hearts.Add(img);
        }

        // Disable extras (better than destroy if max hearts can fluctuate)
        for (int i = 0; i < hearts.Count; i++)
        {
            hearts[i].gameObject.SetActive(i < maxHearts);
        }
    }

    void UpdateHeartSprites(int currentHearts, int maxHearts)
    {
        currentHearts = Mathf.Clamp(currentHearts, 0, maxHearts);

        for (int i = 0; i < maxHearts; i++)
        {
            var img = hearts[i];
            if (img == null) continue;

            img.sprite = (i < currentHearts) ? fullHeartSprite : emptyHeartSprite;
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (fixedMaxHearts < 1) fixedMaxHearts = 1;
        if (healthPerHeart < 0.1f) healthPerHeart = 0.1f;
    }
#endif
}
