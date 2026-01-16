using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

/// <summary>
/// Optimized roguelite-style enemy spawner with level-based enemy unlocks.
/// Uses Stack-based object pooling, frame-cached selection weights, and trig-less math.
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

    [Header("Boss Settings")]
    [Tooltip("Boss enemy configuration (only one boss can exist at a time)")]
    [SerializeField] private BossSpawnEntry bossEntry;
    [Tooltip("Time in seconds before boss spawns (0 = never)")]
    [SerializeField] private float bossSpawnTime = 120f;

    [Header("Runtime Info")]
    [SerializeField] private int currentEnemyCount;
    [SerializeField] private float gameTime;

    [Header("Stall Recovery")]
    [SerializeField] private bool enableStallRecovery = true;
    [SerializeField] private float stallSeconds = 3f;
    [SerializeField] private int recoveryBurst = 6;
    [SerializeField] private bool logRecoveryInEditor = true;

    private float lastSuccessfulSpawnUnscaledTime;

    // OPTIMIZATION: Stack provides better cache locality (LIFO) than Queue
    private Stack<EnemyBase>[] enemyPools;
    private int[] prefabInstanceIds;

    // Cached calculations
    private float currentSpawnInterval;
    private int currentEnemiesPerSpawn;
    private float spawnTimer;
    private bool isSpawning = true;

    // Pre-allocated for spawn selection
    private int[] availableIndices;
    private float[] cumulativeWeights;
    private int availableCount;
    private int lastWeightCalculationFrame = -1; // For frame caching
    private float cachedTotalWeight;

    // Cached vectors
    private Vector3 spawnPosition;
    private Vector3 playerPos;

    // Cached math
    private float spawnDistanceRange;

    public int CurrentEnemyCount => currentEnemyCount;
    public float GameTime => gameTime;

    private int cachedPlayerLevel = 1;

    // Boss tracking
    private BossEnemy currentBoss;
    private bool bossSpawned;
    private bool bossHasBeenDefeated;

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
        enemyPools = new Stack<EnemyBase>[typeCount];
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

        // Check for boss spawn
        if (bossEntry != null && bossEntry.bossPrefab != null && 
            !bossSpawned && !bossHasBeenDefeated && 
            bossSpawnTime > 0f && gameTime >= bossSpawnTime)
        {
            SpawnBoss();
        }

        spawnTimer += dt;
        if (spawnTimer >= currentSpawnInterval)
        {
            spawnTimer = 0f;
            SpawnWave();
        }

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
                    Debug.Log($"[EnemySpawner] Stall recovery triggered.");
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

        // The weight calculation will happen on the first SpawnEnemy call of this frame,
        // and subsequent calls in this loop will reuse the result.
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
            return;

        EnemyBase enemy = GetFromPool(entryIndex, entry);
        if (enemy == null) return;

        Transform enemyTransform = enemy.transform;
        enemyTransform.position = spawnPosition;

        float lookX = playerPos.x - spawnPosition.x;
        float lookZ = playerPos.z - spawnPosition.z;
        if (lookX * lookX + lookZ * lookZ > 0.001f)
        {
            enemyTransform.rotation = Quaternion.LookRotation(new Vector3(lookX, 0f, lookZ));
        }

        enemy.Initialize(entry.stats, playerTransform);
        enemy.gameObject.SetActive(true);
        lastSuccessfulSpawnUnscaledTime = Time.unscaledTime;

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
        // OPTIMIZATION: Only recalculate weights once per frame.
        // This is crucial for SpawnWave which calls this method multiple times in a loop.
        int currentFrame = Time.frameCount;

        if (lastWeightCalculationFrame != currentFrame)
        {
            availableCount = 0;
            cachedTotalWeight = 0f;

            // Iterate array directly, no LINQ
            for (int i = 0; i < enemyTypes.Length; i++)
            {
                EnemySpawnEntry entry = enemyTypes[i];

                if (entry.stats != null &&
                    cachedPlayerLevel >= entry.unlockAtLevel &&
                    gameTime >= entry.stats.minSpawnTime)
                {
                    availableIndices[availableCount] = i;
                    cachedTotalWeight += entry.stats.spawnWeight;
                    cumulativeWeights[availableCount] = cachedTotalWeight;
                    availableCount++;
                }
            }
            lastWeightCalculationFrame = currentFrame;
        }

        if (availableCount == 0) return -1;

        // Weighted random selection
        float random = Random.value * cachedTotalWeight;
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
        currentSpawnInterval = spawnInterval - (spawnIntervalDecreaseRate * level);
        if (currentSpawnInterval < minSpawnInterval)
            currentSpawnInterval = minSpawnInterval;

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
            // OPTIMIZATION: Replaced expensive sin/cos loop with normalized random vector.
            // insideUnitCircle is a fast native call.
            Vector2 dir = UnityEngine.Random.insideUnitCircle;

            // Normalize safely
            float sqrMag = dir.sqrMagnitude;
            if (sqrMag < 0.0001f)
            {
                dir.x = 1f; dir.y = 0f; // Default fallback
            }
            else
            {
                // Fast manual normalize if needed, or just use built-in which is optimized
                float invMag = 1f / Mathf.Sqrt(sqrMag);
                dir.x *= invMag;
                dir.y *= invMag;
            }

            float distance = minSpawnDistance + Random.value * spawnDistanceRange;

            // Manual multiply
            spawnPosition.x = playerPos.x + dir.x * distance;
            spawnPosition.y = playerPos.y;
            spawnPosition.z = playerPos.z + dir.y * distance;

            if (IsValidSpawnPosition(spawnPosition))
                return true;
        }

        // Fallback (unchanged logic, just safer clamp)
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

            // OPTIMIZATION: Using Stack instead of Queue
            Stack<EnemyBase> pool = new Stack<EnemyBase>(entry.poolSize);

            // Pre-warm pool
            for (int j = 0; j < entry.poolSize; j++)
            {
                GameObject obj = Instantiate(entry.prefab, transform);
                EnemyBase enemy = obj.GetComponent<EnemyBase>();
                obj.SetActive(false);
                pool.Push(enemy);
            }

            enemyPools[i] = pool;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private EnemyBase GetFromPool(int index, EnemySpawnEntry entry)
    {
        Stack<EnemyBase> pool = enemyPools[index];

        EnemyBase enemy;
        if (pool.Count > 0)
        {
            enemy = pool.Pop();
            enemy.ResetEnemy();
        }
        else
        {
            // Expansion
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
        enemyPools[index].Push(enemy);
    }

    private void OnEnemyDeath(EnemyBase enemy)
    {
        currentEnemyCount--;

        // Linear scan is fine for small type counts (usually < 20)
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

    // --- BOSS SPAWNING ---

    private void SpawnBoss()
    {
        if (bossEntry == null || bossEntry.bossPrefab == null) return;
        if (bossSpawned || currentBoss != null) return;

        if (!CalculateSpawnPosition())
            return;

        // Instantiate boss
        GameObject bossObj = Instantiate(bossEntry.bossPrefab, spawnPosition, Quaternion.identity);
        BossEnemy boss = bossObj.GetComponent<BossEnemy>();

        if (boss == null)
        {
            Debug.LogError("[EnemySpawner] Boss prefab missing BossEnemy component!");
            Destroy(bossObj);
            return;
        }

        currentBoss = boss;
        bossSpawned = true;

        // Face player
        float lookX = playerPos.x - spawnPosition.x;
        float lookZ = playerPos.z - spawnPosition.z;
        if (lookX * lookX + lookZ * lookZ > 0.001f)
        {
            bossObj.transform.rotation = Quaternion.LookRotation(new Vector3(lookX, 0f, lookZ));
        }

        // Initialize boss
        if (bossEntry.stats != null)
        {
            boss.Initialize(bossEntry.stats, playerTransform);
        }
        else
        {
            boss.SetTarget(playerTransform);
        }

        bossObj.SetActive(true);

        // Register with EnemyManager
        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.RegisterEnemy(boss);
            boss.SetManagedByManager(true);
        }

        // Listen to boss death
        boss.OnDeath += OnBossDeathInternal;

        Debug.Log($"[EnemySpawner] Boss spawned at {gameTime:F1}s!");
    }

    private void OnBossDeathInternal(EnemyBase boss)
    {
        boss.OnDeath -= OnBossDeathInternal;
        bossHasBeenDefeated = true;
        currentBoss = null;

        if (EnemyManager.Instance != null)
            EnemyManager.Instance.UnregisterEnemy(boss);

        Debug.Log("[EnemySpawner] Boss defeated!");
    }

    /// <summary>
    /// Called by BossEnemy when it dies (alternative notification path)
    /// </summary>
    public void OnBossDeath()
    {
        bossSpawned = false;
        bossHasBeenDefeated = true;
        currentBoss = null;
    }

    public bool IsBossActive => currentBoss != null && !currentBoss.IsDead;
    public bool HasBossBeenDefeated => bossHasBeenDefeated;

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
        lastWeightCalculationFrame = -1; // Force weight recalc
        
        // Reset boss state
        bossSpawned = false;
        bossHasBeenDefeated = false;
        currentBoss = null;
    }

    public List<string> GetUnlockedEnemyTypes()
    {
        List<string> unlocked = new List<string>();
        for (int i = 0; i < enemyTypes.Length; i++)
        {
            if (enemyTypes[i].prefab != null && cachedPlayerLevel >= enemyTypes[i].unlockAtLevel)
            {
                unlocked.Add(enemyTypes[i].prefab.name);
            }
        }
        return unlocked;
    }

#if UNITY_EDITOR
    [ContextMenu("Spawn Enemy")]
    public void DebugSpawnEnemy() => SpawnEnemy();

    [ContextMenu("Spawn Wave")]
    public void DebugSpawnWave() => SpawnWave();

    [ContextMenu("Show Unlocked Enemies")]
    public void ShowUnlockedEnemies()
    {
        var unlocked = GetUnlockedEnemyTypes();
        Debug.Log($"[EnemySpawner] Level {cachedPlayerLevel} - Unlocked enemies: {string.Join(", ", unlocked)}");
    }

    private void OnDrawGizmosSelected()
    {
        Transform center = playerTransform != null ? playerTransform : transform;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(center.position, minSpawnDistance);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(center.position, maxSpawnDistance);
        // ... (Remaining Gizmos identical to original)
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
            int unlockedCount = 0;
            for (int i = 0; i < enemyTypes.Length; i++)
            {
                if (enemyTypes[i].prefab != null && cachedPlayerLevel >= enemyTypes[i].unlockAtLevel)
                    unlockedCount++;
            }

            return new DifficultySnapshot
            {
                level = cachedPlayerLevel,
                spawnInterval = currentSpawnInterval,
                enemiesPerSpawn = currentEnemiesPerSpawn,
                enemyHealthMultiplier = 1f + cachedPlayerLevel * 0.1f,
                enemyDamageMultiplier = 1f + cachedPlayerLevel * 0.05f,
                unlockedEnemyTypes = unlockedCount
            };
        }
    }
}

// THIS CLASS WAS MISSING BEFORE:
[System.Serializable]
public class EnemySpawnEntry
{
    [Tooltip("Enemy prefab to spawn")]
    public GameObject prefab;

    [Tooltip("Enemy stats configuration")]
    public EnemyStats stats;

    [Tooltip("Number of enemies to pre-create in the pool")]
    public int poolSize = 20;

    [Tooltip("Player level required to unlock this enemy type")]
    public int unlockAtLevel = 1;
}

[System.Serializable]
public class BossSpawnEntry
{
    [Tooltip("Boss enemy prefab (must have BossEnemy component)")]
    public GameObject bossPrefab;

    [Tooltip("Boss stats configuration")]
    public EnemyStats stats;
}