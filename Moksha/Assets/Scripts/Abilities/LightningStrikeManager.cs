using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Optimized pooling manager.
/// Centralizes Audio to avoid AudioSource overhead on prefabs.
/// </summary>
public class LightningStrikeManager : MonoBehaviour
{
    public static LightningStrikeManager Instance { get; private set; }

    [Header("Pool Settings")]
    [SerializeField] private LightningStrikeVFX lightningPrefab;
    [SerializeField] private int initialPoolSize = 10;
    [SerializeField] private int maxPoolSize = 50;

    [Header("Centralized Audio")]
    [SerializeField] private AudioSource mainAudioSource; // One source to rule them all
    [SerializeField] private AudioClip strikeSound;
    [SerializeField] private float minPitch = 0.9f;
    [SerializeField] private float maxPitch = 1.1f;
    [Tooltip("Limit concurrent sounds to prevent ear destruction")]
    [SerializeField] private float minSoundInterval = 0.05f;

    // Object pool
    private Queue<LightningStrikeVFX> pool;
    private List<LightningStrikeVFX> activeList;
    private float lastSoundTime;

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

        if (mainAudioSource == null) mainAudioSource = GetComponent<AudioSource>();

        if (lightningPrefab != null)
        {
            for (int i = 0; i < initialPoolSize; i++) CreateNewInstance();
        }
    }

    public void PlayStrikeSound()
    {
        if (mainAudioSource == null || strikeSound == null) return;

        // Prevent 10 sounds playing in 1 frame
        if (Time.time - lastSoundTime < minSoundInterval) return;

        mainAudioSource.pitch = Random.Range(minPitch, maxPitch);
        mainAudioSource.PlayOneShot(strikeSound);
        lastSoundTime = Time.time;
    }

    private LightningStrikeVFX CreateNewInstance()
    {
        if (lightningPrefab == null) return null;

        // Instantiate
        LightningStrikeVFX vfx = Instantiate(lightningPrefab, transform);

        // Force initialization / caching
        vfx.gameObject.SetActive(false);

        // Add to pool
        pool.Enqueue(vfx);
        return vfx;
    }

    public LightningStrikeVFX SpawnLightningFollowing(Transform target, Vector3 offset = default)
    {
        LightningStrikeVFX vfx = GetFromPool();
        if (vfx != null)
        {
            vfx.ActivateAndFollow(target, offset);
            activeList.Add(vfx);
        }
        return vfx;
    }

    // Kept for compatibility if needed, but Following is preferred
    public LightningStrikeVFX SpawnLightningAtTarget(Vector3 targetPosition)
    {
        LightningStrikeVFX vfx = GetFromPool();
        if (vfx != null)
        {
            vfx.ActivateAtTarget(targetPosition);
            activeList.Add(vfx);
        }
        return vfx;
    }

    public void ReturnToPool(LightningStrikeVFX vfx)
    {
        if (vfx == null) return;

        // Fast removal: Swap with last element is O(1) in List, but order changes.
        // Since order doesn't matter for VFX, let's just use Remove (O(N)).
        // For 50 items, O(N) is negligible.
        activeList.Remove(vfx);

        pool.Enqueue(vfx);
    }

    private LightningStrikeVFX GetFromPool()
    {
        if (pool.Count > 0) return pool.Dequeue();

        // Expand
        if ((pool.Count + activeList.Count) < maxPoolSize)
        {
            LightningStrikeVFX newVfx = CreateNewInstance();
            if (newVfx != null) return pool.Dequeue();
        }

        // Steal oldest (ActiveList[0] is usually oldest)
        if (activeList.Count > 0)
        {
            LightningStrikeVFX stolen = activeList[0];
            activeList.RemoveAt(0); // O(N) shift, but list is small
            stolen.Deactivate();
            // Deactivate puts it back in pool, so we dequeue again
            if (pool.Count > 0) return pool.Dequeue();
        }

        return null;
    }
}