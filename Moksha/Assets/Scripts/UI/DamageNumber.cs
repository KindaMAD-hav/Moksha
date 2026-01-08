using UnityEngine;
using TMPro;

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

    [Header("Glow Mode")]
    public bool useAttackerColorGlow = false;

    [Header("Animated Glow (Disabled if attacker glow is ON)")]
    public float minGlowCycleSpeed = 0.6f;
    public float maxGlowCycleSpeed = 3.5f;
    public float glowIntensity = 1.6f;
    [Range(0f, 1f)] public float forbiddenHueMin = 0.10f;
    [Range(0f, 1f)] public float forbiddenHueMax = 0.20f;

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
    public float stackScalePulse = 0.12f;     // how much scale increases per stack
    public float stackPulseDecay = 18f;        // how fast it settles
    public float stackGlowBoost = 0.35f;        // temporary glow boost

    private Transform cameraTransform;


    float _stackPulse;
    float _stackGlowTimer;

    Vector3 _velocity;
    bool _stopped;

    Vector3 _moveDir;
    float _time;
    int _damage;

    float _glowHue;
    float _glowSpeed;
    float _baseSettledScale = 1f;

    Color _baseColor;
    Material _runtimeMat;

    void Awake()
    {
        Vector2 spread = new(
            Random.Range(-randomSpread.x, randomSpread.x),
            Random.Range(0f, randomSpread.y)
        );

        _moveDir = (spread + Vector2.up * upwardBias).normalized;
        transform.localScale = Vector3.one * startScale;

        if (text != null)
        {
            _runtimeMat = Instantiate(text.fontMaterial);
            text.fontMaterial = _runtimeMat;
        }
        cameraTransform = Camera.main.transform;
    }

    public void SetValue(int damage)
    {
        _damage = damage;
        text.text = _damage.ToString();
        RecalculateVisuals();
    }

    public void AddDamage(int extraDamage)
    {
        _damage += extraDamage;
        text.text = _damage.ToString();

        _time = 0f; // still refresh lifetime

        // ✨ subtle stacking response (no pop restart)
        _stackPulse += stackScalePulse;
        _stackGlowTimer = 0.12f;

        RecalculateVisuals();
    }
    private void LateUpdate()
    {
        if (cameraTransform == null) return;

        Vector3 dir = transform.position - cameraTransform.position;
        dir.y = 0f; // keep upright

        if (dir.sqrMagnitude < 0.0001f) return;

        transform.rotation = Quaternion.LookRotation(dir);
    }


    void RecalculateVisuals()
    {
        float t = Mathf.Clamp01(_damage / maxDamageForColor);

        _baseColor = damageColorGradient.Evaluate(t);
        _baseColor.a = 1f;
        text.color = _baseColor;

        _glowSpeed = Mathf.Lerp(minGlowCycleSpeed, maxGlowCycleSpeed, t);
        _glowHue = Random.value;
    }

    public void SetAttackerColor(Color c)
    {
        if (_runtimeMat == null) return;
        _runtimeMat.SetColor("_GlowColor", c * glowIntensity);
    }

    void Update()
    {
        _time += Time.deltaTime;
        if (!_stopped)
        {
            Vector3 targetVelocity = _moveDir * floatSpeed;
            _velocity = Vector3.Lerp(_velocity, targetVelocity, Time.deltaTime * 10f);

            Vector3 nextPos = transform.position + _velocity * Time.deltaTime;

            if (movementBounds != null)
            {
                Bounds area = movementBounds.bounds;

                transform.position = nextPos;

                Bounds textBounds = GetTextWorldBounds();

                Vector3 correction = Vector3.zero;

                if (textBounds.min.x < area.min.x)
                    correction.x += area.min.x - textBounds.min.x;
                if (textBounds.max.x > area.max.x)
                    correction.x -= textBounds.max.x - area.max.x;

                if (textBounds.min.y < area.min.y)
                    correction.y += area.min.y - textBounds.min.y;
                if (textBounds.max.y > area.max.y)
                    correction.y -= textBounds.max.y - area.max.y;

                if (correction != Vector3.zero)
                {
                    transform.position += correction;
                    _velocity = Vector3.Lerp(_velocity, Vector3.zero, Time.deltaTime / stopSmoothTime);
                    _stopped = _velocity.sqrMagnitude < 0.001f;
                }
            }
            else
            {
                transform.position = nextPos;
            }
        }

        if (_time < popDuration)
        {
            float t = _time / popDuration;
            float bonus = Mathf.Min(maxBonusScale, bonusScalePerDamage * _damage);

            float s = Mathf.Lerp(startScale, peakScale + bonus, EaseOutBack(t));
            transform.localScale = Vector3.one * s;

            // cache where we should actually settle
            _baseSettledScale = s;
        }
        else
        {
            _stackPulse = Mathf.Lerp(_stackPulse, 0f, Time.deltaTime * stackPulseDecay);
            transform.localScale = Vector3.one * (_baseSettledScale + _stackPulse);
        }


        if (_runtimeMat != null && !useAttackerColorGlow)
        {
            _glowHue = (_glowHue + Time.deltaTime * _glowSpeed) % 1f;

            if (_glowHue > forbiddenHueMin && _glowHue < forbiddenHueMax)
                _glowHue = forbiddenHueMax;

            Color glow = Color.HSVToRGB(_glowHue, 1f, 1f) * glowIntensity;
            _runtimeMat.SetColor("_GlowColor", glow);
        }

        if (_time > lifetime - fadeOutTime)
        {
            float t = Mathf.InverseLerp(lifetime - fadeOutTime, lifetime, _time);
            Color c = _baseColor;
            c.a = Mathf.Lerp(1f, 0f, t);
            text.color = c;
        }

        if (_time >= lifetime)
            Destroy(gameObject);
    }
    Bounds GetTextWorldBounds()
    {
        text.ForceMeshUpdate();
        var meshBounds = text.mesh.bounds;

        Vector3 center = transform.TransformPoint(meshBounds.center);
        Vector3 size = Vector3.Scale(meshBounds.size, transform.lossyScale);

        return new Bounds(center, size);
    }


    void OnDestroy()
    {
        OnDestroyed?.Invoke();
    }

    float EaseOutBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f)
                   + c1 * Mathf.Pow(t - 1f, 2f);
    }
}
