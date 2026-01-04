using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Centralized enemy manager that handles all enemy updates in batches.
/// This dramatically reduces overhead from individual Update() calls.
/// </summary>
public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private int enemiesPerFrame = 50; // Process enemies in batches
    [SerializeField] private bool useJobSystem = false; // Future: Unity Jobs

    [Header("Debug")]
    [SerializeField] private int activeEnemyCount;
    [SerializeField] private int processedThisFrame;

    // Use array for cache-friendly iteration
    private EnemyBase[] enemies;
    private int enemyCount;
    private int capacity;
    private int currentIndex;

    // Cached values
    private Vector3 playerPosition;
    private float deltaTime;

    // Free list for recycling indices
    private Stack<int> freeIndices;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Pre-allocate arrays
        capacity = 500;
        enemies = new EnemyBase[capacity];
        freeIndices = new Stack<int>(capacity);
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

        deltaTime = Time.deltaTime;
        playerPosition = playerTransform.position;
        processedThisFrame = 0;

        // Process all enemies every frame for consistent behavior
        // For massive counts, you could process in batches across frames
        for (int i = 0; i < capacity && processedThisFrame < enemyCount; i++)
        {
            EnemyBase enemy = enemies[i];
            if (enemy != null && !enemy.IsDead)
            {
                enemy.Tick(deltaTime, playerPosition);
                processedThisFrame++;
            }
        }

        activeEnemyCount = enemyCount;
    }

    /// <summary>
    /// Register an enemy with the manager
    /// </summary>
    public void RegisterEnemy(EnemyBase enemy)
    {
        int index;
        
        if (freeIndices.Count > 0)
        {
            index = freeIndices.Pop();
        }
        else
        {
            index = enemyCount;
            
            // Grow array if needed
            if (index >= capacity)
            {
                capacity *= 2;
                System.Array.Resize(ref enemies, capacity);
            }
        }

        enemies[index] = enemy;
        enemy.Index = index;
        enemyCount++;
    }

    /// <summary>
    /// Unregister an enemy from the manager
    /// </summary>
    public void UnregisterEnemy(EnemyBase enemy)
    {
        int index = enemy.Index;
        if (index >= 0 && index < capacity && enemies[index] == enemy)
        {
            enemies[index] = null;
            freeIndices.Push(index);
            enemyCount--;
        }
    }

    /// <summary>
    /// Get current player position (cached, updated once per frame)
    /// </summary>
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
        for (int i = 0; i < capacity; i++)
        {
            if (enemies[i] != null && !enemies[i].IsDead)
                result.Add(enemies[i]);
        }
    }

    /// <summary>
    /// Find enemies within radius (squared distance for performance)
    /// </summary>
    public void GetEnemiesInRadius(Vector3 center, float radius, List<EnemyBase> result)
    {
        result.Clear();
        float radiusSqr = radius * radius;
        
        for (int i = 0; i < capacity; i++)
        {
            EnemyBase enemy = enemies[i];
            if (enemy != null && !enemy.IsDead)
            {
                Vector3 diff = enemy.transform.position - center;
                if (diff.x * diff.x + diff.y * diff.y + diff.z * diff.z <= radiusSqr)
                {
                    result.Add(enemy);
                }
            }
        }
    }
}
