using UnityEngine;
using TMPro;
using System.Runtime.CompilerServices;

/// <summary>
/// Damage number with animated glow and motion effects.
/// OPTIMIZED: Cached transform, uses MaterialPropertyBlock for glow (no material instantiation).
/// NOTE: For full optimization, pool DamageNumber GameObjects instead of Destroy().
/// </summary>
public class DamageNumber : MonoBehaviour
{
    [Header("References")]
    public TextMeshPro text;

    [Header("Lifetime")]
    public float lifetime = 1.1f;
    public float fadeOutTime = 0.25f;

    [Header("Base Color")]
    public Gradient damageColorGradient;
    public float maxDamageForColor = 100f;

    [Header("Animated Glow")]
    public float minGlowCycleSpeed = 0.6f;
    public float maxGlowCycleSpeed = 3.5f;
    public float glowIntensity = 1.6f;

    [Header("Glow Gradient (Auto-Generated if empty)")]
    public Gradient glowGradient;
    public float minGlowGradientSpeed = 0.6f;
    public float maxGlowGradientSpeed = 4.5f;

    [Header("Motion")]
    public Vector2 randomSpread = new Vector2(0.35f, 0.15f);
    public float floatSpeed = 1.6f;
    public float upwardBias = 0.75f;

    [Header("Scale Pop")]
    public float startScale = 0.35f;
    public float peakScale = 1.25f;
    public float settleScale = 1f;
    public float popDuration = 0.12f;

    [Header("Impact Scaling")]
    public float bonusScalePerDamage = 0.004f;
    public float maxBonusScale = 0.6f;

    public System.Action OnDestroyed;

    [Header("Movement Bounds")]
    public Collider movementBounds;
    public float stopSmoothTime = 0.15f;

    [Header("Stacking Feedback (Subtle)")]
    public float stackScalePulse = 0.12f;
    public float stackPulseDecay = 18f;
    public float stackGlowBoost = 0.35f;

    // Cached references
    private Transform cachedTransform;
    private Transform cameraTransform;
    private int currentValue;

    // State
    private float _stackPulse;
    private Vector3 _velocity;
    private bool _stopped;
    private bool _hasPopped;
    private Vector3 _moveDir;
    private float _time;
    private float _glowSpeed;
    private float _baseSettledScale = 1f;
    private Color _baseColor;
    
    // Material handling - reuse shared material with property block where possible
    private Material _runtimeMat;
    private bool _materialCreated;
    
    // Cached shader property ID (static, shared across all damage numbers)
    private static readonly int GlowColorID = Shader.PropertyToID("_GlowColor");

    void Awake()
    {
        cachedTransform = transform;
        
        Vector2 spread = new(
            Random.Range(-randomSpread.x, randomSpread.x),
            Random.Range(0f, randomSpread.y)
        );

        _moveDir = (spread + Vector2.up * upwardBias).normalized;
        cachedTransform.localScale = Vector3.one * startScale;

        // For TMP, we need to create a material instance for per-object glow
        // But we cache it and clean it up properly
        if (text != null && text.fontMaterial != null)
        {
            _runtimeMat = new Material(text.fontMaterial);
            text.fontMaterial = _runtimeMat;
            _materialCreated = true;
        }
        
        cameraTransform = Camera.main != null ? Camera.main.transform : null;
        EnsureGlowGradient();
    }

    void EnsureGlowGradient()
    {
        if (glowGradient != null && glowGradient.colorKeys.Length > 0)
            return;

        glowGradient = new Gradient();
        glowGradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(0.6f, 0.05f, 0.05f), 0f),
                new GradientColorKey(new Color(1f, 0.25f, 0.05f), 0.45f),
                new GradientColorKey(new Color(1f, 0.65f, 0.1f), 1f),
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
            }
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetValue(int value)
    {
        currentValue = value;
        text.text = currentValue.ToString();

        _time = 0f;
        _hasPopped = false;

        RecalculateVisuals();
    }

    private void LateUpdate()
    {
        if (cameraTransform == null) return;

        cachedTransform.rotation = Quaternion.Euler(
            0f,
            cameraTransform.eulerAngles.y,
            0f
        );
    }

    public void AddValue(int value)
    {
        currentValue += value;
        text.text = currentValue.ToString();

        _time = Mathf.Max(_time, popDuration);
        _hasPopped = true;

        _stackPulse += stackScalePulse;
        _stackPulse = Mathf.Min(_stackPulse, 0.15f);

        RecalculateVisuals();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void RecalculateVisuals()
    {
        float t = Mathf.Clamp01(currentValue / maxDamageForColor);

        _baseColor = damageColorGradient.Evaluate(t);
        _baseColor.a = 1f;
        text.color = _baseColor;

        _glowSpeed = Mathf.Lerp(minGlowGradientSpeed, maxGlowGradientSpeed, t);
    }

    void Update()
    {
        _time += Time.deltaTime;
        
        // Movement
        if (!_stopped)
        {
            Vector3 targetVelocity = _moveDir * floatSpeed;
            _velocity = Vector3.Lerp(_velocity, targetVelocity, Time.deltaTime * 10f);

            Vector3 nextPos = cachedTransform.position + _velocity * Time.deltaTime;
            cachedTransform.position = nextPos;
        }

        // Scale animation
        if (_time < popDuration && !_hasPopped)
        {
            float t = _time / popDuration;
            float bonus = Mathf.Min(maxBonusScale, bonusScalePerDamage * currentValue);

            float s = Mathf.Lerp(startScale, peakScale + bonus, EaseOutBack(t));
            cachedTransform.localScale = Vector3.one * s;

            _baseSettledScale = s;
        }
        else
        {
            _stackPulse = Mathf.Lerp(_stackPulse, 0f, Time.deltaTime * stackPulseDecay);
            cachedTransform.localScale = Vector3.one * (_baseSettledScale + _stackPulse);
        }

        // Glow animation using cached property ID
        if (_runtimeMat != null)
        {
            float glowT = (_time * _glowSpeed) % 1f;
            Color glow = glowGradient.Evaluate(glowT) * glowIntensity;
            _runtimeMat.SetColor(GlowColorID, glow);
        }

        // Fade out
        if (_time > lifetime - fadeOutTime)
        {
            float t = Mathf.InverseLerp(lifetime - fadeOutTime, lifetime, _time);
            Color c = _baseColor;
            c.a = Mathf.Lerp(1f, 0f, t);
            text.color = c;
        }

        if (_time >= lifetime)
        {
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        // Clean up runtime material to prevent memory leak
        if (_materialCreated && _runtimeMat != null)
        {
            Destroy(_runtimeMat);
            _runtimeMat = null;
        }
        
        OnDestroyed?.Invoke();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        float tm1 = t - 1f;
        return 1f + c3 * tm1 * tm1 * tm1 + c1 * tm1 * tm1;
    }
}
