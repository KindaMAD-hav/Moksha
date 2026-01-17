using System.Collections;
using UnityEngine;

/// <summary>
/// Keeps exactly TWO playable floors prepped at all times:
/// - Top floor (active / can collapse next)
/// - One extra floor below (landing floor)
///
/// Flow:
/// 1) On start, Level 1 (top) + Level 2 (below) are spawned/generated.
/// 2) When Level 1 collapses, Level 2 becomes the new top/active floor, and Level 3 starts
///    generating BELOW it gradually, spread over the global collapse cooldown to avoid a lag spike.
/// 3) Repeat forever.
/// </summary>
public class FloorManager : MonoBehaviour
{
    public static FloorManager Instance { get; private set; }

    [Header("Floor Settings")]
    [SerializeField] private float floorHeightOffset = -10f;

    [Header("Floor Prefab")]
    [Tooltip(
        "Prefab for a FLOOR ROOT (must contain FloorTileGenerator + FloorDecayController).\n" +
        "IMPORTANT: The prefab should NOT already have tile children baked in.\n" +
        "Tiles are spawned by FloorTileGenerator at runtime (immediate or gradual)."
    )]
    [SerializeField] private GameObject floorRootPrefab;

    [Header("Startup")]
    [Tooltip(
        "Optional: assign the starting TOP floor generator already placed in the scene.\n" +
        "If null, FloorManager will pick the highest FloorTileGenerator in the scene."
    )]
    [SerializeField] private FloorTileGenerator startingTopFloor;

    [Tooltip("If no starting floor is found in the scene, spawn Level 1 from the prefab at the FloorManager's position.")]
    [SerializeField] private bool spawnTopFloorIfMissing = true;

    [SerializeField] private bool spawnSecondFloorOnStart = true;

    [Header("Async Generation")]
    [Tooltip("Max tiles instantiated per frame during gradual generation.")]
    [SerializeField] private int maxTilesPerFrame = 25;

    [Tooltip("How much of the global collapse cooldown we use for generating the next floor (finish a bit early).")]
    [Range(0.1f, 1f)]
    [SerializeField] private float generationFillRatio = 0.9f;

    [Header("Debug")]
    [SerializeField] private bool logDebug = true;

    // Current active (top) floor
    private FloorTileGenerator topGen;
    private FloorDecayController topDecay;
    private int topLevel = 1;

    // Preloaded landing floor below
    private FloorTileGenerator belowGen;
    private FloorDecayController belowDecay;
    private int belowLevel = 2;

    private float landingY;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        StartCoroutine(InitializeRoutine());
    }

    private IEnumerator InitializeRoutine()
    {
        // Resolve starting floor.
        if (startingTopFloor == null)
            startingTopFloor = FindHighestFloorGeneratorInScene();

        if (startingTopFloor == null && spawnTopFloorIfMissing)
        {
            if (floorRootPrefab == null)
            {
                Debug.LogError("[FloorManager] No starting floor found, and floorRootPrefab is not assigned.");
                yield break;
            }

            GameObject topGO = Instantiate(floorRootPrefab, transform.position, Quaternion.identity, transform);
            topGO.name = "Floor_L1";
            startingTopFloor = topGO.GetComponent<FloorTileGenerator>();
        }

        if (startingTopFloor == null)
        {
            Debug.LogError("[FloorManager] No FloorTileGenerator found in the scene for the starting floor.");
            yield break;
        }

        topGen = startingTopFloor;
        topDecay = topGen.GetComponent<FloorDecayController>();
        if (topDecay == null)
        {
            Debug.LogError("[FloorManager] Starting floor has no FloorDecayController.");
            yield break;
        }

        // We take control of generation to avoid execution-order double generates.
        topGen.AutoGenerateOnStart = false;

        if (!topGen.IsGenerated)
            topGen.GenerateImmediate();

        // Wait one frame so any Destroy() from Clear() finishes and colliders settle.
        yield return null;

        if (!topGen.IsGenerated)
        {
            Debug.LogError("[FloorManager] Starting top floor failed to generate.");
            yield break;
        }

        // Set enemy spawner to the ACTIVE (top) floor.
        if (EnemySpawner.Instance != null)
            EnemySpawner.Instance.SetCurrentFloorDecay(topDecay);

        if (logDebug)
            Debug.Log($"[FloorManager] Top floor ready (L{topLevel}) @ Y={topDecay.FloorY}");

        // Spawn floor 2 under floor 1 at game start.
        if (spawnSecondFloorOnStart)
        {
            EnsureBelowFloorExistsImmediate();
        }
    }

    /// <summary>
    /// Called by FloorDecayController when a collapse begins.
    /// </summary>
    public void OnFloorCollapseStarted(FloorDecayController collapsingFloor)
    {
        if (collapsingFloor == null)
            return;

        // Only react if the CURRENT TOP floor is the one collapsing.
        if (topDecay != null && collapsingFloor != topDecay)
        {
            if (logDebug)
                Debug.LogWarning("[FloorManager] Collapse started on a non-top floor. Ignored.");
            return;
        }

        if (logDebug)
            Debug.Log($"[FloorManager] Collapse started (L{topLevel}). Landing floor should be L{belowLevel}.");

        // 1) Make sure the landing floor already exists.
        EnsureBelowFloorExistsImmediate();

        // 2) If it's still generating (edge case), force completion so we never fall onto nothing.
        if (belowGen != null && !belowGen.IsGenerated)
        {
            if (logDebug)
                Debug.LogWarning("[FloorManager] Landing floor still generating. Forcing completion now.");
            belowGen.ForceCompleteNow();
        }

        landingY = belowDecay != null ? belowDecay.FloorY : (collapsingFloor.transform.position.y + floorHeightOffset);

        // 3) Redirect spawner to the NEW top floor (the one we land on).
        if (EnemySpawner.Instance != null && belowDecay != null)
            EnemySpawner.Instance.SetCurrentFloorDecay(belowDecay);

        // 4) Schedule enemies to fall onto landingY.
        ScheduleEnemyFalls(collapsingFloor);

        // 5) Promote below -> top.
        PromoteBelowToTop();

        // 6) Start generating the NEXT floor below the new top, spread over the collapse cooldown.
        BeginGeneratingNextBelowFloor();
    }

    private void ScheduleEnemyFalls(FloorDecayController collapsingFloor)
    {
        if (EnemyManager.Instance == null)
            return;

        var enemies = EnemyManager.Instance.GetActiveEnemiesUnsafe();
        for (int i = 0; i < enemies.Length; i++)
        {
            EnemyBase enemy = enemies[i];
            if (enemy == null) continue;

            float delay = Vector3.Distance(enemy.transform.position, collapsingFloor.CollapseCenter) / 10f;
            StartCoroutine(DelayedFall(enemy, delay));
        }
    }

    private IEnumerator DelayedFall(EnemyBase enemy, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (enemy != null)
            enemy.ForceFall(landingY);
    }

    private void PromoteBelowToTop()
    {
        // Top floor will get destroyed by its own FloorDecayController after its delay.
        topGen = belowGen;
        topDecay = belowDecay;
        topLevel = belowLevel;

        belowGen = null;
        belowDecay = null;
        belowLevel = topLevel + 1;

        if (logDebug)
            Debug.Log($"[FloorManager] Promoted to new top: L{topLevel}");
    }

    private void BeginGeneratingNextBelowFloor()
    {
        if (floorRootPrefab == null)
        {
            Debug.LogError("[FloorManager] floorRootPrefab is not assigned. Cannot spawn/generate next floors.");
            return;
        }

        if (topDecay == null)
        {
            Debug.LogError("[FloorManager] No active top floor decay controller. Cannot spawn next floor.");
            return;
        }

        Vector3 newPos = topDecay.transform.position;
        newPos.y += floorHeightOffset;

        GameObject newFloorGO = Instantiate(floorRootPrefab, newPos, Quaternion.identity, transform);
        newFloorGO.name = $"Floor_L{belowLevel}";

        belowGen = newFloorGO.GetComponent<FloorTileGenerator>();
        belowDecay = newFloorGO.GetComponent<FloorDecayController>();

        if (belowGen == null || belowDecay == null)
        {
            Debug.LogError("[FloorManager] Floor prefab is missing FloorTileGenerator or FloorDecayController.");
            Destroy(newFloorGO);
            belowGen = null;
            belowDecay = null;
            return;
        }

        // Make sure it won't auto-generate in Start() (we control it).
        belowGen.AutoGenerateOnStart = false;

        float cooldown = Mathf.Max(0.1f, topDecay.GlobalCollapseCooldown);
        float duration = Mathf.Max(0.05f, cooldown * generationFillRatio);

        if (logDebug)
            Debug.Log($"[FloorManager] Generating below floor (L{belowLevel}) over {duration:0.00}s (cooldown={cooldown:0.00}s, cap={maxTilesPerFrame}/frame)");

        // This spreads instantiation across the cooldown window to minimize spikes.
        belowGen.GenerateGradually(duration, maxTilesPerFrame);
    }

    private void EnsureBelowFloorExistsImmediate()
    {
        if (belowGen != null && belowDecay != null)
            return;

        if (floorRootPrefab == null)
        {
            Debug.LogError("[FloorManager] floorRootPrefab is not assigned. Cannot spawn the second floor.");
            return;
        }

        if (topDecay == null)
        {
            Debug.LogError("[FloorManager] Top floor not ready yet. Cannot spawn the second floor.");
            return;
        }

        Vector3 newPos = topDecay.transform.position;
        newPos.y += floorHeightOffset;

        GameObject newFloorGO = Instantiate(floorRootPrefab, newPos, Quaternion.identity, transform);
        newFloorGO.name = $"Floor_L{belowLevel}";

        belowGen = newFloorGO.GetComponent<FloorTileGenerator>();
        belowDecay = newFloorGO.GetComponent<FloorDecayController>();

        if (belowGen == null || belowDecay == null)
        {
            Debug.LogError("[FloorManager] Floor prefab is missing FloorTileGenerator or FloorDecayController.");
            Destroy(newFloorGO);
            belowGen = null;
            belowDecay = null;
            return;
        }

        belowGen.AutoGenerateOnStart = false;
        belowGen.GenerateImmediate();

        if (logDebug)
            Debug.Log($"[FloorManager] Spawned below floor (L{belowLevel}) immediately @ Y={belowDecay.FloorY}");
    }

    private static FloorTileGenerator FindHighestFloorGeneratorInScene()
    {
        FloorTileGenerator[] gens = FindObjectsOfType<FloorTileGenerator>();
        if (gens == null || gens.Length == 0)
            return null;

        FloorTileGenerator best = null;
        float bestY = float.NegativeInfinity;
        for (int i = 0; i < gens.Length; i++)
        {
            if (gens[i] == null) continue;
            float y = gens[i].transform.position.y;
            if (y > bestY)
            {
                bestY = y;
                best = gens[i];
            }
        }

        return best;
    }

    // Legacy compatibility: older versions called this from FloorTileGenerator.
    public void RegisterCurrentFloor(FloorTileGenerator floor)
    {
        if (startingTopFloor == null && floor != null)
            startingTopFloor = floor;
    }
}
