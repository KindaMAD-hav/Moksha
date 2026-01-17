using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Individual slot UI for displaying a single powerup in the ability panel.
/// Shows grayscale when not owned, colored when owned, with stack count.
/// </summary>
public class PowerUpSlotUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI stackCountText;

    [Header("Optional")]
    [SerializeField] private GameObject stackCountBackground;

    // Cached data
    private Sprite grayscaleSprite;
    private Sprite coloredSprite;
    private int currentStacks;

    /// <summary>
    /// Initialize the slot with sprite data.
    /// </summary>
    public void Initialize(Sprite grayscale, Sprite colored)
    {
        grayscaleSprite = grayscale;
        coloredSprite = colored;
        currentStacks = 0;

        UpdateVisuals();
    }

    /// <summary>
    /// Update the slot to reflect new stack count.
    /// </summary>
    public void SetStacks(int stacks)
    {
        if (currentStacks == stacks)
            return;

        currentStacks = stacks;
        UpdateVisuals();
    }

    /// <summary>
    /// Get current stack count.
    /// </summary>
    public int GetStacks() => currentStacks;

    private void UpdateVisuals()
    {
        if (iconImage == null)
            return;

        // Switch sprite based on ownership
        if (currentStacks > 0)
        {
            iconImage.sprite = coloredSprite;
        }
        else
        {
            iconImage.sprite = grayscaleSprite;
        }

        // Update stack count text
        if (stackCountText != null)
        {
            stackCountText.text = currentStacks.ToString();
            
            // Optionally hide text when 0
            // stackCountText.gameObject.SetActive(currentStacks > 0);
        }

        // Optionally hide stack background when 0
        if (stackCountBackground != null)
        {
            stackCountBackground.SetActive(currentStacks > 0);
        }
    }

    /// <summary>
    /// Reset slot to default state.
    /// </summary>
    public void ResetSlot()
    {
        currentStacks = 0;
        UpdateVisuals();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Auto-find components if not assigned
        if (iconImage == null)
            iconImage = GetComponentInChildren<Image>();
        if (stackCountText == null)
            stackCountText = GetComponentInChildren<TextMeshProUGUI>();
    }
#endif
}
