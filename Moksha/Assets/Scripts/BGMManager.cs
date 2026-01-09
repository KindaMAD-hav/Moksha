using UnityEngine;
using UnityEngine.SceneManagement;

public class BGMManager : MonoBehaviour
{
    public static BGMManager Instance;

    private const string VolumePrefKey = "BGM_VOLUME";

    [Header("Audio Source")]
    [SerializeField] private AudioSource audioSource;

    [Header("Main Menu")]
    [SerializeField] private AudioClip mainMenuBGM;

    [Header("Gameplay BGMs")]
    [SerializeField] private AudioClip[] gameplayBGMs;

    private AudioClip currentClip;
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

        audioSource.loop = true;

        // Load saved volume
        volume = PlayerPrefs.GetFloat(VolumePrefKey, 0.6f);
        audioSource.volume = volume;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        HandleScene(SceneManager.GetActiveScene());
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        HandleScene(scene);
    }

    private void HandleScene(Scene scene)
    {
        if (scene.buildIndex == 0)
            PlayMainMenu();
        else
            PlayGameplay();
    }

    private void PlayMainMenu()
    {
        if (currentClip == mainMenuBGM) return;

        currentClip = mainMenuBGM;
        audioSource.clip = currentClip;
        audioSource.Play();
    }

    private void PlayGameplay()
    {
        if (gameplayBGMs == null || gameplayBGMs.Length == 0)
            return;

        if (currentClip != null && currentClip != mainMenuBGM)
            return;

        currentClip = gameplayBGMs[Random.Range(0, gameplayBGMs.Length)];
        audioSource.clip = currentClip;
        audioSource.Play();
    }

    // =========================
    // VOLUME API
    // =========================

    public void SetVolume(float value)
    {
        volume = Mathf.Clamp01(value);
        audioSource.volume = Mathf.Pow(volume, 2.0f);

        PlayerPrefs.SetFloat(VolumePrefKey, volume);
        PlayerPrefs.Save();
    }

    public float GetVolume()
    {
        return volume;
    }
}
