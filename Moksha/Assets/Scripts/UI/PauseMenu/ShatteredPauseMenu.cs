using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ShatteredPauseMenu : MonoBehaviour
{
    public static ShatteredPauseMenu Instance;

    public static event Action OnPaused;
    public static event Action OnResumed;

    [Header("References")]
    [SerializeField] private Canvas menuCanvas;
    [SerializeField] private GameObject menuContent;

    [Tooltip("Optional. If empty, will be auto-created under the canvas.")]
    [SerializeField] private Image background;

    [Tooltip("Put your 4 shard root objects here (each must have a PauseShard).")]
    [SerializeField] private PauseShard[] shards;

    [Header("Input")]
    [SerializeField] private KeyCode pauseKey = KeyCode.Escape;

    [Header("Timing")]
    [SerializeField] private float openDuration = 0.35f;
    [SerializeField] private float closeDuration = 0.25f;

    [Header("Background")]
    [SerializeField] private Color backgroundColor = new Color(0, 0, 0, 0.75f);

    private Texture2D screenshot;
    private bool isPaused;
    private bool animating;

    // Auto-collected from shards
    private RawImage[] shardImages;

    public bool IsPaused => isPaused;

    void Awake()
    {
        Instance = this;

        if (menuCanvas != null) menuCanvas.gameObject.SetActive(false);
        if (menuContent != null) menuContent.SetActive(false);

        EnsureBackground();
        CollectShardImages();

        // Put shards in closed state in case they were moved in editor
        if (shards != null)
        {
            for (int i = 0; i < shards.Length; i++)
                if (shards[i] != null) shards[i].SetOpen(0f);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(pauseKey) && !animating)
        {
            if (isPaused) Resume();
            else Pause();
        }
    }

    public void Pause()
    {
        if (isPaused || animating) return;
        StartCoroutine(PauseRoutine());
    }

    public void Resume()
    {
        if (!isPaused || animating) return;
        StartCoroutine(ResumeRoutine());
    }

    public void SilentPause()
    {
        if (isPaused) return;

        isPaused = true;
        Time.timeScale = 0f;
        OnPaused?.Invoke();
    }

    public void SilentResume()
    {
        if (!isPaused) return;

        isPaused = false;
        Time.timeScale = 1f;
        OnResumed?.Invoke();
    }

    IEnumerator PauseRoutine()
    {
        animating = true;

        // Re-collect in case you changed hierarchy while testing
        EnsureBackground();
        CollectShardImages();

        yield return new WaitForEndOfFrame();
        CaptureScreenshot();

        if (shardImages == null || shardImages.Length == 0)
        {
            Debug.LogError("[ShatteredPauseMenu] No RawImages found under shards. Each shard needs a RawImage (usually as a child) to display the screenshot.");
            animating = false;
            yield break;
        }

        for (int i = 0; i < shardImages.Length; i++)
            if (shardImages[i] != null) shardImages[i].texture = screenshot;

        if (menuCanvas != null) menuCanvas.gameObject.SetActive(true);
        if (background != null)
        {
            background.gameObject.SetActive(true);
            background.color = Color.clear;
        }

        Time.timeScale = 0f;
        isPaused = true;
        OnPaused?.Invoke();

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.0001f, openDuration);
            float eased = Mathf.SmoothStep(0, 1, t);

            if (shards != null)
                for (int i = 0; i < shards.Length; i++)
                    if (shards[i] != null) shards[i].SetOpen(eased);

            if (background != null)
                background.color = Color.Lerp(Color.clear, backgroundColor, eased);

            yield return null;
        }

        if (shards != null)
            for (int i = 0; i < shards.Length; i++)
                if (shards[i] != null) shards[i].SetOpen(1f);

        if (background != null) background.color = backgroundColor;
        if (menuContent != null) menuContent.SetActive(true);

        animating = false;
    }

    IEnumerator ResumeRoutine()
    {
        animating = true;

        if (menuContent != null) menuContent.SetActive(false);

        float t = 1f;
        while (t > 0f)
        {
            t -= Time.unscaledDeltaTime / Mathf.Max(0.0001f, closeDuration);

            if (shards != null)
                for (int i = 0; i < shards.Length; i++)
                    if (shards[i] != null) shards[i].SetOpen(t);

            if (background != null)
                background.color = Color.Lerp(Color.clear, backgroundColor, t);

            yield return null;
        }

        if (shards != null)
            for (int i = 0; i < shards.Length; i++)
                if (shards[i] != null) shards[i].SetOpen(0f);

        if (background != null) background.gameObject.SetActive(false);
        if (menuCanvas != null) menuCanvas.gameObject.SetActive(false);

        Time.timeScale = 1f;
        isPaused = false;
        OnResumed?.Invoke();

        animating = false;
    }

    void CaptureScreenshot()
    {
        if (screenshot == null || screenshot.width != Screen.width || screenshot.height != Screen.height)
        {
            screenshot = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        }

        screenshot.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        screenshot.Apply();
    }

    void EnsureBackground()
    {
        if (background != null) return;

        if (menuCanvas == null)
        {
            Debug.LogError("[ShatteredPauseMenu] MenuCanvas is not assigned.");
            return;
        }

        // Create a background image under the canvas (behind shards/menu)
        GameObject bg = new GameObject("PauseBackground", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(menuCanvas.transform, false);
        bg.transform.SetAsFirstSibling();

        RectTransform rt = bg.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        background = bg.GetComponent<Image>();
        background.raycastTarget = false;
        background.color = Color.clear;
        background.gameObject.SetActive(false);
    }

    void CollectShardImages()
    {
        if (shards == null || shards.Length == 0)
        {
            shardImages = Array.Empty<RawImage>();
            return;
        }

        // One RawImage per shard (usually as a child under the mask)
        shardImages = new RawImage[shards.Length];
        for (int i = 0; i < shards.Length; i++)
        {
            if (shards[i] == null) continue;
            shardImages[i] = shards[i].GetComponentInChildren<RawImage>(true);
        }
    }
}
