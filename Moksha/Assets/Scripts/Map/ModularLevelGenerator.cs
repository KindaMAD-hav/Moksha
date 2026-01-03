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

    [Header("Wall Orientation")]
    public Vector3 wallBaseEuler = Vector3.zero;
    public bool applyWallRotation = true;

    [Header("Wall Placement")]
    public bool preferRendererForLength = true;
    [Tooltip("Small overlap to remove tiny gaps (0.01–0.05 recommended)")]
    public float wallJoinEpsilon = 0.02f;

    // ---------- Floor ----------
    [Header("Floor Area Settings")]
    public int floorWidth = 12;
    public int floorHeight = 12;
    public Vector3 floorStartLocalPos = Vector3.zero;

#if UNITY_EDITOR
    [Header("Debug")]
    public bool generateWallStripDebug = false;
    public Vector3 debugWallStart = Vector3.zero;
    public Vector3 debugWallEnd = new Vector3(10, 0, 0);
#endif

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
            BuildWallLineBetween(debugWallStart, debugWallEnd, Vector3.forward);
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

        float minX = floorStartLocalPos.x - tile * 0.5f;
        float maxX = floorStartLocalPos.x + (floorWidth - 1) * tile + tile * 0.5f;
        float minZ = floorStartLocalPos.z - tile * 0.5f;
        float maxZ = floorStartLocalPos.z + (floorHeight - 1) * tile + tile * 0.5f;

        BuildWallLineBetween(
            new Vector3(minX, floorStartLocalPos.y, minZ),
            new Vector3(maxX, floorStartLocalPos.y, minZ),
            Vector3.forward
        );

        BuildWallLineBetween(
            new Vector3(minX, floorStartLocalPos.y, maxZ),
            new Vector3(maxX, floorStartLocalPos.y, maxZ),
            Vector3.back
        );

        BuildWallLineBetween(
            new Vector3(minX, floorStartLocalPos.y, minZ),
            new Vector3(minX, floorStartLocalPos.y, maxZ),
            Vector3.right
        );

        BuildWallLineBetween(
            new Vector3(maxX, floorStartLocalPos.y, minZ),
            new Vector3(maxX, floorStartLocalPos.y, maxZ),
            Vector3.left
        );
    }

    // ------------------------------------------------------
    // WALL LINE (CORE LOGIC)
    // ------------------------------------------------------

    private void BuildWallLineBetween(Vector3 startLocal, Vector3 endLocal, Vector3 faceDirection)
    {
        Vector3 startWorld = generatedParent.TransformPoint(startLocal);
        Vector3 endWorld = generatedParent.TransformPoint(endLocal);

        Vector3 dirWorld = (endWorld - startWorld).normalized;
        float remaining = Vector3.Distance(startWorld, endWorld);

        Quaternion rot = Quaternion.LookRotation(
            Vector3.Cross(Vector3.up, faceDirection),
            Vector3.up
        );

        Vector3 posWorld = startWorld;
        int safety = 0;

        while (remaining > 0.001f && safety++ < 1000)
        {
            var prefab = PickVariant(modules.wallStraightVariants, ref lastWallIndex);
            var seg = Instantiate(prefab, generatedParent);

            seg.transform.rotation = applyWallRotation
                ? rot * Quaternion.Euler(wallBaseEuler)
                : Quaternion.Euler(wallBaseEuler);

            seg.transform.position = posWorld;

            float length = GetWallLength(seg, dirWorld);

            if (length > remaining + 0.01f)
            {
                SafeDestroy(seg);
                break;
            }

            posWorld += dirWorld * length;
            remaining -= length;
        }
    }

    // ------------------------------------------------------
    // UTILS
    // ------------------------------------------------------

    private float GetWallLength(GameObject wall, Vector3 worldDir)
    {
        worldDir = worldDir.normalized;
        Bounds b;

        if (preferRendererForLength)
        {
            var rend = wall.GetComponentInChildren<Renderer>();
            if (rend != null) b = rend.bounds;
            else
            {
                var col = wall.GetComponentInChildren<Collider>();
                if (col != null) b = col.bounds;
                else return modules.wallSegmentLength;
            }
        }
        else
        {
            var col = wall.GetComponentInChildren<Collider>();
            if (col != null) b = col.bounds;
            else
            {
                var rend = wall.GetComponentInChildren<Renderer>();
                if (rend != null) b = rend.bounds;
                else return modules.wallSegmentLength;
            }
        }

        float length =
            2f * (
                Mathf.Abs(worldDir.x) * b.extents.x +
                Mathf.Abs(worldDir.y) * b.extents.y +
                Mathf.Abs(worldDir.z) * b.extents.z
            );

        return Mathf.Max(0.01f, length - wallJoinEpsilon);
    }

    private void SafeDestroy(GameObject go)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying) DestroyImmediate(go);
        else Destroy(go);
#else
        Destroy(go);
#endif
    }

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
