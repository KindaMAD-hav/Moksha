using UnityEngine;
using System.Collections.Generic;

public class FloorTileGenerator : MonoBehaviour
{
    [Header("Floor Prefabs")]
    public GameObject[] floorTiles;

    [Header("Grid Size")]
    public int tilesX = 10;
    public int tilesZ = 10;

    [Header("Placement")]
    public Vector3 startLocalPosition = Vector3.zero;

    [Header("Orientation (LOCAL SPACE)")]
    public Vector3 gridRight = Vector3.right;
    public Vector3 gridForward = Vector3.forward;
    public Vector3 tileUp = Vector3.up;
    public Vector3 baseEulerOffset = Vector3.zero;

    [Header("Random Rotation")]
    public bool randomizeRotation = true;
    public int rotationStep = 90;

    [Header("Blue Noise Settings")]
    public int historySize = 6;

    [Range(0f, 1f)]
    public float repeatPenalty = 0.7f;

    [ContextMenu("Generate Floor")]
    public void Generate()
    {
        if (floorTiles == null || floorTiles.Length == 0)
        {
            Debug.LogError("No floor tiles assigned.");
            return;
        }

        Clear();

        Vector3 right = gridRight.normalized;
        Vector3 forward = gridForward.normalized;
        Vector3 up = tileUp.normalized;

        Quaternion baseRotation = Quaternion.LookRotation(forward, up);
        baseRotation *= Quaternion.Euler(baseEulerOffset);

        Vector3 tileSize = GetTileSize(floorTiles[0], right, forward);

        List<int> history = new List<int>();

        for (int z = 0; z < tilesZ; z++)
        {
            for (int x = 0; x < tilesX; x++)
            {
                int prefabIndex = PickBlueNoiseIndex(history);

                GameObject tile = Instantiate(floorTiles[prefabIndex], transform);

                Vector3 pos = startLocalPosition;
                pos += right * tileSize.x * x;
                pos += forward * tileSize.z * z;

                Quaternion finalRot = baseRotation;

                if (randomizeRotation && rotationStep > 0)
                {
                    int steps = Mathf.Max(1, 360 / rotationStep);
                    int stepIndex = Random.Range(0, steps);
                    float angle = stepIndex * rotationStep;

                    // WORLD Y rotation (correct)
                    Quaternion yaw = Quaternion.AngleAxis(angle, Vector3.up);
                    finalRot = yaw * baseRotation;
                }

                tile.transform.localPosition = pos;
                tile.transform.localRotation = finalRot;

                history.Add(prefabIndex);
                if (history.Count > historySize)
                    history.RemoveAt(0);
            }
        }
    }

    // --------------------------------------------------
    // Blue-noise-ish picker
    // --------------------------------------------------

    private int PickBlueNoiseIndex(List<int> history)
    {
        float[] weights = new float[floorTiles.Length];

        for (int i = 0; i < weights.Length; i++)
            weights[i] = 1f;

        foreach (int h in history)
            weights[h] *= (1f - repeatPenalty);

        float total = 0f;
        for (int i = 0; i < weights.Length; i++)
            total += weights[i];

        float r = Random.value * total;

        for (int i = 0; i < weights.Length; i++)
        {
            r -= weights[i];
            if (r <= 0f)
                return i;
        }

        return Random.Range(0, floorTiles.Length);
    }

    // --------------------------------------------------
    // Utils
    // --------------------------------------------------

    private Vector3 GetTileSize(GameObject prefab, Vector3 right, Vector3 forward)
    {
        Renderer r = prefab.GetComponentInChildren<Renderer>();
        if (!r)
        {
            Debug.LogWarning("Tile has no Renderer, defaulting to 1x1");
            return Vector3.one;
        }

        Bounds b = r.bounds;
        Vector3 e = b.extents;

        float sizeX =
            2f * (Mathf.Abs(right.x) * e.x +
                  Mathf.Abs(right.y) * e.y +
                  Mathf.Abs(right.z) * e.z);

        float sizeZ =
            2f * (Mathf.Abs(forward.x) * e.x +
                  Mathf.Abs(forward.y) * e.y +
                  Mathf.Abs(forward.z) * e.z);

        return new Vector3(sizeX, b.size.y, sizeZ);
    }

    [ContextMenu("Clear")]
    public void Clear()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);
    }
}
