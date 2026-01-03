using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Optimized roguelite-style enemy spawner.
/// Uses object pooling, avoids LINQ, minimizes allocations.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    public static EnemySpawner Instance { get; private set; }

    [Header("Spawn Settings")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private float minSpawnDistance = 15f;
    [SerializeField] private float maxSpawnDistance = 25f;
    [SerializeField] private float spawnInterval = 2f;
    [SerializeField] private int maxEnemies = 200;

    [Header("Difficulty Scaling")]
    [SerializeField] private float minSpawnInterval = 0.3f;
    [SerializeField] private float spawnIntervalDecreaseRate = 0.01f;
    [SerializeField] private int baseEnemiesPerSpawn = 1;
    [SerializeField] private float enemiesPerSpawnIncreaseRate = 0.1f;

    [Header("Enemy Types")]
    [SerializeField] private EnemySpawnEntry[] enemyTypes;

    [Header("Runtime Info")]
    [SerializeField] private int currentEnemyCount;
    [SerializeField] private float gameTime;

    // Object pools - using arrays for cache efficiency
    private Dictionary<int, Queue<EnemyBase>> enemyPools;
    private Dictionary<int, EnemySpawnEntry> entryByPrefabId;
    
    // Cached calculations
    private float currentSpawnInterval;
    private int currentEnemiesPerSpawn;
    private float spawnTimer;
    private bool isSpawning = true;
    
    // Pre-allocated lists for spawn selection (avoid allocations)
    private List<EnemySpawnEntry> availableEntries;
    private float[] cumulativeWeights;
    
    // Cached vectors
    private Vector3 spawnPosition;
    private Vector3 playerPos;
    private Quaternion spawnRotation;

    public int CurrentEnemyCount => currentEnemyCount;
    public float GameTime => gameTime;

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
        
        // Pre-allocate collections
        int typeCount = enemyTypes != null ? enemyTypes.Length : 0;
        enemyPools = new Dictionary<int, Queue<EnemyBase>>(typeCount);
        entryByPrefabId = new Dictionary<int, EnemySpawnEntry>(typeCount);
        availableEntries = new List<EnemySpawnEntry>(typeCount);
        cumulativeWeights = new float[typeCount];
    }

    private void Start()
    {
        if (playerTransform == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerTransform = player.transform;
        }

        InitializePools();
    }

    private void Update()
    {
        if (!isSpawning || playerTransform == null) return;

        float dt = Time.deltaTime;
        gameTime += dt;
        
        // Update difficulty (simple math, no allocations)
        currentSpawnInterval = spawnInterval - (spawnIntervalDecreaseRate * gameTime);
        if (currentSpawnInterval < minSpawnInterval) 
            currentSpawnInterval = minSpawnInterval;
        
        currentEnemiesPerSpawn = baseEnemiesPerSpawn + (int)(enemiesPerSpawnIncreaseRate * gameTime);

        spawnTimer += dt;
        if (spawnTimer >= currentSpawnInterval)
        {
            spawnTimer = 0f;
            SpawnWave();
        }
    }

    private void SpawnWave()
    {
        int available = maxEnemies - currentEnemyCount;
        int toSpawn = currentEnemiesPerSpawn < available ? currentEnemiesPerSpawn : available;

        for (int i = 0; i < toSpawn; i++)
        {
            SpawnEnemy();
        }
    }

    private void SpawnEnemy()
    {
        if (currentEnemyCount >= maxEnemies) return;

        EnemySpawnEntry entry = SelectEnemyType();
        if (entry == null || entry.prefab == null) return;

        // Calculate spawn position
        CalculateSpawnPosition();

        // Get from pool
        int prefabId = entry.prefab.GetInstanceID();
        EnemyBase enemy = GetFromPool(prefabId, entry);
        if (enemy == null) return;

        // Setup enemy
        Transform enemyTransform = enemy.transform;
        enemyTransform.position = spawnPosition;
        
        // Calculate rotation to face player
        Vector3 lookDir = playerPos - spawnPosition;
        lookDir.y = 0f;
        if (lookDir.sqrMagnitude > 0.001f)
            spawnRotation = Quaternion.LookRotation(lookDir);
        enemyTransform.rotation = spawnRotation;
        
        enemy.Initialize(entry.stats, playerTransform);
        enemy.gameObject.SetActive(true);
        
        // Register with manager and subscribe to death
        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.RegisterEnemy(enemy);
            enemy.SetManagedByManager(true);
        }
        else
        {
            enemy.SetManagedByManager(false);
        }
            
        enemy.OnDeath += OnEnemyDeath;
        currentEnemyCount++;
    }

    private EnemySpawnEntry SelectEnemyType()
    {
        // Build available list without allocations (reuse list)
        availableEntries.Clear();
        float totalWeight = 0f;
        int count = 0;

        for (int i = 0; i < enemyTypes.Length; i++)
        {
            EnemySpawnEntry entry = enemyTypes[i];
            if (entry.stats != null && gameTime >= entry.stats.minSpawnTime)
            {
                availableEntries.Add(entry);
                totalWeight += entry.stats.spawnWeight;
                
                // Store cumulative weight
                if (count < cumulativeWeights.Length)
                    cumulativeWeights[count] = totalWeight;
                count++;
            }
        }

        if (count == 0) return null;

        // Weighted random selection
        float random = Random.value * totalWeight;
        for (int i = 0; i < count; i++)
        {
            if (random <= cumulativeWeights[i])
                return availableEntries[i];
        }

        return availableEntries[count - 1];
    }

    private void CalculateSpawnPosition()
    {
        playerPos = playerTransform.position;
        
        // Use faster sin/cos approximation for angles
        float angle = Random.value * 6.28318f; // 2 * PI
        float distance = minSpawnDistance + Random.value * (maxSpawnDistance - minSpawnDistance);

        spawnPosition.x = playerPos.x + Mathf.Cos(angle) * distance;
        spawnPosition.y = playerPos.y;
        spawnPosition.z = playerPos.z + Mathf.Sin(angle) * distance;
    }

    private void InitializePools()
    {
        if (enemyTypes == null) return;

        for (int i = 0; i < enemyTypes.Length; i++)
        {
            EnemySpawnEntry entry = enemyTypes[i];
            if (entry.prefab == null) continue;

            int prefabId = entry.prefab.GetInstanceID();
            entryByPrefabId[prefabId] = entry;
            
            if (!enemyPools.ContainsKey(prefabId))
            {
                Queue<EnemyBase> pool = new Queue<EnemyBase>(entry.poolSize);
                
                // Pre-warm pool
                for (int j = 0; j < entry.poolSize; j++)
                {
                    GameObject obj = Instantiate(entry.prefab, transform);
                    EnemyBase enemy = obj.GetComponent<EnemyBase>();
                    obj.SetActive(false);
                    pool.Enqueue(enemy);
                }
                
                enemyPools[prefabId] = pool;
            }
        }
    }

    private EnemyBase GetFromPool(int prefabId, EnemySpawnEntry entry)
    {
        if (!enemyPools.TryGetValue(prefabId, out Queue<EnemyBase> pool))
        {
            pool = new Queue<EnemyBase>(16);
            enemyPools[prefabId] = pool;
        }

        EnemyBase enemy;
        if (pool.Count > 0)
        {
            enemy = pool.Dequeue();
            enemy.ResetEnemy();
        }
        else
        {
            GameObject obj = Instantiate(entry.prefab, transform);
            enemy = obj.GetComponent<EnemyBase>();
        }

        return enemy;
    }

    private void ReturnToPool(EnemyBase enemy)
    {
        enemy.OnDeath -= OnEnemyDeath;
        
        if (EnemyManager.Instance != null)
            EnemyManager.Instance.UnregisterEnemy(enemy);

        enemy.gameObject.SetActive(false);

        // Find pool by stats reference
        for (int i = 0; i < enemyTypes.Length; i++)
        {
            if (enemyTypes[i].stats == enemy.Stats)
            {
                int prefabId = enemyTypes[i].prefab.GetInstanceID();
                if (enemyPools.TryGetValue(prefabId, out Queue<EnemyBase> pool))
                {
                    pool.Enqueue(enemy);
                }
                break;
            }
        }
    }

    private void OnEnemyDeath(EnemyBase enemy)
    {
        currentEnemyCount--;
        ReturnToPool(enemy);
    }

    public void StartSpawning() => isSpawning = true;
    public void StopSpawning() => isSpawning = false;

    public void KillAllEnemies()
    {
        // Use EnemyManager to iterate efficiently
        if (EnemyManager.Instance != null)
        {
            List<EnemyBase> tempList = new List<EnemyBase>(currentEnemyCount);
            EnemyManager.Instance.GetActiveEnemies(tempList);
            
            for (int i = tempList.Count - 1; i >= 0; i--)
            {
                tempList[i].TakeDamage(float.MaxValue);
            }
        }
    }

    public void ResetSpawner()
    {
        KillAllEnemies();
        gameTime = 0f;
        spawnTimer = 0f;
        currentSpawnInterval = spawnInterval;
        currentEnemiesPerSpawn = baseEnemiesPerSpawn;
    }

#if UNITY_EDITOR
    [ContextMenu("Spawn Enemy")]
    public void DebugSpawnEnemy() => SpawnEnemy();

    [ContextMenu("Spawn Wave")]
    public void DebugSpawnWave() => SpawnWave();

    private void OnDrawGizmosSelected()
    {
        Transform center = playerTransform != null ? playerTransform : transform;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(center.position, minSpawnDistance);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(center.position, maxSpawnDistance);
    }
#endif
}

[System.Serializable]
public class EnemySpawnEntry
{
    public GameObject prefab;
    public EnemyStats stats;
    public int poolSize = 20;
}
