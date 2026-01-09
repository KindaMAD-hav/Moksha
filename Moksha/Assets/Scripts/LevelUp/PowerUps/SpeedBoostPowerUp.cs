using UnityEngine;

/// <summary>
/// Power-up that increases player movement speed.
/// </summary>
[CreateAssetMenu(fileName = "SpeedBoost", menuName = "Power-Ups/Speed Boost")]
public class SpeedBoostPowerUp : PowerUp
{
    [Header("Speed Boost Settings")]
    public float speedIncrease = 1f;
    public bool isPercentage = false;

    public override void Apply(GameObject player)
    {
        var controller = player.GetComponent<PlayerController>();
        if (controller != null)
        {
            if (isPercentage)
                controller.moveSpeed *= (1 + speedIncrease / 100f);
            else
                controller.moveSpeed += speedIncrease;
            
            Debug.Log($"[PowerUp] Speed Boost applied! New speed: {controller.moveSpeed}");
        }
    }

    public override void Remove(GameObject player)
    {
        var controller = player.GetComponent<PlayerController>();
        if (controller != null)
        {
            if (isPercentage)
                controller.moveSpeed /= (1 + speedIncrease / 100f);
            else
                controller.moveSpeed -= speedIncrease;
        }
    }
}
