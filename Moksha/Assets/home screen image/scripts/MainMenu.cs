using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private StorySequence storySequence;

    public void Play()
    {
        if (storySequence != null)
        {
            storySequence.StartStory();
            gameObject.SetActive(false); // hide menu
        }
        else
        {
            SceneManager.LoadScene("Game");
        }
    }

    public void Quit()
    {
        Application.Quit();
    }
}
