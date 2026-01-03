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

    [Header("Input")]
    [SerializeField] private KeyCode pauseKey = KeyCode.Escape;

    [Header("Shard Settings")]
    [SerializeField] private int shardColumns = 5;
    [SerializeField] private int shardRows = 4;

    [Header("Animation Settings")]
    [SerializeField] private float openDuration = 0.6f;
    [SerializeField] private float closeDuration = 0.4f;
    [SerializeField] private float maxScatterDistance = 300f;
    [SerializeField] private float maxRotation = 45f;
    [SerializeField] private float staggerAmount = 0.05f;
    [SerializeField] private AnimationCurve openCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve closeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Center Gap")]
    [SerializeField] private float centerGapWidth = 400f;
    [SerializeField] private float centerGapHeight = 300f;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

    // Runtime
    private List<UIGlassShard> shards = new List<UIGlassShard>();
    private Texture2D screenshotTexture;
    private bool isPaused;
    private bool isAnimating;
    private Coroutine animationCoroutine;

    public bool IsPaused => isPaused;

    private void Awake()
    {
        Log("ShatteredPauseMenu Awake called");
        
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            Log($"Main camera auto-assigned: {(mainCamera != null ? mainCamera.name : "NULL")}");
        }

        if (menuCanvas != null)
        {
            menuCanvas.gameObject.SetActive(false);
            Log("Menu canvas hidden");
        }
        else
        {
            Log("WARNING: menuCanvas is not assigned!");
        }

        if (menuContent != null)
        {
            menuContent.SetActive(false);
        }

        if (shardContainer == null)
        {
            Log("WARNING: shardContainer is not assigned!");
        }
    }

    private void Start()
    {
        Log($"ShatteredPauseMenu Start - Pause key is: {pauseKey}");
    }

    private void Update()
    {
        if (Input.GetKeyDown(pauseKey))
        {
            Log($"Pause key ({pauseKey}) pressed! isPaused={isPaused}, isAnimating={isAnimating}");
            
            if (!isAnimating)
            {
                if (isPaused)
                    Resume();
                else
                    Pause();
            }
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

        // Capture screenshot at end of frame
        yield return new WaitForEndOfFrame();
        CaptureScreenshot();

        // Show canvas first (so shards are visible)
        if (menuCanvas != null)
            menuCanvas.gameObject.SetActive(true);

        // Create shards
        CreateShards();
        Log($"Created {shards.Count} shards");

        // Pause time
        Time.timeScale = 0f;
        isPaused = true;

        // Animate shards opening
        yield return StartCoroutine(AnimateShardsOpen());

        // Show menu content after shards open
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

        yield return StartCoroutine(AnimateShardsClose());

        ClearShards();

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
        Log($"Capturing screenshot: {width}x{height}");

        if (screenshotTexture == null || screenshotTexture.width != width || screenshotTexture.height != height)
        {
            if (screenshotTexture != null)
                Destroy(screenshotTexture);

            screenshotTexture = new Texture2D(width, height, TextureFormat.RGB24, false);
        }

        // Read pixels directly from screen
        screenshotTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        screenshotTexture.Apply();
        Log("Screenshot captured");
    }

    private void CreateShards()
    {
        ClearShards();

        if (shardContainer == null)
        {
            Log("ERROR: shardContainer is null!");
            return;
        }

        // Get the canvas rect size
        RectTransform canvasRect = menuCanvas.GetComponent<RectTransform>();
        float canvasWidth = canvasRect.rect.width;
        float canvasHeight = canvasRect.rect.height;
        
        float shardWidth = canvasWidth / shardColumns;
        float shardHeight = canvasHeight / shardRows;

        Vector2 canvasCenter = new Vector2(canvasWidth / 2f, canvasHeight / 2f);

        Log($"Canvas size: {canvasWidth}x{canvasHeight}, Shard size: {shardWidth}x{shardHeight}");

        for (int row = 0; row < shardRows; row++)
        {
            for (int col = 0; col < shardColumns; col++)
            {
                // Calculate shard position (bottom-left origin)
                float x = col * shardWidth + shardWidth / 2f;
                float y = row * shardHeight + shardHeight / 2f;
                Vector2 shardCenter = new Vector2(x, y);

                // Calculate UV rect for this shard
                float uvX = (float)col / shardColumns;
                float uvY = (float)row / shardRows;
                float uvW = 1f / shardColumns;
                float uvH = 1f / shardRows;
                Rect uvRect = new Rect(uvX, uvY, uvW, uvH);

                // Calculate scatter direction (away from center)
                Vector2 dirFromCenter = shardCenter - canvasCenter;
                if (dirFromCenter.sqrMagnitude < 0.01f)
                    dirFromCenter = Random.insideUnitCircle.normalized;
                else
                    dirFromCenter.Normalize();

                // Distance-based scatter
                float distFromCenter = Vector2.Distance(shardCenter, canvasCenter);
                float maxDist = Vector2.Distance(Vector2.zero, canvasCenter);
                float normalizedDist = distFromCenter / maxDist;

                Vector2 scatterOffset = dirFromCenter * maxScatterDistance * (0.3f + normalizedDist * 0.7f);

                // Extra offset to create center gap
                float centerThresholdX = canvasWidth * 0.35f;
                float centerThresholdY = canvasHeight * 0.35f;

                if (Mathf.Abs(shardCenter.x - canvasCenter.x) < centerThresholdX)
                {
                    float gapMult = 1f - Mathf.Abs(shardCenter.x - canvasCenter.x) / centerThresholdX;
                    scatterOffset.x += Mathf.Sign(dirFromCenter.x) * centerGapWidth * gapMult;
                }
                if (Mathf.Abs(shardCenter.y - canvasCenter.y) < centerThresholdY)
                {
                    float gapMult = 1f - Mathf.Abs(shardCenter.y - canvasCenter.y) / centerThresholdY;
                    scatterOffset.y += Mathf.Sign(dirFromCenter.y) * centerGapHeight * gapMult;
                }

                float randomRot = Random.Range(-maxRotation, maxRotation);

                // Create UI shard
                UIGlassShard shard = CreateUIShard(shardCenter, shardWidth, shardHeight, uvRect, scatterOffset, randomRot);
                shards.Add(shard);
            }
        }
    }

    private UIGlassShard CreateUIShard(Vector2 position, float width, float height, Rect uvRect, 
        Vector2 scatterOffset, float rotation)
    {
        GameObject shardObj = new GameObject($"Shard_{shards.Count}");
        shardObj.transform.SetParent(shardContainer, false);

        // Setup RectTransform
        RectTransform rt = shardObj.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = new Vector2(width, height);

        // Add RawImage to display the screenshot portion
        RawImage rawImage = shardObj.AddComponent<RawImage>();
        rawImage.texture = screenshotTexture;
        rawImage.uvRect = uvRect;
        rawImage.raycastTarget = false;

        // Add shard component
        UIGlassShard shard = shardObj.AddComponent<UIGlassShard>();
        shard.Initialize(position, scatterOffset, rotation);

        return shard;
    }

    private IEnumerator AnimateShardsOpen()
    {
        if (shards.Count == 0)
        {
            Log("WARNING: No shards to animate!");
            yield break;
        }

        float[] delays = CalculateStaggerDelays();
        float maxDelay = 0f;
        foreach (float d in delays)
            maxDelay = Mathf.Max(maxDelay, d);

        float totalDuration = openDuration + maxDelay;
        float elapsed = 0f;

        Log($"Animating {shards.Count} shards over {totalDuration:F2}s");

        while (elapsed < totalDuration)
        {
            elapsed += Time.unscaledDeltaTime;

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

            yield return null;
        }

        foreach (var shard in shards)
            shard.SetOpenAmount(1f);
    }

    private IEnumerator AnimateShardsClose()
    {
        float elapsed = 0f;

        while (elapsed < closeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = 1f - closeCurve.Evaluate(elapsed / closeDuration);

            foreach (var shard in shards)
                shard.SetOpenAmount(t);

            yield return null;
        }

        foreach (var shard in shards)
            shard.SetOpenAmount(0f);
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
