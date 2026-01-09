using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ExperienceBar : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image fillImage;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI xpText;

    [Header("Animation")]
    [SerializeField] private bool animate = true;
    [SerializeField] private float lerpSpeed = 8f;

    private float currentFill;
    private float targetFill;

    private void Start()
    {
        var xp = ExperienceManager.Instance;
        if (xp == null) return;

        xp.OnXPChanged += OnXPChanged;
        xp.OnLevelUp += OnLevelUp;

        // Initial state
        OnXPChanged(xp.CurrentXP, xp.XPToNextLevel);
        OnLevelUp(xp.CurrentLevel);
    }

    private void Update()
    {
        if (!animate) return;

        currentFill = Mathf.Lerp(currentFill, targetFill, Time.deltaTime * lerpSpeed);
        fillImage.fillAmount = currentFill;
    }

    private void OnXPChanged(int currentXP, int xpToNextLevel)
    {
        targetFill = (float)currentXP / xpToNextLevel;

        if (!animate)
            fillImage.fillAmount = targetFill;

        if (xpText != null)
            xpText.text = $"{currentXP} / {xpToNextLevel}";
    }

    private void OnLevelUp(int level)
    {
        currentFill = 0f;
        fillImage.fillAmount = 0f;

        if (levelText != null)
            levelText.text = $"Lv {level}";
    }

    private void OnDestroy()
    {
        if (ExperienceManager.Instance == null) return;

        ExperienceManager.Instance.OnXPChanged -= OnXPChanged;
        ExperienceManager.Instance.OnLevelUp -= OnLevelUp;
    }
}
