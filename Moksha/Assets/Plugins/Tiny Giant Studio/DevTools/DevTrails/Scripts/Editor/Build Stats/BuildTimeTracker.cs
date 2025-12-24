using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace TinyGiantStudio.DevTools.DevTrails
{
    public sealed class BuildTimeTracker :
        IPreprocessBuildWithReport,
        IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;

        const string BuildStartKey = "DevTrails_BuildStartTime";

        public void OnPreprocessBuild(BuildReport report)
        {
            SessionState.SetFloat(
                BuildStartKey,
                (float)EditorApplication.timeSinceStartup
            );
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            float startTime = SessionState.GetFloat(BuildStartKey, float.NaN);
            SessionState.EraseFloat(BuildStartKey);

            if (float.IsNaN(startTime))
            {
                Debug.LogWarning("Build start time missing. Build duration cannot be calculated.");
                return;
            }

            double elapsed = EditorApplication.timeSinceStartup - startTime;

            BuildResult result = report.summary.result;

            ProcessBuildReport(report, result, elapsed);
        }

        static void ProcessBuildReport(BuildReport report, BuildResult result, double elapsed)
        {
            UserStats_Today todayStats = UserStats_Today.instance;
            todayStats.BuildRecords.Add(new BuildRecord(report, result, elapsed));
            todayStats.SaveToDisk();
            
            UserStats_Project projectStats = UserStats_Project.instance;
            projectStats.BuildRecords.Add(new BuildRecord(report, result, elapsed));
            projectStats.Save();
            
            UserStats_Global globalStats = UserStats_Global.instance;
            globalStats.BuildRecords.Add(new BuildRecord(report, result, elapsed));
            globalStats.SaveToDisk();
        }
    }
}