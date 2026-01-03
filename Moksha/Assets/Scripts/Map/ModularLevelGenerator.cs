using System.Collections.Generic;
using UnityEngine;

public class ModularLevelGenerator : MonoBehaviour
{
    [Header("Modules")]
    public ModuleSet modules;

    [Header("Random Seed")]
    public bool useRandomSeed = true;
    public int seed = 12345;

    [Header("Parent / Cleanup")]
    public Transform generatedParent;
    public bool autoClearBeforeGenerate = true;

    [Header("Generation Toggles")]
    public bool generateFloor = true;
    public bool generatePerimeterWalls = true;

#if UNITY_EDITOR
    [Header("Debug / Tools")]
    public bool generateWallStripDebug = false;
#endif

    [Header("Wall Orientation")]
    public Vector3 wallBaseEuler = Vector3.zero; // degrees
    public bool applyWallRotation = true;

    // ---------- Wall Strip (DEBUG ONLY) ----------
    [Header("Wall Strip Debug Settings")]
    public int wallSegments = 20;
    public Vector3 wallStartLocalPos = Vector3.zero;
    public Vector3 wallDirectionLocal = Vector3.forward;

    // ---------- Floor Area ----------
    [Header("Floor Area Settings")]
    public int floorWidth = 12;   // tiles in X
    public int floorHeight = 12;  // tiles in Z
    public Vector3 floorStartLocalPos = Vector3.zero;

    private System.Random rng;
    private int lastWallIndex = -1;
    private int lastFloorIndex = -1;

    // ------------------------------------------------------

    [ContextMenu("Generate")]
    public void Generate()
    {
        if (modules == null)
        {
            Debug.LogError("No ModuleSet assigned.");
            return;
        }

        if (generatedParent == null)
        {
            var go = new GameObject("Generated_Level");
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;
            generatedParent = go.transform;
        }

        if (autoClearBeforeGenerate)
            ClearGenerated();

        rng = useRandomSeed ? new System.Random(seed) : new System.Random();

        if (generateFloor)
            GenerateFloorArea();

        if (generatePerimeterWalls)
            GenerateRoomPerimeterWalls();

#if UNITY_EDITOR
        if (generateWallStripDebug)
            GenerateWallStripDebug();
#endif
    }

    [ContextMenu("Clear Generated")]
    public void ClearGenerated()
    {
        if (generatedParent == null) return;

        var toDestroy = new List<GameObject>();
        for (int i = 0; i < generatedParent.childCount; i++)
            toDestroy.Add(generatedParent.GetChild(i).gameObject);

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            foreach (var go in toDestroy)
                UnityEditor.Undo.DestroyObjectImmediate(go);
            return;
        }
#endif

        foreach (var go in toDestroy)
            Destroy(go);
    }

    // ------------------------------------------------------
    // FLOOR
    // ------------------------------------------------------

    private void GenerateFloorArea()
    {
        if (modules.floorTileVariants == null || modules.floorTileVariants.Length == 0)
        {
            Debug.LogError("ModuleSet has no floorTileVariants.");
            return;
        }

        for (int z = 0; z < floorHeight; z++)
        {
            for (int x = 0; x < floorWidth; x++)
            {
                var prefab = PickVariant(modules.floorTileVariants, ref lastFloorIndex);
                var tile = Instantiate(prefab, generatedParent);

                Vector3 pos = floorStartLocalPos +
                              new Vector3(
                                  x * modules.floorTileSize,
                                  0f,
                                  z * modules.floorTileSize
                              );

                tile.transform.localPosition = pos;
                tile.transform.localRotation = Quaternion.identity;
            }
        }
    }

    // ------------------------------------------------------
    // PERIMETER WALLS
    // ------------------------------------------------------

    private void GenerateRoomPerimeterWalls()
    {
        if (modules.wallStraightVariants == null || modules.wallStraightVariants.Length == 0)
            return;

        float tile = modules.floorTileSize;

        // ASSUMPTION: floorStartLocalPos is the CENTER of the first tile (0,0)
        // Tile centers go: 0, tile, 2*tile ... (floorWidth-1)*tile
        // So the OUTER BOUNDS are offset by tile/2
        float minX = floorStartLocalPos.x - tile * 0.5f;
        float maxX = floorStartLocalPos.x + (floorWidth - 1) * tile + tile * 0.5f;

        float minZ = floorStartLocalPos.z - tile * 0.5f;
        float maxZ = floorStartLocalPos.z + (floorHeight - 1) * tile + tile * 0.5f;

        float inset = 0f; // optional: push walls slightly outward if needed

        // Bottom (Z = minZ)
        BuildWallLineBetween(
            start: new Vector3(minX, floorStartLocalPos.y, minZ - inset),
            end: new Vector3(maxX, floorStartLocalPos.y, minZ - inset),
            faceDirection: Vector3.forward
        );

        // Top (Z = maxZ)
        BuildWallLineBetween(
            start: new Vector3(minX, floorStartLocalPos.y, maxZ + inset),
            end: new Vector3(maxX, floorStartLocalPos.y, maxZ + inset),
            faceDirection: Vector3.back
        );

        // Left (X = minX)
        BuildWallLineBetween(
            start: new Vector3(minX - inset, floorStartLocalPos.y, minZ),
            end: new Vector3(minX - inset, floorStartLocalPos.y, maxZ),
            faceDirection: Vector3.right
        );

        // Right (X = maxX)
        BuildWallLineBetween(
            start: new Vector3(maxX + inset, floorStartLocalPos.y, minZ),
            end: new Vector3(maxX + inset, floorStartLocalPos.y, maxZ),
            faceDirection: Vector3.left
        );
    }


    private void BuildWallLine(Vector3 start, Vector3 direction, float totalLength, Vector3 faceDirection)
    {
        Vector3 dirLocal = direction.normalized;

        // local -> world direction (because bounds are world space)
        Vector3 dirWorld = transform.TransformDirection(dirLocal).normalized;

        int count = Mathf.Max(1, Mathf.RoundToInt(totalLength / modules.wallSegmentLength));

        Quaternion baseRot = Quaternion.LookRotation(
            Vector3.Cross(Vector3.up, faceDirection),
            Vector3.up
        );

        // We'll track the "end" in LOCAL space, but compute half-length from WORLD bounds.
        Vector3 endPosLocal = start;

        for (int i = 0; i < count; i++)
        {
            var prefab = PickVariant(modules.wallStraightVariants, ref lastWallIndex);
            var seg = Instantiate(prefab, generatedParent);

            // Apply rotation FIRST so bounds reflect correct orientation
            seg.transform.localRotation = applyWallRotation
                ? baseRot * Quaternion.Euler(wallBaseEuler)
                : Quaternion.Euler(wallBaseEuler);

            // Temporarily place it (so bounds are valid in world space)
            seg.transform.localPosition = endPosLocal;

            float halfLen = GetWallHalfLength(seg, dirWorld);

            // Place center based on previous end + this half length
            seg.transform.localPosition = endPosLocal + dirLocal * halfLen;

            // Update end = this center + half length
            endPosLocal = seg.transform.localPosition + dirLocal * halfLen;
        }
    }

    private void BuildWallLineBetween(Vector3 start, Vector3 end, Vector3 faceDirection)
    {
        Vector3 dirLocal = (end - start).normalized;
        float remaining = Vector3.Distance(start, end);

        // bounds are world space, but our objects are under generatedParent (local space)
        Vector3 dirWorld = transform.TransformDirection(dirLocal).normalized;

        Quaternion baseRot = Quaternion.LookRotation(
            Vector3.Cross(Vector3.up, faceDirection),
            Vector3.up
        );

        Vector3 endPosLocal = start;

        // safety so we don't infinite loop if something returns 0 length
        int safety = 0;

        while (remaining > 0.001f && safety++ < 5000)
        {
            var prefab = PickVariant(modules.wallStraightVariants, ref lastWallIndex);
            var seg = Instantiate(prefab, generatedParent);

            seg.transform.localRotation = applyWallRotation
                ? baseRot * Quaternion.Euler(wallBaseEuler)
                : Quaternion.Euler(wallBaseEuler);

            // temp place to read bounds
            seg.transform.localPosition = endPosLocal;

            float halfLen = GetWallHalfLength(seg, dirWorld);
            float segLen = Mathf.Max(0.01f, halfLen * 2f);

            // If we’re near the end, stop before overshooting too much
            if (segLen > remaining + 0.05f)
            {
                DestroyImmediate(seg);
                break;
            }

            // center = previous end + halfLen
            seg.transform.localPosition = endPosLocal + dirLocal * halfLen;

            // new end = center + halfLen
            endPosLocal = seg.transform.localPosition + dirLocal * halfLen;

            remaining -= segLen;
        }
    }

    private float GetWallLength(GameObject wallInstance, Vector3 direction)
    {
        // Prefer collider bounds if available
        if (wallInstance.TryGetComponent<Collider>(out var col))
        {
            Bounds b = col.bounds;
            return ProjectBoundsLength(b, direction);
        }

        // Fallback to renderer bounds
        if (wallInstance.TryGetComponent<Renderer>(out var rend))
        {
            Bounds b = rend.bounds;
            return ProjectBoundsLength(b, direction);
        }

        // Absolute fallback (should never happen)
        return modules.wallSegmentLength;
    }

    private float ProjectBoundsLength(Bounds bounds, Vector3 direction)
    {
        direction = direction.normalized;

        // Project the size of the bounds onto the direction vector
        Vector3 size = bounds.size;

        return Mathf.Abs(Vector3.Dot(size, direction));
    }
    private float GetWallHalfLength(GameObject wallInstance, Vector3 worldDir)
    {
        worldDir = worldDir.normalized;

        // Prefer collider bounds (more reliable for modular pieces)
        if (wallInstance.TryGetComponent<Collider>(out var col))
            return ProjectHalfLength(col.bounds, worldDir);

        // Fallback to renderer bounds
        if (wallInstance.TryGetComponent<Renderer>(out var rend))
            return ProjectHalfLength(rend.bounds, worldDir);

        // fallback
        return modules.wallSegmentLength * 0.5f;
    }

    private float ProjectHalfLength(Bounds b, Vector3 worldDir)
    {
        // bounds.extents is half-size in world axes
        Vector3 e = b.extents;

        // Project extents onto direction (absolute because direction could be negative)
        return Mathf.Abs(worldDir.x) * e.x + Mathf.Abs(worldDir.y) * e.y + Mathf.Abs(worldDir.z) * e.z;
    }


#if UNITY_EDITOR
    // ------------------------------------------------------
    // DEBUG WALL STRIP
    // ------------------------------------------------------

    private void GenerateWallStripDebug()
    {
        if (modules.wallStraightVariants == null || modules.wallStraightVariants.Length == 0)
            return;

        Vector3 dir = wallDirectionLocal.normalized;
        if (dir.sqrMagnitude < 0.001f)
            dir = Vector3.forward;

        Quaternion baseRot = Quaternion.LookRotation(
            Vector3.Cross(Vector3.up, dir),
            Vector3.up
        );

        Vector3 pos = wallStartLocalPos;

        for (int i = 0; i < wallSegments; i++)
        {
            var prefab = PickVariant(modules.wallStraightVariants, ref lastWallIndex);
            var seg = Instantiate(prefab, generatedParent);

            seg.transform.localPosition = pos;

            if (applyWallRotation)
                seg.transform.localRotation = baseRot * Quaternion.Euler(wallBaseEuler);
            else
                seg.transform.localRotation = Quaternion.Euler(wallBaseEuler);

            float segmentLength = GetWallLength(seg, dir);
            pos += dir * segmentLength;
        }
    }
#endif

    // ------------------------------------------------------
    // VARIANT PICKER
    // ------------------------------------------------------

    private GameObject PickVariant(GameObject[] arr, ref int lastIndex)
    {
        if (arr.Length == 1)
            return arr[0];

        int idx = rng.Next(0, arr.Length);

        if (modules.avoidSameVariantTwice && arr.Length > 1 && idx == lastIndex)
            idx = (idx + 1) % arr.Length;

        lastIndex = idx;
        return arr[idx];
    }
}
