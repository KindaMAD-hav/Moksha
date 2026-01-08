using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple health bar UI that follows player health.
/// Uses Unity UI with optimized updates.
/// </summary>
public class PlayerHealthBar : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Image fillImage;
    [SerializeField] private Image backgroundImage;
    
    [Header("Colors")]
    [SerializeField] private Color fullHealthColor = Color.green;
    [SerializeField] private Color lowHealthColor = Color.red;
    [SerializeField] private float lowHealthThreshold = 0.3f;
    
    [Header("Animation")]
    [SerializeField] private float smoothSpeed = 5f;
    [SerializeField] private bool animateFill = true;
    
    // Cached
    private float targetFill;
    private float currentFill;
    private bool isDirty;

    private void Awake()
    {
        // Auto-find PlayerHealth if not assigned
        if (playerHealth == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerHealth = player.GetComponent<PlayerHealth>();
            }
        }
    }

    private void OnEnable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged += OnHealthChanged;
            // Initialize with current health
            OnHealthChanged(playerHealth.CurrentHealth, playerHealth.MaxHealth);
        }
    }

    private void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= OnHealthChanged;
        }
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
        {
            SetFillImmediate(targetFill);
        }
    }

    private void SetFillImmediate(float fill)
    {
        currentFill = fill;
        UpdateVisuals(fill);
    }

    private void UpdateVisuals(float fill)
    {
        if (fillImage != null)
        {
            fillImage.fillAmount = fill;
            
            // Lerp color based on health
            fillImage.color = Color.Lerp(lowHealthColor, fullHealthColor, 
                Mathf.InverseLerp(0f, lowHealthThreshold, fill));
        }
    }

    /// <summary>
    /// Manually set the PlayerHealth reference (useful for late binding)
    /// </summary>
    public void SetPlayerHealth(PlayerHealth health)
    {
        // Unsubscribe from old
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= OnHealthChanged;
        }
        
        playerHealth = health;
        
        // Subscribe to new
        if (playerHealth != null && enabled)
        {
            playerHealth.OnHealthChanged += OnHealthChanged;
            OnHealthChanged(playerHealth.CurrentHealth, playerHealth.MaxHealth);
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (lowHealthThreshold < 0f) lowHealthThreshold = 0f;
        if (lowHealthThreshold > 1f) lowHealthThreshold = 1f;
        if (smoothSpeed < 0.1f) smoothSpeed = 0.1f;
    }
#endif
}
