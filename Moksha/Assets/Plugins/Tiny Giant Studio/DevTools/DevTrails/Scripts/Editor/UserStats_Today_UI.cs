using System;
using UnityEditor;
using UnityEngine.UIElements;

namespace TinyGiantStudio.DevTools.DevTrails
{
    public class UserStatsTodayUI
    {
        readonly UserStats_Today _userStats;
        readonly UserStats_Project _userStatsProject;
        readonly UserStats_Global _userStatsGlobal;

        readonly DevTrailSettings _devTrailSettings;

        readonly GroupBox _todayTimeStatsGroupBox;

        readonly Label _usageTimeThisSession;
        readonly Label _focusedUsageTimeThisSession;
        readonly Label _activeUsageTimeThisSession;
        readonly Label _usageTimeToday;
        readonly Label _focusedUsageTimeToday;
        readonly Label _activeUsageTimeToday;

        readonly GroupBox _todaySceneSavedGroupBox;
        readonly Label _todaySceneSavedCounter;
        readonly GroupBox _todaySceneOpenedGroupBox;
        readonly Label _todaySceneOpenedCounter;

        readonly GroupBox _todayPlayModeGroupBox;
        readonly Label _todayPlayModeCounter;
        readonly Label _todayPlayModeUsageTime;

        readonly GroupBox _todayUndoRedoGroupBox;
        readonly Label _todayUndoRedoCounter;

        readonly GroupBox _compilationGroupBox;
        readonly Label _compileCounter;
        readonly Label _averageCompileTime;
        readonly Label _compileTime;
        readonly Label _domainReloadTime;

        readonly GroupBox _consoleLogGroupBox;

        readonly Label _normalLogCounterPlayMode;
        readonly Label _warningLogCounterPlayMode;
        readonly Label _exceptionLogCounterPlayMode;
        readonly Label _errorLogCounterPlayMode;

        readonly Label _normalLogCounterEditor;
        readonly Label _warningLogCounterEditor;
        readonly Label _exceptionLogCounterEditor;
        readonly Label _errorLogCounterEditor;

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

        public UserStatsTodayUI(VisualElement container)
        {
            _devTrailSettings = new();

            _userStats = UserStats_Today.instance;
            if (_userStats.TotalTimeSpentInDomainReload < 0) _userStats.TotalTimeSpentInDomainReload = 0;
            _userStatsProject = UserStats_Project.instance;
            _userStatsGlobal = UserStats_Global.instance;

            _todayTimeStatsGroupBox = container.Q<GroupBox>("TimeStats");

            _usageTimeThisSession = container.Q<Label>("UsageTimeSession");
            _focusedUsageTimeThisSession = container.Q<Label>("FocusedUsageTime");
            _activeUsageTimeThisSession = container.Q<Label>("ActiveUsageTime");
            _usageTimeToday = container.Q<Label>("UsageTimeSessionToday");
            _focusedUsageTimeToday = container.Q<Label>("FocusedUsageTimeToday");
            _activeUsageTimeToday = container.Q<Label>("ActiveUsageTimeToday");

            _todaySceneSavedGroupBox = container.Q<GroupBox>("SceneSaved");
            _todaySceneSavedCounter = container.Q<Label>("SceneSavedCounter");
            _todaySceneOpenedGroupBox = container.Q<GroupBox>("SceneOpened");
            _todaySceneOpenedCounter = container.Q<Label>("SceneOpenedCounter");

            _todayPlayModeGroupBox = container.Q<GroupBox>("PlayMode");
            _todayPlayModeCounter = container.Q<Label>("PlayModeCounter");
            _todayPlayModeUsageTime = container.Q<Label>("PlayModeUsageTime");

            _todayUndoRedoGroupBox = container.Q<GroupBox>("UndoRedo");
            _todayUndoRedoCounter = container.Q<Label>("UndoRedoCounter");

            _compilationGroupBox = container.Q<GroupBox>("Compilation");
            _compileCounter = container.Q<Label>("CompileCounter");
            _averageCompileTime = container.Q<Label>("AverageCompileTime");
            _compileTime = container.Q<Label>("CompileTime");
            _domainReloadTime = container.Q<Label>("DomainReloadTime");

            _consoleLogGroupBox = container.Q<GroupBox>("ConsoleLogs");
            _normalLogCounterEditor = container.Q<Label>("NormalLogCounter_Editor");
            _warningLogCounterEditor = container.Q<Label>("WarningLogCounter_Editor");
            _exceptionLogCounterEditor = container.Q<Label>("ExceptionLogCounter_Editor");
            _errorLogCounterEditor = container.Q<Label>("ErrorLogCounter_Editor");

            _normalLogCounterPlayMode = container.Q<Label>("NormalLogCounter_PlayMode");
            _warningLogCounterPlayMode = container.Q<Label>("WarningLogCounter_PlayMode");
            _exceptionLogCounterPlayMode = container.Q<Label>("ExceptionLogCounter_PlayMode");
            _errorLogCounterPlayMode = container.Q<Label>("ErrorLogCounter_PlayMode");

            _devToolsWindowOpenedCounterGroupBox = container.Q<GroupBox>("DevToolsEditorWindowOpened");
            _devToolsEditorWindowOpenedCounter =
                _devToolsWindowOpenedCounterGroupBox.Q<Label>("DevToolsEditorWindowOpenedCounter");


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
                _todayTimeStatsGroupBox.style.display = DisplayStyle.Flex;
            else
                _todayTimeStatsGroupBox.style.display = DisplayStyle.None;

            if (_devTrailSettings.TrackSceneSave)
                _todaySceneSavedGroupBox.style.display = DisplayStyle.Flex;
            else
                _todaySceneSavedGroupBox.style.display = DisplayStyle.None;

            if (_devTrailSettings.TrackSceneOpen)
                _todaySceneOpenedGroupBox.style.display = DisplayStyle.Flex;
            else
                _todaySceneOpenedGroupBox.style.display = DisplayStyle.None;

            if (_devTrailSettings.TrackUndoRedo)
                _todayUndoRedoGroupBox.style.display = DisplayStyle.Flex;
            else
                _todayUndoRedoGroupBox.style.display = DisplayStyle.None;

            if (_devTrailSettings.TrackPlayMode)
                _todayPlayModeGroupBox.style.display = DisplayStyle.Flex;
            else
                _todayPlayModeGroupBox.style.display = DisplayStyle.None;

            if (_devTrailSettings.TrackCompilation)
                _compilationGroupBox.style.display = DisplayStyle.Flex;
            else
                _compilationGroupBox.style.display = DisplayStyle.None;

            if (_devTrailSettings.TrackConsoleLogs)
                _consoleLogGroupBox.style.display = DisplayStyle.Flex;
            else
                _consoleLogGroupBox.style.display = DisplayStyle.None;

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
            _userStats.VerifyNewDay(true);
            _usageTimeThisSession.text = SmallStringTime(CurrentSessionUsageTime());
            _usageTimeToday.text = SmallStringTime(CurrentSessionUsageTime() + TimeSpentInUnityToday());
            _focusedUsageTimeThisSession.text = SmallStringTime(CurrentSessionFocusedUseTime());
            _focusedUsageTimeToday.text =
                SmallStringTime(CurrentSessionFocusedUseTime() + FocusedTimeSpentInUnityToday());
            _activeUsageTimeThisSession.text = SmallStringTime(CurrentSessionActiveUseTime());
            _activeUsageTimeToday.text = SmallStringTime(CurrentSessionActiveUseTime() + ActiveTimeSpentInUnityToday());

            _todayPlayModeCounter.text = BetterString.Number(_userStats.EnteredPlayMode);
            _todayPlayModeUsageTime.text = SmallStringTime(_userStats.PlayModeUseTime);

            _todaySceneSavedCounter.text = BetterString.Number(_userStats.SceneSaved);
            _todaySceneOpenedCounter.text = BetterString.Number(_userStats.SceneOpened);

            _todayUndoRedoCounter.text = BetterString.Number(_userStats.UndoRedoCounter);

            _compileCounter.text = BetterString.Number(_userStats.CompileCounter);

            _averageCompileTime.text = _userStats.CompileCounter == 0
                ? "Not recorded"
                : SmallStringTime((_userStats.TotalTimeSpentCompiling + _userStats.TimeSpentCompiling) /
                                  _userStats.CompileCounter);

            _compileTime.text = SmallStringTime(_userStats.TotalTimeSpentCompiling + _userStats.TimeSpentCompiling);
            _domainReloadTime.text = SmallStringTime(MathF.Abs(_userStats.TotalTimeSpentInDomainReload) +
                                                     _userStats.TimeSpentInDomainReload);

            _normalLogCounterEditor.text = BetterString.Number(_userStats.LogCounter_editor);
            _warningLogCounterEditor.text = BetterString.Number(_userStats.WarningLogCounter_editor);
            _exceptionLogCounterEditor.text = BetterString.Number(_userStats.ExceptionLogCounter_editor);
            _errorLogCounterEditor.text = BetterString.Number(_userStats.ErrorLogCounter_editor);

            _normalLogCounterPlayMode.text = BetterString.Number(_userStats.LogCounter_playMode);
            _warningLogCounterPlayMode.text = BetterString.Number(_userStats.WarningLogCounter_playMode);
            _exceptionLogCounterPlayMode.text = BetterString.Number(_userStats.ExceptionLogCounter_playMode);
            _errorLogCounterPlayMode.text = BetterString.Number(_userStats.ErrorLogCounter_playMode);

            _devToolsEditorWindowOpenedCounter.text = BetterString.Number(_userStats.DevToolsEditorWindowOpened);

            BuildStatsRetriever.UpdateBuildInfo(
                _userStats.BuildRecords, _totalBuildTime, _averageBuildTime, _totalBuilds, _successfulBuilds,
                _failedBuilds, _canceledBuilds, _warningBuildLogs, _errorBuildLogs);
        }


        public double CurrentSessionUsageTime()
        {
            double timeSinceStartup = EditorApplication.timeSinceStartup;

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

        int TimeSpentInUnityToday()
        {
            if (_userStatsGlobal.DayRecords != null)
            {
                if (_userStatsGlobal.DayRecords.Count > 0)
                {
                    UserStats_Global.DayRecord lastRecord =
                        _userStatsGlobal.DayRecords[_userStatsGlobal.DayRecords.Count - 1];

                    if (lastRecord.date.SameDay(DateTime.Now))
                    {
                        int timeSpent = 0;
                        for (int i = 0; i < lastRecord.Sessions.Count; i++)
                        {
                            timeSpent += lastRecord.Sessions[i].totalTime;
                        }

                        return timeSpent;
                    }
                }
            }

            return 0;
        }

        int CurrentSessionFocusedUseTime()
        {
            int focusedElapsedTime =
                (int)TimeSpan.FromMilliseconds(EditorAnalyticsSessionInfo.focusedElapsedTime).TotalSeconds;

            //Currently paused
            if (_devTrailSettings.PauseTimeTracking)
            {
                var pauseDuration =
                    (int)TimeSpan.FromMilliseconds(EditorAnalyticsSessionInfo.focusedElapsedTime).TotalSeconds -
                    EditorPrefs.GetInt("PauseStartFocusedTime");
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

        int FocusedTimeSpentInUnityToday()
        {
            if (_userStatsGlobal.DayRecords != null)
            {
                if (_userStatsGlobal.DayRecords.Count > 0)
                {
                    UserStats_Global.DayRecord lastRecord =
                        _userStatsGlobal.DayRecords[_userStatsGlobal.DayRecords.Count - 1];

                    if (lastRecord.date.SameDay(DateTime.Now))
                    {
                        int timeSpent = 0;
                        for (int i = 0; i < lastRecord.Sessions.Count; i++)
                        {
                            timeSpent += lastRecord.Sessions[i].focusedUseTime;
                        }

                        return timeSpent;
                    }
                }
            }

            return 0;
        }

        int CurrentSessionActiveUseTime()
        {
            var activeElapsedTime =
                (int)TimeSpan.FromMilliseconds(EditorAnalyticsSessionInfo.activeElapsedTime).TotalSeconds;

            //Currently paused
            if (_devTrailSettings.PauseTimeTracking)
            {
                var pauseDuration =
                    (int)TimeSpan.FromMilliseconds(EditorAnalyticsSessionInfo.activeElapsedTime).TotalSeconds -
                    EditorPrefs.GetInt("PauseStartActiveTime");
                activeElapsedTime -= pauseDuration;
            }

            if (_userStatsProject.PauseRecords.sessionID != EditorAnalyticsSessionInfo.id)
                return activeElapsedTime;

            for (int i = 0; i < _userStatsProject.PauseRecords.sessions.Count; i++)
            {
                //activeElapsedTime -= userStats_project.PauseRecords.sessions[i].usageTime;
                activeElapsedTime -= _userStatsProject.PauseRecords.sessions[i].activeTime;
            }

            activeElapsedTime = Math.Abs(activeElapsedTime);

            return activeElapsedTime;
        }

        int ActiveTimeSpentInUnityToday()
        {
            if (_userStatsGlobal.DayRecords != null)
            {
                if (_userStatsGlobal.DayRecords.Count > 0)
                {
                    UserStats_Global.DayRecord lastRecord =
                        _userStatsGlobal.DayRecords[_userStatsGlobal.DayRecords.Count - 1];
                    if (lastRecord.date.SameDay(DateTime.Now))
                    {
                        int timeSpent = 0;
                        for (int i = 0; i < lastRecord.Sessions.Count; i++)
                        {
                            timeSpent += lastRecord.Sessions[i].activeUseTime;
                        }

                        return timeSpent;
                    }
                }
            }

            return 0;
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