using System;
using UnityEditor;
using UnityEngine.UIElements;

namespace TinyGiantStudio.DevTools.DevTrails
{
    // ReSharper disable once InconsistentNaming
    public class UserStats_Project_UI
    {
        readonly UserStats_Project _userStats;
        readonly DevTrailSettings _devTrailSettings;

        readonly GroupBox _projectTimeStatsGroupBox;

        readonly Label _projectUsageTime;
        readonly Label _projectFocusedUsageTime;
        readonly Label _projectActiveUsageTime;

        readonly GroupBox _projectSceneSavedGroupBox;
        readonly Label _projectSceneSavedCounter;
        readonly GroupBox _projectSceneOpenedGroupBox;
        readonly Label _projectSceneOpenedCounter;

        readonly GroupBox _projectUndoRedoGroupBox;
        readonly Label _projectUndoRedoCounter;

        readonly GroupBox _projectPlayModeGroupBox;
        readonly Label _projectPlayModeCounter;
        readonly Label _projectPlayModeUsageTime;

        readonly GroupBox _projectCompilationGroupBox;
        readonly Label _projectCompileCounter;
        readonly Label _averageCompileTime;
        readonly Label _compileTime;
        readonly Label _domainReloadTime;

        readonly GroupBox _projectConsoleLogGroupBox;

        readonly Label _projectNormalLogCounterPlayMode;
        readonly Label _projectWarningLogCounterPlayMode;
        readonly Label _projectExceptionLogCounterPlayMode;
        readonly Label _projectErrorLogCounterPlayMode;

        readonly Label _projectNormalLogCounterEditor;
        readonly Label _projectWarningLogCounterEditor;
        readonly Label _projectExceptionLogCounterEditor;
        readonly Label _projectErrorLogCounterEditor;

        readonly GroupBox _devToolsWindowOpenedCounterGroupBox;
        readonly Label _devToolsEditorWindowOpenedCounter;

        readonly GroupBox _editorCrashesGroupBox;
        
        readonly Label _totalBuildTime;
        readonly Label _averageBuildTime;
        readonly Label _totalBuilds;
        readonly Label _successfulBuilds;
        readonly Label _failedBuilds;
        readonly Label _canceledBuilds;
        readonly Label _warningBuildLogs;
        readonly Label _errorBuildLogs;
        
        public UserStats_Project_UI(VisualElement container)
        {
            _devTrailSettings = new();
            
            _userStats = UserStats_Project.instance;
            if (_userStats.TotalTimeSpentInDomainReload < 0) _userStats.TotalTimeSpentInDomainReload = 0;

            _projectTimeStatsGroupBox = container.Q<GroupBox>("TimeStats");

            _projectUsageTime = container.Q<Label>("UsageTime");
            _projectFocusedUsageTime = container.Q<Label>("FocusedUsageTime");
            _projectActiveUsageTime = container.Q<Label>("ActiveUsageTime");

            _projectSceneSavedGroupBox = container.Q<GroupBox>("SceneSaved");
            _projectSceneSavedCounter = container.Q<Label>("SceneSavedCounter");
            _projectSceneOpenedGroupBox = container.Q<GroupBox>("SceneOpened");
            _projectSceneOpenedCounter = container.Q<Label>("SceneOpenedCounter");

            _projectPlayModeGroupBox = container.Q<GroupBox>("PlayMode");
            _projectPlayModeCounter = container.Q<Label>("PlayModeCounter");
            _projectPlayModeUsageTime = container.Q<Label>("PlayModeUsageTime");

            _projectUndoRedoGroupBox = container.Q<GroupBox>("UndoRedo");
            _projectUndoRedoCounter = container.Q<Label>("UndoRedoCounter");

            _projectCompilationGroupBox = container.Q<GroupBox>("Compilation");
            _projectCompileCounter = container.Q<Label>("CompileCounter");
            _averageCompileTime = container.Q<Label>("AverageCompileTime");
            _compileTime = container.Q<Label>("CompileTime");
            _domainReloadTime = container.Q<Label>("DomainReloadTime");

            _projectConsoleLogGroupBox = container.Q<GroupBox>("ConsoleLogs");
            _projectNormalLogCounterPlayMode = container.Q<Label>("NormalLogCounter_PlayMode");
            _projectWarningLogCounterPlayMode = container.Q<Label>("WarningLogCounter_PlayMode");
            _projectExceptionLogCounterPlayMode = container.Q<Label>("ExceptionLogCounter_PlayMode");
            _projectErrorLogCounterPlayMode = container.Q<Label>("ErrorLogCounter_PlayMode");

            _projectNormalLogCounterEditor = container.Q<Label>("NormalLogCounter_Editor");
            _projectWarningLogCounterEditor = container.Q<Label>("WarningLogCounter_Editor");
            _projectExceptionLogCounterEditor = container.Q<Label>("ExceptionLogCounter_Editor");
            _projectErrorLogCounterEditor = container.Q<Label>("ErrorLogCounter_Editor");

            _devToolsWindowOpenedCounterGroupBox = container.Q<GroupBox>("DevToolsEditorWindowOpened");
            _devToolsEditorWindowOpenedCounter = _devToolsWindowOpenedCounterGroupBox.Q<Label>("DevToolsEditorWindowOpenedCounter");

            _editorCrashesGroupBox = container.Q<GroupBox>("EditorCrashes");
            _editorCrashesGroupBox.Q<Label>("EditorCrashCounter").text = _userStats.probablyCrashes.ToString();
            
            _totalBuildTime = container.Q<Label>("TotalBuildTime");
            _totalBuilds = container.Q<Label>("TotalBuilds");
            _averageBuildTime = container.Q<Label>("AverageBuildTime");
            _successfulBuilds = container.Q<Label>("BuildSucceeded");
            _canceledBuilds = container.Q<Label>("BuildCanceled");
            _failedBuilds = container.Q<Label>("BuildFailed");
            _warningBuildLogs = container.Q<Label>("BuildWarnings");
            _errorBuildLogs = container.Q<Label>("BuildErrors");
        }

        public void HideUntrackedData()
        {
            _projectTimeStatsGroupBox.style.display = _devTrailSettings.TrackTime ? DisplayStyle.Flex : DisplayStyle.None;

            if (_devTrailSettings.TrackSceneSave)
                _projectSceneSavedGroupBox.style.display = DisplayStyle.Flex;
            else
                _projectSceneSavedGroupBox.style.display = DisplayStyle.None;

            if (_devTrailSettings.TrackSceneOpen)
                _projectSceneOpenedGroupBox.style.display = DisplayStyle.Flex;
            else
                _projectSceneOpenedGroupBox.style.display = DisplayStyle.None;

            if (_devTrailSettings.TrackUndoRedo)
                _projectUndoRedoGroupBox.style.display = DisplayStyle.Flex;
            else
                _projectUndoRedoGroupBox.style.display = DisplayStyle.None;

            if (_devTrailSettings.TrackPlayMode)
                _projectPlayModeGroupBox.style.display = DisplayStyle.Flex;
            else
                _projectPlayModeGroupBox.style.display = DisplayStyle.None;

            if (_devTrailSettings.TrackCompilation)
                _projectCompilationGroupBox.style.display = DisplayStyle.Flex;
            else
                _projectCompilationGroupBox.style.display = DisplayStyle.None;

            if (_devTrailSettings.TrackConsoleLogs)
                _projectConsoleLogGroupBox.style.display = DisplayStyle.Flex;
            else
                _projectConsoleLogGroupBox.style.display = DisplayStyle.None;

            if (_devTrailSettings.ShowDevToolsEditorWindowTrack)
                _devToolsWindowOpenedCounterGroupBox.style.display = DisplayStyle.Flex;
            else
                _devToolsWindowOpenedCounterGroupBox.style.display = DisplayStyle.None;

            if (_devTrailSettings.TrackEditorCrashes)
                _editorCrashesGroupBox.style.display = DisplayStyle.Flex;
            else
                _editorCrashesGroupBox.style.display = DisplayStyle.None;
        }

        internal void UpdateInfo()
        {
            _projectUsageTime.text = SmallStringTime(_userStats.totalUseTime + CurrentSessionUseTime());
            _projectFocusedUsageTime.text = SmallStringTime(_userStats.focusedUseTime + CurrentSessionFocusedUseTime());
            _projectActiveUsageTime.text = SmallStringTime(_userStats.activeUseTime + CurrentSessionActiveUseTime());

            _projectPlayModeCounter.text = BetterString.Number(_userStats.EnteredPlayMode);
            _projectPlayModeUsageTime.text = SmallStringTime(_userStats.PlayModeUseTime);

            _projectSceneSavedCounter.text = BetterString.Number(_userStats.SceneSaved);
            _projectSceneOpenedCounter.text = BetterString.Number(_userStats.SceneOpened);

            _projectUndoRedoCounter.text = BetterString.Number(_userStats.UndoRedoCounter);

            _projectCompileCounter.text = BetterString.Number(_userStats.CompileCounter);

            _averageCompileTime.text = _userStats.CompileCounter == 0 ? "Not recorded" : SmallStringTime((_userStats.TotalTimeSpentCompiling + _userStats.TimeSpentCompiling) / _userStats.CompileCounter);

            _compileTime.text = SmallStringTime(_userStats.TotalTimeSpentCompiling + _userStats.TimeSpentCompiling);
            _domainReloadTime.text = SmallStringTime(MathF.Abs(_userStats.TotalTimeSpentInDomainReload) + _userStats.TimeSpentInDomainReload);

            _projectNormalLogCounterPlayMode.text = BetterString.Number(_userStats.LogCounter_playMode);
            _projectWarningLogCounterPlayMode.text = BetterString.Number(_userStats.WarningLogCounter_playMode);
            _projectExceptionLogCounterPlayMode.text = BetterString.Number(_userStats.ExceptionLogCounter_playMode);
            _projectErrorLogCounterPlayMode.text = BetterString.Number(_userStats.ErrorLogCounter_playMode);

            _projectNormalLogCounterEditor.text = BetterString.Number(_userStats.LogCounter_editor);
            _projectWarningLogCounterEditor.text = BetterString.Number(_userStats.WarningLogCounter_editor);
            _projectExceptionLogCounterEditor.text = BetterString.Number(_userStats.ExceptionLogCounter_editor);
            _projectErrorLogCounterEditor.text = BetterString.Number(_userStats.ErrorLogCounter_editor);

            _devToolsEditorWindowOpenedCounter.text = BetterString.Number(_userStats.DevToolsEditorWindowOpened);
            
            BuildStatsRetriever.UpdateBuildInfo(
                _userStats.BuildRecords, _totalBuildTime, _averageBuildTime, _totalBuilds, _successfulBuilds,
                _failedBuilds, _canceledBuilds, _warningBuildLogs, _errorBuildLogs);
        }

        int CurrentSessionActiveUseTime()
        {
            var activeElapsedTime = (int)TimeSpan.FromMilliseconds(EditorAnalyticsSessionInfo.activeElapsedTime).TotalSeconds;

            //Currently paused
            if (_devTrailSettings.PauseTimeTracking)
            {
                var pauseDuration = (int)TimeSpan.FromMilliseconds(EditorAnalyticsSessionInfo.activeElapsedTime).TotalSeconds - EditorPrefs.GetInt("PauseStartActiveTime");
                activeElapsedTime -= pauseDuration;
            }

            if (_userStats.PauseRecords.sessionID != EditorAnalyticsSessionInfo.id)
                return activeElapsedTime;

            for (int i = 0; i < _userStats.PauseRecords.sessions.Count; i++)
            {
                activeElapsedTime -= _userStats.PauseRecords.sessions[i].usageTime;
            }
            activeElapsedTime = Math.Abs(activeElapsedTime);

            return activeElapsedTime;
        }

        int CurrentSessionFocusedUseTime()
        {
            int focusedElapsedTime = (int)TimeSpan.FromMilliseconds(EditorAnalyticsSessionInfo.focusedElapsedTime).TotalSeconds;

            //Currently paused
            if (_devTrailSettings.PauseTimeTracking)
            {
                var pauseDuration = (int)TimeSpan.FromMilliseconds(EditorAnalyticsSessionInfo.focusedElapsedTime).TotalSeconds - EditorPrefs.GetInt("PauseStartFocusedTime");
                focusedElapsedTime -= pauseDuration;
                //(int)TimeSpan.FromMilliseconds(EditorAnalyticsSessionInfo.activeElapsedTime).TotalSeconds - EditorPrefs.GetInt("PauseStartActiveTime");
            }

            if (_userStats.PauseRecords.sessionID != EditorAnalyticsSessionInfo.id)
                return focusedElapsedTime;

            for (int i = 0; i < _userStats.PauseRecords.sessions.Count; i++)
            {
                focusedElapsedTime -= _userStats.PauseRecords.sessions[i].focusedTime;
            }
            focusedElapsedTime = Math.Abs(focusedElapsedTime);

            return focusedElapsedTime;
        }

        double CurrentSessionUseTime()
        {
            var timeSinceStartup = EditorApplication.timeSinceStartup;

            //Currently paused
            if (_devTrailSettings.PauseTimeTracking)
            {
                var pauseDuration = timeSinceStartup - EditorPrefs.GetInt("PauseStartUsageTime");
                timeSinceStartup -= pauseDuration;
            }

            //Check old pause sessions
            if (_userStats.PauseRecords.sessionID != EditorAnalyticsSessionInfo.id)
                return timeSinceStartup;

            for (int i = 0; i < _userStats.PauseRecords.sessions.Count; i++)
            {
                timeSinceStartup -= _userStats.PauseRecords.sessions[i].usageTime;
            }
            timeSinceStartup = Math.Abs(timeSinceStartup);

            return timeSinceStartup;
        }

        string SmallStringTime(double time)
        {
            TimeSpan t = TimeSpan.FromSeconds(time);

            if (t.Days > 0)
                return string.Format("{0:D1}d {1:D1}h {2:D2}m", t.Days, t.Hours, t.Minutes);
            else if (t.Hours > 0)
            {
                return string.Format("{0:D1}h {1:D2}m", t.Hours, t.Minutes);
            }
            else
            {
                if (t.Minutes > 0) //hour haven't reached
                {
                    return string.Format("{0:D2}m {1:D2}s", t.Minutes, t.Seconds);
                }
                else //minute haven't reached
                {
                    if (t.Seconds > 0)
                        return string.Format("{0:D2}s", t.Seconds);
                    else
                        return string.Format("{0:D2}ms", t.Milliseconds);
                }
            }
            //return string.Format("{0:D1}h {1:D2}m {2:D1}s", t.Hours, t.Minutes, t.Seconds);
        }
    }
}