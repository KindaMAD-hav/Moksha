using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

/// <summary>
/// Centralized enemy manager that handles all enemy updates in batches.
/// Optimized with parallel arrays to eliminate extern calls during lookups.
/// </summary>
public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private Transform playerTransform;

    [Header("Debug")]
    [SerializeField] private int activeEnemyCount;

    // Dense array for cache-friendly iteration (no holes)
    private EnemyBase[] enemies;

    // OPTIMIZATION: Parallel array to cache positions. 
    // Removes the need to call transform.position (Slow C++ Interop) during distance checks.
    private Vector3[] cachedPositions;

    private int enemyCount;
    private int capacity;

    // Cached values - updated once per frame
    private Vector3 playerPosition;
    private float deltaTime;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Pre-allocate arrays
        capacity = 512;
        enemies = new EnemyBase[capacity];
        cachedPositions = new Vector3[capacity];
        enemyCount = 0;
    }

    private void Start()
    {
        if (playerTransform == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerTransform = player.transform;
        }
    }

    private void Update()
    {
        if (playerTransform == null || enemyCount == 0) return;

        // Cache once per frame
        deltaTime = Time.deltaTime;
        playerPosition = playerTransform.position;

        // Local caching of count for loop speed
        int count = enemyCount;

        // OPTIMIZATION: Single loop to Tick logic AND update cached positions
        for (int i = 0; i < count; i++)
        {
            // 1. Run Logic
            enemies[i].Tick(deltaTime, playerPosition);

            // 2. Cache Position for querying (e.g., by Projectiles)
            // This is safer than updating in Tick because we own the memory here
            cachedPositions[i] = enemies[i].transform.position;
        }

        activeEnemyCount = count;
    }

    /// <summary>
    /// Register an enemy with the manager. O(1) operation.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RegisterEnemy(EnemyBase enemy)
    {
        // Grow arrays if needed
        if (enemyCount >= capacity)
        {
            capacity *= 2;
            System.Array.Resize(ref enemies, capacity);
            System.Array.Resize(ref cachedPositions, capacity);
        }

        enemy.Index = enemyCount;
        enemies[enemyCount] = enemy;
        // Position will be valid next frame, or we can set it now to be safe
        cachedPositions[enemyCount] = enemy.transform.position;

        enemyCount++;
    }

    /// <summary>
    /// Unregister an enemy from the manager. O(1) operation using swap-remove.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UnregisterEnemy(EnemyBase enemy)
    {
        int index = enemy.Index;
        if (index < 0 || index >= enemyCount) return;

        int lastIndex = enemyCount - 1;

        // Swap with last element (if not already last) to keep array dense
        if (index < lastIndex)
        {
            EnemyBase lastEnemy = enemies[lastIndex];

            // Swap Object Reference
            enemies[index] = lastEnemy;

            // Swap Cached Position (to keep data aligned)
            cachedPositions[index] = cachedPositions[lastIndex];

            // Update Index
            lastEnemy.Index = index;
        }

        // Clear last slot
        enemies[lastIndex] = null;
        enemy.Index = -1;
        enemyCount--;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3 GetPlayerPosition() => playerPosition;

    public void SetPlayer(Transform player)
    {
        playerTransform = player;
    }

    public void GetActiveEnemies(List<EnemyBase> result)
    {
        result.Clear();
        if (result.Capacity < enemyCount)
            result.Capacity = enemyCount;

        for (int i = 0; i < enemyCount; i++)
        {
            EnemyBase enemy = enemies[i];
            // Bitwise check is faster than bool logic
            if (!enemy.IsDead | enemy.IsDissolving)
                result.Add(enemy);
        }
    }

    /// <summary>
    /// Find enemies within radius (squared distance for performance).
    /// OPTIMIZED: Uses cached float array instead of Transform access.
    /// </summary>
    public void GetEnemiesInRadius(Vector3 center, float radius, List<EnemyBase> result)
    {
        result.Clear();
        float radiusSqr = radius * radius;
        int count = enemyCount;

        // Cache vector components to stack variables
        float cx = center.x;
        float cy = center.y;
        float cz = center.z;

        for (int i = 0; i < count; i++)
        {
            // Check flags before distance (faster fail)
            EnemyBase enemy = enemies[i];
            if (enemy.IsDead && !enemy.IsDissolving) continue;

            // OPTIMIZATION: Read from C# array, NO External Call to transform.position
            Vector3 pos = cachedPositions[i];

            float dx = pos.x - cx;
            float dy = pos.y - cy;
            float dz = pos.z - cz;

            if ((dx * dx + dy * dy + dz * dz) <= radiusSqr)
            {
                result.Add(enemy);
            }
        }
    }

    public int EnemyCount => enemyCount;
}