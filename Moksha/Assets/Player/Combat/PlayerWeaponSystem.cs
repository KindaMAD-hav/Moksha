using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Minimal weapon manager for MOKSHA.
/// - Ticks equipped weapons
/// - Provides simple "aim + soft auto-aim" target selection
/// - Spawns projectile prefabs (no pooling yet)
/// </summary>
public class PlayerWeaponSystem : MonoBehaviour
{
    [Header("Wiring")]
    [Tooltip("Usually PlayerController.aimPivot. If empty we'll try to auto-find it.")]
    public Transform aimTransform;

    [Tooltip("Projectile spawn point. If empty, defaults to aimTransform.")]
    public Transform firePoint;

    [Header("Targeting")]
    [Tooltip("Layer(s) considered enemies for auto-aim.")]
    public LayerMask enemyMask;

    [Header("Auto Aim")]
    [SerializeField] private bool autoAimEnabled = true;

    [SerializeField] private AttackLayerController attackLayerController;

    [Tooltip("How far the weapon system will look for targets.")]
    public float autoAimRange = 12f;

    [Header("Loadout")]
    public WeaponDefinition[] startingWeapons;

    private PlayerController playerController;

    readonly List<WeaponRuntime> weapons = new();
    private Transform currentAutoAimTarget;

    void Awake()
    {
        playerController = GetComponent<PlayerController>();

        if (!aimTransform)
        {
            var pc = GetComponent<PlayerController>();
            if (pc != null && pc.aimPivot != null) aimTransform = pc.aimPivot;
        }

        if (!aimTransform) aimTransform = transform;
        if (!firePoint) firePoint = aimTransform;

        if (startingWeapons != null)
        {
            foreach (var def in startingWeapons)
            {
                if (def != null) weapons.Add(new WeaponRuntime(def));
            }
        }
    }

    public void NotifyWeaponFired(float animSpeed)
    {
        if (attackLayerController != null)
            attackLayerController.PlayRandomAttack(animSpeed);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            autoAimEnabled = !autoAimEnabled;
            Debug.Log("Auto Aim: " + (autoAimEnabled ? "ON" : "OFF"));
        }
    }


    // LateUpdate so we read the final aimTransform rotation after PlayerController.Update.
    void LateUpdate()
    {
        // AUTO AIM ROTATION (only if player is NOT manually aiming)
        if (autoAimEnabled && playerController != null && !playerController.HasManualAimInput)
        {
            if (TryAcquireTarget(out Transform target))
            {
                Vector3 dir = target.position - transform.position;
                playerController.RotateTowardsAutoAim(dir);
            }
        }

        float dt = Time.deltaTime;
        for (int i = 0; i < weapons.Count; i++)
            weapons[i].Tick(this, dt);
    }




    public Vector3 GetFlatAimDirection()
    {
        Vector3 dir = aimTransform.forward;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) dir = transform.forward;
        return dir.normalized;
    }

    /// <summary>
    /// Finds a "best" target: biased toward your aim direction, then distance.
    /// </summary>
    public bool TryAcquireTarget(out Transform target)
    {
        target = null;

        if (!autoAimEnabled)
            return false;

        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            autoAimRange,
            enemyMask,
            QueryTriggerInteraction.Ignore
        );

        if (hits == null || hits.Length == 0)
        {
            currentAutoAimTarget = null;
            return false;
        }

        Transform closest = null;
        float closestSqrDist = float.PositiveInfinity;

        Vector3 origin = transform.position;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider col = hits[i];
            if (col == null) continue;

            Vector3 p = col.bounds.center;
            Vector3 diff = p - origin;
            diff.y = 0f;

            float sqrDist = diff.sqrMagnitude;
            if (sqrDist < closestSqrDist)
            {
                closestSqrDist = sqrDist;
                closest = col.transform;
            }
        }

        currentAutoAimTarget = closest;
        target = closest;
        return closest != null;
    }


    // --------- Runtime hooks for blessings / progression ---------

    public void AddWeapon(WeaponDefinition def)
    {
        if (def == null) return;
        weapons.Add(new WeaponRuntime(def));
    }

    public IReadOnlyList<WeaponRuntime> Weapons => weapons;

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, autoAimRange);
    }
}
