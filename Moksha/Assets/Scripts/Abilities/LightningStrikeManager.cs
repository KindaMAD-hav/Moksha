using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton manager for lightning strike VFX pooling.
/// Handles spawning and recycling of lightning effects.
/// </summary>
public class LightningStrikeManager : MonoBehaviour
{
    public static LightningStrikeManager Instance { get; private set; }

    [Header("Pool Settings")]
    [Tooltip("The lightning VFX prefab to pool")]
    [SerializeField] private LightningStrikeVFX lightningPrefab;
    
    [Tooltip("Initial pool size")]
    [SerializeField] private int initialPoolSize = 10;
    
    [Tooltip("Maximum pool size (prevents runaway spawning)")]
    [SerializeField] private int maxPoolSize = 50;

    [Header("Debug")]
    [SerializeField] private int activeCount;
    [SerializeField] private int pooledCount;

    // Object pool
    private Queue<LightningStrikeVFX> pool;
    private List<LightningStrikeVFX> activeList;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        pool = new Queue<LightningStrikeVFX>(initialPoolSize);
        activeList = new List<LightningStrikeVFX>(initialPoolSize);

        // Pre-warm pool
        if (lightningPrefab != null)
        {
            for (int i = 0; i < initialPoolSize; i++)
            {
                CreateNewInstance();
            }
        }
    }

    private LightningStrikeVFX CreateNewInstance()
    {
        if (lightningPrefab == null)
        {
            Debug.LogError("[LightningStrikeManager] No prefab assigned!");
            return null;
        }

        GameObject obj = Instantiate(lightningPrefab.gameObject, transform);
        LightningStrikeVFX vfx = obj.GetComponent<LightningStrikeVFX>();
        obj.SetActive(false);
        pool.Enqueue(vfx);
        
        return vfx;
    }

    /// <summary>
    /// Spawn a lightning strike at the target position
    /// </summary>
    public LightningStrikeVFX SpawnLightning(Vector3 targetPosition)
    {
        LightningStrikeVFX vfx = GetFromPool();
        
        if (vfx != null)
        {
            vfx.Activate(targetPosition);
            activeList.Add(vfx);
            UpdateDebugCounts();
        }
        
        return vfx;
    }

    /// <summary>
    /// Return a lightning VFX to the pool
    /// </summary>
    public void ReturnToPool(LightningStrikeVFX vfx)
    {
        if (vfx == null) return;
        
        activeList.Remove(vfx);
        pool.Enqueue(vfx);
        UpdateDebugCounts();
    }

    private LightningStrikeVFX GetFromPool()
    {
        if (pool.Count > 0)
        {
            return pool.Dequeue();
        }
        
        // Create new instance if under max
        int totalCount = pool.Count + activeList.Count;
        if (totalCount < maxPoolSize)
        {
            LightningStrikeVFX newVfx = CreateNewInstance();
            if (newVfx != null)
            {
                return pool.Dequeue();
            }
        }
        
        // Pool exhausted - reuse oldest active (steal)
        if (activeList.Count > 0)
        {
            LightningStrikeVFX oldest = activeList[0];
            activeList.RemoveAt(0);
            oldest.Deactivate();
            return oldest;
        }
        
        return null;
    }

    private void UpdateDebugCounts()
    {
        activeCount = activeList.Count;
        pooledCount = pool.Count;
    }

    /// <summary>
    /// Set the lightning prefab at runtime (if not set in inspector)
    /// </summary>
    public void SetPrefab(LightningStrikeVFX prefab)
    {
        lightningPrefab = prefab;
        
        // Pre-warm if pool is empty
        if (pool.Count == 0 && lightningPrefab != null)
        {
            for (int i = 0; i < initialPoolSize; i++)
            {
                CreateNewInstance();
            }
        }
    }

    /// <summary>
    /// Clear all active lightning effects
    /// </summary>
    public void ClearAll()
    {
        for (int i = activeList.Count - 1; i >= 0; i--)
        {
            if (activeList[i] != null)
            {
                activeList[i].Deactivate();
            }
        }
        activeList.Clear();
        UpdateDebugCounts();
    }

#if UNITY_EDITOR
    [ContextMenu("Spawn Test Lightning")]
    public void SpawnTestLightning()
    {
        SpawnLightning(transform.position + Vector3.forward * 5f);
    }
#endif
}
