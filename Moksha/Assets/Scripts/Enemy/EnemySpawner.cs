using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Roguelite-style enemy spawner. Spawns enemies around the player
/// with increasing difficulty over time. Uses object pooling for performance.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    public static EnemySpawner Instance { get; private set; }

    [Header("Spawn Settings")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private float minSpawnDistance = 15f;
    [SerializeField] private float maxSpawnDistance = 25f;
    [SerializeField] private float spawnInterval = 2f;
    [SerializeField] private int maxEnemies = 100;

    [Header("Difficulty Scaling")]
    [Tooltip("Spawn interval decreases over time")]
    [SerializeField] private float minSpawnInterval = 0.3f;
    [SerializeField] private float spawnIntervalDecreaseRate = 0.01f; // per second
    [Tooltip("More enemies spawn per wave over time")]
    [SerializeField] private int baseEnemiesPerSpawn = 1;
    [SerializeField] private float enemiesPerSpawnIncreaseRate = 0.1f; // per second

    [Header("Enemy Types")]
    [SerializeField] private List<EnemySpawnEntry> enemyTypes = new List<EnemySpawnEntry>();

    [Header("Spawn Area")]
    [SerializeField] private bool useSpawnZones = false;
    [SerializeField] private List<Transform> spawnZones = new List<Transform>();
    [SerializeField] private float spawnZoneRadius = 5f;

    [Header("Runtime Info (Read Only)")]
    [SerializeField] private int currentEnemyCount;
    [SerializeField] private float gameTime;
    [SerializeField] private float currentSpawnInterval;
    [SerializeField] private int currentEnemiesPerSpawn;

    // Object pooling
    private Dictionary<GameObject, Queue<EnemyBase>> enemyPools = new Dictionary<GameObject, Queue<EnemyBase>>();
    private List<EnemyBase> activeEnemies = new List<EnemyBase>();
    
    private float spawnTimer;
    private bool isSpawning = true;

    public int CurrentEnemyCount => currentEnemyCount;
    public float GameTime => gameTime;
    public bool IsSpawning => isSpawning;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        currentSpawnInterval = spawnInterval;
        currentEnemiesPerSpawn = baseEnemiesPerSpawn;
    }

    private void Start()
    {
        // Auto-find player
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerTransform = player.transform;
        }

        // Initialize pools
        InitializePools();
    }

    private void Update()
    {
        if (!isSpawning || playerTransform == null) return;

        gameTime += Time.deltaTime;
        UpdateDifficulty();

        spawnTimer += Time.deltaTime;
        if (spawnTimer >= currentSpawnInterval)
        {
            spawnTimer = 0f;
            SpawnWave();
        }
    }

    private void UpdateDifficulty()
    {
        // Decrease spawn interval over time
        currentSpawnInterval = Mathf.Max(
            minSpawnInterval,
            spawnInterval - (spawnIntervalDecreaseRate * gameTime)
        );

        // Increase enemies per spawn over time
        currentEnemiesPerSpawn = baseEnemiesPerSpawn + Mathf.FloorToInt(enemiesPerSpawnIncreaseRate * gameTime);
    }

    private void SpawnWave()
    {
        int enemiesToSpawn = Mathf.Min(currentEnemiesPerSpawn, maxEnemies - currentEnemyCount);

        for (int i = 0; i < enemiesToSpawn; i++)
        {
            SpawnEnemy();
        }
    }

    private void SpawnEnemy()
    {
        if (currentEnemyCount >= maxEnemies) return;

        // Select enemy type based on weights and time
        EnemySpawnEntry entry = SelectEnemyType();
        if (entry == null || entry.prefab == null) return;

        // Get spawn position
        Vector3 spawnPos = GetSpawnPosition();

        // Get from pool or instantiate
        EnemyBase enemy = GetEnemyFromPool(entry.prefab);
        if (enemy == null) return;

        // Setup enemy
        enemy.transform.position = spawnPos;
        enemy.transform.rotation = Quaternion.LookRotation(playerTransform.position - spawnPos);
        enemy.Initialize(entry.stats, playerTransform);
        enemy.gameObject.SetActive(true);

        // Subscribe to death event
        enemy.OnDeath += OnEnemyDeath;

        activeEnemies.Add(enemy);
        currentEnemyCount++;
    }

    private EnemySpawnEntry SelectEnemyType()
    {
        // Filter by min spawn time
        List<EnemySpawnEntry> available = enemyTypes.FindAll(e => 
            e.stats != null && gameTime >= e.stats.minSpawnTime);

        if (available.Count == 0) return null;

        // Weighted random selection
        float totalWeight = 0f;
        foreach (var entry in available)
            totalWeight += entry.stats.spawnWeight;

        float random = Random.Range(0f, totalWeight);
        float current = 0f;

        foreach (var entry in available)
        {
            current += entry.stats.spawnWeight;
            if (random <= current)
                return entry;
        }

        return available[available.Count - 1];
    }

    private Vector3 GetSpawnPosition()
    {
        if (useSpawnZones && spawnZones.Count > 0)
        {
            // Spawn in a random zone
            Transform zone = spawnZones[Random.Range(0, spawnZones.Count)];
            Vector2 randomCircle = Random.insideUnitCircle * spawnZoneRadius;
            return zone.position + new Vector3(randomCircle.x, 0, randomCircle.y);
        }
        else
        {
            // Spawn around player at random distance/angle
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float distance = Random.Range(minSpawnDistance, maxSpawnDistance);

            Vector3 offset = new Vector3(
                Mathf.Cos(angle) * distance,
                0,
                Mathf.Sin(angle) * distance
            );

            return playerTransform.position + offset;
        }
    }

    private void InitializePools()
    {
        foreach (var entry in enemyTypes)
        {
            if (entry.prefab != null && !enemyPools.ContainsKey(entry.prefab))
            {
                enemyPools[entry.prefab] = new Queue<EnemyBase>();
                
                // Pre-warm pool
                for (int i = 0; i < entry.poolSize; i++)
                {
                    EnemyBase enemy = Instantiate(entry.prefab).GetComponent<EnemyBase>();
                    enemy.gameObject.SetActive(false);
                    enemy.transform.SetParent(transform);
                    enemyPools[entry.prefab].Enqueue(enemy);
                }
            }
        }
    }

    private EnemyBase GetEnemyFromPool(GameObject prefab)
    {
        if (!enemyPools.ContainsKey(prefab))
            enemyPools[prefab] = new Queue<EnemyBase>();

        Queue<EnemyBase> pool = enemyPools[prefab];

        if (pool.Count > 0)
        {
            EnemyBase enemy = pool.Dequeue();
            enemy.ResetEnemy();
            return enemy;
        }
        else
        {
            // Create new if pool empty
            EnemyBase enemy = Instantiate(prefab).GetComponent<EnemyBase>();
            enemy.transform.SetParent(transform);
            return enemy;
        }
    }

    private void ReturnToPool(EnemyBase enemy, GameObject prefab)
    {
        enemy.OnDeath -= OnEnemyDeath;
        enemy.gameObject.SetActive(false);

        if (!enemyPools.ContainsKey(prefab))
            enemyPools[prefab] = new Queue<EnemyBase>();

        enemyPools[prefab].Enqueue(enemy);
    }

    private void OnEnemyDeath(EnemyBase enemy)
    {
        activeEnemies.Remove(enemy);
        currentEnemyCount--;

        // Find the prefab for this enemy type and return to pool
        foreach (var entry in enemyTypes)
        {
            if (entry.stats == enemy.Stats)
            {
                ReturnToPool(enemy, entry.prefab);
                break;
            }
        }
    }

    /// <summary>
    /// Start/resume spawning
    /// </summary>
    public void StartSpawning()
    {
        isSpawning = true;
    }

    /// <summary>
    /// Pause spawning
    /// </summary>
    public void StopSpawning()
    {
        isSpawning = false;
    }

    /// <summary>
    /// Kill all active enemies
    /// </summary>
    public void KillAllEnemies()
    {
        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            if (activeEnemies[i] != null)
                activeEnemies[i].TakeDamage(float.MaxValue);
        }
    }

    /// <summary>
    /// Reset spawner state (for new game)
    /// </summary>
    public void ResetSpawner()
    {
        KillAllEnemies();
        gameTime = 0f;
        spawnTimer = 0f;
        currentSpawnInterval = spawnInterval;
        currentEnemiesPerSpawn = baseEnemiesPerSpawn;
    }

#if UNITY_EDITOR
    [ContextMenu("Spawn Single Enemy")]
    public void DebugSpawnEnemy()
    {
        SpawnEnemy();
    }

    [ContextMenu("Spawn Wave")]
    public void DebugSpawnWave()
    {
        SpawnWave();
    }

    private void OnDrawGizmosSelected()
    {
        Transform center = playerTransform != null ? playerTransform : transform;

        // Min spawn distance
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(center.position, minSpawnDistance);

        // Max spawn distance
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(center.position, maxSpawnDistance);

        // Spawn zones
        if (useSpawnZones)
        {
            Gizmos.color = Color.cyan;
            foreach (var zone in spawnZones)
            {
                if (zone != null)
                    Gizmos.DrawWireSphere(zone.position, spawnZoneRadius);
            }
        }
    }
#endif
}

[System.Serializable]
public class EnemySpawnEntry
{
    public GameObject prefab;
    public EnemyStats stats;
    [Tooltip("Initial pool size for this enemy type")]
    public int poolSize = 10;
}
