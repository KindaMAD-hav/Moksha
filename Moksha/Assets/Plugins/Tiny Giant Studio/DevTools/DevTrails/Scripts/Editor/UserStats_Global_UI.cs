using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace TinyGiantStudio.DevTools.DevTrails
{
    // ReSharper disable once InconsistentNaming
    public class UserStats_Global_UI
    {
        readonly DevTrailSettings _devTrailSettings;

        readonly UserStats_Global _userStats;
        readonly UserStats_Project _userStatsProject;

        readonly Label _globalSessionCount;

        readonly Label _unityUsageTimeGlobal;
        readonly Label _activeUsageTime;
        readonly Label _focusedUsageTime;

        readonly Label _compileTime;
        readonly Label _domainReloadTime;
        readonly Label _averageCompileTime;
        readonly GroupBox _globalCompilationGroupBox;
        readonly Label _globalCompileCounter;
        readonly GroupBox _globalConsoleLogGroupBox;
        readonly Label _globalErrorLogCounterEditor;
        readonly Label _globalErrorLogCounterPlayMode;
        readonly Label _globalExceptionLogCounterEditor;
        readonly Label _globalExceptionLogCounterPlayMode;
        readonly Label _globalNormalLogCounterEditor;
        readonly Label _globalNormalLogCounterPlayMode;
        readonly GroupBox _globalPlayModeGroupBox;
        readonly Label _globalPlayModeCounter;
        readonly Label _globalPlayModeUsageTime;
        readonly GroupBox _globalSceneOpenedGroupBox;
        readonly Label _globalSceneOpenedCounter;
        readonly GroupBox _globalSceneSavedGroupBox;
        readonly Label _globalSceneSavedCounter;
        readonly GroupBox _globalTimeStatsGroupBox;
        readonly GroupBox _globalUndoRedoGroupBox;
        readonly Label _globalUndoRedoCounter;
        readonly Label _globalWarningLogCounterEditor;
        readonly Label _globalWarningLogCounterPlayMode;

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


        public UserStats_Global_UI(VisualElement container)
        {
            _devTrailSettings = new();

            _userStats = UserStats_Global.instance;
            if (_userStats.TotalTimeSpentInDomainReload < 0) _userStats.TotalTimeSpentInDomainReload = 0;
            _userStatsProject = UserStats_Project.instance;

            _globalTimeStatsGroupBox = container.Q<GroupBox>("TimeStats");

            _globalSessionCount = container.Q<Label>("SessionCount");

            _unityUsageTimeGlobal = container.Q<Label>("UsageTime");
            _focusedUsageTime = container.Q<Label>("FocusedUsageTime");
            _activeUsageTime = container.Q<Label>("ActiveUsageTime");

            _globalPlayModeGroupBox = container.Q<GroupBox>("PlayMode");
            _globalPlayModeCounter = container.Q<Label>("PlayModeCounter");
            _globalPlayModeUsageTime = container.Q<Label>("PlayModeUsageTime");

            _globalSceneSavedGroupBox = container.Q<GroupBox>("SceneSaved");
            _globalSceneSavedCounter = container.Q<Label>("SceneSavedCounter");
            _globalSceneOpenedGroupBox = container.Q<GroupBox>("SceneOpened");
            _globalSceneOpenedCounter = container.Q<Label>("SceneOpenedCounter");

            _globalUndoRedoGroupBox = container.Q<GroupBox>("UndoRedo");
            _globalUndoRedoCounter = container.Q<Label>("UndoRedoCounter");

            _globalCompilationGroupBox = container.Q<GroupBox>("Compilation");
            _globalCompileCounter = container.Q<Label>("CompileCounter");
            _averageCompileTime = container.Q<Label>("AverageCompileTime");
            _compileTime = container.Q<Label>("CompileTime");
            _domainReloadTime = container.Q<Label>("DomainReloadTime");

            _globalConsoleLogGroupBox = container.Q<GroupBox>("ConsoleLogs");

            _globalNormalLogCounterPlayMode = container.Q<Label>("NormalLogCounter_PlayMode");
            _globalWarningLogCounterPlayMode = container.Q<Label>("WarningLogCounter_PlayMode");
            _globalExceptionLogCounterPlayMode = container.Q<Label>("ExceptionLogCounter_PlayMode");
            _globalErrorLogCounterPlayMode = container.Q<Label>("ErrorLogCounter_PlayMode");

            _globalNormalLogCounterEditor = container.Q<Label>("NormalLogCounter_Editor");
            _globalWarningLogCounterEditor = container.Q<Label>("WarningLogCounter_Editor");
            _globalExceptionLogCounterEditor = container.Q<Label>("ExceptionLogCounter_Editor");
            _globalErrorLogCounterEditor = container.Q<Label>("ErrorLogCounter_Editor");

            _devToolsWindowOpenedCounterGroupBox = container.Q<GroupBox>("DevToolsEditorWindowOpened");
            _devToolsEditorWindowOpenedCounter = _devToolsWindowOpenedCounterGroupBox.Q<Label>("DevToolsEditorWindowOpenedCounter");

            _editorCrashesGroupBox = container.Q<GroupBox>("EditorCrashes");
            _editorCrashesGroupBox.Q<Label>("EditorCrashCounter").text = _userStats.ProbableCrashes.ToString();
            
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
            if (_devTrailSettings.TrackTime)
                _globalTimeStatsGroupBox.style.display = DisplayStyle.Flex;
            else
                _globalTimeStatsGroupBox.style.display = DisplayStyle.None;

            if (_devTrailSettings.TrackSceneSave)
                _globalSceneSavedGroupBox.style.display = DisplayStyle.Flex;
            else
                _globalSceneSavedGroupBox.style.display = DisplayStyle.None;

            if (_devTrailSettings.TrackSceneOpen)
                _globalSceneOpenedGroupBox.style.display = DisplayStyle.Flex;
            else
                _globalSceneOpenedGroupBox.style.display = DisplayStyle.None;

            if (_devTrailSettings.TrackUndoRedo)
                _globalUndoRedoGroupBox.style.display = DisplayStyle.Flex;
            else
                _globalUndoRedoGroupBox.style.display = DisplayStyle.None;

            if (_devTrailSettings.TrackPlayMode)
                _globalPlayModeGroupBox.style.display = DisplayStyle.Flex;
            else
                _globalPlayModeGroupBox.style.display = DisplayStyle.None;

            if (_devTrailSettings.TrackCompilation)
                _globalCompilationGroupBox.style.display = DisplayStyle.Flex;
            else
                _globalCompilationGroupBox.style.display = DisplayStyle.None;

            if (_devTrailSettings.TrackConsoleLogs)
                _globalConsoleLogGroupBox.style.display = DisplayStyle.Flex;
            else
                _globalConsoleLogGroupBox.style.display = DisplayStyle.None;

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
            _globalSessionCount.text = BetterString.Number((int)EditorAnalyticsSessionInfo.sessionCount);

            _unityUsageTimeGlobal.text = SmallStringTime(_userStats.totalUseTime + CurrentSessionUseTime());
            _focusedUsageTime.text = SmallStringTime(_userStats.focusedUseTime + CurrentSessionFocusedUseTime());
            _activeUsageTime.text = SmallStringTime(_userStats.activeUseTime + CurrentSessionActiveUseTime());

            _globalPlayModeCounter.text = BetterString.Number(_userStats.EnteredPlayMode);
            _globalPlayModeUsageTime.text = SmallStringTime(_userStats.PlayModeUseTime);

            _globalSceneSavedCounter.text = BetterString.Number(_userStats.SceneSaved);
            _globalSceneOpenedCounter.text = BetterString.Number(_userStats.SceneOpened);

            _globalUndoRedoCounter.text = BetterString.Number(_userStats.UndoRedoCounter);

            _globalCompileCounter.text = BetterString.Number(_userStats.CompileCounter);

            _averageCompileTime.text = _userStats.CompileCounter == 0 ? "Not recorded" : SmallStringTime((_userStats.TotalTimeSpentCompiling + _userStats.TimeSpentCompiling) / _userStats.CompileCounter);

            _compileTime.text = SmallStringTime(_userStats.TotalTimeSpentCompiling + _userStats.TimeSpentCompiling);
            _domainReloadTime.text = SmallStringTime(Mathf.Abs(_userStats.TotalTimeSpentInDomainReload) + _userStats.TimeSpentInDomainReload); //Added Mathf.Abs for just in case type situation. Remove later.

            _globalNormalLogCounterPlayMode.text = BetterString.Number(_userStats.LogCounterPlayMode);
            _globalWarningLogCounterPlayMode.text = BetterString.Number(_userStats.WarningLogCounterPlayMode);
            _globalExceptionLogCounterPlayMode.text = BetterString.Number(_userStats.ExceptionLogCounterPlayMode);
            _globalErrorLogCounterPlayMode.text = BetterString.Number(_userStats.ErrorLogCounterPlayMode);

            _globalNormalLogCounterEditor.text = BetterString.Number(_userStats.LogCounterEditor);
            _globalWarningLogCounterEditor.text = BetterString.Number(_userStats.WarningLogCounterEditor);
            _globalExceptionLogCounterEditor.text = BetterString.Number(_userStats.ExceptionLogCounterEditor);
            _globalErrorLogCounterEditor.text = BetterString.Number(_userStats.ErrorLogCounterEditor);

            _devToolsEditorWindowOpenedCounter.text = BetterString.Number(_userStats.DevToolsEditorWindowOpened);
            
            BuildStatsRetriever.UpdateBuildInfo(
                _userStats.BuildRecords, _totalBuildTime, _averageBuildTime, _totalBuilds, _successfulBuilds,
                _failedBuilds, _canceledBuilds, _warningBuildLogs, _errorBuildLogs);
        }

        int CurrentSessionActiveUseTime()
        {
            int activeElapsedTime = (int)TimeSpan.FromMilliseconds(EditorAnalyticsSessionInfo.activeElapsedTime).TotalSeconds;

            //Currently paused
            if (_devTrailSettings.PauseTimeTracking)
            {
                var pauseDuration = (int)TimeSpan.FromMilliseconds(EditorAnalyticsSessionInfo.activeElapsedTime).TotalSeconds - EditorPrefs.GetInt("PauseStartActiveTime");
                activeElapsedTime -= pauseDuration;
            }

            if (_userStatsProject.PauseRecords.sessionID != EditorAnalyticsSessionInfo.id)
                return activeElapsedTime;

            for (int i = 0; i < _userStatsProject.PauseRecords.sessions.Count; i++)
            {
                activeElapsedTime -= _userStatsProject.PauseRecords.sessions[i].usageTime;
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

            if (_userStatsProject.PauseRecords.sessionID != EditorAnalyticsSessionInfo.id)
                return focusedElapsedTime;

            for (int i = 0; i < _userStatsProject.PauseRecords.sessions.Count; i++)
            {
                focusedElapsedTime -= _userStatsProject.PauseRecords.sessions[i].focusedTime;
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
            if (_userStatsProject.PauseRecords.sessionID != EditorAnalyticsSessionInfo.id)
                return timeSinceStartup;

            for (int i = 0; i < _userStatsProject.PauseRecords.sessions.Count; i++)
            {
                timeSinceStartup -= _userStatsProject.PauseRecords.sessions[i].usageTime;
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