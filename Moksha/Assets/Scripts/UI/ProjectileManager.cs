using System.Collections.Generic;
using UnityEngine;

public class ProjectileManager : MonoBehaviour
{
    public static ProjectileManager Instance;

    [Header("Pool Settings")]
    [SerializeField] private GameObject defaultProjectileVisual;
    [SerializeField] private int initialPoolSize = 200;
    [SerializeField] private LayerMask collisionLayers;

    private class ActiveProjectile
    {
        public bool IsActive;
        public Transform VisualTransform;
        public Vector3 Position;
        public Vector3 Direction;
        public float Speed;
        public float Damage;

        // CHANGED: We now track time instead of distance
        public float Lifetime;
        public float Age;

        public GameObject HitEffectPrefab;
        public AudioClip HitSound;
    }

    private List<ActiveProjectile> activeProjectiles;
    private Stack<ActiveProjectile> projectilePool;
    private Stack<Transform> visualPool;

    private void Awake()
    {
        Instance = this;
        activeProjectiles = new List<ActiveProjectile>(initialPoolSize);
        projectilePool = new Stack<ActiveProjectile>(initialPoolSize);
        visualPool = new Stack<Transform>(initialPoolSize);

        for (int i = 0; i < initialPoolSize; i++) CreateNewPoolObject();
    }

    private void CreateNewPoolObject()
    {
        GameObject go = Instantiate(defaultProjectileVisual, transform);
        go.SetActive(false);
        visualPool.Push(go.transform);
        projectilePool.Push(new ActiveProjectile());
    }

    // UPDATED: Now accepts 'lifetime' from EnemyStats
    public void FireProjectile(Vector3 startPos, Vector3 direction, float speed, float damage, float lifetime, GameObject hitEffect = null, AudioClip hitSound = null)
    {
        if (projectilePool.Count == 0) CreateNewPoolObject();

        ActiveProjectile p = projectilePool.Pop();
        Transform t = visualPool.Pop();

        p.IsActive = true;
        p.Position = startPos;
        p.Direction = direction;
        p.Speed = speed;
        p.Damage = damage;

        // Set Lifetime
        p.Lifetime = lifetime;
        p.Age = 0f;

        p.HitEffectPrefab = hitEffect;
        p.HitSound = hitSound;
        p.VisualTransform = t;

        t.position = startPos;
        t.rotation = Quaternion.LookRotation(direction);
        t.gameObject.SetActive(true);

        activeProjectiles.Add(p);
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        // Loop backwards to remove safely
        for (int i = activeProjectiles.Count - 1; i >= 0; i--)
        {
            ActiveProjectile p = activeProjectiles[i];

            // 1. Move
            float moveDist = p.Speed * dt;
            Vector3 nextPos = p.Position + (p.Direction * moveDist);

            // 2. Raycast Collision
            if (Physics.Raycast(p.Position, p.Direction, out RaycastHit hit, moveDist, collisionLayers))
            {
                HandleHit(p, hit);
                ReturnToPool(i);
                continue;
            }

            // 3. Update Visuals
            p.Position = nextPos;
            p.VisualTransform.position = nextPos;

            // 4. Update Age (Duration Check)
            p.Age += dt;
            if (p.Age >= p.Lifetime)
            {
                ReturnToPool(i);
            }
        }
    }

    private void HandleHit(ActiveProjectile p, RaycastHit hit)
    {
        IDamageable target = hit.collider.GetComponent<IDamageable>();
        if (target != null)
        {
            target.TakeDamage(p.Damage);
        }

        if (p.HitEffectPrefab != null)
        {
            Instantiate(p.HitEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
        }
    }

    private void ReturnToPool(int index)
    {
        ActiveProjectile p = activeProjectiles[index];
        p.VisualTransform.gameObject.SetActive(false);
        visualPool.Push(p.VisualTransform);

        p.IsActive = false;
        p.VisualTransform = null;
        projectilePool.Push(p);

        int lastIndex = activeProjectiles.Count - 1;
        activeProjectiles[index] = activeProjectiles[lastIndex];
        activeProjectiles.RemoveAt(lastIndex);
    }
}