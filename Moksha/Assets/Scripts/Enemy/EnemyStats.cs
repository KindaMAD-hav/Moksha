using UnityEngine;

/// <summary>
/// ScriptableObject containing all stats for an enemy type.
/// Create different assets for different enemy variants.
/// </summary>
[CreateAssetMenu(fileName = "NewEnemyStats", menuName = "Enemies/Enemy Stats")]
public class EnemyStats : ScriptableObject
{
    [Header("Identity")]
    public string enemyName = "Enemy";
    public Sprite icon;

    [Header("Health")]
    public float maxHealth = 100f;

    [Header("Movement")]
    public float moveSpeed = 3f;
    public float rotationSpeed = 360f;
    [Tooltip("How close before stopping to attack")]
    public float stoppingDistance = 1.5f;

    [Header("Combat")]
    public float damage = 10f;
    public float attackCooldown = 1f;
    [Tooltip("Range at which enemy can attack")]
    public float attackRange = 2f;

    [Header("Rewards")]
    public int xpReward = 10;

    [Header("Effects")]
    public GameObject hitEffect;
    public GameObject deathEffect;

    [Header("Audio")]
    public AudioClip hitSound;
    public AudioClip deathSound;
    public AudioClip attackSound;

    [Header("Spawning")]
    [Tooltip("Base weight for spawn probability (higher = more common)")]
    public float spawnWeight = 10f;
    [Tooltip("Minimum game time before this enemy can spawn")]
    public float minSpawnTime = 0f;
}
