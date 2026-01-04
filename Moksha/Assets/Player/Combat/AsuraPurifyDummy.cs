using UnityEngine;

/// <summary>
/// Quick test enemy script:
/// - Purification = "damage" to corruption
/// - Destroys itself when corruption reaches 0
/// </summary>
public class AsuraPurifyDummy : Purifiable
{
    [Header("Corruption (HP)")]
    public float maxCorruption = 20f;
    public float currentCorruption;

    [Header("Hit Feedback (optional)")]
    public Renderer[] flashRenderers;
    public float flashDuration = 0.06f;

    float flashTimer;

    void Awake()
    {
        currentCorruption = maxCorruption;

        // Auto-find a renderer if none assigned (works for quick testing)
        if (flashRenderers == null || flashRenderers.Length == 0)
        {
            var r = GetComponentInChildren<Renderer>();
            if (r != null) flashRenderers = new[] { r };
        }
    }

    void Update()
    {
        // Tiny, cheap "hit flash" timer (no coroutines needed)
        if (flashTimer > 0f)
        {
            flashTimer -= Time.deltaTime;
            if (flashTimer <= 0f) SetFlash(false);
        }
    }

    public override void Purify(float amount)
    {
        if (amount <= 0f) return;

        currentCorruption -= amount;

        // Optional hit feedback
        Flash();

        if (currentCorruption <= 0f)
        {
            Die();
        }
    }

    void Flash()
    {
        flashTimer = flashDuration;
        SetFlash(true);
    }

    void SetFlash(bool on)
    {
        if (flashRenderers == null) return;

        // Super-minimal: toggle renderer enabled briefly
        // Later you can swap this to material tint or shader keyword.
        for (int i = 0; i < flashRenderers.Length; i++)
        {
            if (flashRenderers[i] != null)
                flashRenderers[i].enabled = !on;
        }
    }

    void Die()
    {
        // Later: spawn soul embers / VFX / audio / notify wave manager
        Destroy(gameObject);
    }
}
