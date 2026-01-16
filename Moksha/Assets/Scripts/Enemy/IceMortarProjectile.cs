using UnityEngine;

/// <summary>
/// Ice mortar projectile that travels in a parabolic arc toward a target position.
/// Features:
/// - Parabolic arc trajectory
/// - Red AOE indicator showing landing zone
/// - Configurable damage on impact
/// - Slow effect (freeze) applied to player on hit
/// </summary>
public class IceMortarProjectile : MonoBehaviour
{
    [Header("Impact Settings")]
    [Tooltip("Radius of the AOE damage zone")]
    [SerializeField] private float impactRadius = 2.5f;
    [Tooltip("Damage dealt on impact")]
    [SerializeField] private float damage = 10f;

    [Header("Slow Effect")]
    [Tooltip("How much to slow the player (0.0 = no slow, 1.0 = complete stop)")]
    [SerializeField] private float slowPercentage = 0.5f;
    [Tooltip("Duration of the slow effect in seconds")]
    [SerializeField] private float slowDuration = 2f;

    [Header("AOE Indicator")]
    [Tooltip("Prefab for the ground AOE indicator (should be a red circle/ring)")]
    [SerializeField] private GameObject aoeIndicatorPrefab;
    [Tooltip("Color of the AOE indicator")]
    [SerializeField] private Color aoeColor = new Color(1f, 0.2f, 0.2f, 0.5f);
    [Tooltip("Height offset for the indicator above ground")]
    [SerializeField] private float indicatorHeightOffset = 0.05f;

    [Header("Visual Effects")]
    [Tooltip("Effect spawned on impact")]
    [SerializeField] private GameObject impactEffectPrefab;
    [Tooltip("Trail renderer for projectile path")]
    [SerializeField] private TrailRenderer trailRenderer;

    [Header("Audio")]
    [SerializeField] private AudioClip launchSound;
    [SerializeField] private AudioClip impactSound;

    // Flight parameters
    private Vector3 startPosition;
    private Vector3 targetPosition;
    private float arcHeight;
    private float flightDuration;
    private float flightTimer;
    private bool isInitialized;
    private bool hasImpacted;

    // Cached
    private Transform cachedTransform;
    private GameObject aoeIndicatorInstance;
    private static int groundLayer = -1;
    private static int playerLayer = -1;

    private void Awake()
    {
        cachedTransform = transform;

        // Cache layer lookups
        if (groundLayer < 0)
        {
            groundLayer = LayerMask.NameToLayer("Ground");
            playerLayer = LayerMask.NameToLayer("Player");
        }

        // Get trail renderer if not assigned
        if (trailRenderer == null)
            trailRenderer = GetComponent<TrailRenderer>();
    }

    /// <summary>
    /// Initialize the mortar projectile with trajectory parameters.
    /// </summary>
    /// <param name="start">Starting position (spawn point)</param>
    /// <param name="target">Target landing position</param>
    /// <param name="height">Peak height of the arc</param>
    /// <param name="duration">Time to reach target</param>
    /// <param name="damageAmount">Damage to deal on impact</param>
    public void Initialize(Vector3 start, Vector3 target, float height, float duration, float damageAmount)
    {
        startPosition = start;
        targetPosition = target;
        arcHeight = height;
        flightDuration = duration;
        damage = damageAmount;

        flightTimer = 0f;
        hasImpacted = false;
        isInitialized = true;

        cachedTransform.position = startPosition;

        // Create AOE indicator at target position
        SpawnAOEIndicator();

        // Play launch sound
        if (launchSound != null)
        {
            AudioSource.PlayClipAtPoint(launchSound, startPosition, 0.8f);
        }
    }

    private void SpawnAOEIndicator()
    {
        if (aoeIndicatorPrefab != null)
        {
            // Use prefab
            Vector3 indicatorPos = new Vector3(targetPosition.x, GetGroundHeight(targetPosition) + indicatorHeightOffset, targetPosition.z);
            aoeIndicatorInstance = Instantiate(aoeIndicatorPrefab, indicatorPos, Quaternion.Euler(90f, 0f, 0f));
            aoeIndicatorInstance.transform.localScale = new Vector3(impactRadius * 2f, impactRadius * 2f, 1f);
        }
        else
        {
            // Create a simple procedural indicator using a cylinder
            aoeIndicatorInstance = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            aoeIndicatorInstance.name = "AOE_Indicator";

            // Remove collider (it's just visual)
            Collider col = aoeIndicatorInstance.GetComponent<Collider>();
            if (col != null) Destroy(col);

            // Position and scale
            Vector3 indicatorPos = new Vector3(targetPosition.x, GetGroundHeight(targetPosition) + indicatorHeightOffset, targetPosition.z);
            aoeIndicatorInstance.transform.position = indicatorPos;
            aoeIndicatorInstance.transform.localScale = new Vector3(impactRadius * 2f, 0.01f, impactRadius * 2f);

            // Apply material/color
            Renderer rend = aoeIndicatorInstance.GetComponent<Renderer>();
            if (rend != null)
            {
                // Create a transparent material
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                if (mat != null)
                {
                    // Set to transparent mode
                    mat.SetFloat("_Surface", 1); // Transparent
                    mat.SetFloat("_Blend", 0); // Alpha
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mat.SetInt("_ZWrite", 0);
                    mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    mat.renderQueue = 3000;
                    mat.SetColor("_BaseColor", aoeColor);
                    rend.material = mat;
                }
            }
        }
    }

    private float GetGroundHeight(Vector3 position)
    {
        // Raycast down to find ground
        if (Physics.Raycast(position + Vector3.up * 50f, Vector3.down, out RaycastHit hit, 100f, LayerMask.GetMask("Ground", "Default", "Terrain")))
        {
            return hit.point.y;
        }
        return 0f;
    }

    private void Update()
    {
        if (!isInitialized || hasImpacted) return;

        flightTimer += Time.deltaTime;
        float t = Mathf.Clamp01(flightTimer / flightDuration);

        // Calculate parabolic arc position
        Vector3 currentPos = CalculateArcPosition(t);
        cachedTransform.position = currentPos;

        // Rotate to face movement direction
        if (t < 1f)
        {
            float nextT = Mathf.Min(t + 0.01f, 1f);
            Vector3 nextPos = CalculateArcPosition(nextT);
            Vector3 direction = (nextPos - currentPos).normalized;
            if (direction.sqrMagnitude > 0.001f)
            {
                cachedTransform.rotation = Quaternion.LookRotation(direction);
            }
        }

        // Scale indicator as projectile approaches (pulsing warning effect)
        if (aoeIndicatorInstance != null)
        {
            float pulse = 1f + Mathf.Sin(Time.time * 8f) * 0.1f * t;
            float baseScale = impactRadius * 2f;
            aoeIndicatorInstance.transform.localScale = new Vector3(baseScale * pulse, 0.01f, baseScale * pulse);

            // Increase alpha as impact approaches
            Renderer rend = aoeIndicatorInstance.GetComponent<Renderer>();
            if (rend != null && rend.material != null)
            {
                Color col = aoeColor;
                col.a = Mathf.Lerp(0.3f, 0.7f, t);
                rend.material.SetColor("_BaseColor", col);
            }
        }

        // Check for impact
        if (t >= 1f)
        {
            OnImpact();
        }
    }

    /// <summary>
    /// Calculate position along the parabolic arc at time t (0 to 1).
    /// </summary>
    private Vector3 CalculateArcPosition(float t)
    {
        // Linear interpolation for horizontal movement
        Vector3 horizontalPos = Vector3.Lerp(startPosition, targetPosition, t);

        // Parabolic arc for vertical: peaks at t=0.5
        // h(t) = 4 * arcHeight * t * (1 - t) gives a nice parabola peaking at t=0.5
        float verticalOffset = 4f * arcHeight * t * (1f - t);

        // Start height lerps to target height (usually ground)
        float baseHeight = Mathf.Lerp(startPosition.y, targetPosition.y, t);

        return new Vector3(horizontalPos.x, baseHeight + verticalOffset, horizontalPos.z);
    }

    private void OnImpact()
    {
        if (hasImpacted) return;
        hasImpacted = true;

        // Destroy AOE indicator
        if (aoeIndicatorInstance != null)
        {
            Destroy(aoeIndicatorInstance);
        }

        // Play impact sound
        if (impactSound != null)
        {
            AudioSource.PlayClipAtPoint(impactSound, cachedTransform.position, 1f);
        }

        // Spawn impact effect
        if (impactEffectPrefab != null)
        {
            GameObject effect = Instantiate(impactEffectPrefab, cachedTransform.position, Quaternion.identity);
            Destroy(effect, 3f);
        }

        // Deal damage and apply slow to targets in radius
        ApplyAreaEffect();

        // Destroy projectile
        Destroy(gameObject);
    }

    private void ApplyAreaEffect()
    {
        // Find all colliders in impact radius
        Collider[] hitColliders = Physics.OverlapSphere(targetPosition, impactRadius);

        foreach (Collider col in hitColliders)
        {
            // Check for player
            if (col.CompareTag("Player"))
            {
                // Apply damage via IDamageable
                IDamageable damageable = col.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(damage);
                }

                // Apply slow effect via PlayerController
                PlayerController playerController = col.GetComponent<PlayerController>();
                if (playerController != null)
                {
                    playerController.ApplySlow(slowPercentage, slowDuration);
                }

                // Only hit player once
                break;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Draw impact radius in editor
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.5f);
        Gizmos.DrawWireSphere(Application.isPlaying ? targetPosition : transform.position, impactRadius);

        // Draw arc preview in editor (when not playing)
        if (!Application.isPlaying)
        {
            Gizmos.color = Color.cyan;
            Vector3 start = transform.position;
            Vector3 end = transform.position + transform.forward * 10f;

            Vector3 prev = start;
            for (int i = 1; i <= 20; i++)
            {
                float t = i / 20f;
                Vector3 horizontal = Vector3.Lerp(start, end, t);
                float vertical = 4f * arcHeight * t * (1f - t);
                Vector3 point = new Vector3(horizontal.x, start.y + vertical, horizontal.z);
                Gizmos.DrawLine(prev, point);
                prev = point;
            }
        }
    }
}
