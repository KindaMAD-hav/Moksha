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

    [Range(0f, 1f)]
    public float openAmount;

    private RectTransform rt;
    private Vector2 startPos;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        startPos = rt.anchoredPosition;
    }

#if UNITY_EDITOR
    void OnEnable()
    {
        if (!Application.isPlaying)
        {
            rt = GetComponent<RectTransform>();
            startPos = rt.anchoredPosition;
        }
    }
#endif

    public void SetOpen(float t)
    {
        openAmount = t;
        Apply();
    }

    void Apply()
    {
        if (!rt) return;

        Vector2 offset = moveDirection.normalized * moveDistance * openAmount;
        rt.anchoredPosition = startPos + offset;

        rt.localRotation =
            Quaternion.Euler(0f, 0f, rotationDegrees * openAmount);

        float scale = 1f + scaleAmount * openAmount;
        rt.localScale = new Vector3(scale, scale, 1f);
    }

#if UNITY_EDITOR
    void Update()
    {
        if (!Application.isPlaying)
            Apply();
    }
#endif
}
