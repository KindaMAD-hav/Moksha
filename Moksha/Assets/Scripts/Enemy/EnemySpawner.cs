using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

/// <summary>
/// Optimized roguelite-style enemy spawner with level-based enemy type unlocking.
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
    [Tooltip("Each enemy type has its own unlock level setting")]
    [SerializeField] private EnemySpawnEntry[] enemyTypes;

    [Header("Runtime Info")]
    [SerializeField] private int currentEnemyCount;
    [SerializeField] private float gameTime;

    [Header("Stall Recovery (Safety Net)")]
    [SerializeField] private bool enableStallRecovery = true;
    [SerializeField] private float stallSeconds = 3f;
    [SerializeField] private int recoveryBurst = 6;
    [SerializeField] private bool logRecoveryInEditor = true;

    private float lastSuccessfulSpawnUnscaledTime;

    // Object pools - array-based for cache efficiency
    private Queue<EnemyBase>[] enemyPools;
    private int[] prefabInstanceIds;

    // Pre-allocated for spawn selection (avoid allocations)
    private int[] availableIndices;
    private float[] cumulativeWeights;
    private int availableCount;

    // Cached calculations
    private float currentSpawnInterval;
    private int currentEnemiesPerSpawn;
    private float spawnTimer;
    private bool isSpawning = true;

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
        lastSuccessfulSpawnUnscaledTime = Time.unscaledTime;
    }

    private void Update()
    {
        if (!isSpawning | playerTransform == null) return;

        float dt = Time.deltaTime;
        gameTime += dt;

        spawnTimer += dt;
        if (spawnTimer >= currentSpawnInterval)
        {
            spawnTimer = 0f;
            SpawnWave();
        }

        // --- Stall Recovery Watchdog ---
        if (enableStallRecovery && Time.timeScale > 0f && playerTransform != null)
        {
            if (currentEnemyCount < maxEnemies &&
                (Time.unscaledTime - lastSuccessfulSpawnUnscaledTime) > stallSeconds)
            {
                isSpawning = true;

                int burst = Mathf.Min(recoveryBurst, maxEnemies - currentEnemyCount);
                for (int i = 0; i < burst; i++)
                    SpawnEnemy();

#if UNITY_EDITOR
                if (logRecoveryInEditor)
                    Debug.Log($"[EnemySpawner] Stall recovery triggered. Burst={burst}, Level={cachedPlayerLevel}, Count={currentEnemyCount}");
#endif

                lastSuccessfulSpawnUnscaledTime = Time.unscaledTime;
            }
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

        // Select from all unlocked enemy types using weighted random
        int entryIndex = SelectEnemyTypeIndex();
        if (entryIndex < 0) return;

        EnemySpawnEntry entry = enemyTypes[entryIndex];
        if (entry.prefab == null) return;

        if (!CalculateSpawnPosition())
            return;

        // Get from pool
        EnemyBase enemy = GetFromPool(entryIndex, entry);
        if (enemy == null) return;

        // Setup enemy
        Transform enemyTransform = enemy.transform;
        enemyTransform.position = spawnPosition;

        // Calculate rotation to face player
        float lookX = playerPos.x - spawnPosition.x;
        float lookZ = playerPos.z - spawnPosition.z;
        if (lookX * lookX + lookZ * lookZ > 0.001f)
        {
            enemyTransform.rotation = Quaternion.LookRotation(new Vector3(lookX, 0f, lookZ));
        }

        enemy.Initialize(entry.stats, playerTransform);
        enemy.gameObject.SetActive(true);
        lastSuccessfulSpawnUnscaledTime = Time.unscaledTime;

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

    /// <summary>
    /// Selects an enemy type from all unlocked types using weighted random selection.
    /// Only considers enemies whose unlock level is less than or equal to current player level.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int SelectEnemyTypeIndex()
    {
        // Build available list without allocations
        availableCount = 0;
        float totalWeight = 0f;

        for (int i = 0; i < enemyTypes.Length; i++)
        {
            EnemySpawnEntry entry = enemyTypes[i];
            // Check if enemy is unlocked (player level >= unlock level)
            if (entry.stats != null && cachedPlayerLevel >= entry.unlockLevel)
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

#if UNITY_EDITOR
        // Log which enemies are now available
        int unlockedCount = 0;
        for (int i = 0; i < enemyTypes.Length; i++)
        {
            if (enemyTypes[i].unlockLevel <= newLevel)
                unlockedCount++;
        }
        Debug.Log($"[EnemySpawner] Level {newLevel} - {unlockedCount} enemy types unlocked");
#endif
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

        // Fallback (clamp to world bounds)
        spawnPosition.x = Mathf.Clamp(
            playerPos.x + Random.Range(-maxSpawnDistance, maxSpawnDistance),
            worldMin.x,
            worldMax.x
        );

        spawnPosition.z = Mathf.Clamp(
            playerPos.z + Random.Range(-maxSpawnDistance, maxSpawnDistance),
            worldMin.y,
            worldMax.y
        );

        spawnPosition.y = playerPos.y;
        return true;
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
        cachedPlayerLevel = 1;
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
        public int unlockedEnemyTypes;
    }

    public DifficultySnapshot CurrentDifficulty
    {
        get
        {
            int unlocked = 0;
            for (int i = 0; i < enemyTypes.Length; i++)
            {
                if (enemyTypes[i].unlockLevel <= cachedPlayerLevel)
                    unlocked++;
            }

            return new DifficultySnapshot
            {
                level = cachedPlayerLevel,
                spawnInterval = currentSpawnInterval,
                enemiesPerSpawn = currentEnemiesPerSpawn,
                enemyHealthMultiplier = 1f + cachedPlayerLevel * 0.1f,
                enemyDamageMultiplier = 1f + cachedPlayerLevel * 0.05f,
                unlockedEnemyTypes = unlocked
            };
        }
    }
}

[System.Serializable]
public class EnemySpawnEntry
{
    [Tooltip("The enemy prefab to spawn")]
    public GameObject prefab;

    [Tooltip("Stats for this enemy type")]
    public EnemyStats stats;

    [Tooltip("Pool size for object pooling")]
    public int poolSize = 20;

    [Tooltip("Player level required to unlock this enemy type")]
    public int unlockLevel = 1;
}