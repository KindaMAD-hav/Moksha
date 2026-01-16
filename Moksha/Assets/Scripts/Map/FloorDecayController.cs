using System.Collections.Generic;
using UnityEngine;

public class FloorDecayController : MonoBehaviour
{
    [Header("Decay Settings")]
    [SerializeField] private float decayRadius = 4f;
    [SerializeField] private float decayAmount = 1f;

    [Header("Collapse Settings")]
    [SerializeField] private float collapseSpeed = 12f;
    [SerializeField] private float maxCollapseRadius = 100f;
    [SerializeField] private float floorYTolerance = 1.5f;

    private readonly List<FloorTileDecay> tiles = new();

    private bool collapseTriggered;
    private bool isCollapsing;

    private float collapseRadius;
    private Vector3 collapseCenter;

    private Vector3 lastDecaySourcePos;

    private readonly List<FloorTileDecay> criticalThisPulse = new();

    [SerializeField] private Collider[] tileColliders;

    // Track which controller is currently controlling shader globals
    private static FloorDecayController s_activeCollapsingFloor;

    public float FloorY => transform.position.y;
    public float CollapseRadius => collapseRadius;
    public Vector3 CollapseCenter => collapseCenter;
    public bool IsCollapsing => isCollapsing;

    private void Awake()
    {
        // Initialize shader to "no collapse" state if no collapse is active
        if (s_activeCollapsingFloor == null)
        {
            Shader.SetGlobalFloat("_CollapseRadius", -9999f);
            Shader.SetGlobalVector("_CollapseCenter", Vector3.zero);
            Shader.SetGlobalFloat("_FloorYTolerance", 9999f);
        }
    }

    /* -------------------- RESET FOR CLONED FLOORS -------------------- */

    /// <summary>
    /// Resets the decay controller state. Call this after cloning a floor.
    /// </summary>
    public void ResetController()
    {
        collapseTriggered = false;
        isCollapsing = false;
        collapseRadius = 0f;
        collapseCenter = Vector3.zero;
        criticalThisPulse.Clear();

        // Clear old tile references (they belong to the old floor)
        tiles.Clear();

        // Re-register all tiles under this floor and reset them
        FloorTileDecay[] childTiles = GetComponentsInChildren<FloorTileDecay>();
        foreach (FloorTileDecay tile in childTiles)
        {
            RegisterTile(tile);
            tile.ResetDecay();
        }

        // Re-cache colliders
        CacheTileColliders();

        // Re-enable all colliders
        if (tileColliders != null)
        {
            foreach (var col in tileColliders)
            {
                if (col != null)
                    col.enabled = true;
            }
        }

        Debug.Log($"[FloorDecayController] Reset complete. {childTiles.Length} tiles registered.");
    }

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

        // Iterate backwards to safely handle destroyed tiles
        for (int i = tiles.Count - 1; i >= 0; i--)
        {
            FloorTileDecay tile = tiles[i];
            
            // Remove null/destroyed tiles
            if (tile == null)
            {
                tiles.RemoveAt(i);
                continue;
            }

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
                if (tile == null) continue;
                
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
        // Register as the active collapsing floor
        s_activeCollapsingFloor = this;

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

        // Only the active collapsing floor should set shader globals
        // This prevents the new floor from interfering with the old floor's dissolve
        if (s_activeCollapsingFloor != this)
            return;

        // Stop expanding if we've reached max radius
        if (collapseRadius < maxCollapseRadius)
        {
            collapseRadius += collapseSpeed * Time.deltaTime;
            collapseRadius = Mathf.Min(collapseRadius, maxCollapseRadius);
        }

        Shader.SetGlobalVector("_CollapseCenter", collapseCenter);
        Shader.SetGlobalFloat("_CollapseRadius", collapseRadius);
        Shader.SetGlobalFloat("_FloorYTolerance", floorYTolerance);
    }

    private void OnDisable()
    {
        // Only clear shader globals if we were the active collapsing floor
        if (s_activeCollapsingFloor == this)
        {
            s_activeCollapsingFloor = null;
            Shader.SetGlobalFloat("_CollapseRadius", -9999f);
            Shader.SetGlobalVector("_CollapseCenter", Vector3.zero);
            Shader.SetGlobalFloat("_FloorYTolerance", 9999f);
        }
    }

    private void OnDestroy()
    {
        // Clear active reference if this floor is destroyed
        if (s_activeCollapsingFloor == this)
        {
            s_activeCollapsingFloor = null;
            Shader.SetGlobalFloat("_CollapseRadius", -9999f);
            Shader.SetGlobalVector("_CollapseCenter", Vector3.zero);
            Shader.SetGlobalFloat("_FloorYTolerance", 9999f);
        }
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
