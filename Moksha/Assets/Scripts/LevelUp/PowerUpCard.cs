using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Individual power-up card UI component.
/// Displays power-up info and handles selection.
/// </summary>
public class PowerUpCard : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI rarityText;
    [SerializeField] private Image borderImage;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Button selectButton;

    [Header("Hover Effects")]
    [SerializeField] private float hoverScale = 1.05f;
    [SerializeField] private float hoverDuration = 0.1f;

    private PowerUp powerUp;
    private Action<PowerUp> onSelected;
    private Vector3 originalScale;
    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalScale = rectTransform != null ? rectTransform.localScale : Vector3.one;

        if (selectButton != null)
            selectButton.onClick.AddListener(OnButtonClicked);
    }

    /// <summary>
    /// Setup the card with a power-up and selection callback
    /// </summary>
    public void Setup(PowerUp powerUp, Action<PowerUp> onSelected)
    {
        this.powerUp = powerUp;
        this.onSelected = onSelected;

        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (powerUp == null) return;

        // Set texts
        if (nameText != null)
            nameText.text = powerUp.powerUpName;

        if (descriptionText != null)
            descriptionText.text = powerUp.description;

        if (rarityText != null)
        {
            rarityText.text = powerUp.rarity.ToString();
            rarityText.color = powerUp.GetRarityColor();
        }

        // Set icon
        if (iconImage != null)
        {
            if (powerUp.icon != null)
            {
                iconImage.sprite = powerUp.icon;
                iconImage.enabled = true;
            }
            else
            {
                iconImage.enabled = false;
            }
        }

        // Set rarity color on border
        if (borderImage != null)
            borderImage.color = powerUp.GetRarityColor();

        // Optional: tint background based on rarity
        if (backgroundImage != null)
        {
            Color bgColor = powerUp.GetRarityColor();
            bgColor.a = 0.2f; // Subtle tint
            backgroundImage.color = bgColor;
        }
    }

    private void OnButtonClicked()
    {
        onSelected?.Invoke(powerUp);
    }

    // Hover effects (called by Event Trigger or pointer events)
    public void OnPointerEnter()
    {
        if (rectTransform != null)
            rectTransform.localScale = originalScale * hoverScale;
    }

    public void OnPointerExit()
    {
        if (rectTransform != null)
            rectTransform.localScale = originalScale;
    }
}
