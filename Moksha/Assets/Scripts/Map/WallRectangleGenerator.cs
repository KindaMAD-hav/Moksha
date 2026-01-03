using UnityEngine;

public class WallRectangleGenerator : MonoBehaviour
{
    [Header("Modules")]
    public ModuleSet modules;

    [Header("Wall Counts")]
    public int lengthWallCount = 10;
    public int breadthWallCount = 6;

    [Header("Parent")]
    public Transform generatedParent;

    [Header("Wall Orientation")]
    public Vector3 wallBaseEuler = Vector3.zero;
    public bool applyWallRotation = true;

    [Header("Placement Fixes")]
    [Tooltip("Small overlap to remove floating point gaps (0.01–0.05)")]
    public float joinEpsilon = 0.02f;

    [ContextMenu("Generate Walls")]
    public void GenerateWalls()
    {
        if (modules == null || modules.wallStraightVariants.Length == 0)
        {
            Debug.LogError("No wall modules assigned.");
            return;
        }

        if (generatedParent == null)
        {
            var go = new GameObject("Generated_Walls");
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;
            generatedParent = go.transform;
        }

        Clear();

        Vector3 pos = Vector3.zero;

        // Pick one wall to measure thickness (all share same profile)
        var temp = Instantiate(modules.wallStraightVariants[0]);
        float thickness = GetWallThickness(temp) * 0.5f;
        DestroyImmediate(temp);

        // Bottom (+X)
        BuildWallLine(pos, Vector3.right, lengthWallCount, Vector3.forward, out pos);

        // Shift inward before turning corner
        pos += Vector3.forward * thickness;

        // Right (+Z)
        BuildWallLine(pos, Vector3.forward, breadthWallCount, Vector3.left, out pos);

        pos += Vector3.left * thickness;

        // Top (-X)
        BuildWallLine(pos, Vector3.left, lengthWallCount, Vector3.back, out pos);

        pos += Vector3.back * thickness;

        // Left (-Z)
        BuildWallLine(pos, Vector3.back, breadthWallCount, Vector3.right, out pos);
    }

    private void BuildWallLine(
    Vector3 startPos,
    Vector3 direction,
    int count,
    Vector3 faceDirection,
    out Vector3 endPos
)
    {
        Vector3 pos = startPos;
        direction = direction.normalized;
        faceDirection = faceDirection.normalized;

        // Construct rotation from explicit axes
        Quaternion wallRotation = Quaternion.LookRotation(faceDirection, Vector3.up);
        wallRotation = Quaternion.FromToRotation(
            wallRotation * Vector3.right,
            direction
        ) * wallRotation;

        if (applyWallRotation)
            wallRotation *= Quaternion.Euler(wallBaseEuler);

        for (int i = 0; i < count; i++)
        {
            var prefab = modules.wallStraightVariants[
                Random.Range(0, modules.wallStraightVariants.Length)
            ];

            var seg = Instantiate(prefab, generatedParent);
            seg.transform.localPosition = pos;
            seg.transform.localRotation = wallRotation;

            float length = GetActualWallLength(seg, direction);
            pos += direction * length;
        }

        endPos = pos;
    }



    // -----------------------------
    // Length calculation (important)
    // -----------------------------

    private float GetActualWallLength(GameObject wall, Vector3 direction)
    {
        direction = direction.normalized;

        var rend = wall.GetComponentInChildren<Renderer>();
        if (rend == null)
        {
            Debug.LogWarning("Wall has no Renderer, falling back to module length.");
            return modules.wallSegmentLength;
        }

        Bounds b = rend.bounds;

        // Project AABB extents onto direction
        Vector3 e = b.extents;

        float projectedHalf =
            Mathf.Abs(direction.x) * e.x +
            Mathf.Abs(direction.y) * e.y +
            Mathf.Abs(direction.z) * e.z;

        return projectedHalf * 2f;
    }

    [ContextMenu("Clear")]
    private void Clear()
    {
        if (generatedParent == null) return;

        for (int i = generatedParent.childCount - 1; i >= 0; i--)
            DestroyImmediate(generatedParent.GetChild(i).gameObject);
    }

    private float GetWallThickness(GameObject wall)
    {
        var rend = wall.GetComponentInChildren<Renderer>();
        if (rend == null) return 0f;

        // thickness assumed along local Z
        return rend.bounds.size.z;
    }

}
