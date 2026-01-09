using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private StorySequence storySequence;

    public void Play()
    {
        if (!StorySequence.HasSeenStory())
        {
            storySequence.StartStory();
            gameObject.SetActive(false); // hide menu
        }
        else
        {
            LoadGame();
        }
    }

    private void LoadGame()
    {
        int currentIndex = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(currentIndex + 1);
    }

    public void Quit()
    {
#if UNITY_EDITOR
        Debug.Log("Quit Game");
#else
        Application.Quit();
#endif
    }
}
