using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shattered glass pause menu effect using UI system.
/// Takes a screenshot, shatters it into pieces, and animates them apart to reveal menu.
/// </summary>
public class ShatteredPauseMenu : MonoBehaviour
{
    public static ShatteredPauseMenu Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Canvas menuCanvas;
    [SerializeField] private GameObject menuContent;
    [SerializeField] private RectTransform shardContainer;
    [SerializeField] private Image backgroundImage; // Black background

    [Header("Input")]
    [SerializeField] private KeyCode pauseKey = KeyCode.Escape;

    [Header("Shard Settings")]
    [SerializeField] private int shardColumns = 12;
    [SerializeField] private int shardRows = 8;

    [Header("Animation Settings")]
    [SerializeField] private float openDuration = 0.6f;
    [SerializeField] private float closeDuration = 0.4f;
    [SerializeField] private float staggerAmount = 0.05f;
    [SerializeField] private AnimationCurve openCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve closeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Shard Movement")]
    [SerializeField] private float edgeOffset = 100f; // How far off-screen shards move
    [SerializeField] private float maxRotation = 30f;
    [SerializeField] private ShardMoveDirection moveDirection = ShardMoveDirection.ToEdges;

    [Header("Background")]
    [SerializeField] private Color backgroundColor = Color.black;
    [SerializeField] private float backgroundFadeDuration = 0.3f;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

    public enum ShardMoveDirection
    {
        ToEdges,        // Shards move to nearest edge
        SplitHorizontal, // Left half goes left, right half goes right
        SplitVertical,   // Top goes up, bottom goes down
        Explode,        // All shards move outward from center
        LightningBolt   // Zigzag split like a horizontal lightning bolt
    }

    [Header("Lightning Bolt Settings")]
    [Tooltip("Number of zigzag peaks. More = more jagged. Should match or divide evenly into shardColumns for best results.")]
    [SerializeField] private int zigzagSegments = 6;
    [Tooltip("How far the zigzag deviates from center (0.1 = subtle, 0.4 = dramatic). As fraction of screen height.")]
    [SerializeField] private float zigzagAmplitude = 0.25f;

    // Runtime
    private List<UIGlassShard> shards = new List<UIGlassShard>();
    private Texture2D screenshotTexture;
    private bool isPaused;
    private bool isAnimating;
    private Coroutine animationCoroutine;

    public bool IsPaused => isPaused;

    private void Awake()
    {
        Log("ShatteredPauseMenu Awake");
        
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (menuCanvas != null)
            menuCanvas.gameObject.SetActive(false);

        if (menuContent != null)
            menuContent.SetActive(false);

        // Setup background image if not assigned
        if (backgroundImage == null && shardContainer != null)
        {
            CreateBackgroundImage();
        }

        if (backgroundImage != null)
        {
            backgroundImage.color = new Color(backgroundColor.r, backgroundColor.g, backgroundColor.b, 0f);
            backgroundImage.gameObject.SetActive(false);
        }
    }

    private void CreateBackgroundImage()
    {
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(shardContainer.parent, false);
        bgObj.transform.SetAsFirstSibling(); // Behind everything
        
        RectTransform rt = bgObj.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        
        backgroundImage = bgObj.AddComponent<Image>();
        backgroundImage.color = new Color(backgroundColor.r, backgroundColor.g, backgroundColor.b, 0f);
        backgroundImage.raycastTarget = false;
        
        Log("Created background image");
    }

    private void Start()
    {
        Log($"ShatteredPauseMenu Start - Pause key: {pauseKey}");
    }

    private void Update()
    {
        if (Input.GetKeyDown(pauseKey) && !isAnimating)
        {
            if (isPaused)
                Resume();
            else
                Pause();
        }
    }

    public void Pause()
    {
        if (isPaused || isAnimating) return;

        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

        animationCoroutine = StartCoroutine(PauseRoutine());
    }

    public void Resume()
    {
        if (!isPaused || isAnimating) return;

        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

        animationCoroutine = StartCoroutine(ResumeRoutine());
    }

    private IEnumerator PauseRoutine()
    {
        Log("PauseRoutine started");
        isAnimating = true;

        // Capture screenshot
        yield return new WaitForEndOfFrame();
        CaptureScreenshot();

        // Show canvas
        if (menuCanvas != null)
            menuCanvas.gameObject.SetActive(true);

        // Show and prepare background (start transparent)
        if (backgroundImage != null)
        {
            backgroundImage.gameObject.SetActive(true);
            backgroundImage.color = new Color(backgroundColor.r, backgroundColor.g, backgroundColor.b, 0f);
        }

        // Create shards
        CreateShards();

        // Pause time
        Time.timeScale = 0f;
        isPaused = true;

        // Animate shards opening AND fade in background simultaneously
        yield return StartCoroutine(AnimateShardsOpen());

        // Show menu content
        if (menuContent != null)
            menuContent.SetActive(true);

        isAnimating = false;
        Log("PauseRoutine complete");
    }

    private IEnumerator ResumeRoutine()
    {
        Log("ResumeRoutine started");
        isAnimating = true;

        if (menuContent != null)
            menuContent.SetActive(false);

        // Animate shards closing AND fade out background
        yield return StartCoroutine(AnimateShardsClose());

        ClearShards();

        if (backgroundImage != null)
            backgroundImage.gameObject.SetActive(false);

        if (menuCanvas != null)
            menuCanvas.gameObject.SetActive(false);

        Time.timeScale = 1f;
        isPaused = false;
        isAnimating = false;
        Log("ResumeRoutine complete");
    }

    private void CaptureScreenshot()
    {
        int width = Screen.width;
        int height = Screen.height;

        if (screenshotTexture == null || screenshotTexture.width != width || screenshotTexture.height != height)
        {
            if (screenshotTexture != null)
                Destroy(screenshotTexture);

            screenshotTexture = new Texture2D(width, height, TextureFormat.RGB24, false);
        }

        screenshotTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        screenshotTexture.Apply();
        Log($"Screenshot captured: {width}x{height}");
    }

    private void CreateShards()
    {
        ClearShards();

        if (shardContainer == null)
        {
            Log("ERROR: shardContainer is null!");
            return;
        }

        RectTransform canvasRect = menuCanvas.GetComponent<RectTransform>();
        float canvasWidth = canvasRect.rect.width;
        float canvasHeight = canvasRect.rect.height;
        
        float shardWidth = canvasWidth / shardColumns;
        float shardHeight = canvasHeight / shardRows;

        Vector2 canvasCenter = new Vector2(canvasWidth / 2f, canvasHeight / 2f);

        Log($"Creating {shardColumns}x{shardRows} shards, size: {shardWidth}x{shardHeight}");

        for (int row = 0; row < shardRows; row++)
        {
            for (int col = 0; col < shardColumns; col++)
            {
                // Calculate shard position (center of each grid cell)
                float x = col * shardWidth + shardWidth / 2f;
                float y = row * shardHeight + shardHeight / 2f;
                Vector2 shardCenter = new Vector2(x, y);

                // Calculate UV rect
                float uvX = (float)col / shardColumns;
                float uvY = (float)row / shardRows;
                Rect uvRect = new Rect(uvX, uvY, 1f / shardColumns, 1f / shardRows);

                // Calculate target position based on move direction
                Vector2 targetPos = CalculateTargetPosition(shardCenter, canvasWidth, canvasHeight, canvasCenter, col, row);
                Vector2 moveOffset = targetPos - shardCenter;

                // Random rotation
                float randomRot = Random.Range(-maxRotation, maxRotation);

                // Create shard
                UIGlassShard shard = CreateUIShard(shardCenter, shardWidth, shardHeight, uvRect, moveOffset, randomRot);
                shards.Add(shard);
            }
        }

        Log($"Created {shards.Count} shards");
    }

    private Vector2 CalculateTargetPosition(Vector2 shardPos, float canvasWidth, float canvasHeight, 
        Vector2 center, int col, int row)
    {
        switch (moveDirection)
        {
            case ShardMoveDirection.SplitHorizontal:
                // Left half goes left, right half goes right
                if (shardPos.x < center.x)
                    return new Vector2(-edgeOffset, shardPos.y);
                else
                    return new Vector2(canvasWidth + edgeOffset, shardPos.y);

            case ShardMoveDirection.SplitVertical:
                // Bottom half goes down, top half goes up
                if (shardPos.y < center.y)
                    return new Vector2(shardPos.x, -edgeOffset);
                else
                    return new Vector2(shardPos.x, canvasHeight + edgeOffset);

            case ShardMoveDirection.ToEdges:
                // Move to nearest edge
                float distLeft = shardPos.x;
                float distRight = canvasWidth - shardPos.x;
                float distBottom = shardPos.y;
                float distTop = canvasHeight - shardPos.y;

                float minDist = Mathf.Min(distLeft, distRight, distBottom, distTop);

                if (minDist == distLeft)
                    return new Vector2(-edgeOffset, shardPos.y);
                else if (minDist == distRight)
                    return new Vector2(canvasWidth + edgeOffset, shardPos.y);
                else if (minDist == distBottom)
                    return new Vector2(shardPos.x, -edgeOffset);
                else
                    return new Vector2(shardPos.x, canvasHeight + edgeOffset);

            case ShardMoveDirection.Explode:
                // Move outward from center
                Vector2 dir = (shardPos - center).normalized;
                if (dir.sqrMagnitude < 0.01f)
                    dir = Random.insideUnitCircle.normalized;
                
                // Calculate distance to edge in this direction
                float distToEdge = CalculateDistanceToEdge(shardPos, dir, canvasWidth, canvasHeight);
                return shardPos + dir * (distToEdge + edgeOffset);

            case ShardMoveDirection.LightningBolt:
            default:
                // Horizontal zigzag split - top goes up, bottom goes down
                // The zigzag line runs LEFT to RIGHT, oscillating UP and DOWN
                bool isAboveBolt = IsAboveLightningBolt(shardPos, canvasWidth, canvasHeight);
                if (isAboveBolt)
                    return new Vector2(shardPos.x, canvasHeight + edgeOffset); // Move up
                else
                    return new Vector2(shardPos.x, -edgeOffset); // Move down
        }
    }

    /// <summary>
    /// Determines if a point is above the horizontal lightning bolt zigzag line.
    /// The zigzag runs from left to right, oscillating up and down around the center.
    /// </summary>
    private bool IsAboveLightningBolt(Vector2 pos, float canvasWidth, float canvasHeight)
    {
        float centerY = canvasHeight / 2f;
        float amplitude = canvasHeight * zigzagAmplitude;
        
        // Calculate which segment this X position falls into
        float segmentWidth = canvasWidth / zigzagSegments;
        int segmentIndex = Mathf.FloorToInt(pos.x / segmentWidth);
        segmentIndex = Mathf.Clamp(segmentIndex, 0, zigzagSegments - 1);
        
        // Calculate the X position within the segment (0 to 1)
        float segmentStartX = segmentIndex * segmentWidth;
        float tInSegment = (pos.x - segmentStartX) / segmentWidth;
        
        // Determine the Y positions at the start and end of this segment
        // Alternate direction: even segments go up, odd segments go down
        float startY, endY;
        if (segmentIndex % 2 == 0)
        {
            // Going from down to up as we go right
            startY = centerY - amplitude;
            endY = centerY + amplitude;
        }
        else
        {
            // Going from up to down as we go right
            startY = centerY + amplitude;
            endY = centerY - amplitude;
        }
        
        // Interpolate to find the Y position of the bolt at this X
        float boltY = Mathf.Lerp(startY, endY, tInSegment);
        
        return pos.y > boltY;
    }

    private float CalculateDistanceToEdge(Vector2 pos, Vector2 dir, float width, float height)
    {
        float dist = float.MaxValue;

        // Check intersection with each edge
        if (dir.x > 0)
            dist = Mathf.Min(dist, (width - pos.x) / dir.x);
        else if (dir.x < 0)
            dist = Mathf.Min(dist, -pos.x / dir.x);

        if (dir.y > 0)
            dist = Mathf.Min(dist, (height - pos.y) / dir.y);
        else if (dir.y < 0)
            dist = Mathf.Min(dist, -pos.y / dir.y);

        return Mathf.Max(0, dist);
    }

    private UIGlassShard CreateUIShard(Vector2 position, float width, float height, Rect uvRect, 
        Vector2 moveOffset, float rotation)
    {
        GameObject shardObj = new GameObject($"Shard_{shards.Count}");
        shardObj.transform.SetParent(shardContainer, false);

        RectTransform rt = shardObj.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = new Vector2(width, height);

        RawImage rawImage = shardObj.AddComponent<RawImage>();
        rawImage.texture = screenshotTexture;
        rawImage.uvRect = uvRect;
        rawImage.raycastTarget = false;

        UIGlassShard shard = shardObj.AddComponent<UIGlassShard>();
        shard.Initialize(position, moveOffset, rotation);

        return shard;
    }

    private IEnumerator AnimateShardsOpen()
    {
        if (shards.Count == 0) yield break;

        float[] delays = CalculateStaggerDelays();
        float maxDelay = 0f;
        foreach (float d in delays)
            maxDelay = Mathf.Max(maxDelay, d);

        float totalDuration = openDuration + maxDelay;
        float elapsed = 0f;

        while (elapsed < totalDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            // Animate shards
            for (int i = 0; i < shards.Count; i++)
            {
                float shardElapsed = elapsed - delays[i];
                if (shardElapsed > 0f)
                {
                    float t = Mathf.Clamp01(shardElapsed / openDuration);
                    t = openCurve.Evaluate(t);
                    shards[i].SetOpenAmount(t);
                }
            }

            // Fade in background
            if (backgroundImage != null)
            {
                float bgT = Mathf.Clamp01(elapsed / backgroundFadeDuration);
                backgroundImage.color = new Color(
                    backgroundColor.r, 
                    backgroundColor.g, 
                    backgroundColor.b, 
                    bgT * backgroundColor.a
                );
            }

            yield return null;
        }

        // Ensure final state
        foreach (var shard in shards)
            shard.SetOpenAmount(1f);

        if (backgroundImage != null)
            backgroundImage.color = backgroundColor;
    }

    private IEnumerator AnimateShardsClose()
    {
        float elapsed = 0f;
        float startAlpha = backgroundImage != null ? backgroundImage.color.a : 0f;

        while (elapsed < closeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = 1f - closeCurve.Evaluate(elapsed / closeDuration);

            foreach (var shard in shards)
                shard.SetOpenAmount(t);

            // Fade out background
            if (backgroundImage != null)
            {
                float bgT = 1f - Mathf.Clamp01(elapsed / closeDuration);
                backgroundImage.color = new Color(
                    backgroundColor.r, 
                    backgroundColor.g, 
                    backgroundColor.b, 
                    startAlpha * bgT
                );
            }

            yield return null;
        }

        foreach (var shard in shards)
            shard.SetOpenAmount(0f);

        if (backgroundImage != null)
            backgroundImage.color = new Color(backgroundColor.r, backgroundColor.g, backgroundColor.b, 0f);
    }

    private float[] CalculateStaggerDelays()
    {
        RectTransform canvasRect = menuCanvas.GetComponent<RectTransform>();
        Vector2 canvasCenter = new Vector2(canvasRect.rect.width / 2f, canvasRect.rect.height / 2f);
        float maxDist = canvasCenter.magnitude;

        float[] delays = new float[shards.Count];
        for (int i = 0; i < shards.Count; i++)
        {
            float dist = Vector2.Distance(shards[i].OriginalPosition, canvasCenter);
            // Center shards animate first
            delays[i] = (1f - dist / maxDist) * staggerAmount * shards.Count;
        }

        return delays;
    }

    private void ClearShards()
    {
        foreach (var shard in shards)
        {
            if (shard != null)
                Destroy(shard.gameObject);
        }
        shards.Clear();
    }

    private void Log(string message)
    {
        if (enableDebugLogs)
            Debug.Log($"[ShatteredPauseMenu] {message}");
    }

    private void OnDestroy()
    {
        if (screenshotTexture != null)
            Destroy(screenshotTexture);
        ClearShards();
    }

#if UNITY_EDITOR
    [ContextMenu("Test Pause")]
    public void TestPause()
    {
        if (isPaused) Resume();
        else Pause();
    }
#endif
}
