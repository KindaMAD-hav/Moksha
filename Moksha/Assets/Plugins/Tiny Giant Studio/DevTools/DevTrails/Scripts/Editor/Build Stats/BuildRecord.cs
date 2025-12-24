using UnityEditor.Build.Reporting;

namespace TinyGiantStudio.DevTools.DevTrails
{
    [System.Serializable]
    public class BuildRecord
    {
        public BuildRecord(BuildReport report, BuildResult result, double elapsed)
        {
            timeSpent = (float)elapsed;
            reportedTime = (float)report.summary.totalTime.TotalSeconds;
            buildResult = report.summary.result switch
            {
                BuildResult.Succeeded => BuildAttemptResult.Succeeded,
                BuildResult.Failed => BuildAttemptResult.Failed,
                BuildResult.Cancelled => BuildAttemptResult.Canceled,
                _ => buildResult
            };

            warnings = report.summary.totalErrors;
            errors = report.summary.totalErrors;
        }

        /// <summary>
        /// This is the time manually tracked by DevTrails
        /// </summary>
        public float timeSpent;

        /// <summary>
        /// This is the time reported by Unity.
        /// it is not guaranteed to represent the true wall-clock time a developer experiences.
        /// It is calculated internally by Unity and may exclude time spent in domain reloads,
        /// external toolchains such as IL2CPP, Gradle, or Xcode, and certain delays or cleanup that occur during failures.
        /// </summary>
        public float reportedTime;

        public BuildAttemptResult buildResult;
        public int warnings;
        public int errors;
        public Platform platform; //Keeping this for future use
    }

    [System.Serializable]
    public enum BuildAttemptResult
    {
        Succeeded,
        Failed,
        Canceled,
    }

    [System.Serializable]
    public enum Platform
    {
        NotRecorded,
    }
}