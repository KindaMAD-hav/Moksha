using System;
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

        // 🔒 Disable player completely
        if (playerHealth != null)
            playerHealth.gameObject.SetActive(false);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        if (pauseTimeOnGameOver && ShatteredPauseMenu.Instance != null)
        {
            ShatteredPauseMenu.Instance.SilentPause();
        }
    }


    public void Retry()
    {
        IsGameOver = false;

        if (ShatteredPauseMenu.Instance != null)
            ShatteredPauseMenu.Instance.SilentResume();

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

    }

    public void GoHome()
    {
        IsGameOver = false;

        if (ShatteredPauseMenu.Instance != null)
            ShatteredPauseMenu.Instance.SilentResume();

        if (!string.IsNullOrEmpty(homeSceneName))
            SceneManager.LoadScene(homeSceneName);
        else
            SceneManager.LoadScene(0);
    }
}
