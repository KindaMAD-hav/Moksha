using UnityEngine;

/// <summary>
/// One-shot camera shake that works with camera follow scripts
/// </summary>
public class CameraShake : MonoBehaviour
{
    private float shakeTimer;
    private float shakeStrength;

    private Vector3 shakeOffset;

    /// <summary>
    /// Trigger a one-time camera shake
    /// </summary>
    public void Shake(float duration, float strength)
    {
        shakeTimer = duration;
        shakeStrength = strength;
    }

    private void LateUpdate()
    {
        if (shakeTimer > 0f)
        {
            shakeTimer -= Time.deltaTime;
            shakeOffset = Random.insideUnitSphere * shakeStrength;
        }
        else
        {
            shakeOffset = Vector3.zero;
        }

        // Apply shake as an additive offset
        transform.position += shakeOffset;
    }
}
