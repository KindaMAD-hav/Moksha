using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// HIGH-PERFORMANCE PROJECTILE SYSTEM
/// - 1 Update loop for 1000s of bullets.
/// - Uses Raycasts instead of Colliders (Physics optimization).
/// - Pools visual objects to avoid Instantiate/Destroy lag.
/// </summary>
public class ProjectileManager : MonoBehaviour
{
    public static ProjectileManager Instance;

    [Header("Pool Settings")]
    [SerializeField] private GameObject defaultProjectileVisual; // Drag your simple sphere/bullet prefab here
    [SerializeField] private int initialPoolSize = 200;
    [SerializeField] private LayerMask collisionLayers; // Set to 'Player' and 'Obstacles'

    // --- DATA STRUCTURES ---
    // We use a class for the active bullet logic so we can just modify values without struct copying
    private class ActiveProjectile
    {
        public bool IsActive;
        public Transform VisualTransform;
        public Vector3 Position;
        public Vector3 Direction;
        public float Speed;
        public float Damage;
        public float MaxDistanceSqr;
        public float DistanceTraveledSqr;
        public GameObject HitEffectPrefab; // Passed from enemy stats
        public AudioClip HitSound;         // Passed from enemy stats
    }

    private List<ActiveProjectile> activeProjectiles;
    private Stack<ActiveProjectile> projectilePool; // Pool of logic objects
    private Stack<Transform> visualPool;           // Pool of gameobjects

    private void Awake()
    {
        Instance = this;
        activeProjectiles = new List<ActiveProjectile>(initialPoolSize);
        projectilePool = new Stack<ActiveProjectile>(initialPoolSize);
        visualPool = new Stack<Transform>(initialPoolSize);

        // Pre-warm the pool
        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateNewPoolObject();
        }
    }

    private void CreateNewPoolObject()
    {
        // 1. Create Visual
        GameObject go = Instantiate(defaultProjectileVisual, transform);
        go.SetActive(false);
        visualPool.Push(go.transform);

        // 2. Create Logic Container
        projectilePool.Push(new ActiveProjectile());
    }

    /// <summary>
    /// Called by RangedEnemy to fire a bullet.
    /// </summary>
    public void FireProjectile(Vector3 startPos, Vector3 direction, float speed, float damage, float range, GameObject hitEffect = null, AudioClip hitSound = null)
    {
        if (projectilePool.Count == 0) CreateNewPoolObject(); // Expand pool if needed

        // Get from pool
        ActiveProjectile p = projectilePool.Pop();
        Transform t = visualPool.Pop();

        // Setup Logic
        p.IsActive = true;
        p.Position = startPos;
        p.Direction = direction;
        p.Speed = speed;
        p.Damage = damage;
        p.MaxDistanceSqr = range * range;
        p.DistanceTraveledSqr = 0f;
        p.HitEffectPrefab = hitEffect;
        p.HitSound = hitSound;
        p.VisualTransform = t;

        // Setup Visual
        t.position = startPos;
        t.rotation = Quaternion.LookRotation(direction);
        t.gameObject.SetActive(true);

        activeProjectiles.Add(p);
    }

    private void Update()
    {
        // Iterate BACKWARDS so we can remove items efficiently
        float dt = Time.deltaTime;

        for (int i = activeProjectiles.Count - 1; i >= 0; i--)
        {
            ActiveProjectile p = activeProjectiles[i];

            // 1. Calculate Move
            float moveDist = p.Speed * dt;
            Vector3 nextPos = p.Position + (p.Direction * moveDist);

            // 2. Check Collision (Raycast from old pos to new pos)
            // We use RaycastNonAlloc if we wanted extreme optimization, but simple Raycast is fine for now
            if (Physics.Raycast(p.Position, p.Direction, out RaycastHit hit, moveDist, collisionLayers))
            {
                HandleHit(p, hit);
                ReturnToPool(i);
                continue;
            }

            // 3. Update Position
            p.Position = nextPos;
            p.VisualTransform.position = nextPos; // Only touch Transform once per frame

            // 4. Check Range Limit
            p.DistanceTraveledSqr += moveDist * moveDist;
            if (p.DistanceTraveledSqr >= p.MaxDistanceSqr)
            {
                ReturnToPool(i);
            }
        }
    }

    private void HandleHit(ActiveProjectile p, RaycastHit hit)
    {
        // Damage Logic
        IDamageable target = hit.collider.GetComponent<IDamageable>();
        if (target != null)
        {
            target.TakeDamage(p.Damage);
        }

        // VFX (Optional: You should pool these too ideally, but Instantiate is okay for rare impacts)
        if (p.HitEffectPrefab != null)
        {
            Instantiate(p.HitEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
        }

        // SFX
        // if (p.HitSound != null) AudioSource.PlayClipAtPoint(p.HitSound, hit.point);
    }

    private void ReturnToPool(int index)
    {
        ActiveProjectile p = activeProjectiles[index];

        // Reset Visual
        p.VisualTransform.gameObject.SetActive(false);
        visualPool.Push(p.VisualTransform);

        // Reset Logic
        p.IsActive = false;
        p.VisualTransform = null; // Break reference
        projectilePool.Push(p);

        // Remove from active list (Swap with last element for O(1) removal)
        int lastIndex = activeProjectiles.Count - 1;
        activeProjectiles[index] = activeProjectiles[lastIndex];
        activeProjectiles.RemoveAt(lastIndex);
    }
}