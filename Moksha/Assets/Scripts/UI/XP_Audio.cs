using UnityEngine;

public class XP_Audio : MonoBehaviour
{
    [Header("SFX")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip levelUpSfx;

    private void OnEnable()
    {
        if (ExperienceManager.Instance != null)
            ExperienceManager.Instance.OnLevelUp += OnLevelUp;
    }

    private void OnDisable()
    {
        if (ExperienceManager.Instance != null)
            ExperienceManager.Instance.OnLevelUp -= OnLevelUp;
    }

    private void OnLevelUp(int newLevel)
    {
        if (levelUpSfx != null && audioSource != null)
            audioSource.PlayOneShot(levelUpSfx);
    }
}
