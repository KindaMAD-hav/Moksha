using System;
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
    private const int DIAMOND_SHARD_COUNT = 4;

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

    public static event Action OnPaused;
    public static event Action OnResumed;


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
        poolCapacity = DIAMOND_SHARD_COUNT; // <<< THIS WAS MISSING
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
        OnPaused?.Invoke();

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
        OnResumed?.Invoke();

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
        float w = canvasRect.rect.width;
        float h = canvasRect.rect.height;

        Vector2 center = new Vector2(w * 0.5f, h * 0.5f);
        float diamondRadius = Mathf.Min(w, h) * 0.18f; // tweak this

        Vector2 top = center + Vector2.up * diamondRadius;
        Vector2 right = center + Vector2.right * diamondRadius;
        Vector2 bottom = center + Vector2.down * diamondRadius;
        Vector2 left = center + Vector2.left * diamondRadius;

        activeShardCount = 4;

        CreateDiamondShard(0,
            new Vector2(0, h), left, top,
            new Vector2(-1, 1));

        CreateDiamondShard(1,
            top, new Vector2(w, h), right,
            new Vector2(1, 1));

        CreateDiamondShard(2,
            left, new Vector2(0, 0), bottom,
            new Vector2(-1, -1));

        CreateDiamondShard(3,
            bottom, right, new Vector2(w, 0),
            new Vector2(1, -1));

        for (int i = 0; i < activeShardCount; i++)
            staggerDelays[i] = i * staggerAmount;

    }

    private void CreateDiamondShard(
    int index,
    Vector2 a, Vector2 b, Vector2 c,
    Vector2 moveDir)
    {
        UIGlassShard shard = shardPool[index];

        Vector2 center = (a + b + c) / 3f;

        Rect uv = new Rect(
            a.x / canvasRect.rect.width,
            a.y / canvasRect.rect.height,
            (b.x - a.x) / canvasRect.rect.width,
            (c.y - a.y) / canvasRect.rect.height
        );

        shard.Configure(
            center,
            canvasRect.rect.width,
            canvasRect.rect.height,
            uv,
            screenshotTexture
        );

        Vector2 scatter = moveDir.normalized * edgeOffset;

        shard.Initialize(
            center,
            scatter,
            UnityEngine.Random.Range(-12f, 12f)
        );

        shard.SetOpenAmount(0f);
        shard.gameObject.SetActive(true);
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
                    dir = UnityEngine.Random.insideUnitCircle.normalized;
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
    public void SilentPause()
    {
        if (isPaused) return;

        isPaused = true;
        Time.timeScale = 0f;

        // fire pause event WITHOUT opening UI
        OnPaused?.Invoke();
    }

    public void SilentResume()
    {
        if (!isPaused) return;

        isPaused = false;
        Time.timeScale = 1f;

        OnResumed?.Invoke();
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
