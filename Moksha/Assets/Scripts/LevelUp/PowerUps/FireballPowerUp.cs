using UnityEngine;

/// <summary>
/// Fireball Power-Up - Creates orbiting fireballs around the player that damage enemies on contact.
/// Each stack adds one additional fireball to the orbit.
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

    public override void Apply(GameObject player)
    {
        // Get or add the fireball orbit ability component
        FireballOrbitAbility ability = player.GetComponent<FireballOrbitAbility>();
        
        if (ability == null)
        {
            ability = player.AddComponent<FireballOrbitAbility>();
            ability.Initialize(this);
        }
        else
        {
            ability.AddStack();
        }
        
        Debug.Log($"[PowerUp] Fireball applied! Stacks: {ability.CurrentStacks}");
    }

    public override void Remove(GameObject player)
    {
        FireballOrbitAbility ability = player.GetComponent<FireballOrbitAbility>();
        if (ability != null)
        {
            Object.Destroy(ability);
        }
    }

    /// <summary>
    /// Calculate damage for given stack count
    /// </summary>
    public float GetDamage(int stacks)
    {
        return baseDamage + damagePerStack * (stacks - 1);
    }

    /// <summary>
    /// Calculate orbit speed for given stack count
    /// </summary>
    public float GetOrbitSpeed(int stacks)
    {
        return orbitSpeed + orbitSpeedPerStack * (stacks - 1);
    }

    /// <summary>
    /// Get the number of fireballs (equals stack count)
    /// </summary>
    public int GetFireballCount(int stacks)
    {
        return stacks;
    }
}
