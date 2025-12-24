using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace TinyGiantStudio.DevTools.DevTrails
{
    public static class BuildStatsRetriever
    {
        public static void UpdateBuildInfo(
            List<BuildRecord> buildRecords, Label totalBuildTime,
            Label averageBuildTime, Label totalBuilds,
            Label successfulBuilds, Label failedBuilds,
            Label canceledBuilds, Label warningBuildLogs,
            Label errorBuildLogs
        )
        {
            totalBuildTime.text = BetterString.SmallStringTime(TotalBuildTime(buildRecords));
            averageBuildTime.text = BetterString.SmallStringTime(AverageSuccessfulBuildTime(buildRecords));
            totalBuilds.text = BetterString.Number(buildRecords.Count);
            successfulBuilds.text = BetterString.Number(SuccessfulBuilds(buildRecords));
            failedBuilds.text = BetterString.Number(FailedBuilds(buildRecords));
            canceledBuilds.text = BetterString.Number(CanceledBuilds(buildRecords));
            warningBuildLogs.text = BetterString.Number(WarningLogs(buildRecords));
            errorBuildLogs.text = BetterString.Number(ErrorLogs(buildRecords));
        }

        static float TotalBuildTime(List<BuildRecord> buildRecords) =>
            buildRecords.Sum(buildRecord => buildRecord.timeSpent);

        static float AverageSuccessfulBuildTime(List<BuildRecord> buildRecords) =>
            buildRecords.Where(buildRecord => buildRecord.buildResult == BuildAttemptResult.Succeeded)
                .Sum(buildRecord => buildRecord.timeSpent);

        static int SuccessfulBuilds(List<BuildRecord> buildRecords) =>
            buildRecords.Count(buildRecord => buildRecord.buildResult == BuildAttemptResult.Succeeded);

        static int FailedBuilds(List<BuildRecord> buildRecords) =>
            buildRecords.Count(buildRecord => buildRecord.buildResult == BuildAttemptResult.Failed);

        static int CanceledBuilds(List<BuildRecord> buildRecords) =>
            buildRecords.Count(buildRecord => buildRecord.buildResult == BuildAttemptResult.Canceled);

        static int WarningLogs(List<BuildRecord> buildRecords) =>
            buildRecords.Sum(buildRecord => buildRecord.warnings);

        static int ErrorLogs(List<BuildRecord> buildRecords) =>
            buildRecords.Sum(buildRecord => buildRecord.errors);
    }
}