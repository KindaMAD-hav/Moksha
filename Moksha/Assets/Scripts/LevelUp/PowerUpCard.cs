using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Optimized power-up card. Caches all component references.
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

    [Header("Hover")]
    [SerializeField] private float hoverScale = 1.05f;

    // Cached
    private PowerUp powerUp;
    private Action<PowerUp> onSelected;
    private RectTransform rectTransform;
    private Vector3 normalScale;
    private Vector3 hoverScaleVec;
    private bool hasIcon;
    private bool hasName;
    private bool hasDescription;
    private bool hasRarity;
    private bool hasBorder;
    private bool hasBackground;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        normalScale = rectTransform != null ? rectTransform.localScale : Vector3.one;
        hoverScaleVec = normalScale * hoverScale;

        // Cache null checks
        hasIcon = iconImage != null;
        hasName = nameText != null;
        hasDescription = descriptionText != null;
        hasRarity = rarityText != null;
        hasBorder = borderImage != null;
        hasBackground = backgroundImage != null;

        if (selectButton != null)
            selectButton.onClick.AddListener(OnClick);
    }

    public void Setup(PowerUp powerUp, Action<PowerUp> onSelected)
    {
        this.powerUp = powerUp;
        this.onSelected = onSelected;
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (powerUp == null) return;

        if (hasName)
            nameText.SetText(powerUp.powerUpName);

        if (hasDescription)
            descriptionText.SetText(powerUp.description);

        Color rarityColor = powerUp.GetRarityColor();

        if (hasRarity)
        {
            rarityText.SetText(powerUp.rarity.ToString());
            rarityText.color = rarityColor;
        }

        if (hasIcon)
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

        if (hasBorder)
            borderImage.color = rarityColor;

        if (hasBackground)
        {
            Color bg = rarityColor;
            bg.a = 0.2f;
            backgroundImage.color = bg;
        }
    }

    private void OnClick()
    {
        onSelected?.Invoke(powerUp);
    }

    public void OnPointerEnter()
    {
        if (rectTransform != null)
            rectTransform.localScale = hoverScaleVec;
    }

    public void OnPointerExit()
    {
        if (rectTransform != null)
            rectTransform.localScale = normalScale;
    }
}
