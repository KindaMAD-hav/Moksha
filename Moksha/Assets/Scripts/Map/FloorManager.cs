using UnityEngine;
using System.Collections;


public class FloorManager : MonoBehaviour
{
    public static FloorManager Instance { get; private set; }

    [Header("Floor Settings")]
    [SerializeField] private float floorHeightOffset = -10f;

    private FloorTileGenerator currentFloor;
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

    public void RegisterCurrentFloor(FloorTileGenerator floor)
    {
        currentFloor = floor;
    }

    public void OnFloorCollapseStarted(FloorDecayController collapsingFloor)
    {
        Debug.Log("[FloorManager] Floor collapse started");

        // 1. Generate the next floor below first (sets landingY)
        GenerateNextFloorBelow(collapsingFloor.transform);

        // 2. Now schedule falls safely
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
    private IEnumerator DelayedFall(EnemyBase enemy, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (enemy != null)
            enemy.ForceFall(landingY);
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
        landingY = newDecay.FloorY;

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
