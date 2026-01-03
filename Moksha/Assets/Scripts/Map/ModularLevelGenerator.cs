using System.Collections.Generic;
using UnityEngine;

public class ModularLevelGenerator : MonoBehaviour
{
    public enum BuildMode { WallStrip, FloorArea, SimpleRoom }

    [Header("Mode")]
    public BuildMode mode = BuildMode.SimpleRoom;

    [Header("Modules")]
    public ModuleSet modules;

    [Header("Random Seed")]
    public bool useRandomSeed = true;
    public int seed = 12345;

    [Header("Parent / Cleanup")]
    public Transform generatedParent;
    public bool autoClearBeforeGenerate = true;

    // ---------- Wall Strip ----------
    [Header("Wall Strip Settings")]
    public int wallSegments = 20;
    public Vector3 wallStartLocalPos = Vector3.zero;
    public Vector3 wallDirectionLocal = Vector3.forward; // uses local space
    public bool rotateSegmentsToDirection = true;

    // ---------- Floor Area ----------
    [Header("Floor Area Settings")]
    public int floorWidth = 12;  // tiles in X
    public int floorHeight = 12; // tiles in Z
    public Vector3 floorStartLocalPos = Vector3.zero;

    // ---------- Room Settings ----------
    [Header("Room Settings (perimeter walls around the floor area)")]
    public bool buildPerimeterWalls = true;

    private System.Random rng;
    private int lastWallIndex = -1;
    private int lastFloorIndex = -1;

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
            // default: create a child parent
            var go = new GameObject("Generated_Level");
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;
            generatedParent = go.transform;
        }

        if (autoClearBeforeGenerate)
            ClearGenerated();

        rng = useRandomSeed ? new System.Random(seed) : new System.Random();

        switch (mode)
        {
            case BuildMode.WallStrip:
                GenerateWallStrip();
                break;

            case BuildMode.FloorArea:
                GenerateFloorArea();
                break;

            case BuildMode.SimpleRoom:
                GenerateFloorArea();
                if (buildPerimeterWalls) GenerateRoomPerimeterWalls();
                break;
        }
    }

    [ContextMenu("Clear Generated")]
    public void ClearGenerated()
    {
        if (generatedParent == null) return;

        // Destroy children safely in edit mode & play mode
        var toDestroy = new List<GameObject>();
        for (int i = 0; i < generatedParent.childCount; i++)
            toDestroy.Add(generatedParent.GetChild(i).gameObject);

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            foreach (var go in toDestroy) UnityEditor.Undo.DestroyObjectImmediate(go);
            return;
        }
#endif
        foreach (var go in toDestroy) Destroy(go);
    }

    private void GenerateWallStrip()
    {
        if (modules.wallStraightVariants == null || modules.wallStraightVariants.Length == 0)
        {
            Debug.LogError("ModuleSet has no wallStraightVariants.");
            return;
        }

        Vector3 dir = wallDirectionLocal.normalized;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector3.forward;

        Quaternion rot = Quaternion.identity;
        if (rotateSegmentsToDirection)
            rot = Quaternion.LookRotation(transform.TransformDirection(dir), Vector3.up);

        Vector3 pos = wallStartLocalPos;

        for (int i = 0; i < wallSegments; i++)
        {
            var prefab = PickVariant(modules.wallStraightVariants, ref lastWallIndex);
            var seg = Instantiate(prefab, generatedParent);

            seg.transform.localPosition = pos;
            if (rotateSegmentsToDirection)
                seg.transform.rotation = rot;

            // advance
            pos += dir * modules.wallSegmentLength;
        }
    }

    private void GenerateFloorArea()
    {
        if (modules.floorTileVariants == null || modules.floorTileVariants.Length == 0)
        {
            Debug.LogError("ModuleSet has no floorTileVariants.");
            return;
        }

        // We tile in local XZ grid
        for (int z = 0; z < floorHeight; z++)
        {
            for (int x = 0; x < floorWidth; x++)
            {
                var prefab = PickVariant(modules.floorTileVariants, ref lastFloorIndex);
                var tile = Instantiate(prefab, generatedParent);

                Vector3 p = floorStartLocalPos + new Vector3(x * modules.floorTileSize, 0f, z * modules.floorTileSize);
                tile.transform.localPosition = p;
                tile.transform.localRotation = Quaternion.identity;
            }
        }
    }

    private void GenerateRoomPerimeterWalls()
    {
        if (modules.wallStraightVariants == null || modules.wallStraightVariants.Length == 0)
            return;

        // Perimeter uses floor dimensions (in tiles) * tileSize => room size in units
        float tile = modules.floorTileSize;
        float len = modules.wallSegmentLength;

        float roomW = floorWidth * tile;
        float roomH = floorHeight * tile;

        // We build walls along the outer edges of the floor grid.
        // Assumes wall segments are aligned and length == modules.wallSegmentLength.
        // Place them slightly outside the floor to avoid z-fighting if needed.
        float inset = 0f; // set to 0.05f if needed

        // Bottom edge (+Z forward along X)
        BuildWallLine(
            start: floorStartLocalPos + new Vector3(0f, 0f, -inset),
            direction: Vector3.right,
            totalLength: roomW,
            faceDirection: Vector3.forward
        );

        // Top edge (along X)
        BuildWallLine(
            start: floorStartLocalPos + new Vector3(0f, 0f, roomH - tile + inset),
            direction: Vector3.right,
            totalLength: roomW,
            faceDirection: Vector3.back
        );

        // Left edge (along Z)
        BuildWallLine(
            start: floorStartLocalPos + new Vector3(-inset, 0f, 0f),
            direction: Vector3.forward,
            totalLength: roomH,
            faceDirection: Vector3.right
        );

        // Right edge (along Z)
        BuildWallLine(
            start: floorStartLocalPos + new Vector3(roomW - tile + inset, 0f, 0f),
            direction: Vector3.forward,
            totalLength: roomH,
            faceDirection: Vector3.left
        );
    }

    private void BuildWallLine(Vector3 start, Vector3 direction, float totalLength, Vector3 faceDirection)
    {
        int count = Mathf.Max(1, Mathf.RoundToInt(totalLength / modules.wallSegmentLength));
        Vector3 dir = direction.normalized;

        Quaternion rot = Quaternion.LookRotation(faceDirection, Vector3.up);

        Vector3 pos = start;
        for (int i = 0; i < count; i++)
        {
            var prefab = PickVariant(modules.wallStraightVariants, ref lastWallIndex);
            var seg = Instantiate(prefab, generatedParent);

            seg.transform.localPosition = pos;
            seg.transform.localRotation = rot;

            pos += dir * modules.wallSegmentLength;
        }
    }

    private GameObject PickVariant(GameObject[] arr, ref int lastIndex)
    {
        if (arr.Length == 1) return arr[0];

        int idx = rng.Next(0, arr.Length);

        if (modules.avoidSameVariantTwice && arr.Length > 1)
        {
            // reroll once if same as last
            if (idx == lastIndex) idx = (idx + 1 + rng.Next(0, arr.Length - 1)) % arr.Length;
        }

        lastIndex = idx;
        return arr[idx];
    }
}
