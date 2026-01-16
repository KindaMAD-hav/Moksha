using System.Collections.Generic;
using UnityEngine;

public class FloorDecayController : MonoBehaviour
{
    [Header("Decay Settings")]
    [SerializeField] private float decayRadius = 4f;
    [SerializeField] private float decayAmount = 1f;

    private readonly List<FloorTileDecay> tiles = new();
    private bool collapseTriggered;
    public float FloorY => transform.position.y;

    [SerializeField] private float collapseRadius;
    [SerializeField] private float collapseSpeed = 12f;
    public float CollapseRadius => collapseRadius;
    public void RegisterTile(FloorTileDecay tile)
    {
        tiles.Add(tile);
        tile.OnCriticalDecayReached += HandleCriticalTile;
    }
    private void Update()
    {
        if (!isCollapsing)
            return;

        collapseRadius += collapseSpeed * Time.deltaTime;

        // 🔥 THIS IS THE MISSING LINK
        Shader.SetGlobalVector("_CollapseCenter", collapseCenter);
        Shader.SetGlobalFloat("_CollapseRadius", collapseRadius);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!isCollapsing) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(collapseCenter, collapseRadius);
    }
#endif

    public void UnregisterTile(FloorTileDecay tile)
    {
        tile.OnCriticalDecayReached -= HandleCriticalTile;
        tiles.Remove(tile);
    }
    [SerializeField] private bool isCollapsing;
    [SerializeField] private Vector3 collapseCenter;
    [SerializeField] private Collider[] tileColliders;

    public bool IsCollapsing => isCollapsing;
    public Vector3 CollapseCenter => collapseCenter;

    private void CacheTileColliders()
    {
        tileColliders = GetComponentsInChildren<Collider>();
    }

    /// <summary>
    /// Called when an enemy dies on this floor
    /// </summary>
    public void ApplyDecayPulse(Vector3 worldPosition)
    {
        Debug.Log("[TEST] ApplyDecayPulse called");

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
        if (isCollapsing) return;

        isCollapsing = true;
        collapseCenter = tile.transform.position;

        BeginCollapse();
    }
    private void BeginCollapse()
    {
        DisableTileColliders();

        // Stop further decay pulses on this floor
        collapseTriggered = true;

        Debug.Log($"[FloorDecayController] Collapse started at {collapseCenter}");

        // Notify a higher-level system (FloorManager / MapManager)
        // We keep this loose so the controller does not own generation logic
        if (FloorManager.Instance != null)
        {
            FloorManager.Instance.OnFloorCollapseStarted(this);
        }
    }
    private void DisableTileColliders()
    {
        if (tileColliders == null) return;

        foreach (var col in tileColliders)
        {
            if (col != null)
                col.enabled = false;
        }
    }

}
