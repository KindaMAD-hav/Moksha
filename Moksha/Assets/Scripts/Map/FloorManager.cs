using UnityEngine;

public class FloorManager : MonoBehaviour
{
    public static FloorManager Instance { get; private set; }

    [Header("Floor Settings")]
    [SerializeField] private float floorHeightOffset = -10f;

    private FloorTileGenerator currentFloor;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void RegisterCurrentFloor(FloorTileGenerator floor)
    {
        currentFloor = floor;
    }

    public void OnFloorCollapseStarted(FloorDecayController collapsingFloor)
    {
        Debug.Log("[FloorManager] Floor collapse started");

        // 1. Generate the next floor below
        GenerateNextFloorBelow(collapsingFloor.transform);
    }

    private void GenerateNextFloorBelow(Transform collapsingFloor)
    {
        if (currentFloor == null)
        {
            Debug.LogError("[FloorManager] No current floor registered");
            return;
        }

        Vector3 newPos = collapsingFloor.position;
        newPos.y += floorHeightOffset;

        // Clone the floor root (layout stays identical)
        GameObject newFloorGO = Instantiate(
            currentFloor.gameObject,
            newPos,
            Quaternion.identity
        );

        FloorTileGenerator newGenerator =
            newFloorGO.GetComponent<FloorTileGenerator>();

        FloorDecayController newDecay =
            newFloorGO.GetComponent<FloorDecayController>();

        // Register new floor
        currentFloor = newGenerator;

        // Redirect spawner to the new floor
        if (EnemySpawner.Instance != null)
        {
            EnemySpawner.Instance.SetCurrentFloorDecay(newDecay);
        }

        Debug.Log("[FloorManager] New floor generated below");
    }
}
