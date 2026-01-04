using UnityEngine;

[DisallowMultipleComponent]
public class BulletVisual : MonoBehaviour
{
    [Header("Wiring")]
    public Renderer[] bodyRenderers;
    public TrailRenderer trail;

    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int ColorId = Shader.PropertyToID("_Color");
    static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    MaterialPropertyBlock mpb;

    void Awake()
    {
        mpb ??= new MaterialPropertyBlock();

        if ((bodyRenderers == null || bodyRenderers.Length == 0))
        {
            var r = GetComponentInChildren<Renderer>();
            if (r != null) bodyRenderers = new[] { r };
        }

        if (!trail) trail = GetComponentInChildren<TrailRenderer>();
    }

    public void ApplyProfile(BulletVisualProfile profile)
    {
        if (profile == null) return;

        // Optional: override shared materials (still optimized if shared)
        if (profile.bodyMaterialOverride != null && bodyRenderers != null)
        {
            for (int i = 0; i < bodyRenderers.Length; i++)
                if (bodyRenderers[i] != null)
                    bodyRenderers[i].sharedMaterial = profile.bodyMaterialOverride;
        }

        ApplyBody(profile);
        ApplyTrail(profile);
    }

    void ApplyBody(BulletVisualProfile profile)
    {
        if (bodyRenderers == null) return;

        // Emission = bodyColor * intensity (cheap "glow" when Bloom is on)
        Color emission = profile.bodyColor * profile.emissionIntensity;

        for (int i = 0; i < bodyRenderers.Length; i++)
        {
            var r = bodyRenderers[i];
            if (r == null) continue;

            r.GetPropertyBlock(mpb);

            // Support both URP Lit (_BaseColor) and legacy (_Color)
            mpb.SetColor(BaseColorId, profile.bodyColor);
            mpb.SetColor(ColorId, profile.bodyColor);

            // Requires material that supports emission (and emission enabled on the material)
            mpb.SetColor(EmissionColorId, emission);

            r.SetPropertyBlock(mpb);
        }
    }

    void ApplyTrail(BulletVisualProfile profile)
    {
        if (!trail) return;

        trail.enabled = profile.enableTrail;

        if (!profile.enableTrail)
            return;

        trail.time = profile.trailTime;
        trail.widthMultiplier = profile.trailWidth;
        trail.minVertexDistance = profile.minVertexDistance;

        // Keep trails camera-facing for top-down readability
        trail.alignment = LineAlignment.View;

        // Color
        trail.startColor = profile.trailColor;
        trail.endColor = new Color(profile.trailColor.r, profile.trailColor.g, profile.trailColor.b, 0f);

        // Material
        if (profile.trailMaterial != null)
            trail.sharedMaterial = profile.trailMaterial;

        // Reset so changes apply immediately
        trail.Clear();
    }
}
