using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shattered glass pause menu effect using UI system.
/// Takes a screenshot, shatters it into pieces, and animates them apart to reveal menu.
/// Optimized with shard pooling and reduced allocations.
/// </summary>
public class ShatteredPauseMenu : MonoBehaviour
{
    public static ShatteredPauseMenu Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Canvas menuCanvas;
    [SerializeField] private GameObject menuContent;
    [SerializeField] private RectTransform shardContainer;
    [SerializeField] private Image backgroundImage;

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
    [SerializeField] private float edgeOffset = 100f;
    [SerializeField] private float maxRotation = 30f;
    [SerializeField] private ShardMoveDirection moveDirection = ShardMoveDirection.ToEdges;

    [Header("Background")]
    [SerializeField] private Color backgroundColor = Color.black;
    [SerializeField] private float backgroundFadeDuration = 0.3f;

    [Header("Lightning Bolt Settings")]
    [SerializeField] private int zigzagSegments = 6;
    [SerializeField] private float zigzagAmplitude = 0.25f;

    public enum ShardMoveDirection
    {
        ToEdges,
        SplitHorizontal,
        SplitVertical,
        Explode,
        LightningBolt
    }

    // Shard pool (pre-allocated)
    private UIGlassShard[] shardPool;
    private int activeShardCount;
    private int poolCapacity;
    
    // Pre-allocated delays array
    private float[] staggerDelays;
    
    // Runtime
    private Texture2D screenshotTexture;
    private bool isPaused;
    private bool isAnimating;
    private Coroutine animationCoroutine;
    
    // Cached
    private RectTransform canvasRect;

    public bool IsPaused => isPaused;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (menuCanvas != null)
        {
            menuCanvas.gameObject.SetActive(false);
            canvasRect = menuCanvas.GetComponent<RectTransform>();
        }

        if (menuContent != null)
            menuContent.SetActive(false);

        if (backgroundImage == null && shardContainer != null)
            CreateBackgroundImage();

        if (backgroundImage != null)
        {
            backgroundImage.color = new Color(backgroundColor.r, backgroundColor.g, backgroundColor.b, 0f);
            backgroundImage.gameObject.SetActive(false);
        }
        
        // Pre-allocate shard pool
        InitializeShardPool();
    }

    private void InitializeShardPool()
    {
        poolCapacity = shardColumns * shardRows;
        shardPool = new UIGlassShard[poolCapacity];
        staggerDelays = new float[poolCapacity];
        
        for (int i = 0; i < poolCapacity; i++)
        {
            GameObject shardObj = new GameObject($"Shard_{i}");
            shardObj.transform.SetParent(shardContainer, false);
            
            RectTransform rt = shardObj.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            
            RawImage rawImage = shardObj.AddComponent<RawImage>();
            rawImage.raycastTarget = false;
            
            UIGlassShard shard = shardObj.AddComponent<UIGlassShard>();
            shardPool[i] = shard;
            
            shardObj.SetActive(false);
        }
    }

    private void CreateBackgroundImage()
    {
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(shardContainer.parent, false);
        bgObj.transform.SetAsFirstSibling();
        
        RectTransform rt = bgObj.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        
        backgroundImage = bgObj.AddComponent<Image>();
        backgroundImage.color = new Color(backgroundColor.r, backgroundColor.g, backgroundColor.b, 0f);
        backgroundImage.raycastTarget = false;
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
        if (isPaused | isAnimating) return;

        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

        animationCoroutine = StartCoroutine(PauseRoutine());
    }

    public void Resume()
    {
        if (!isPaused | isAnimating) return;

        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

        animationCoroutine = StartCoroutine(ResumeRoutine());
    }

    private IEnumerator PauseRoutine()
    {
        isAnimating = true;

        yield return new WaitForEndOfFrame();
        CaptureScreenshot();

        if (menuCanvas != null)
            menuCanvas.gameObject.SetActive(true);

        if (backgroundImage != null)
        {
            backgroundImage.gameObject.SetActive(true);
            backgroundImage.color = new Color(backgroundColor.r, backgroundColor.g, backgroundColor.b, 0f);
        }

        SetupShards();

        Time.timeScale = 0f;
        isPaused = true;

        yield return StartCoroutine(AnimateShardsOpen());

        if (menuContent != null)
            menuContent.SetActive(true);

        isAnimating = false;
    }

    private IEnumerator ResumeRoutine()
    {
        isAnimating = true;

        if (menuContent != null)
            menuContent.SetActive(false);

        yield return StartCoroutine(AnimateShardsClose());

        HideShards();

        if (backgroundImage != null)
            backgroundImage.gameObject.SetActive(false);

        if (menuCanvas != null)
            menuCanvas.gameObject.SetActive(false);

        Time.timeScale = 1f;
        isPaused = false;
        isAnimating = false;
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
    }

    private void SetupShards()
    {
        float canvasWidth = canvasRect.rect.width;
        float canvasHeight = canvasRect.rect.height;
        float shardWidth = canvasWidth / shardColumns;
        float shardHeight = canvasHeight / shardRows;
        Vector2 canvasCenter = new Vector2(canvasWidth * 0.5f, canvasHeight * 0.5f);
        float maxDist = canvasCenter.magnitude;

        activeShardCount = 0;

        for (int row = 0; row < shardRows; row++)
        {
            for (int col = 0; col < shardColumns; col++)
            {
                int index = row * shardColumns + col;
                if (index >= poolCapacity) break;

                float x = col * shardWidth + shardWidth * 0.5f;
                float y = row * shardHeight + shardHeight * 0.5f;
                Vector2 shardCenter = new Vector2(x, y);

                // UV rect
                Rect uvRect = new Rect(
                    (float)col / shardColumns,
                    (float)row / shardRows,
                    1f / shardColumns,
                    1f / shardRows
                );

                // Target position
                Vector2 targetPos = CalculateTargetPosition(shardCenter, canvasWidth, canvasHeight, canvasCenter);
                Vector2 moveOffset = targetPos - shardCenter;

                float randomRot = Random.Range(-maxRotation, maxRotation);

                UIGlassShard shard = shardPool[index];
                shard.Configure(shardCenter, shardWidth, shardHeight, uvRect, screenshotTexture);
                shard.Initialize(shardCenter, moveOffset, randomRot);
                shard.gameObject.SetActive(true);
                shard.SetOpenAmount(0f);

                // Calculate stagger delay
                float dist = Vector2.Distance(shardCenter, canvasCenter);
                staggerDelays[index] = (1f - dist / maxDist) * staggerAmount * poolCapacity;

                activeShardCount++;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Vector2 CalculateTargetPosition(Vector2 shardPos, float canvasWidth, float canvasHeight, Vector2 center)
    {
        switch (moveDirection)
        {
            case ShardMoveDirection.SplitHorizontal:
                return shardPos.x < center.x 
                    ? new Vector2(-edgeOffset, shardPos.y) 
                    : new Vector2(canvasWidth + edgeOffset, shardPos.y);

            case ShardMoveDirection.SplitVertical:
                return shardPos.y < center.y 
                    ? new Vector2(shardPos.x, -edgeOffset) 
                    : new Vector2(shardPos.x, canvasHeight + edgeOffset);

            case ShardMoveDirection.ToEdges:
                float distLeft = shardPos.x;
                float distRight = canvasWidth - shardPos.x;
                float distBottom = shardPos.y;
                float distTop = canvasHeight - shardPos.y;
                float minDist = Mathf.Min(distLeft, Mathf.Min(distRight, Mathf.Min(distBottom, distTop)));

                if (minDist == distLeft) return new Vector2(-edgeOffset, shardPos.y);
                if (minDist == distRight) return new Vector2(canvasWidth + edgeOffset, shardPos.y);
                if (minDist == distBottom) return new Vector2(shardPos.x, -edgeOffset);
                return new Vector2(shardPos.x, canvasHeight + edgeOffset);

            case ShardMoveDirection.Explode:
                Vector2 dir = (shardPos - center).normalized;
                if (dir.sqrMagnitude < 0.01f)
                    dir = Random.insideUnitCircle.normalized;
                float distToEdge = CalculateDistanceToEdge(shardPos, dir, canvasWidth, canvasHeight);
                return shardPos + dir * (distToEdge + edgeOffset);

            case ShardMoveDirection.LightningBolt:
            default:
                bool isAbove = IsAboveLightningBolt(shardPos, canvasWidth, canvasHeight);
                return isAbove 
                    ? new Vector2(shardPos.x, canvasHeight + edgeOffset) 
                    : new Vector2(shardPos.x, -edgeOffset);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsAboveLightningBolt(Vector2 pos, float canvasWidth, float canvasHeight)
    {
        float centerY = canvasHeight * 0.5f;
        float amplitude = canvasHeight * zigzagAmplitude;
        float segmentWidth = canvasWidth / zigzagSegments;
        
        int segmentIndex = Mathf.Clamp(Mathf.FloorToInt(pos.x / segmentWidth), 0, zigzagSegments - 1);
        float tInSegment = (pos.x - segmentIndex * segmentWidth) / segmentWidth;
        
        float startY, endY;
        if ((segmentIndex & 1) == 0)
        {
            startY = centerY - amplitude;
            endY = centerY + amplitude;
        }
        else
        {
            startY = centerY + amplitude;
            endY = centerY - amplitude;
        }
        
        return pos.y > Mathf.Lerp(startY, endY, tInSegment);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float CalculateDistanceToEdge(Vector2 pos, Vector2 dir, float width, float height)
    {
        float dist = float.MaxValue;

        if (dir.x > 0) dist = Mathf.Min(dist, (width - pos.x) / dir.x);
        else if (dir.x < 0) dist = Mathf.Min(dist, -pos.x / dir.x);

        if (dir.y > 0) dist = Mathf.Min(dist, (height - pos.y) / dir.y);
        else if (dir.y < 0) dist = Mathf.Min(dist, -pos.y / dir.y);

        return Mathf.Max(0, dist);
    }

    private IEnumerator AnimateShardsOpen()
    {
        if (activeShardCount == 0) yield break;

        float maxDelay = 0f;
        for (int i = 0; i < activeShardCount; i++)
            if (staggerDelays[i] > maxDelay) maxDelay = staggerDelays[i];

        float totalDuration = openDuration + maxDelay;
        float elapsed = 0f;

        while (elapsed < totalDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            for (int i = 0; i < activeShardCount; i++)
            {
                float shardElapsed = elapsed - staggerDelays[i];
                if (shardElapsed > 0f)
                {
                    float t = Mathf.Clamp01(shardElapsed / openDuration);
                    shardPool[i].SetOpenAmount(openCurve.Evaluate(t));
                }
            }

            if (backgroundImage != null)
            {
                float bgT = Mathf.Clamp01(elapsed / backgroundFadeDuration);
                backgroundImage.color = new Color(backgroundColor.r, backgroundColor.g, backgroundColor.b, bgT * backgroundColor.a);
            }

            yield return null;
        }

        for (int i = 0; i < activeShardCount; i++)
            shardPool[i].SetOpenAmount(1f);

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

            for (int i = 0; i < activeShardCount; i++)
                shardPool[i].SetOpenAmount(t);

            if (backgroundImage != null)
            {
                float bgT = 1f - Mathf.Clamp01(elapsed / closeDuration);
                backgroundImage.color = new Color(backgroundColor.r, backgroundColor.g, backgroundColor.b, startAlpha * bgT);
            }

            yield return null;
        }

        for (int i = 0; i < activeShardCount; i++)
            shardPool[i].SetOpenAmount(0f);

        if (backgroundImage != null)
            backgroundImage.color = new Color(backgroundColor.r, backgroundColor.g, backgroundColor.b, 0f);
    }

    private void HideShards()
    {
        for (int i = 0; i < activeShardCount; i++)
        {
            shardPool[i].ResetShard();
            shardPool[i].gameObject.SetActive(false);
        }
        activeShardCount = 0;
    }

    private void OnDestroy()
    {
        if (screenshotTexture != null)
            Destroy(screenshotTexture);
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
