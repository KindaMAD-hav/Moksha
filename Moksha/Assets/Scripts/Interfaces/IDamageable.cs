/// <summary>
/// Interface for anything that can take damage.
/// Used by enemies to damage the player, and projectiles to damage enemies.
/// </summary>
public interface IDamageable
{
    void TakeDamage(float damage);
    float CurrentHealth { get; }
    float MaxHealth { get; }
    bool IsDead { get; }
}
