using UnityEngine;

public enum WeaponTargetMode
{
    AllWeapons = 0,
    ByWeaponName = 1,
    ByWeaponDefinition = 2
}

public enum WeaponUpgradeKind
{
    DamagePercent = 0,
    FireRatePercent = 1,
    ProjectileSpeedPercent = 2,
    AddProjectiles = 3,
    AddPierce = 4
}

[CreateAssetMenu(fileName = "WeaponModifier", menuName = "Power-Ups/Weapon Modifier")]
public class WeaponModifierPowerUp : PowerUp
{
    [Header("Target")]
    public WeaponTargetMode targetMode = WeaponTargetMode.AllWeapons;

    [Tooltip("Used only when Target Mode = ByWeaponName. Matches WeaponDefinition.weaponName")]
    public string targetWeaponName;

    [Tooltip("Used only when Target Mode = ByWeaponDefinition.")]
    public WeaponDefinition targetWeapon;

    [Header("Upgrade")]
    public WeaponUpgradeKind upgradeKind = WeaponUpgradeKind.DamagePercent;

    [Tooltip("Used for Percent upgrades. Example: 20 = +20%")]
    public float percentValue = 20f;

    [Tooltip("Used for integer upgrades like +Projectiles, +Pierce")]
    public int intValue = 1;

    public override void Apply(GameObject player)
    {
        if (player == null) return;

        var ws = player.GetComponent<PlayerWeaponSystem>();
        if (ws == null)
        {
            Debug.LogWarning("[WeaponModifierPowerUp] PlayerWeaponSystem not found on player.");
            return;
        }

        var weapons = ws.Weapons;
        if (weapons == null || weapons.Count == 0)
        {
            Debug.LogWarning("[WeaponModifierPowerUp] No weapons equipped.");
            return;
        }

        int appliedCount = 0;

        for (int i = 0; i < weapons.Count; i++)
        {
            var wr = weapons[i];
            if (wr == null || wr.def == null) continue;

            if (!MatchesTarget(wr.def)) continue;

            ApplyToRuntime(wr);
            appliedCount++;
        }

        Debug.Log($"[WeaponModifierPowerUp] Applied {upgradeKind} to {appliedCount} weapon(s).");
    }

    bool MatchesTarget(WeaponDefinition def)
    {
        switch (targetMode)
        {
            case WeaponTargetMode.AllWeapons:
                return true;

            case WeaponTargetMode.ByWeaponName:
                if (string.IsNullOrWhiteSpace(targetWeaponName)) return false;
                return string.Equals(def.weaponName, targetWeaponName, System.StringComparison.OrdinalIgnoreCase);

            case WeaponTargetMode.ByWeaponDefinition:
                return targetWeapon != null && def == targetWeapon;

            default:
                return false;
        }
    }

    void ApplyToRuntime(WeaponRuntime wr)
    {
        float pct = percentValue * 0.01f;

        switch (upgradeKind)
        {
            case WeaponUpgradeKind.DamagePercent:
                wr.AddDamagePercent(pct);
                break;

            case WeaponUpgradeKind.FireRatePercent:
                wr.AddFireRatePercent(pct);
                break;

            case WeaponUpgradeKind.ProjectileSpeedPercent:
                wr.AddSpeedPercent(pct);
                break;

            case WeaponUpgradeKind.AddProjectiles:
                wr.AddProjectiles(intValue);
                break;

            case WeaponUpgradeKind.AddPierce:
                wr.AddPierce(intValue);
                break;
        }
    }
}
