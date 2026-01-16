using System.Collections.Generic;
using UnityEngine;

public class FloorDecayController : MonoBehaviour
{
    [Header("Decay Settings")]
    [SerializeField] private float decayRadius = 4f;
    [SerializeField] private float decayAmount = 1f;

    private readonly List<FloorTileDecay> tiles = new();
    private bool collapseTriggered;

    public void RegisterTile(FloorTileDecay tile)
    {
        tiles.Add(tile);
        tile.OnCriticalDecayReached += HandleCriticalTile;
    }

    public void UnregisterTile(FloorTileDecay tile)
    {
        tile.OnCriticalDecayReached -= HandleCriticalTile;
        tiles.Remove(tile);
    }

    /// <summary>
    /// Called when an enemy dies on this floor
    /// </summary>
    public void ApplyDecayPulse(Vector3 worldPosition)
    {
        if (collapseTriggered) return;

        float radiusSqr = decayRadius * decayRadius;

        foreach (var tile in tiles)
        {
            Vector3 tilePos = tile.transform.position;
            float dx = tilePos.x - worldPosition.x;
            float dz = tilePos.z - worldPosition.z;

            if (dx * dx + dz * dz <= radiusSqr)
            {
                tile.AddDecay(decayAmount);
            }
        }
    }

    private void HandleCriticalTile(FloorTileDecay tile)
    {
        if (collapseTriggered) return;

        collapseTriggered = true;

        // For now, just log – collapse controller comes next
        Debug.Log($"[FloorDecayController] Collapse requested by {tile.name}");
    }
}
