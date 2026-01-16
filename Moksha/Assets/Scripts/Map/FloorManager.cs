using UnityEngine;
using System.Collections;


public class FloorManager : MonoBehaviour
{
    public static FloorManager Instance { get; private set; }

    [Header("Floor Settings")]
    [SerializeField] private float floorHeightOffset = -10f;

    private FloorTileGenerator currentFloor;
    private float landingY;

    // Store the original floor settings to copy for new floors
    private FloorTileGenerator templateFloor;

    // PERSISTENT: Store template materials from the first floor for ALL subsequent floors
    // This ensures floors 2, 3, 4, etc. all use the correct materials
    private Material[] templateMaterials;
    private bool hasTemplateMaterials;


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Stores the template materials from the first floor. Call once when first floor initializes.
    /// </summary>
    public void SetTemplateMaterials(Material[] materials)
    {
        if (hasTemplateMaterials || materials == null || materials.Length == 0)
            return;

        // Deep copy the materials array
        templateMaterials = new Material[materials.Length];
        for (int i = 0; i < materials.Length; i++)
        {
            templateMaterials[i] = materials[i];
        }
        hasTemplateMaterials = true;
        Debug.Log($"[FloorManager] Template materials stored: {string.Join(", ", System.Array.ConvertAll(templateMaterials, m => m != null ? m.name : "null"))}");
    }

    /// <summary>
    /// Gets the template materials for applying to new floor tiles.
    /// </summary>
    public Material[] GetTemplateMaterials()
    {
        return hasTemplateMaterials ? templateMaterials : null;
    }

    /// <summary>
    /// Checks if template materials have been captured.
    /// </summary>
    public bool HasTemplateMaterials => hasTemplateMaterials;

    public void RegisterCurrentFloor(FloorTileGenerator floor)
    {
        currentFloor = floor;

        // Store the first floor as our template
        if (templateFloor == null)
        {
            templateFloor = floor;
            Debug.Log("[FloorManager] Template floor registered");
        }
    }

    public void OnFloorCollapseStarted(FloorDecayController collapsingFloor)
    {
        Debug.Log("[FloorManager] Floor collapse started");

        // 1. Generate the next floor below first (sets landingY)
        GenerateNextFloorBelow(collapsingFloor.transform);

        // 2. Now schedule falls safely
        if (EnemyManager.Instance != null)
        {
            var enemies = EnemyManager.Instance.GetActiveEnemiesUnsafe();
            for (int i = 0; i < enemies.Length; i++)
            {
                EnemyBase enemy = enemies[i];
                if (enemy == null) continue;

                float delay = Vector3.Distance(
                    enemy.transform.position,
                    collapsingFloor.CollapseCenter
                ) / 10f;

                StartCoroutine(DelayedFall(enemy, delay));
            }
        }
    }

    private IEnumerator DelayedFall(EnemyBase enemy, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (enemy != null)
            enemy.ForceFall(landingY);
    }

    private void GenerateNextFloorBelow(Transform collapsingFloorTransform)
    {
        if (templateFloor == null)
        {
            Debug.LogError("[FloorManager] No template floor registered!");
            return;
        }

        // Calculate new position
        Vector3 newPos = collapsingFloorTransform.position;
        newPos.y += floorHeightOffset;

        // Create new floor GameObject
        GameObject newFloorGO = new GameObject("Floor_" + Time.frameCount);
        newFloorGO.transform.position = newPos;
        newFloorGO.transform.rotation = Quaternion.identity;

        // Add FloorTileGenerator component and copy ALL settings from template
        FloorTileGenerator newGenerator = newFloorGO.AddComponent<FloorTileGenerator>();
        CopyGeneratorSettings(templateFloor, newGenerator);
        
        // IMPORTANT: Disable auto-generate since we call Generate() manually after setup
        newGenerator.autoGenerateOnStart = false;

        // Add FloorDecayController component and copy settings
        FloorDecayController newDecay = newFloorGO.AddComponent<FloorDecayController>();
        CopyDecayControllerSettings(templateFloor.GetComponent<FloorDecayController>(), newDecay);

        // NOW generate the floor - this uses the exact same logic as the first floor
        newGenerator.Generate();

        // Set landing Y for enemies
        landingY = newDecay.FloorY;

        // Update current floor reference
        currentFloor = newGenerator;

        Debug.Log($"[FloorManager] New floor generated at Y={newPos.y} with {newGenerator.tilesX * newGenerator.tilesZ} tiles");
    }

    private void CopyGeneratorSettings(FloorTileGenerator from, FloorTileGenerator to)
    {
        // Copy all public fields that affect floor generation
        to.floorTiles = from.floorTiles;  // Same prefabs!
        to.tilesX = from.tilesX;
        to.tilesZ = from.tilesZ;
        to.startLocalPosition = from.startLocalPosition;
        to.gridRight = from.gridRight;
        to.gridForward = from.gridForward;
        to.tileUp = from.tileUp;
        to.baseEulerOffset = from.baseEulerOffset;
        to.randomizeRotation = from.randomizeRotation;
        to.rotationStep = from.rotationStep;
        to.historySize = from.historySize;
        to.repeatPenalty = from.repeatPenalty;
        to.floorColliderHeight = from.floorColliderHeight;
        to.colliderPaddingPercent = from.colliderPaddingPercent;
    }

    private void CopyDecayControllerSettings(FloorDecayController from, FloorDecayController to)
    {
        if (from == null || to == null) return;

        // Use Unity's JSON serialization to copy all serialized fields
        string json = JsonUtility.ToJson(from);
        JsonUtility.FromJsonOverwrite(json, to);
    }
}
