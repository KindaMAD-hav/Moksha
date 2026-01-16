using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

/// <summary>
/// Centralized enemy manager that handles all enemy updates in batches.
/// Optimized for cache efficiency and minimal overhead.
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
    private int enemyCount;
    private int capacity;

    // Cached values - updated once per frame
    private Vector3 playerPosition;
    private float deltaTime;


    public EnemyBase[] GetActiveEnemiesUnsafe()
    {
        return enemies; // or whatever your internal array is
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Pre-allocate array
        capacity = 512;
        enemies = new EnemyBase[capacity];
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

        // Process all enemies - dense array means no null checks needed
        int count = enemyCount;
        //for (int i = 0; i < count; i++)
        //{
        //    enemies[i].Tick(deltaTime, playerPosition);
        //}

        activeEnemyCount = count;
    }

    /// <summary>
    /// Register an enemy with the manager. O(1) operation.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RegisterEnemy(EnemyBase enemy)
    {
        // Grow array if needed
        if (enemyCount >= capacity)
        {
            capacity *= 2;
            System.Array.Resize(ref enemies, capacity);
        }

        enemy.Index = enemyCount;
        enemies[enemyCount] = enemy;
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

        // Swap with last element (if not already last)
        if (index < lastIndex)
        {
            EnemyBase lastEnemy = enemies[lastIndex];
            enemies[index] = lastEnemy;
            lastEnemy.Index = index;
        }

        enemies[lastIndex] = null;
        enemy.Index = -1;
        enemyCount--;
    }

    /// <summary>
    /// Get current player position (cached, updated once per frame)
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3 GetPlayerPosition() => playerPosition;

    /// <summary>
    /// Set the player transform reference
    /// </summary>
    public void SetPlayer(Transform player)
    {
        playerTransform = player;
    }

    /// <summary>
    /// Get all active enemies (for special abilities, etc.)
    /// </summary>
    public void GetActiveEnemies(List<EnemyBase> result)
    {
        result.Clear();
        if (result.Capacity < enemyCount)
            result.Capacity = enemyCount;
            
        for (int i = 0; i < enemyCount; i++)
        {
            EnemyBase enemy = enemies[i];
            if (!enemy.IsDead || enemy.IsDissolving)
                result.Add(enemy);
        }
    }

    /// <summary>
    /// Find enemies within radius (squared distance for performance)
    /// </summary>
    public void GetEnemiesInRadius(Vector3 center, float radius, List<EnemyBase> result)
    {
        result.Clear();
        float radiusSqr = radius * radius;

        for (int i = 0; i < enemyCount; i++)
        {
            EnemyBase enemy = enemies[i];
            if (enemy.IsDead && !enemy.IsDissolving) continue;
            
            // Manual squared distance (faster than Vector3.Distance)
            Vector3 pos = enemy.transform.position;
            float dx = pos.x - center.x;
            float dy = pos.y - center.y;
            float dz = pos.z - center.z;
            
            if (dx * dx + dy * dy + dz * dz <= radiusSqr)
            {
                result.Add(enemy);
            }
        }
    }

    /// <summary>
    /// Get enemy count
    /// </summary>
    public int EnemyCount => enemyCount;
}
