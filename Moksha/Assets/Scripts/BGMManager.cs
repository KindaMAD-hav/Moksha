using UnityEngine;
using UnityEngine.SceneManagement;

public class BGMManager : MonoBehaviour
{
    public static BGMManager Instance;

    [Header("Audio Source")]
    [SerializeField] private AudioSource audioSource;

    [Header("Main Menu")]
    [SerializeField] private AudioClip mainMenuBGM;

    [Header("Gameplay BGMs (Random Pick)")]
    [SerializeField] private AudioClip[] gameplayBGMs;

    private AudioClip currentClip;

    private void Awake()
    {
        // Singleton
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
        // Handle first scene (Main Menu)
        HandleScene(SceneManager.GetActiveScene());
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        HandleScene(scene);
    }

    private void HandleScene(Scene scene)
    {
        // Main Menu assumed to be build index 0
        if (scene.buildIndex == 0)
        {
            PlayMainMenu();
        }
        else
        {
            PlayGameplay();
        }
    }

    private void PlayMainMenu()
    {
        if (currentClip == mainMenuBGM) return;

        currentClip = mainMenuBGM;
        audioSource.clip = mainMenuBGM;
        audioSource.Play();
    }

    private void PlayGameplay()
    {
        if (gameplayBGMs == null || gameplayBGMs.Length == 0)
            return;

        // If already playing a gameplay BGM, don't restart
        if (currentClip != null && currentClip != mainMenuBGM)
            return;

        int index = Random.Range(0, gameplayBGMs.Length);
        currentClip = gameplayBGMs[index];
        audioSource.clip = currentClip;
        audioSource.Play();
    }
}
