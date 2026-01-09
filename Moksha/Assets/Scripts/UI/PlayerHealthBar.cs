using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple health bar UI that follows player health.
/// (You can keep this for now even if you switch to hearts UI later)
/// </summary>
public class PlayerHealthBar : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Image fillImage;

    [Header("Colors")]
    [SerializeField] private Color fullHealthColor = Color.green;
    [SerializeField] private Color lowHealthColor = Color.red;
    [SerializeField] private float lowHealthThreshold = 0.3f;

    [Header("Animation")]
    [SerializeField] private float smoothSpeed = 5f;
    [SerializeField] private bool animateFill = true;

    private float targetFill;
    private float currentFill;
    private bool isDirty;

    private void Awake()
    {
        if (playerHealth == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerHealth = player.GetComponent<PlayerHealth>();
        }
    }

    private void OnEnable()
    {
        if (playerHealth == null) return;

        playerHealth.OnHealthChanged += OnHealthChanged;
        OnHealthChanged(playerHealth.CurrentHearts, playerHealth.MaxHearts);
    }

    private void OnDisable()
    {
        if (playerHealth == null) return;
        playerHealth.OnHealthChanged -= OnHealthChanged;
    }

    private void Update()
    {
        if (!isDirty) return;

        if (!animateFill)
        {
            SetFillImmediate(targetFill);
            isDirty = false;
            return;
        }

        currentFill = Mathf.Lerp(currentFill, targetFill, smoothSpeed * Time.deltaTime);

        if (Mathf.Abs(currentFill - targetFill) < 0.001f)
        {
            currentFill = targetFill;
            isDirty = false;
        }

        UpdateVisuals(currentFill);
    }

    private void OnHealthChanged(float current, float max)
    {
        targetFill = max > 0f ? current / max : 0f;
        isDirty = true;

        if (!animateFill)
            SetFillImmediate(targetFill);
    }

    private void SetFillImmediate(float fill)
    {
        currentFill = fill;
        UpdateVisuals(fill);
    }

    private void UpdateVisuals(float fill)
    {
        if (fillImage == null) return;

        fillImage.fillAmount = fill;

        fillImage.color = Color.Lerp(
            lowHealthColor,
            fullHealthColor,
            Mathf.InverseLerp(0f, lowHealthThreshold, fill)
        );
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        lowHealthThreshold = Mathf.Clamp01(lowHealthThreshold);
        if (smoothSpeed < 0.1f) smoothSpeed = 0.1f;
    }
#endif
}
