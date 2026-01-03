using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Displays the player's current XP progress.
/// </summary>
public class ExperienceBar : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Slider xpSlider;
    [SerializeField] private Image fillImage;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI xpText;

    [Header("Settings")]
    [SerializeField] private bool animateChanges = true;
    [SerializeField] private float animationSpeed = 5f;

    private float targetValue;
    private float currentValue;

    private void Start()
    {
        if (ExperienceManager.Instance != null)
        {
            ExperienceManager.Instance.OnXPChanged += UpdateXPDisplay;
            ExperienceManager.Instance.OnLevelUp += UpdateLevelDisplay;
            
            // Initial update
            UpdateXPDisplay(ExperienceManager.Instance.CurrentXP, ExperienceManager.Instance.XPToNextLevel);
            UpdateLevelDisplay(ExperienceManager.Instance.CurrentLevel);
        }
    }

    private void OnDestroy()
    {
        if (ExperienceManager.Instance != null)
        {
            ExperienceManager.Instance.OnXPChanged -= UpdateXPDisplay;
            ExperienceManager.Instance.OnLevelUp -= UpdateLevelDisplay;
        }
    }

    private void Update()
    {
        if (animateChanges && xpSlider != null)
        {
            currentValue = Mathf.Lerp(currentValue, targetValue, Time.deltaTime * animationSpeed);
            xpSlider.value = currentValue;
        }
    }

    private void UpdateXPDisplay(int currentXP, int xpToNextLevel)
    {
        targetValue = (float)currentXP / xpToNextLevel;

        if (!animateChanges && xpSlider != null)
            xpSlider.value = targetValue;

        if (xpText != null)
            xpText.text = $"{currentXP} / {xpToNextLevel}";
    }

    private void UpdateLevelDisplay(int level)
    {
        if (levelText != null)
            levelText.text = $"Lv. {level}";

        // Reset animation on level up
        currentValue = 0f;
    }
}
