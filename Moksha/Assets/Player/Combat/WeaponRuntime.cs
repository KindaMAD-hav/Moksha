using UnityEngine;

/// <summary>
/// Runtime state + simple upgrade modifiers for a weapon.
/// Not a MonoBehaviour (keeps things lightweight).
/// </summary>
[System.Serializable]
public class WeaponRuntime
{
    public WeaponDefinition def;

    // Minimal "upgrade surface" (easy to iterate, no heavy stat system yet)
    public float damageMult = 1f;
    public float fireRateMult = 1f;
    public float speedMult = 1f;
    public float spreadMult = 1f;
    public int bonusProjectiles = 0;
    public int bonusPierce = 0;
   

    float cooldown;

    public WeaponRuntime(WeaponDefinition def)
    {
        this.def = def;
        // Small desync so multiple weapons don't always fire on the same frame.
        cooldown = Random.Range(0f, 0.12f);
    }

    public void Tick(PlayerWeaponSystem owner, float dt)
    {
        if (def == null) return;
        if (owner == null) return;

        cooldown -= dt;
        if (cooldown > 0f) return;

        Fire(owner);

        float fireRate = Mathf.Max(0.01f, def.baseFireRate * fireRateMult);
        cooldown = 1f / fireRate;
    }

    void Fire(PlayerWeaponSystem owner)
    {
        Transform fp = owner.firePoint != null ? owner.firePoint : owner.transform;
        Vector3 firePos = fp.position;

        // Default: shoot where the player is aiming
        Vector3 dir = owner.GetFlatAimDirection();

        // Fallback: use aimPivot forward if no input this frame
        if (dir.sqrMagnitude < 0.0001f)
        {
            dir = owner.aimTransform.forward;
            dir.y = 0f;
            dir.Normalize();
        }

        // Soft auto-aim: if there's a target, shoot directly at it
        if (owner.TryAcquireTarget(out Transform target) && target != null)
        {
            Vector3 to = target.position - firePos;
            to.y = 0f;
            if (to.sqrMagnitude > 0.001f) dir = to.normalized;
        }

        int projectileCount = Mathf.Max(1, def.baseProjectiles + bonusProjectiles);
        float baseSpread = def.spreadDegrees * spreadMult;

        // Ensure pellets are visible when projectile count > 1
        if (projectileCount > 1 && baseSpread <= 0.001f)
        {
            baseSpread = 6f; // degrees, tweakable
        }

        float totalSpread = baseSpread;

        float step = 0f;
        float start = 0f;

        float dmg = def.baseDamage * damageMult;
        float spd = def.projectileSpeed * speedMult;
        int pierce = def.basePierce + bonusPierce;

        if (projectileCount > 1)
        {
            step = totalSpread / (projectileCount - 1);
            start = -totalSpread * 0.5f;
        }

        for (int i = 0; i < projectileCount; i++)
        {
            float angle = (projectileCount == 1) ? 0f : (start + step * i);
            Vector3 shotDir = Quaternion.AngleAxis(angle, Vector3.up) * dir;
            SpawnProjectile(firePos, shotDir, dmg, spd, pierce);
        }
    }

    void SpawnProjectile(Vector3 pos, Vector3 dir, float damage, float speed, int pierce)
    {
        if (def.projectilePrefab == null) return;

        Quaternion rot = Quaternion.LookRotation(dir, Vector3.up);
        GameObject go = Object.Instantiate(def.projectilePrefab, pos, rot);

        var vis = go.GetComponent<BulletVisual>();
        if (vis != null)
            vis.ApplyProfile(def.visualProfile);

        SimpleProjectile proj = go.GetComponent<SimpleProjectile>();
        if (proj != null)
        {
            proj.Init(dir, damage, speed, pierce, def.lifeTime, def.hitMask);
        }
    }

    // --------- Minimal upgrade hooks (used by blessings later) ---------

    /// <param name="pct">Example: 0.2f = +20%</param>
    public void AddDamagePercent(float pct) => damageMult *= (1f + pct);

    /// <param name="pct">Example: 0.15f = +15%</param>
    public void AddFireRatePercent(float pct) => fireRateMult *= (1f + pct);

    public void AddProjectiles(int amount) => bonusProjectiles += amount;

    public void AddPierce(int amount) => bonusPierce += amount;

    /// <param name="pct">Example: 0.25f = +25%</param>
    public void AddSpeedPercent(float pct) => speedMult *= (1f + pct);

    /// <param name="pct">Example: 0.25f = +25% spread</param>
    public void AddSpreadPercent(float pct)
    {
        spreadMult *= (1f + pct);
    }
}
