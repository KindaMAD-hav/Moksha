using UnityEngine;

[CreateAssetMenu(menuName = "MOKSHA/Bullets/Bullet Visual Profile", fileName = "BVP_")]
public class BulletVisualProfile : ScriptableObject
{
    [Header("Body (Glow)")]
    public Color bodyColor = Color.white;

    [Tooltip("Emission intensity multiplier. Requires an emissive-capable material.")]
    [Range(0f, 25f)]
    public float emissionIntensity = 2f;

    [Header("Trail")]
    public bool enableTrail = true;

    public Color trailColor = Color.white;

    [Tooltip("How long the trail persists (seconds). Keep small for bullet hell.")]
    [Range(0.01f, 0.25f)]
    public float trailTime = 0.08f;

    [Tooltip("Trail width in world units.")]
    [Range(0.01f, 0.5f)]
    public float trailWidth = 0.08f;

    [Tooltip("Higher = fewer trail vertices (cheaper).")]
    [Range(0.02f, 1.0f)]
    public float minVertexDistance = 0.12f;

    [Tooltip("Optional shared material for trail. If null, uses TrailRenderer's current material.")]
    public Material trailMaterial;

    [Header("Optional")]
    [Tooltip("If assigned, will set the projectile renderer's sharedMaterial to this (shared, not instanced).")]
    public Material bodyMaterialOverride;
}
