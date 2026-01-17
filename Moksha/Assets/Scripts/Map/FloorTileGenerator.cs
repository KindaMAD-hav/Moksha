using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

// [RequireComponent(typeof(BoxCollider))]
public class FloorTileGenerator : MonoBehaviour
{
    [Header("Auto Generation")]
    [Tooltip("If enabled, this floor will generate itself in Start().\n" +
             "For runtime-spawned floors, FloorManager will set this to false and call GenerateGradually().")]
    [SerializeField] private bool autoGenerateOnStart = true;

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

    [Header("Async Generation")]
    [Tooltip("Used when a caller passes maxTilesPerFrame <= 0")]
    [SerializeField] private int defaultMaxTilesPerFrame = 25;

    private BoxCollider floorCollider;

    private bool isGenerating;
    private bool isGenerated;
    private Coroutine generationRoutine;

    // Reused buffers to reduce allocations.
    private float[] weightsBuffer;
    private readonly List<int> history = new List<int>(16);

    public bool AutoGenerateOnStart
    {
        get => autoGenerateOnStart;
        set => autoGenerateOnStart = value;
    }

    public bool IsGenerating => isGenerating;
    public bool IsGenerated => isGenerated;
    public int TotalTiles => Mathf.Max(0, tilesX) * Mathf.Max(0, tilesZ);

    public event Action<FloorTileGenerator> OnGenerationComplete;

    private struct GenerationContext
    {
        public Vector3 right;
        public Vector3 forward;
        public Vector3 up;
        public Quaternion baseRotation;
        public Vector3 tileSize;
    }

    private void Awake()
    {
        EnsureWeightsBuffer();
    }

    private void Start()
    {
        if (autoGenerateOnStart)
        {
            GenerateImmediate();
        }
    }

    // Backwards-compat wrapper (older code / context menus may still call Generate()).
    public void Generate() => GenerateImmediate();

    [ContextMenu("Generate Floor (Immediate)")]
    public void GenerateImmediate()
    {
        StopCurrentGeneration();

        FloorDecayController decayController = GetComponent<FloorDecayController>();
        if (!ValidateSetup(decayController))
            return;

        isGenerating = true;
        isGenerated = false;

        decayController.ResetForNewGeneration();
        Clear();

        GenerationContext ctx = BuildContext();
        UpdateUnifiedFloorCollider(ctx.tileSize, ctx.right, ctx.forward, ctx.up);

        history.Clear();

        for (int z = 0; z < tilesZ; z++)
        {
            for (int x = 0; x < tilesX; x++)
            {
                SpawnTile(x, z, ctx, decayController);
            }
        }

        FinishGeneration(decayController);
    }

    /// <summary>
    /// Generates the grid over time to avoid frame spikes.
    /// </summary>
    public void GenerateGradually(float durationSeconds, int maxTilesPerFrame)
    {
        StopCurrentGeneration();

        if (durationSeconds <= 0f)
        {
            GenerateImmediate();
            return;
        }

        FloorDecayController decayController = GetComponent<FloorDecayController>();
        if (!ValidateSetup(decayController))
            return;

        generationRoutine = StartCoroutine(GenerateGraduallyRoutine(decayController, durationSeconds, maxTilesPerFrame));
    }

    /// <summary>
    /// Safety fallback: if a floor is about to be needed and it's still generating,
    /// this will complete it immediately (may spike, but prevents "no floor" bugs).
    /// </summary>
    public void ForceCompleteNow()
    {
        if (isGenerated)
            return;

        GenerateImmediate();
    }

    private IEnumerator GenerateGraduallyRoutine(FloorDecayController decayController, float durationSeconds, int maxTilesPerFrame)
    {
        isGenerating = true;
        isGenerated = false;

        decayController.ResetForNewGeneration();
        Clear();

        GenerationContext ctx = BuildContext();
        UpdateUnifiedFloorCollider(ctx.tileSize, ctx.right, ctx.forward, ctx.up);

        history.Clear();

        int total = TotalTiles;
        if (total <= 0)
        {
            FinishGeneration(decayController);
            yield break;
        }

        int capPerFrame = maxTilesPerFrame > 0 ? maxTilesPerFrame : defaultMaxTilesPerFrame;
        capPerFrame = Mathf.Max(1, capPerFrame);

        // Budget-based spawning: spreads instantiation evenly across the duration.
        float tilesPerSecond = total / Mathf.Max(0.01f, durationSeconds);
        float budget = 0f;
        int spawned = 0;

        while (spawned < total)
        {
            budget += tilesPerSecond * Time.deltaTime;

            int toSpawn = Mathf.Min(capPerFrame, Mathf.FloorToInt(budget));
            if (toSpawn <= 0)
            {
                yield return null;
                continue;
            }

            for (int i = 0; i < toSpawn && spawned < total; i++)
            {
                int z = spawned / tilesX;
                int x = spawned - (z * tilesX);

                SpawnTile(x, z, ctx, decayController);
                spawned++;
                budget -= 1f;
            }

            yield return null;
        }

        FinishGeneration(decayController);
    }

    private void FinishGeneration(FloorDecayController decayController)
    {
        decayController.CacheTileColliders();

        // 🔽🔽🔽 ADD THIS BLOCK 🔽🔽🔽
        var wallLines = GetComponentsInChildren<WallLineGenerator>(includeInactive: true);
        for (int i = 0; i < wallLines.Length; i++)
        {
            wallLines[i].Generate();
        }
        // 🔼🔼🔼 END ADD 🔼🔼🔼

        isGenerating = false;
        isGenerated = true;
        generationRoutine = null;

        OnGenerationComplete?.Invoke(this);
    }


    private void StopCurrentGeneration()
    {
        if (generationRoutine != null)
        {
            StopCoroutine(generationRoutine);
            generationRoutine = null;
        }

        isGenerating = false;
    }

    private bool ValidateSetup(FloorDecayController decayController)
    {
        if (decayController == null)
        {
            Debug.LogError("[FloorTileGenerator] FloorDecayController missing on floor root.");
            return false;
        }

        if (floorTiles == null || floorTiles.Length == 0)
        {
            Debug.LogError("[FloorTileGenerator] No floor tiles assigned.");
            return false;
        }

        if (tilesX <= 0 || tilesZ <= 0)
        {
            Debug.LogError("[FloorTileGenerator] tilesX/tilesZ must be > 0.");
            return false;
        }

        EnsureWeightsBuffer();
        return true;
    }

    private void EnsureWeightsBuffer()
    {
        int len = floorTiles != null ? floorTiles.Length : 0;
        if (len <= 0)
            return;

        if (weightsBuffer == null || weightsBuffer.Length != len)
            weightsBuffer = new float[len];
    }

    private GenerationContext BuildContext()
    {
        Vector3 right = gridRight.sqrMagnitude > 0.0001f ? gridRight.normalized : Vector3.right;
        Vector3 forward = gridForward.sqrMagnitude > 0.0001f ? gridForward.normalized : Vector3.forward;
        Vector3 up = tileUp.sqrMagnitude > 0.0001f ? tileUp.normalized : Vector3.up;

        Quaternion baseRotation = Quaternion.LookRotation(forward, up) * Quaternion.Euler(baseEulerOffset);
        GameObject sizeProbePrefab = null;
        for (int i = 0; i < floorTiles.Length; i++)
        {
            if (floorTiles[i] != null)
            {
                sizeProbePrefab = floorTiles[i];
                break;
            }
        }

        Vector3 tileSize = GetTileSize(sizeProbePrefab, right, forward);

        return new GenerationContext
        {
            right = right,
            forward = forward,
            up = up,
            baseRotation = baseRotation,
            tileSize = tileSize
        };
    }

    private void SpawnTile(int x, int z, GenerationContext ctx, FloorDecayController decayController)
    {
        int prefabIndex = PickBlueNoiseIndex(history);
        if (prefabIndex < 0 || prefabIndex >= floorTiles.Length)
            prefabIndex = 0;

        GameObject prefab = floorTiles[prefabIndex];
        if (prefab == null)
            return;

        GameObject tile = Instantiate(prefab, transform);

        Vector3 pos = startLocalPosition;
        pos += ctx.right * ctx.tileSize.x * x;
        pos += ctx.forward * ctx.tileSize.z * z;

        Quaternion finalRot = ctx.baseRotation;

        if (randomizeRotation && rotationStep > 0)
        {
            int steps = Mathf.Max(1, 360 / rotationStep);
            int stepIndex = Random.Range(0, steps);
            float angle = stepIndex * rotationStep;

            Quaternion yaw = Quaternion.AngleAxis(angle, ctx.up);
            finalRot = yaw * ctx.baseRotation;
        }

        tile.transform.localPosition = pos;
        tile.transform.localRotation = finalRot;

        // Maintain "blue-noise-ish" history.
        history.Add(prefabIndex);
        if (history.Count > historySize)
            history.RemoveAt(0);

        // Register for decay.
        FloorTileDecay decay = tile.GetComponent<FloorTileDecay>();
        if (decay != null)
            decayController.RegisterTile(decay);
    }

    // --------------------------------------------------
    // Unified Floor Collider
    // --------------------------------------------------

    private void UpdateUnifiedFloorCollider(Vector3 tileSize, Vector3 right, Vector3 forward, Vector3 up)
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

        floorCollider.size = new Vector3(paddedWidth, floorColliderHeight, paddedDepth);

        Vector3 center =
            startLocalPosition +
            right * (baseWidth * 0.5f) +
            forward * (baseDepth * 0.5f) +
            up * (floorColliderHeight * 0.5f);

        floorCollider.center = center;
    }

    // --------------------------------------------------
    // Blue-noise-ish picker (allocation-free)
    // --------------------------------------------------

    private int PickBlueNoiseIndex(List<int> recentHistory)
    {
        EnsureWeightsBuffer();
        if (weightsBuffer == null || weightsBuffer.Length == 0)
            return 0;

        for (int i = 0; i < weightsBuffer.Length; i++)
            weightsBuffer[i] = 1f;

        float penaltyMul = 1f - repeatPenalty;
        for (int i = 0; i < recentHistory.Count; i++)
        {
            int h = recentHistory[i];
            if ((uint)h < (uint)weightsBuffer.Length)
                weightsBuffer[h] *= penaltyMul;
        }

        float total = 0f;
        for (int i = 0; i < weightsBuffer.Length; i++)
            total += weightsBuffer[i];

        float r = Random.value * total;

        for (int i = 0; i < weightsBuffer.Length; i++)
        {
            r -= weightsBuffer[i];
            if (r <= 0f)
                return i;
        }

        return Random.Range(0, weightsBuffer.Length);
    }

    // --------------------------------------------------
    // Utils
    // --------------------------------------------------

    private Vector3 GetTileSize(GameObject prefab, Vector3 right, Vector3 forward)
    {
        Renderer r = prefab != null ? prefab.GetComponentInChildren<Renderer>() : null;
        if (!r)
        {
            Debug.LogWarning("[FloorTileGenerator] Tile has no Renderer, defaulting to 1x1");
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
        {
            Transform child = transform.GetChild(i);

            // 🔒 DO NOT DELETE WALL LINE GENERATORS
            if (child.GetComponent<WallLineGenerator>() != null)
                continue;

            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }
    }

}
