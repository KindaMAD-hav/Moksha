using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(BoxCollider))]
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

    [Header("Unified Floor Collider")]
    [Tooltip("Keep this very small. Example: 0.1")]
    public float floorColliderHeight = 0.1f;

    [Tooltip("Extra size added to X/Z as a percentage (0.05 = 5%)")]
    [Range(0f, 0.2f)]
    public float colliderPaddingPercent = 0.05f;

    private BoxCollider floorCollider;

    [ContextMenu("Generate Floor")]
    public void Generate()
    {
        FloorDecayController decayController = GetComponent<FloorDecayController>();
        if (decayController == null)
        {
            Debug.LogError("FloorDecayController missing on floor root.");
            return;
        }

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

                    Quaternion yaw = Quaternion.AngleAxis(angle, Vector3.up);
                    finalRot = yaw * baseRotation;
                }

                tile.transform.localPosition = pos;
                tile.transform.localRotation = finalRot;

                history.Add(prefabIndex);
                if (history.Count > historySize)
                    history.RemoveAt(0);

                FloorTileDecay decay = tile.GetComponent<FloorTileDecay>();
                if (decay != null)
                {
                    decayController.RegisterTile(decay);
                }


            }
        }

        UpdateUnifiedFloorCollider(tileSize, right, forward, up);
        EnemySpawner.Instance.SetCurrentFloorDecay(decayController);
    }

    // --------------------------------------------------
    // Unified Floor Collider
    // --------------------------------------------------

    private void UpdateUnifiedFloorCollider(
    Vector3 tileSize,
    Vector3 right,
    Vector3 forward,
    Vector3 up
)
    {
        if (!floorCollider)
        {
            floorCollider = GetComponent<BoxCollider>();
            if (!floorCollider)
                floorCollider = gameObject.AddComponent<BoxCollider>();
        }

        float baseWidth = tilesX * tileSize.x;
        float baseDepth = tilesZ * tileSize.z;

        float paddedWidth = baseWidth * (1f + colliderPaddingPercent);
        float paddedDepth = baseDepth * (1f + colliderPaddingPercent);

        // Size includes padding (symmetric)
        floorCollider.size = new Vector3(
            paddedWidth,
            floorColliderHeight,
            paddedDepth
        );

        // Center ONLY accounts for grid placement, NOT padding
        Vector3 center =
            startLocalPosition +
            right * (baseWidth * 0.5f) +
            forward * (baseDepth * 0.5f) +
            up * (floorColliderHeight * 0.5f);

        floorCollider.center = center;
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
