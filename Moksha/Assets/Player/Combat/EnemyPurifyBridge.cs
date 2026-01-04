public class EnemyPurifyBridge : Purifiable
{
    EnemyBase enemy;

    void Awake()
    {
        enemy = GetComponent<EnemyBase>();
    }

    public override void Purify(float amount)
    {
        if (enemy == null) return;
        if (enemy.IsDead) return;

        enemy.TakeDamage(amount);
    }
}
