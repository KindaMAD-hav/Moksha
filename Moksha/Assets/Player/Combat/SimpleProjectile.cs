using UnityEngine;

public class SimpleProjectile : MonoBehaviour
{
    Vector3 dir;
    float speed;
    float lifetime;
    float damage;
    int pierce;
    LayerMask hitMask;

    float aliveTime;

    /// <summary>
    /// Called immediately after spawn by WeaponRuntime
    /// </summary>
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
        transform.position += dir * speed * Time.deltaTime;

        aliveTime += Time.deltaTime;
        if (aliveTime >= lifetime)
        {
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("Projectile trigger hit: " + other.name);
        Debug.Log("Collider layer: " + LayerMask.LayerToName(other.gameObject.layer));
        Debug.Log("HitMask allows it: " + ((hitMask.value & (1 << other.gameObject.layer)) != 0));

        var target1 = other.GetComponentInParent<Purifiable>();
        Debug.Log("Found Purifiable: " + (target1 != null));

        // Layer filtering
        if ((hitMask.value & (1 << other.gameObject.layer)) == 0)
            return;

        // IMPORTANT: allow colliders on child objects
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
