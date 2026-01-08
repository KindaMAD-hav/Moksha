using UnityEngine;

/// <summary>
/// Handles visual + audio healing effects
/// - GameObject stays ACTIVE permanently
/// - Particles play on demand and stop after duration
/// </summary>
public class HealingVFX : MonoBehaviour
{
    [Header("VFX")]
    [SerializeField] private ParticleSystem[] particleSystems;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip healSfx;

    [Header("Settings")]
    [SerializeField] private float effectDuration = 1.5f;
    [SerializeField] private bool loopParticles = false;

    private bool isPlaying;
    private float timer;

    private void Awake()
    {
        // Auto-find particle systems (include inactive children)
        if (particleSystems == null || particleSystems.Length == 0)
        {
            particleSystems = GetComponentsInChildren<ParticleSystem>(true);
            Debug.Log($"[HealingVFX] Found {particleSystems.Length} particle systems");
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    private void Update()
    {
        if (!isPlaying) return;

        if (!loopParticles)
        {
            timer += Time.deltaTime;
            if (timer >= effectDuration)
            {
                StopEffect();
            }
        }
    }

    public void PlayEffect()
    {
        Debug.Log("[HealingVFX] PlayEffect");

        // Ensure object is active (never disable it later)
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        timer = 0f;
        isPlaying = true;

        foreach (var ps in particleSystems)
        {
            if (ps == null) continue;

            var main = ps.main;
            main.loop = loopParticles;

            ps.Clear();
            ps.Play();
        }

        if (audioSource != null && healSfx != null)
        {
            audioSource.PlayOneShot(healSfx);
        }
    }

    public void StopEffect()
    {
        Debug.Log("[HealingVFX] StopEffect");

        foreach (var ps in particleSystems)
        {
            if (ps != null)
                ps.Stop();
        }

        isPlaying = false;
    }

#if UNITY_EDITOR
    [ContextMenu("Test Play Effect")]
    private void TestPlay()
    {
        PlayEffect();
    }
#endif
}
