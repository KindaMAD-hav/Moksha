using System.Collections.Generic;
using System.Runtime.CompilerServices;
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

    [Header("Spawn Bounds")]
    [SerializeField] private Vector2 worldMin; // bottom-left (X,Z)
    [SerializeField] private Vector2 worldMax; // top-right (X,Z)


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

    // Object pools - array-based for cache efficiency
    private Queue<EnemyBase>[] enemyPools;
    private int[] prefabInstanceIds;
    
    // Cached calculations
    private float currentSpawnInterval;
    private int currentEnemiesPerSpawn;
    private float spawnTimer;
    private bool isSpawning = true;
    
    // Pre-allocated for spawn selection (avoid allocations)
    private int[] availableIndices;
    private float[] cumulativeWeights;
    private int availableCount;

    
    // Cached vectors
    private Vector3 spawnPosition;
    private Vector3 playerPos;

    // Cached math
    private float spawnDistanceRange;
    private const float TWO_PI = 6.28318530718f;

    public int CurrentEnemyCount => currentEnemyCount;
    public float GameTime => gameTime;

    private int cachedPlayerLevel = 1;


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
        spawnDistanceRange = maxSpawnDistance - minSpawnDistance;
        
        // Pre-allocate collections
        int typeCount = enemyTypes != null ? enemyTypes.Length : 0;
        enemyPools = new Queue<EnemyBase>[typeCount];
        prefabInstanceIds = new int[typeCount];
        availableIndices = new int[typeCount];
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

        if (ExperienceManager.Instance != null)
        {
            cachedPlayerLevel = ExperienceManager.Instance.CurrentLevel;
            ExperienceManager.Instance.OnLevelUp += OnPlayerLevelUp;
        }
    }

    private void Update()
    {
        if (!isSpawning | playerTransform == null) return;

        float dt = Time.deltaTime;
        gameTime += dt;
        
        //// Update difficulty (simple math, no allocations)
        //currentSpawnInterval = spawnInterval - (spawnIntervalDecreaseRate * gameTime);
        //if (currentSpawnInterval < minSpawnInterval) 
        //    currentSpawnInterval = minSpawnInterval;
        
        //currentEnemiesPerSpawn = baseEnemiesPerSpawn + (int)(enemiesPerSpawnIncreaseRate * gameTime);

        spawnTimer += dt;
        if (spawnTimer >= currentSpawnInterval)
        {
            spawnTimer = 0f;
            SpawnWave();
        }
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsInsideBounds(Vector3 pos)
    {
        return pos.x >= worldMin.x &&
               pos.x <= worldMax.x &&
               pos.z >= worldMin.y &&
               pos.z <= worldMax.y;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        int entryIndex = SelectEnemyTypeIndex();
        if (entryIndex < 0) return;
        
        EnemySpawnEntry entry = enemyTypes[entryIndex];
        if (entry.prefab == null) return;

        if (!CalculateSpawnPosition())
            return; // skip spawning this enemy

        // Get from pool
        EnemyBase enemy = GetFromPool(entryIndex, entry);
        if (enemy == null) return;

        // Setup enemy
        Transform enemyTransform = enemy.transform;
        enemyTransform.position = spawnPosition;
        
        // Calculate rotation to face player (inline)
        float lookX = playerPos.x - spawnPosition.x;
        float lookZ = playerPos.z - spawnPosition.z;
        if (lookX * lookX + lookZ * lookZ > 0.001f)
        {
            enemyTransform.rotation = Quaternion.LookRotation(new Vector3(lookX, 0f, lookZ));
        }
        
        enemy.Initialize(entry.stats, playerTransform);
        enemy.gameObject.SetActive(true);
        
        // Register with manager
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int SelectEnemyTypeIndex()
    {
        // Build available list without allocations
        availableCount = 0;
        float totalWeight = 0f;

        for (int i = 0; i < enemyTypes.Length; i++)
        {
            EnemySpawnEntry entry = enemyTypes[i];
            if (entry.stats != null && gameTime >= entry.stats.minSpawnTime)
            {
                availableIndices[availableCount] = i;
                totalWeight += entry.stats.spawnWeight;
                cumulativeWeights[availableCount] = totalWeight;
                availableCount++;
            }
        }

        if (availableCount == 0) return -1;

        // Weighted random selection
        float random = Random.value * totalWeight;
        for (int i = 0; i < availableCount; i++)
        {
            if (random <= cumulativeWeights[i])
                return availableIndices[i];
        }

        return availableIndices[availableCount - 1];
    }
    private void OnDestroy()
    {
        if (ExperienceManager.Instance != null)
            ExperienceManager.Instance.OnLevelUp -= OnPlayerLevelUp;
    }

    private void OnPlayerLevelUp(int newLevel)
    {
        cachedPlayerLevel = newLevel;
        RecalculateDifficulty();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RecalculateDifficulty()
    {
        int level = cachedPlayerLevel;

        // Spawn interval shrinks as level increases
        currentSpawnInterval = spawnInterval - (spawnIntervalDecreaseRate * level);
        if (currentSpawnInterval < minSpawnInterval)
            currentSpawnInterval = minSpawnInterval;

        // Enemies per wave increases with level
        currentEnemiesPerSpawn = baseEnemiesPerSpawn +
                                 Mathf.FloorToInt(enemiesPerSpawnIncreaseRate * level);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool CalculateSpawnPosition()
    {
        playerPos = playerTransform.position;

        const int MAX_ATTEMPTS = 12;

        for (int i = 0; i < MAX_ATTEMPTS; i++)
        {
            float angle = Random.value * TWO_PI;
            float distance = minSpawnDistance + Random.value * spawnDistanceRange;

            spawnPosition.x = playerPos.x + Mathf.Cos(angle) * distance;
            spawnPosition.y = playerPos.y;
            spawnPosition.z = playerPos.z + Mathf.Sin(angle) * distance;

            if (IsValidSpawnPosition(spawnPosition))
                return true;
        }

        // ❌ No fallback clamp — just fail
        return false;
    }



    private void InitializePools()
    {
        if (enemyTypes == null) return;

        for (int i = 0; i < enemyTypes.Length; i++)
        {
            EnemySpawnEntry entry = enemyTypes[i];
            if (entry.prefab == null) continue;

            prefabInstanceIds[i] = entry.prefab.GetInstanceID();
            
            Queue<EnemyBase> pool = new Queue<EnemyBase>(entry.poolSize);
            
            // Pre-warm pool
            for (int j = 0; j < entry.poolSize; j++)
            {
                GameObject obj = Instantiate(entry.prefab, transform);
                EnemyBase enemy = obj.GetComponent<EnemyBase>();
                obj.SetActive(false);
                pool.Enqueue(enemy);
            }
            
            enemyPools[i] = pool;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private EnemyBase GetFromPool(int index, EnemySpawnEntry entry)
    {
        Queue<EnemyBase> pool = enemyPools[index];
        
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsValidSpawnPosition(Vector3 pos)
    {
        float dx = pos.x - playerPos.x;
        float dz = pos.z - playerPos.z;
        float sqrDist = dx * dx + dz * dz;

        return sqrDist >= minSpawnDistance * minSpawnDistance &&
               sqrDist <= maxSpawnDistance * maxSpawnDistance &&
               pos.x >= worldMin.x &&
               pos.x <= worldMax.x &&
               pos.z >= worldMin.y &&
               pos.z <= worldMax.y;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ReturnToPool(EnemyBase enemy, int index)
    {
        enemy.OnDeath -= OnEnemyDeath;
        
        if (EnemyManager.Instance != null)
            EnemyManager.Instance.UnregisterEnemy(enemy);

        enemy.gameObject.SetActive(false);
        enemyPools[index].Enqueue(enemy);
    }

    private void OnEnemyDeath(EnemyBase enemy)
    {
        currentEnemyCount--;
        
        // Find pool index by stats reference
        for (int i = 0; i < enemyTypes.Length; i++)
        {
            if (enemyTypes[i].stats == enemy.Stats)
            {
                ReturnToPool(enemy, i);
                return;
            }
        }
    }

    public void StartSpawning() => isSpawning = true;
    public void StopSpawning() => isSpawning = false;

    public void KillAllEnemies()
    {
        if (EnemyManager.Instance != null)
        {
            // Use stack-allocated list for small counts, otherwise heap
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
        Gizmos.color = Color.cyan;
        Vector3 center1 = new Vector3(
            (worldMin.x + worldMax.x) * 0.5f,
            0f,
            (worldMin.y + worldMax.y) * 0.5f
        );
        Vector3 size = new Vector3(
            worldMax.x - worldMin.x,
            0.1f,
            worldMax.y - worldMin.y
        );
        Gizmos.DrawWireCube(center1, size);

    }
#endif

    public struct DifficultySnapshot
    {
        public int level;
        public float spawnInterval;
        public int enemiesPerSpawn;
        public float enemyHealthMultiplier;
        public float enemyDamageMultiplier;
    }
    public DifficultySnapshot CurrentDifficulty => new DifficultySnapshot
    {
        level = cachedPlayerLevel,
        spawnInterval = currentSpawnInterval,
        enemiesPerSpawn = currentEnemiesPerSpawn,
        enemyHealthMultiplier = 1f + cachedPlayerLevel * 0.1f, // future
        enemyDamageMultiplier = 1f + cachedPlayerLevel * 0.05f // future
    };


}

[System.Serializable]
public class EnemySpawnEntry
{
    public GameObject prefab;
    public EnemyStats stats;
    public int poolSize = 20;
}
