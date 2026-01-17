using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private string storySceneName = "StoryScene";

    public void Play()
    {
        SceneManager.LoadScene(storySceneName);
    }

    public void Quit()
    {
        Application.Quit();
    }
}
