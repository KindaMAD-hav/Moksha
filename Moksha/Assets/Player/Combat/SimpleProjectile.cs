using UnityEngine;

/// <summary>
/// Simple forward-moving projectile.
/// - Moves on XZ plane
/// - Trigger hits anything in hitMask
/// - Calls IPurifiable.Purify(amount)
/// </summary>
public class SimpleProjectile : MonoBehaviour
{
    Vector3 dir;
    float speed;
    float amount;
    int pierceLeft;
    float timeLeft;
    LayerMask hitMask;

    public void Init(Vector3 direction, float purificationAmount, float speed, int pierce, float lifeTime, LayerMask hitMask)
    {
        dir = direction;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) dir = transform.forward;
        dir.Normalize();

        this.speed = speed;
        this.amount = purificationAmount;
        pierceLeft = pierce;
        timeLeft = lifeTime;
        this.hitMask = hitMask;

        transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
    }

    void Update()
    {
        float dt = Time.deltaTime;

        transform.position += dir * speed * dt;

        timeLeft -= dt;
        if (timeLeft <= 0f)
            Destroy(gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        // Only interact with allowed layers
        if ((hitMask.value & (1 << other.gameObject.layer)) == 0)
            return;

        // Try to find something purifiable on this object or its parents.
        IPurifiable purifiable = other.GetComponentInParent<IPurifiable>();
        if (purifiable != null)
        {
            purifiable.Purify(amount);
        }

        if (pierceLeft > 0)
        {
            pierceLeft--;
            return;
        }

        Destroy(gameObject);
    }
}
