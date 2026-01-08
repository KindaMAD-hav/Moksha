using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverManager : MonoBehaviour
{
    public static bool IsGameOver { get; private set; }

    [Header("References")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private GameObject gameOverPanel;

    [Header("Buttons")]
    [SerializeField] private Button retryButton;
    [SerializeField] private Button homeButton;

    [Header("Settings")]
    [SerializeField] private bool pauseTimeOnGameOver = true;
    [SerializeField] private string homeSceneName = "MainMenu";

    private void Awake()
    {
        IsGameOver = false;

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (retryButton != null)
            retryButton.onClick.AddListener(Retry);

        if (homeButton != null)
            homeButton.onClick.AddListener(GoHome);
    }

    private void OnEnable()
    {
        if (playerHealth != null)
            playerHealth.OnDeath += HandlePlayerDeath;
    }

    private void OnDisable()
    {
        if (playerHealth != null)
            playerHealth.OnDeath -= HandlePlayerDeath;
    }

    private void HandlePlayerDeath()
    {
        IsGameOver = true;

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        if (pauseTimeOnGameOver)
            Time.timeScale = 0f;
    }

    public void Retry()
    {
        Time.timeScale = 1f;
        IsGameOver = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void GoHome()
    {
        Time.timeScale = 1f;
        IsGameOver = false;

        if (!string.IsNullOrEmpty(homeSceneName))
            SceneManager.LoadScene(homeSceneName);
        else
            SceneManager.LoadScene(0);
    }
}
