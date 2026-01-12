using UnityEngine;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private StorySequence storySequence;

    public void Play()
    {
        storySequence.StartStory();
        gameObject.SetActive(false); // hide menu
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
