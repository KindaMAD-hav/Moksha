using UnityEngine;

/// <summary>
/// Individual glass shard that animates from original position to scattered position.
/// </summary>
public class GlassShard : MonoBehaviour
{
    private Vector2 originalPosition;
    private Vector2 scatterOffset;
    private float targetRotation;
    private RectTransform rectTransform;
    private Transform cachedTransform;
    private bool isInitialized;

    public Vector2 OriginalPosition => originalPosition;

    /// <summary>
    /// Initialize the shard with its animation parameters
    /// </summary>
    public void Initialize(Vector2 originalPos, Vector2 scatter, float rotation)
    {
        originalPosition = originalPos;
        scatterOffset = scatter;
        targetRotation = rotation;
        
        rectTransform = GetComponent<RectTransform>();
        cachedTransform = transform;
        
        // Start at original position
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = originalPosition;
        }
        
        isInitialized = true;
        
        Debug.Log($"[GlassShard] Initialized at {originalPos}, scatter: {scatter}, rotation: {rotation}");
    }

    /// <summary>
    /// Set how "open" the shard is (0 = closed/original position, 1 = fully scattered)
    /// </summary>
    public void SetOpenAmount(float t)
    {
        if (!isInitialized)
        {
            Debug.LogWarning("[GlassShard] SetOpenAmount called before Initialize!");
            return;
        }

        if (rectTransform != null)
        {
            // Interpolate position
            Vector2 targetPos = originalPosition + scatterOffset * t;
            rectTransform.anchoredPosition = targetPos;
        }
        else if (cachedTransform != null)
        {
            Vector3 targetPos = new Vector3(
                originalPosition.x + scatterOffset.x * t,
                originalPosition.y + scatterOffset.y * t,
                0f
            );
            cachedTransform.localPosition = targetPos;
        }

        // Interpolate rotation
        float rotation = targetRotation * t;
        if (cachedTransform != null)
        {
            cachedTransform.localRotation = Quaternion.Euler(0f, 0f, rotation);

            // Optional: slight scale variation for depth effect
            float scale = 1f + t * 0.05f;
            cachedTransform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
