using UnityEngine;

public class FPSLimiter : MonoBehaviour
{
    [Header("Frame Rate Settings")]
    [Tooltip("Set this to 60, 30, or -1 for unlimited")]
    public int targetFPS = 60;

    void Awake()
    {
        // 0 disables VSync. This is REQUIRED for targetFrameRate to work.
        // If VSync is 1 or 2, Unity forces the FPS to match your monitor (usually 60 or 144).
        QualitySettings.vSyncCount = 0;

        // Sets the actual target frame rate
        Application.targetFrameRate = targetFPS;
    }

    void Update()
    {
        // Optional: Ensures the setting sticks even if another script tries to change it
        if (Application.targetFrameRate != targetFPS)
        {
            Application.targetFrameRate = targetFPS;
        }
    }
}