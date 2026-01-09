using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StorySequence : MonoBehaviour
{
    [Header("UI Images")]
    [SerializeField] private Image imageA;
    [SerializeField] private Image imageB;

    [Header("Story Sprites")]
    [SerializeField] private Sprite[] storySprites;

    [Header("Timings")]
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private float holdDuration = 1.5f;

    [Header("Scene")]
    [SerializeField] private string nextSceneName;

    private const string STORY_KEY = "STORY_SHOWN";

    private Image current;
    private Image next;

    public void StartStory()
    {
        gameObject.SetActive(true);

        imageA.color = SetAlpha(imageA.color, 0f);
        imageB.color = SetAlpha(imageB.color, 0f);

        current = imageA;
        next = imageB;

        StartCoroutine(PlayStory());
    }

    private IEnumerator PlayStory()
    {
        // ----- FIRST IMAGE (FADE IN) -----
        current.sprite = storySprites[0];
        yield return Fade(current, 0f, 1f);
        yield return new WaitForSeconds(holdDuration);

        // ----- MIDDLE IMAGES (CROSSFADE) -----
        for (int i = 1; i < storySprites.Length; i++)
        {
            next.sprite = storySprites[i];
            next.color = SetAlpha(next.color, 0f);

            yield return CrossFade(current, next);

            // swap references
            var temp = current;
            current = next;
            next = temp;

            yield return new WaitForSeconds(holdDuration);
        }

        // ----- LAST IMAGE (FADE OUT) -----
        yield return Fade(current, 1f, 0f);

        PlayerPrefs.SetInt(STORY_KEY, 1);
        PlayerPrefs.Save();

        SceneManager.LoadScene(nextSceneName);
    }

    private IEnumerator Fade(Image img, float from, float to)
    {
        float t = 0f;
        Color c = img.color;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(from, to, t / fadeDuration);
            img.color = c;
            yield return null;
        }

        c.a = to;
        img.color = c;
    }

    private IEnumerator CrossFade(Image fromImg, Image toImg)
    {
        float t = 0f;
        Color fromC = fromImg.color;
        Color toC = toImg.color;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float a = t / fadeDuration;

            fromC.a = Mathf.Lerp(1f, 0f, a);
            toC.a = Mathf.Lerp(0f, 1f, a);

            fromImg.color = fromC;
            toImg.color = toC;

            yield return null;
        }

        fromC.a = 0f;
        toC.a = 1f;

        fromImg.color = fromC;
        toImg.color = toC;
    }

    private Color SetAlpha(Color c, float a)
    {
        c.a = a;
        return c;
    }

    public static bool HasSeenStory()
    {
        return PlayerPrefs.GetInt(STORY_KEY, 0) == 1;
    }
}
