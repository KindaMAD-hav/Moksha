using UnityEngine;

public class SFXManager : MonoBehaviour
{
    public static SFXManager Instance;

    private const string VolumePrefKey = "SFX_VOLUME";

    [Header("Audio Source")]
    [SerializeField] private AudioSource audioSource;

    [Header("UI Sounds")]
    [SerializeField] private AudioClip buttonClickSFX;

    private float volume = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        // Load saved volume
        volume = PlayerPrefs.GetFloat(VolumePrefKey, 0.8f);
        ApplyVolume();
    }

    // =========================
    // PUBLIC API
    // =========================

    public void PlayButtonClick()
    {
        if (buttonClickSFX == null) return;
        audioSource.PlayOneShot(buttonClickSFX, GetPerceptualVolume());
    }

    public void PlayOneShot(AudioClip clip)
    {
        if (clip == null) return;
        audioSource.PlayOneShot(clip, GetPerceptualVolume());
    }

    public void SetVolume(float value)
    {
        volume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(VolumePrefKey, volume);
        PlayerPrefs.Save();
    }

    public float GetVolume()
    {
        return volume;
    }

    // =========================
    // INTERNAL
    // =========================

    public void PlayOneShot(AudioClip clip, float pitch)
    {
        if (clip == null) return;

        audioSource.pitch = pitch;
        audioSource.PlayOneShot(clip, GetPerceptualVolume());
        audioSource.pitch = 1f; // reset immediately
    }


    private void ApplyVolume()
    {
        // AudioSource.volume is not used for one-shots,
        // but kept consistent for future looping SFX if needed
        audioSource.volume = GetPerceptualVolume();
    }

    private float GetPerceptualVolume()
    {
        // Same curve as BGM for consistency
        return Mathf.Pow(volume, 2.0f);
    }
}
