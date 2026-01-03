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

    [Header("Blue Noise Settings")]
    [Tooltip("How many previous tiles are penalized")]
    public int historySize = 6;

    [Tooltip("Higher = stronger avoidance of repeats")]
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

        Vector3 tileSize = GetTileSize(floorTiles[0]);
        List<int> history = new List<int>();

        int index = 0;

        for (int z = 0; z < tilesZ; z++)
        {
            for (int x = 0; x < tilesX; x++)
            {
                int prefabIndex = PickBlueNoiseIndex(history);

                GameObject tile = Instantiate(
                    floorTiles[prefabIndex],
                    transform
                );

                Vector3 pos = startLocalPosition;
                pos += Vector3.right * tileSize.x * x;
                pos += Vector3.forward * tileSize.z * z;

                tile.transform.localPosition = pos;
                tile.transform.localRotation = Quaternion.identity;

                // Update history
                history.Add(prefabIndex);
                if (history.Count > historySize)
                    history.RemoveAt(0);

                index++;
            }
        }
    }

    // --------------------------------------------------
    // Blue-noise-ish weighted picker
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

    private Vector3 GetTileSize(GameObject prefab)
    {
        Renderer r = prefab.GetComponentInChildren<Renderer>();
        if (!r)
        {
            Debug.LogWarning("Tile has no Renderer, defaulting to 1x1");
            return Vector3.one;
        }

        Bounds b = r.bounds;
        return new Vector3(b.size.x, b.size.y, b.size.z);
    }

    [ContextMenu("Clear")]
    public void Clear()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);
    }
}
