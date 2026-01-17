using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StorySequence : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image imageA;
    [SerializeField] private Image imageB;

    [Header("Story")]
    [SerializeField] private Sprite[] storySprites;

    [Header("Timings")]
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private float holdDuration = 1.5f;

    [Header("Scene")]
    [SerializeField] private string nextSceneName;

    private Image current;
    private Image next;
    private Coroutine storyRoutine;
    private bool skipping;

    //[Header("Disable When Story Starts")]
    //[SerializeField] private GameObject[] objectsToDisableOnStart;


    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private void Update()
    {
        if (!skipping && storyRoutine != null && Input.GetKeyDown(KeyCode.Space))
        {
            SkipStory();
        }
    }
    private void Start()
    {
        StartStory();
    }

    public void StartStory()
    {
        skipping = false;

        //// Disable assigned objects
        //if (objectsToDisableOnStart != null)
        //{
        //    foreach (var go in objectsToDisableOnStart)
        //    {
        //        if (go != null)
        //            go.SetActive(false);
        //    }
        //}

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        imageA.color = SetAlpha(imageA.color, 0f);
        imageB.color = SetAlpha(imageB.color, 0f);

        current = imageA;
        next = imageB;

        storyRoutine = StartCoroutine(PlayStory());
    }


    private IEnumerator PlayStory()
    {
        // First image fade in
        current.sprite = storySprites[0];
        yield return Fade(current, 0f, 1f);
        yield return new WaitForSeconds(holdDuration);

        // Crossfades
        for (int i = 1; i < storySprites.Length; i++)
        {
            next.sprite = storySprites[i];
            next.color = SetAlpha(next.color, 0f);

            yield return CrossFade(current, next);

            (current, next) = (next, current);
            yield return new WaitForSeconds(holdDuration);
        }

        // Final fade out
        yield return Fade(current, 1f, 0f);

        LoadNextScene();
    }

    private void SkipStory()
    {
        skipping = true;

        if (storyRoutine != null)
            StopCoroutine(storyRoutine);

        LoadNextScene();
    }

    private void LoadNextScene()
    {
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
    }

    private Color SetAlpha(Color c, float a)
    {
        c.a = a;
        return c;
    }
}
