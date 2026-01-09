using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI-based glass shard that animates from original position to scattered position.
/// Uses RectTransform for proper UI rendering. Pooled for performance.
/// </summary>
public class UIGlassShard : MonoBehaviour
{
    private Vector2 originalPosition;
    private Vector2 scatterOffset;
    private float targetRotation;
    private RectTransform rectTransform;
    private RawImage rawImage;
    
    // Cached scale vector
    private Vector3 scaleVector = Vector3.one;

    public Vector2 OriginalPosition => originalPosition;
    public RectTransform RectTransform => rectTransform;
    public RawImage RawImage => rawImage;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        rawImage = GetComponent<RawImage>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Initialize(Vector2 originalPos, Vector2 scatter, float rotation)
    {
        originalPosition = originalPos;
        scatterOffset = scatter;
        targetRotation = rotation;
        
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = originalPosition;
            rectTransform.localRotation = Quaternion.identity;
            rectTransform.localScale = Vector3.one;
        }
    }

    /// <summary>
    /// Configure the shard's visual properties (for pooling reuse)
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Configure(Vector2 position, float width, float height, Rect uvRect, Texture texture)
    {
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = new Vector2(width, height);
        }
        
        if (rawImage != null)
        {
            rawImage.texture = texture;
            rawImage.uvRect = uvRect;
        }
    }

    /// <summary>
    /// Set how "open" the shard is (0 = original position, 1 = fully scattered)
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetOpenAmount(float t)
    {
        if (rectTransform == null) return;

        // Interpolate position
        rectTransform.anchoredPosition = new Vector2(
            originalPosition.x + scatterOffset.x * t,
            originalPosition.y + scatterOffset.y * t
        );

        // Interpolate rotation
        rectTransform.localRotation = Quaternion.Euler(0f, 0f, targetRotation * t);

        // Slight scale for depth effect
        float scale = 1f + t * 0.05f;
        scaleVector.x = scale;
        scaleVector.y = scale;
        rectTransform.localScale = scaleVector;
    }

    /// <summary>
    /// Reset shard for pooling
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ResetShard()
    {
        if (rectTransform != null)
        {
            rectTransform.localScale = Vector3.one;
            rectTransform.localRotation = Quaternion.identity;
        }
    }
}
