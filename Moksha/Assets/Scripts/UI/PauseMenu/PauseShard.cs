using UnityEngine;

[ExecuteAlways]
public class PauseShard : MonoBehaviour
{
    [Header("Movement")]
    public Vector2 moveDirection = Vector2.up;
    public float moveDistance = 300f;

    [Header("Rotation")]
    public float rotationDegrees = 12f;

    [Header("Scale")]
    public float scaleAmount = 0.03f;

    [Header("Lerp")]
    [Tooltip("Higher = snappier, Lower = smoother")]
    public float lerpSpeed = 12f;

    [Range(0f, 1f)]
    public float openAmount;          // target (set by pause menu)

    private float currentOpen;         // smoothed value

    private RectTransform rt;
    private Vector2 startPos;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        startPos = rt.anchoredPosition;
        currentOpen = openAmount;
    }

#if UNITY_EDITOR
    void OnEnable()
    {
        if (!Application.isPlaying)
        {
            rt = GetComponent<RectTransform>();
            startPos = rt.anchoredPosition;
            currentOpen = openAmount;
        }
    }
#endif

    public void SetOpen(float t)
    {
        openAmount = Mathf.Clamp01(t);
    }

    void Update()
    {
        float dt = Application.isPlaying
            ? Time.unscaledDeltaTime
            : Time.deltaTime;

        // Smooth toward target
        currentOpen = Mathf.Lerp(
            currentOpen,
            openAmount,
            1f - Mathf.Exp(-lerpSpeed * dt)
        );

        Apply(currentOpen);
    }

    void Apply(float t)
    {
        if (!rt) return;

        Vector2 offset = moveDirection.normalized * moveDistance * t;
        rt.anchoredPosition = startPos + offset;

        rt.localRotation =
            Quaternion.Euler(0f, 0f, rotationDegrees * t);

        float scale = 1f + scaleAmount * t;
        rt.localScale = new Vector3(scale, scale, 1f);
    }
}
