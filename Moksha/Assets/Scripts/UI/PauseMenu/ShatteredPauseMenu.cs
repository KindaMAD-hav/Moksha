using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Shattered glass pause menu effect.
/// Takes a screenshot, shatters it into pieces, and animates them apart to reveal menu.
/// </summary>
public class ShatteredPauseMenu : MonoBehaviour
{
    public static ShatteredPauseMenu Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Canvas menuCanvas;
    [SerializeField] private GameObject menuContent; // Your actual menu buttons go here
    [SerializeField] private Transform shardContainer;

    [Header("Input")]
    [SerializeField] private InputActionReference pauseAction;

    [Header("Shard Settings")]
    [SerializeField] private int shardColumns = 5;
    [SerializeField] private int shardRows = 4;
    [SerializeField] private Material shardMaterial; // Uses the screenshot texture
    [SerializeField] private bool randomizeShards = true;
    [SerializeField] private float shardDepth = 0.01f;

    [Header("Animation Settings")]
    [SerializeField] private float openDuration = 0.6f;
    [SerializeField] private float closeDuration = 0.4f;
    [SerializeField] private float maxScatterDistance = 300f;
    [SerializeField] private float maxRotation = 45f;
    [SerializeField] private float staggerAmount = 0.05f;
    [SerializeField] private AnimationCurve openCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve closeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Center Gap")]
    [SerializeField] private float centerGapWidth = 400f; // How much space opens in center
    [SerializeField] private float centerGapHeight = 300f;

    // Runtime
    private List<GlassShard> shards = new List<GlassShard>();
    private RenderTexture screenshotRT;
    private Texture2D screenshotTexture;
    private bool isPaused;
    private bool isAnimating;
    private Coroutine animationCoroutine;

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
            menuCanvas.gameObject.SetActive(false);

        if (menuContent != null)
            menuContent.SetActive(false);
    }

    private void OnEnable()
    {
        if (pauseAction != null)
        {
            pauseAction.action.Enable();
            pauseAction.action.performed += OnPausePressed;
        }
    }

    private void OnDisable()
    {
        if (pauseAction != null)
        {
            pauseAction.action.performed -= OnPausePressed;
        }
    }

    private void OnPausePressed(InputAction.CallbackContext context)
    {
        if (isAnimating) return;

        if (isPaused)
            Resume();
        else
            Pause();
    }

    /// <summary>
    /// Pause the game with shattered glass effect
    /// </summary>
    public void Pause()
    {
        if (isPaused || isAnimating) return;

        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

        animationCoroutine = StartCoroutine(PauseRoutine());
    }

    /// <summary>
    /// Resume the game, closing the shattered effect
    /// </summary>
    public void Resume()
    {
        if (!isPaused || isAnimating) return;

        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

        animationCoroutine = StartCoroutine(ResumeRoutine());
    }

    private IEnumerator PauseRoutine()
    {
        isAnimating = true;

        // Capture screenshot
        yield return new WaitForEndOfFrame();
        CaptureScreenshot();

        // Create shards
        CreateShards();

        // Show canvas
        if (menuCanvas != null)
            menuCanvas.gameObject.SetActive(true);

        // Pause time
        Time.timeScale = 0f;
        isPaused = true;

        // Animate shards opening
        yield return StartCoroutine(AnimateShardsOpen());

        // Show menu content after shards open
        if (menuContent != null)
            menuContent.SetActive(true);

        isAnimating = false;
    }

    private IEnumerator ResumeRoutine()
    {
        isAnimating = true;

        // Hide menu content first
        if (menuContent != null)
            menuContent.SetActive(false);

        // Animate shards closing
        yield return StartCoroutine(AnimateShardsClose());

        // Cleanup
        ClearShards();

        // Hide canvas
        if (menuCanvas != null)
            menuCanvas.gameObject.SetActive(false);

        // Resume time
        Time.timeScale = 1f;
        isPaused = false;

        isAnimating = false;
    }

    private void CaptureScreenshot()
    {
        int width = Screen.width;
        int height = Screen.height;

        // Create or resize render texture
        if (screenshotRT == null || screenshotRT.width != width || screenshotRT.height != height)
        {
            if (screenshotRT != null)
                screenshotRT.Release();

            screenshotRT = new RenderTexture(width, height, 24);
        }

        // Render camera to texture
        RenderTexture currentRT = mainCamera.targetTexture;
        mainCamera.targetTexture = screenshotRT;
        mainCamera.Render();
        mainCamera.targetTexture = currentRT;

        // Convert to Texture2D
        if (screenshotTexture == null || screenshotTexture.width != width || screenshotTexture.height != height)
        {
            if (screenshotTexture != null)
                Destroy(screenshotTexture);

            screenshotTexture = new Texture2D(width, height, TextureFormat.RGB24, false);
        }

        RenderTexture.active = screenshotRT;
        screenshotTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        screenshotTexture.Apply();
        RenderTexture.active = null;
    }

    private void CreateShards()
    {
        ClearShards();

        float screenWidth = Screen.width;
        float screenHeight = Screen.height;
        float shardWidth = screenWidth / shardColumns;
        float shardHeight = screenHeight / shardRows;

        Vector2 screenCenter = new Vector2(screenWidth / 2f, screenHeight / 2f);

        for (int row = 0; row < shardRows; row++)
        {
            for (int col = 0; col < shardColumns; col++)
            {
                // Calculate shard bounds
                float x = col * shardWidth;
                float y = row * shardHeight;

                // Add randomization to create irregular shapes
                Vector2[] corners = new Vector2[4];
                corners[0] = new Vector2(x, y); // Bottom-left
                corners[1] = new Vector2(x + shardWidth, y); // Bottom-right
                corners[2] = new Vector2(x + shardWidth, y + shardHeight); // Top-right
                corners[3] = new Vector2(x, y + shardHeight); // Top-left

                if (randomizeShards)
                {
                    float randomAmount = Mathf.Min(shardWidth, shardHeight) * 0.2f;
                    for (int i = 0; i < 4; i++)
                    {
                        // Don't randomize edge corners
                        bool isEdgeX = (col == 0 && (i == 0 || i == 3)) || (col == shardColumns - 1 && (i == 1 || i == 2));
                        bool isEdgeY = (row == 0 && (i == 0 || i == 1)) || (row == shardRows - 1 && (i == 2 || i == 3));

                        if (!isEdgeX)
                            corners[i].x += Random.Range(-randomAmount, randomAmount);
                        if (!isEdgeY)
                            corners[i].y += Random.Range(-randomAmount, randomAmount);
                    }
                }

                // Calculate center of this shard
                Vector2 shardCenter = (corners[0] + corners[1] + corners[2] + corners[3]) / 4f;

                // Calculate scatter direction (away from center)
                Vector2 dirFromCenter = (shardCenter - screenCenter).normalized;

                // Calculate scatter distance based on distance from center
                float distFromCenter = Vector2.Distance(shardCenter, screenCenter);
                float maxDist = Vector2.Distance(Vector2.zero, screenCenter);
                float normalizedDist = distFromCenter / maxDist;

                // Shards near center move more to create the gap
                Vector2 scatterOffset = dirFromCenter * maxScatterDistance * (0.5f + normalizedDist * 0.5f);

                // Add extra offset to create center gap
                if (Mathf.Abs(shardCenter.x - screenCenter.x) < screenWidth * 0.3f)
                    scatterOffset.x += Mathf.Sign(dirFromCenter.x) * centerGapWidth * 0.5f;
                if (Mathf.Abs(shardCenter.y - screenCenter.y) < screenHeight * 0.3f)
                    scatterOffset.y += Mathf.Sign(dirFromCenter.y) * centerGapHeight * 0.5f;

                // Random rotation
                float randomRot = Random.Range(-maxRotation, maxRotation);

                // Create the shard
                GlassShard shard = CreateShard(corners, shardCenter, scatterOffset, randomRot, screenWidth, screenHeight);
                shards.Add(shard);
            }
        }
    }

    private GlassShard CreateShard(Vector2[] corners, Vector2 center, Vector2 scatterOffset, float rotation,
        float screenWidth, float screenHeight)
    {
        GameObject shardObj = new GameObject("Shard");
        shardObj.transform.SetParent(shardContainer, false);

        // Add RectTransform for UI
        RectTransform rt = shardObj.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = center;
        rt.sizeDelta = new Vector2(100, 100); // Will be overridden by mesh

        // Create mesh for the shard shape
        MeshFilter meshFilter = shardObj.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = shardObj.AddComponent<MeshRenderer>();

        Mesh mesh = CreateShardMesh(corners, center, screenWidth, screenHeight);
        meshFilter.mesh = mesh;

        // Setup material with screenshot
        Material mat = new Material(shardMaterial);
        mat.mainTexture = screenshotTexture;
        meshRenderer.material = mat;

        // Add shard component
        GlassShard shard = shardObj.AddComponent<GlassShard>();
        shard.Initialize(center, scatterOffset, rotation);

        return shard;
    }

    private Mesh CreateShardMesh(Vector2[] corners, Vector2 center, float screenWidth, float screenHeight)
    {
        Mesh mesh = new Mesh();

        // Convert corners to local space (relative to center)
        Vector3[] vertices = new Vector3[4];
        Vector2[] uvs = new Vector2[4];

        for (int i = 0; i < 4; i++)
        {
            // Vertex position relative to center
            vertices[i] = new Vector3(corners[i].x - center.x, corners[i].y - center.y, 0);

            // UV based on screen position
            uvs[i] = new Vector2(corners[i].x / screenWidth, corners[i].y / screenHeight);
        }

        int[] triangles = new int[] { 0, 2, 1, 0, 3, 2 };

        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    private IEnumerator AnimateShardsOpen()
    {
        float elapsed = 0f;

        // Calculate stagger delays based on distance from center
        float[] delays = new float[shards.Count];
        float maxDelay = 0f;

        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        for (int i = 0; i < shards.Count; i++)
        {
            float dist = Vector2.Distance(shards[i].OriginalPosition, screenCenter);
            float maxDist = Vector2.Distance(Vector2.zero, screenCenter);
            delays[i] = (1f - dist / maxDist) * staggerAmount * shards.Count;
            maxDelay = Mathf.Max(maxDelay, delays[i]);
        }

        float totalDuration = openDuration + maxDelay;

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

        // Ensure all shards are fully open
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

        // Ensure all shards are fully closed
        foreach (var shard in shards)
            shard.SetOpenAmount(0f);
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

    private void OnDestroy()
    {
        if (screenshotRT != null)
        {
            screenshotRT.Release();
            Destroy(screenshotRT);
        }

        if (screenshotTexture != null)
            Destroy(screenshotTexture);

        ClearShards();
    }

#if UNITY_EDITOR
    [ContextMenu("Test Pause")]
    public void TestPause()
    {
        if (isPaused)
            Resume();
        else
            Pause();
    }
#endif
}
