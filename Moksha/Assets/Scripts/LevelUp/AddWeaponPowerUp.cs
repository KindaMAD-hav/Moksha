using UnityEngine;

[CreateAssetMenu(fileName = "AddWeapon", menuName = "Power-Ups/Add Weapon")]
public class AddWeaponPowerUp : PowerUp
{
    [Header("Weapon to Add")]
    public WeaponDefinition weaponToAdd;

    public override void Apply(GameObject player)
    {
        if (player == null) return;

        var ws = player.GetComponent<PlayerWeaponSystem>();
        if (ws == null)
        {
            Debug.LogWarning("[AddWeaponPowerUp] PlayerWeaponSystem not found on player.");
            return;
        }

        if (weaponToAdd == null)
        {
            Debug.LogWarning("[AddWeaponPowerUp] weaponToAdd is not assigned.");
            return;
        }

        ws.AddWeapon(weaponToAdd);
        Debug.Log($"[AddWeaponPowerUp] Added weapon: {weaponToAdd.weaponName}");
    }
}
