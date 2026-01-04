using UnityEngine;

/// <summary>
/// Data-only weapon definition.
/// Create via: Assets -> Create -> MOKSHA -> Weapon Definition
/// </summary>
[CreateAssetMenu(menuName = "MOKSHA/Weapon Definition", fileName = "WD_")]
public class WeaponDefinition : ScriptableObject
{
    [Header("Identity")]
    public string weaponName = "Unnamed Shakti";

    [Header("Projectile Prefab")]
    [Tooltip("Prefab should have a SimpleProjectile + trigger Collider + (kinematic) Rigidbody.")]
    public GameObject projectilePrefab;

    [Tooltip("Which layers this projectile can hit (usually: Enemy).")]
    public LayerMask hitMask;

    [Header("Base Stats")]
    [Tooltip("Shots per second.")]
    public float baseFireRate = 3f;

    public float baseDamage = 5f;

    [Tooltip("Units per second.")]
    public float projectileSpeed = 18f;

    [Tooltip("Seconds before despawn.")]
    public float lifeTime = 2.5f;

    [Header("Pattern")]
    [Min(1)]
    public int baseProjectiles = 1;

    [Tooltip("Total arc across all projectiles (degrees). Example: 20 means a 20° fan.")]
    public float spreadDegrees = 0f;

    [Tooltip("How many additional targets the projectile can pass through (0 = no pierce).")]
    public int basePierce = 0;
}
