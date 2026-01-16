using UnityEngine;

public class MortarProjectile : MonoBehaviour
{
    [Header("Visuals")]
    [Tooltip("Prefab to spawn on the ground where it will land (Red Circle)")]
    [SerializeField] private GameObject warningIndicatorPrefab;

    [Tooltip("Prefab to spawn when it hits (The Explosion)")]
    [SerializeField] private GameObject explosionPrefab;

    private Vector3 startPos;
    private Vector3 targetPos;
    private float arcHeight;
    private float totalDuration;
    private float timeElapsed;
    private float damage;

    private bool initialized = false;
    private GameObject activeIndicator;

    public void Initialize(Vector3 start, Vector3 end, float height, float duration, float dmg)
    {
        startPos = start;
        targetPos = end;
        arcHeight = height;
        totalDuration = duration;
        damage = dmg;
        timeElapsed = 0f;
        initialized = true;

        // Spawn the warning circle immediately at the destination
        if (warningIndicatorPrefab != null)
        {
            // Spawn at ground level
            Vector3 indicatorPos = targetPos;
            indicatorPos.y = 0.1f; // Slightly above ground to avoid z-fighting
            activeIndicator = Instantiate(warningIndicatorPrefab, indicatorPos, Quaternion.identity);

            // Optional: destroy indicator after duration in case projectile gets destroyed early
            Destroy(activeIndicator, duration + 0.1f);
        }
    }

    void Update()
    {
        if (!initialized) return;

        timeElapsed += Time.deltaTime;
        float t = timeElapsed / totalDuration;

        if (t >= 1f)
        {
            Impact();
            return;
        }

        // --- THE PARABOLIC MATH ---
        // Linear move from Start to End
        Vector3 currentPos = Vector3.Lerp(startPos, targetPos, t);

        // Add height (Arc)
        // Formula: 4 * height * t * (1-t) creates a 0->1->0 parabola
        float yOffset = 4f * arcHeight * t * (1f - t);

        currentPos.y += yOffset;

        transform.position = currentPos;

        // Optional: Rotate to face direction of travel
        Vector3 nextPos = Vector3.Lerp(startPos, targetPos, t + 0.01f);
        nextPos.y += 4f * arcHeight * (t + 0.01f) * (1f - (t + 0.01f));
        transform.LookAt(nextPos);
    }

    void Impact()
    {
        // 1. Clean up warning
        if (activeIndicator != null) Destroy(activeIndicator);

        // 2. Spawn Explosion
        if (explosionPrefab != null)
        {
            GameObject explosion = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            MortarExplosion explScript = explosion.GetComponent<MortarExplosion>();
            if (explScript != null)
            {
                explScript.Initialize(damage);
            }
        }

        // 3. Destroy self
        Destroy(gameObject);
    }
}