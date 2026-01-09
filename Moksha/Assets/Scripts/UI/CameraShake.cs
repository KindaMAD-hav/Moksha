using UnityEngine;

/// <summary>
/// Configurable one-shot camera shake
/// - Intensity: how far the camera moves
/// - Frequency: how fast it jitters
/// Works with camera follow scripts
/// </summary>
public class CameraShake : MonoBehaviour
{
    [Header("Shake Parameters")]
    [SerializeField] private float defaultIntensity = 0.3f;
    [SerializeField] private float defaultFrequency = 25f;

    public Vector3 CurrentOffset { get; private set; }

    private float shakeTimer;
    private float shakeIntensity;
    private float shakeFrequency;

    private float noiseTime;

    /// <summary>
    /// Trigger camera shake
    /// </summary>
    public void Shake(float duration, float intensity, float frequency)
    {
        shakeTimer = duration;
        shakeIntensity = intensity;
        shakeFrequency = frequency;
        noiseTime = Random.value * 10f;
    }

    /// <summary>
    /// Trigger camera shake using defaults
    /// </summary>
    public void Shake(float duration, float intensity)
    {
        Shake(duration, intensity, defaultFrequency);
    }

    private void LateUpdate()
    {
        if (shakeTimer <= 0f)
        {
            CurrentOffset = Vector3.zero;
            return;
        }

        shakeTimer -= Time.deltaTime;
        noiseTime += Time.deltaTime * shakeFrequency;

        float x = (Mathf.PerlinNoise(noiseTime, 0f) - 0.5f) * 2f;
        float y = (Mathf.PerlinNoise(0f, noiseTime) - 0.5f) * 2f;

        CurrentOffset = new Vector3(x, y, 0f) * shakeIntensity;
    }
}
