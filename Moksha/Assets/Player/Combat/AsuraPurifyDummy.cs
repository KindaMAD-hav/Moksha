using UnityEngine;

/// <summary>
/// Quick test enemy script:
/// - Implements IPurifiable
/// - Destroys itself when purified
/// Replace with your real Asura / soul-anchoring logic later.
/// </summary>
public class AsuraPurifyDummy : MonoBehaviour, IPurifiable
{
    public float maxCorruption = 20f;

    float corruption;

    void Awake()
    {
        corruption = maxCorruption;
    }

    public void Purify(float amount)
    {
        corruption -= amount;

        if (corruption <= 0f)
        {
            // Later: spawn Soul Ember drops / VFX / audio
            Destroy(gameObject);
        }
    }
}
