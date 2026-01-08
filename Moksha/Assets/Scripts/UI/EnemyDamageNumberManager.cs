using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class EnemyDamageNumberManager : MonoBehaviour
{
    public static EnemyDamageNumberManager Instance { get; private set; }

    [Header("Prefab")]
    [SerializeField] private DamageNumber damageNumberPrefab;

    [Header("Spawn Placement")]
    [SerializeField] private float heightOffset = 1.3f;     // above enemy
    [SerializeField] private float radialOffset = 0.45f;     // around enemy
    [SerializeField] private float radialJitter = 0.15f;     // tiny randomness

    [Header("Glow")]
    [SerializeField] private bool useAttackerColorGlow = true;

    // Per-enemy counter so repeated hits "orbit" around them (prevents overlap)
    private readonly Dictionary<int, int> _spawnSerialByEnemy = new Dictionary<int, int>(256);

    private const float TWO_PI = 6.28318530718f;
    private const float GOLDEN_ANGLE = 2.399963229728653f; // ~137.5 degrees in radians

    private readonly Dictionary<int, DamageNumber> activeNumbers =
    new Dictionary<int, DamageNumber>(256);


    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>
    /// Call this when a bullet deals damage to an enemy.
    /// </summary>
    public void ShowDamage(Transform enemy, int damage, Color attackerColor)
    {
        if (damageNumberPrefab == null || enemy == null) return;

        int id = enemy.GetInstanceID();

        DamageNumber dmg;

        // ✅ STACK if already exists
        if (activeNumbers.TryGetValue(id, out dmg) && dmg != null)
        {
            dmg.AddValue(damage);

            if (useAttackerColorGlow)
                dmg.SetAttackerColor(attackerColor);

            return;
        }

        // ❌ otherwise spawn new
        Vector3 pos = enemy.position + GetNonOverlappingOffset(enemy);
        dmg = Instantiate(damageNumberPrefab, pos, Quaternion.identity);

        dmg.useAttackerColorGlow = useAttackerColorGlow;
        dmg.SetValue(damage);

        if (useAttackerColorGlow)
            dmg.SetAttackerColor(attackerColor);

        activeNumbers[id] = dmg;

        // cleanup when destroyed
        StartCoroutine(RemoveWhenDestroyed(enemy, dmg));
    }

    private System.Collections.IEnumerator RemoveWhenDestroyed(
    Transform enemy,
    DamageNumber dmg)
    {
        int id = enemy.GetInstanceID();

        while (dmg != null)
            yield return null;

        activeNumbers.Remove(id);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Vector3 GetNonOverlappingOffset(Transform enemy)
    {
        int id = enemy.GetInstanceID();

        int n;
        if (_spawnSerialByEnemy.TryGetValue(id, out n))
            _spawnSerialByEnemy[id] = n + 1;
        else
        {
            n = 0;
            _spawnSerialByEnemy.Add(id, 1);
        }

        // Spread offsets around the enemy using golden-angle steps (nice distribution)
        float angle = (n * GOLDEN_ANGLE) % TWO_PI;
        float r = radialOffset + Random.value * radialJitter;

        float ox = Mathf.Cos(angle) * r;
        float oz = Mathf.Sin(angle) * r;

        return new Vector3(ox, heightOffset, oz);
    }
}
