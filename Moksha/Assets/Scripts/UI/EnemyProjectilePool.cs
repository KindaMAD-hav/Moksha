using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Object pool for enemy projectiles to avoid instantiation overhead.
/// Singleton pattern for easy global access.
/// </summary>
public class EnemyProjectilePool : MonoBehaviour
{
    public static EnemyProjectilePool Instance { get; private set; }

    [Header("Pool Settings")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private int initialPoolSize = 50;
    [SerializeField] private int maxPoolSize = 200;
    [SerializeField] private bool autoExpand = true;

    private Queue<EnemyProjectile> projectilePool;
    private HashSet<EnemyProjectile> activeProjectiles;
    private Transform poolContainer;

    private void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Create container for organization
        poolContainer = new GameObject("ProjectilePool").transform;
        poolContainer.SetParent(transform);

        // Initialize pool
        projectilePool = new Queue<EnemyProjectile>(initialPoolSize);
        activeProjectiles = new HashSet<EnemyProjectile>();

        // Pre-instantiate projectiles
        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateNewProjectile();
        }
    }

    private EnemyProjectile CreateNewProjectile()
    {
        GameObject obj = Instantiate(projectilePrefab, poolContainer);
        obj.SetActive(false);

        EnemyProjectile projectile = obj.GetComponent<EnemyProjectile>();
        if (projectile == null)
        {
            projectile = obj.AddComponent<EnemyProjectile>();
        }

        projectilePool.Enqueue(projectile);
        return projectile;
    }

    public EnemyProjectile GetProjectile()
    {
        EnemyProjectile projectile;

        // Try to get from pool
        if (projectilePool.Count > 0)
        {
            projectile = projectilePool.Dequeue();
        }
        else if (autoExpand && activeProjectiles.Count < maxPoolSize)
        {
            // Pool empty but can expand
            projectile = CreateNewProjectile();
            projectilePool.Dequeue(); // Remove it immediately since we're using it
        }
        else
        {
            // Pool exhausted and can't expand
            Debug.LogWarning("Projectile pool exhausted! Consider increasing max pool size.");
            return null;
        }

        activeProjectiles.Add(projectile);
        projectile.gameObject.SetActive(true);
        return projectile;
    }

    public void ReturnProjectile(EnemyProjectile projectile)
    {
        if (projectile == null) return;

        if (activeProjectiles.Remove(projectile))
        {
            projectile.gameObject.SetActive(false);
            projectile.transform.SetParent(poolContainer);
            projectilePool.Enqueue(projectile);
        }
    }

    public void ClearAllProjectiles()
    {
        // Return all active projectiles to pool
        foreach (var projectile in activeProjectiles)
        {
            if (projectile != null)
            {
                projectile.gameObject.SetActive(false);
                projectile.transform.SetParent(poolContainer);
                projectilePool.Enqueue(projectile);
            }
        }
        activeProjectiles.Clear();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

#if UNITY_EDITOR
    // DISABLED: OnGUI is extremely expensive and causes major editor slowdown
    // Uncomment only for debugging pool issues
    /*
    [SerializeField] private bool showDebugGUI = false;
    
    private void OnGUI()
    {
        if (!showDebugGUI || !Application.isPlaying) return;

        GUILayout.BeginArea(new Rect(10, 100, 250, 100));
        GUILayout.Label($"Projectile Pool Stats:");
        GUILayout.Label($"Available: {projectilePool.Count}");
        GUILayout.Label($"Active: {activeProjectiles.Count}");
        GUILayout.Label($"Total: {projectilePool.Count + activeProjectiles.Count}");
        GUILayout.EndArea();
    }
    */
#endif
}