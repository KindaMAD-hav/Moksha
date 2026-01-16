using System.Collections.Generic;
using UnityEngine;

public class FloorDecayController : MonoBehaviour
{
    [Header("Decay Settings")]
    [SerializeField] private float decayRadius = 4f;
    [SerializeField] private float decayAmount = 1f;

    [Header("Collapse Settings")]
    [SerializeField] private float collapseSpeed = 12f;

    private readonly List<FloorTileDecay> tiles = new();

    private bool collapseTriggered;
    private bool isCollapsing;

    private float collapseRadius;
    private Vector3 collapseCenter;

    private Vector3 lastDecaySourcePos;

    private readonly List<FloorTileDecay> criticalThisPulse = new();

    [SerializeField] private Collider[] tileColliders;

    public float FloorY => transform.position.y;
    public float CollapseRadius => collapseRadius;
    public Vector3 CollapseCenter => collapseCenter;
    public bool IsCollapsing => isCollapsing;

    /* -------------------- REGISTRATION -------------------- */

    public void RegisterTile(FloorTileDecay tile)
    {
        tiles.Add(tile);
        tile.OnCriticalDecayReached += OnTileCritical;
    }

    public void UnregisterTile(FloorTileDecay tile)
    {
        tile.OnCriticalDecayReached -= OnTileCritical;
        tiles.Remove(tile);
    }

    private void CacheTileColliders()
    {
        tileColliders = GetComponentsInChildren<Collider>();
    }

    /* -------------------- DECAY -------------------- */

    public void ApplyDecayPulse(Vector3 worldPosition)
    {
        if (collapseTriggered)
            return;

        lastDecaySourcePos = worldPosition;
        criticalThisPulse.Clear();

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

        // Resolve collapse origin AFTER all decay is applied
        if (criticalThisPulse.Count > 0)
        {
            FloorTileDecay chosen = null;
            float bestDist = float.MaxValue;

            foreach (var tile in criticalThisPulse)
            {
                float d = (tile.transform.position - lastDecaySourcePos).sqrMagnitude;
                if (d < bestDist)
                {
                    bestDist = d;
                    chosen = tile;
                }
            }

            if (chosen != null)
            {
                TriggerCollapseFromTile(chosen);
            }
        }
    }

    /* -------------------- CRITICAL TRACKING -------------------- */

    private void OnTileCritical(FloorTileDecay tile)
    {
        if (collapseTriggered)
            return;

        criticalThisPulse.Add(tile);
    }

    /* -------------------- COLLAPSE -------------------- */

    private void TriggerCollapseFromTile(FloorTileDecay tile)
    {
        if (isCollapsing)
            return;

        collapseTriggered = true;
        isCollapsing = true;

        collapseCenter = tile.transform.position;
        collapseRadius = 0f; // 🔥 always start from zero

        BeginCollapse();
    }

    private void BeginCollapse()
    {
        DisableTileColliders();

        Debug.Log($"[FloorDecayController] Collapse started at {collapseCenter}");

        if (FloorManager.Instance != null)
        {
            FloorManager.Instance.OnFloorCollapseStarted(this);
        }
    }

    private void DisableTileColliders()
    {
        if (tileColliders == null)
            return;

        foreach (var col in tileColliders)
        {
            if (col != null)
                col.enabled = false;
        }
    }

    /* -------------------- UPDATE / SHADER -------------------- */

    private void Update()
    {
        if (!isCollapsing)
            return;

        collapseRadius += collapseSpeed * Time.deltaTime;

        Shader.SetGlobalVector("_CollapseCenter", collapseCenter);
        Shader.SetGlobalFloat("_CollapseRadius", collapseRadius);
    }

    private void OnDisable()
    {
        Shader.SetGlobalFloat("_CollapseRadius", -9999f);
        Shader.SetGlobalVector("_CollapseCenter", Vector3.zero);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!isCollapsing) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(collapseCenter, collapseRadius);
    }
#endif
}
