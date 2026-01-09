using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Power-up card with pop-up and flip animations.
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
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Card Back (Optional)")]
    [SerializeField] private GameObject cardFront;
    [SerializeField] private GameObject cardBack;

    [Header("Animation Settings")]
    [SerializeField] private float slideDistance = 300f;
    [SerializeField] private float slideDuration = 0.3f;
    [SerializeField] private float flipDuration = 0.3f;
    [SerializeField] private AnimationCurve slideCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve flipCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Hover")]
    [SerializeField] private float hoverScale = 1.08f;
    [SerializeField] private float hoverDuration = 0.1f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip selectSfx;

    // Cached
    private PowerUp powerUp;
    private Action<PowerUp> onSelected;
    private RectTransform rectTransform;
    private Vector3 normalScale;
    private Coroutine animationCoroutine;
    private Coroutine hoverCoroutine;
    private bool isAnimating;
    private bool isInteractable;

    // Cached component checks
    private bool hasIcon;
    private bool hasName;
    private bool hasDescription;
    private bool hasRarity;
    private bool hasBorder;
    private bool hasBackground;
    private bool hasCanvasGroup;
    private bool hasCardBack;

    public RectTransform RectTransform => rectTransform;
    public float SlideDistance => slideDistance;
    public float SlideDuration => slideDuration;
    public float FlipDuration => flipDuration;
    public AnimationCurve SlideCurve => slideCurve;
    public AnimationCurve FlipCurve => flipCurve;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        normalScale = rectTransform != null ? rectTransform.localScale : Vector3.one;

        //// 🔊 FORCE UI audio to play while paused
        //if (audioSource != null)
        //{
        //    audioSource.ignoreListenerPause = true;
        //    audioSource.playOnAwake = false;
        //    audioSource.spatialBlend = 0f; // force 2D
        //}

        hasIcon = iconImage != null;
        hasName = nameText != null;
        hasDescription = descriptionText != null;
        hasRarity = rarityText != null;
        hasBorder = borderImage != null;
        hasBackground = backgroundImage != null;
        hasCanvasGroup = canvasGroup != null;
        hasCardBack = cardBack != null && cardFront != null;

        if (selectButton != null)
            selectButton.onClick.AddListener(OnClick);

        if (!hasCanvasGroup)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
            hasCanvasGroup = true;
        }
    }

    public void Setup(PowerUp powerUp, Action<PowerUp> onSelected)
    {
        this.powerUp = powerUp;
        this.onSelected = onSelected;
        isInteractable = false;
        isAnimating = false;

        if (hasCardBack)
        {
            cardFront.SetActive(false);
            cardBack.SetActive(true);
        }

        UpdateDisplay();
    }

    /// <summary>
    /// Prepare card for animation (call before starting animation)
    /// </summary>
    public void PrepareForAnimation()
    {
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
        if (rectTransform != null)
        {
            rectTransform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            rectTransform.localScale = normalScale * 0.5f;
        }
        isAnimating = true;
        isInteractable = false;
    }

    /// <summary>
    /// Play entry animation from a start position to current position
    /// </summary>
    public void PlayEntryAnimation(Vector3 targetPosition, float delay)
    {
        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

        animationCoroutine = StartCoroutine(EntryAnimationRoutine(targetPosition, delay));
    }

    private IEnumerator EntryAnimationRoutine(Vector3 targetPos, float delay)
    {
        isAnimating = true;
        isInteractable = false;

        // Start position (below target)
        Vector3 startPos = targetPos;
        startPos.y -= slideDistance;
        rectTransform.localPosition = startPos;

        // Initial state
        canvasGroup.alpha = 0f;
        rectTransform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        rectTransform.localScale = normalScale * 0.5f;

        // Stagger delay
        if (delay > 0f)
            yield return new WaitForSecondsRealtime(delay);

        // Phase 1: Slide up and fade in
        float elapsed = 0f;
        while (elapsed < slideDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = slideCurve.Evaluate(elapsed / slideDuration);

            rectTransform.localPosition = Vector3.Lerp(startPos, targetPos, t);
            canvasGroup.alpha = t;
            rectTransform.localScale = Vector3.Lerp(normalScale * 0.5f, normalScale, t);

            yield return null;
        }

        // Ensure final slide state
        rectTransform.localPosition = targetPos;
        canvasGroup.alpha = 1f;
        rectTransform.localScale = normalScale;

        // Small pause before flip
        yield return new WaitForSecondsRealtime(0.05f);

        // Phase 2: Flip animation
        elapsed = 0f;
        while (elapsed < flipDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = flipCurve.Evaluate(elapsed / flipDuration);
            float rotation = Mathf.Lerp(180f, 0f, t);

            rectTransform.localRotation = Quaternion.Euler(0f, rotation, 0f);

            // Swap front/back at midpoint
            if (hasCardBack && rotation < 90f && cardBack.activeSelf)
            {
                cardBack.SetActive(false);
                cardFront.SetActive(true);
            }

            yield return null;
        }

        // Ensure final state
        rectTransform.localRotation = Quaternion.identity;
        if (hasCardBack)
        {
            cardBack.SetActive(false);
            cardFront.SetActive(true);
        }

        isAnimating = false;
        isInteractable = true;
    }

    /// <summary>
    /// Reset card state for pooling
    /// </summary>
    public void ResetCard()
    {
        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);
        if (hoverCoroutine != null)
            StopCoroutine(hoverCoroutine);

        animationCoroutine = null;
        hoverCoroutine = null;

        if (rectTransform != null)
        {
            rectTransform.localScale = normalScale;
            rectTransform.localRotation = Quaternion.identity;
        }
        
        if (canvasGroup != null)
            canvasGroup.alpha = 1f;
            
        isAnimating = false;
        isInteractable = false;

        if (hasCardBack)
        {
            if (cardFront != null) cardFront.SetActive(true);
            if (cardBack != null) cardBack.SetActive(false);
        }
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
        if (!isInteractable || isAnimating) return;
        //if (selectSfx != null && audioSource != null)
        //    audioSource.PlayOneShot(selectSfx);

        onSelected?.Invoke(powerUp);
    }

    public void OnPointerEnter()
    {
        if (!isInteractable || isAnimating) return;

        if (hoverCoroutine != null)
            StopCoroutine(hoverCoroutine);

        hoverCoroutine = StartCoroutine(ScaleToTarget(normalScale * hoverScale));
    }

    public void OnPointerExit()
    {
        if (!isInteractable || isAnimating) return;

        if (hoverCoroutine != null)
            StopCoroutine(hoverCoroutine);

        hoverCoroutine = StartCoroutine(ScaleToTarget(normalScale));
    }

    private IEnumerator ScaleToTarget(Vector3 target)
    {
        Vector3 start = rectTransform.localScale;
        float elapsed = 0f;

        while (elapsed < hoverDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / hoverDuration;
            rectTransform.localScale = Vector3.Lerp(start, target, t);
            yield return null;
        }

        rectTransform.localScale = target;
    }
}
