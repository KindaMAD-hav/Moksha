using UnityEngine;

public class WallLineGenerator : MonoBehaviour
{
    [Header("Wall Prefabs")]
    public GameObject[] wallPrefabs;

    [Header("Line Settings")]
    public int wallCount = 10;
    public Vector3 startLocalPosition = Vector3.zero;
    public Vector3 direction = Vector3.right;        // length direction (local)
    public Vector3 faceDirection = Vector3.forward;  // facing direction (local)

    [Header("Rotation")]
    public Vector3 wallBaseEuler = Vector3.zero;

    [Header("Gap Fix")]
    [Tooltip("Small overlap to hide precision gaps (0.01–0.05)")]
    public float joinEpsilon = 0.02f;

    [ContextMenu("Generate Wall Line")]
    public void Generate()
    {
        if (wallPrefabs == null || wallPrefabs.Length == 0)
        {
            Debug.LogError("No wall prefabs assigned.");
            return;
        }

        Clear();

        Vector3 dirLocal = direction.normalized;
        Vector3 faceLocal = faceDirection.normalized;

        // Build rotation so:
        // local +X → dirLocal
        // local +Z → faceLocal
        Quaternion wallRotation = Quaternion.LookRotation(faceLocal, Vector3.up);
        wallRotation = Quaternion.FromToRotation(
            wallRotation * Vector3.right,
            dirLocal
        ) * wallRotation;

        wallRotation *= Quaternion.Euler(wallBaseEuler);

        Vector3 posLocal = startLocalPosition;

        for (int i = 0; i < wallCount; i++)
        {
            var prefab = wallPrefabs[Random.Range(0, wallPrefabs.Length)];
            var seg = Instantiate(prefab, transform);

            seg.transform.localPosition = posLocal;
            seg.transform.localRotation = wallRotation;

            float length = GetWallLength(seg, transform.TransformDirection(dirLocal));
            posLocal += dirLocal * Mathf.Max(0.01f, length - joinEpsilon);
        }
    }

    // ------------------------------------------------------
    // Utils
    // ------------------------------------------------------
    public void Vanish()
    {
        // Simple & safe: disable renderers
        var renderers = GetComponentsInChildren<Renderer>(includeInactive: true);
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].enabled = false;
        }

        // Optional: also disable colliders if walls have any
        var colliders = GetComponentsInChildren<Collider>(includeInactive: true);
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }
    }

    private float GetWallLength(GameObject wall, Vector3 worldDir)
    {
        worldDir = worldDir.normalized;

        var rend = wall.GetComponentInChildren<Renderer>();
        if (rend == null)
        {
            Debug.LogWarning("Wall has no Renderer, using fallback length 1.");
            return 1f;
        }

        Bounds b = rend.bounds;
        Vector3 e = b.extents;

        // Project AABB onto direction
        return 2f * (
            Mathf.Abs(worldDir.x) * e.x +
            Mathf.Abs(worldDir.y) * e.y +
            Mathf.Abs(worldDir.z) * e.z
        );
    }

    [ContextMenu("Clear")]
    public void Clear()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);
    }
}
