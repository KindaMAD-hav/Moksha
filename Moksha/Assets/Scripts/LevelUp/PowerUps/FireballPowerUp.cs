using UnityEngine;

/// <summary>
/// Fireball Power-Up - Creates orbiting fireballs around the player.
/// </summary>
[CreateAssetMenu(fileName = "FireballPowerUp", menuName = "PowerUps/Fireball")]
public class FireballPowerUp : PowerUp
{
    [Header("Fireball Prefab")]
    [Tooltip("The fireball prefab to spawn (should have a collider set as trigger)")]
    public GameObject fireballPrefab;

    [Header("Orbit Settings")]
    [Tooltip("Distance from the player center to orbit")]
    public float orbitRadius = 2f;

    [Tooltip("Rotation speed in degrees per second")]
    public float orbitSpeed = 180f;

    [Tooltip("Orbit speed increase per stack (degrees/sec)")]
    public float orbitSpeedPerStack = 15f;

    [Header("Damage Settings")]
    [Tooltip("Base damage per fireball hit")]
    public float baseDamage = 15f;

    [Tooltip("Damage increase per stack")]
    public float damagePerStack = 5f;

    [Tooltip("Cooldown between hits on the same enemy (seconds)")]
    public float hitCooldown = 0.5f;

    [Header("Visual Settings")]
    [Tooltip("Scale of each fireball")]
    public float fireballScale = 1f;

    [Tooltip("Height offset from player pivot")]
    public float heightOffset = 1f;

    [Tooltip("Base rotation to apply to the prefab (X, Y, Z in degrees).")]
    public Vector3 modelRotation;

    [Tooltip("Continuous rotation speed around the local Z axis (degrees/sec). Useful for spinning projectiles.")]
    public float selfRotationSpeed = 360f;

    [Tooltip("If true, the fireball will rotate to face the direction it is traveling.")]
    public bool faceMovementDirection = true;

    public override void Apply(GameObject player)
    {
        Debug.Log($"[FireballPowerUp] Apply called on player: {player.name}");

        if (fireballPrefab == null)
        {
            Debug.LogError("[FireballPowerUp] FIREBALL PREFAB IS NULL! Please assign a prefab in the Inspector.");
            return;
        }

        FireballOrbitAbility ability = player.GetComponent<FireballOrbitAbility>();

        if (ability == null)
        {
            Debug.Log("[FireballPowerUp] Adding FireballOrbitAbility component to player");
            ability = player.AddComponent<FireballOrbitAbility>();
            ability.Initialize(this);
        }
        else
        {
            Debug.Log("[FireballPowerUp] FireballOrbitAbility already exists, adding stack");
            ability.AddStack(this);
        }

        Debug.Log($"[PowerUp] Fireball applied! Stacks: {ability.CurrentStacks}");
    }

    public override void Remove(GameObject player)
    {
        FireballOrbitAbility ability = player.GetComponent<FireballOrbitAbility>();
        if (ability != null)
        {
            Destroy(ability);
        }
    }

    public float GetDamage(int stacks)
    {
        return baseDamage + damagePerStack * (stacks - 1);
    }

    public float GetOrbitSpeed(int stacks)
    {
        return orbitSpeed + orbitSpeedPerStack * (stacks - 1);
    }

    public int GetFireballCount(int stacks)
    {
        return stacks;
    }
}