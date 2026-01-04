using UnityEngine;

/// <summary>
/// UI-based glass shard that animates from original position to scattered position.
/// Uses RectTransform for proper UI rendering.
/// </summary>
public class UIGlassShard : MonoBehaviour
{
    private Vector2 originalPosition;
    private Vector2 scatterOffset;
    private float targetRotation;
    private RectTransform rectTransform;

    public Vector2 OriginalPosition => originalPosition;

    public void Initialize(Vector2 originalPos, Vector2 scatter, float rotation)
    {
        originalPosition = originalPos;
        scatterOffset = scatter;
        targetRotation = rotation;
        
        rectTransform = GetComponent<RectTransform>();
        
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = originalPosition;
            rectTransform.localRotation = Quaternion.identity;
            rectTransform.localScale = Vector3.one;
        }
    }

    /// <summary>
    /// Set how "open" the shard is (0 = original position, 1 = fully scattered)
    /// </summary>
    public void SetOpenAmount(float t)
    {
        if (rectTransform == null) return;

        // Interpolate position
        Vector2 targetPos = originalPosition + scatterOffset * t;
        rectTransform.anchoredPosition = targetPos;

        // Interpolate rotation
        float rotation = targetRotation * t;
        rectTransform.localRotation = Quaternion.Euler(0f, 0f, rotation);

        // Slight scale for depth effect
        float scale = 1f + t * 0.05f;
        rectTransform.localScale = new Vector3(scale, scale, 1f);
    }
}
