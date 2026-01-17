using UnityEngine;
using System.Runtime.CompilerServices;

/// <summary>
/// Player projectile with optimized collision handling.
/// OPTIMIZED: Removed Debug.Log calls, single GetComponentInParent call, cached transform.
/// NOTE: For full optimization, integrate with object pooling in WeaponRuntime.
/// </summary>
public class SimpleProjectile : MonoBehaviour
{
    Vector3 dir;
    float speed;
    float lifetime;
    float damage;
    int pierce;
    LayerMask hitMask;

    float aliveTime;
    Transform cachedTransform;

    void Awake()
    {
        cachedTransform = transform;
    }

    /// <summary>
    /// Called immediately after spawn by WeaponRuntime
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Init(
        Vector3 direction,
        float damage,
        float speed,
        int pierce,
        float lifetime,
        LayerMask hitMask
    )
    {
        this.dir = direction.normalized;
        this.damage = damage;
        this.speed = speed;
        this.pierce = pierce;
        this.lifetime = lifetime;
        this.hitMask = hitMask;

        aliveTime = 0f;
    }

    void Update()
    {
        cachedTransform.position += dir * speed * Time.deltaTime;

        aliveTime += Time.deltaTime;
        if (aliveTime >= lifetime)
        {
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Layer filtering first (fast rejection)
        if ((hitMask.value & (1 << other.gameObject.layer)) == 0)
            return;

        // Single GetComponentInParent call (removed duplicate)
        Purifiable target = other.GetComponentInParent<Purifiable>();
        if (target == null)
            return;

        target.Purify(damage);

        if (pierce > 0)
        {
            pierce--;
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
