using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour
{
    public void Play()
    {
        int currentIndex = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(currentIndex + 1);
    }

    public void Quit()
    {
#if UNITY_EDITOR
        // So you see feedback in editor
        Debug.Log("Quit Game");
#else
        Application.Quit();
#endif
    }
}
