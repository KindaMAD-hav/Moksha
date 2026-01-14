using UnityEngine;

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
    public float stoppingDistance = 15f;

    [Header("Combat (Fire Rate)")]
    public float damage = 10f;
    [Tooltip("Time between shots (Lower = Faster Fire Rate)")]
    public float attackCooldown = 1f;
    [Tooltip("Range at which enemy starts attacking")]
    public float attackRange = 20f;

    // --- NEW SECTION ---
    [Header("Projectile Settings")]
    [Tooltip("How fast the bullet travels")]
    public float projectileSpeed = 15f;
    [Tooltip("How long the bullet exists before destroying (if it hits nothing)")]
    public float projectileLifetime = 5f;

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
    public float spawnWeight = 10f;
    public float minSpawnTime = 0f;
}