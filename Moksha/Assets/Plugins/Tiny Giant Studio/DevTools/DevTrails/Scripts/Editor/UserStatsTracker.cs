using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TinyGiantStudio.DevTools.DevTrails
{
    /// <summary>
    /// Handles adding and removing listeners required for UserStats class
    /// </summary>
    [InitializeOnLoad]
    public class UserStatsTracker
    {
        private const string CompileKeyPrefix = "TGS_EditorCompileTimer";
        private const string ReloadKey = "TGS_EditorDomainReloadStart";

        static UserStatsTracker()
        {
            //Load previous data first
            UserStats_Global.instance.LoadFromDisk();
            UserStats_Today.instance.LoadFromDisk();

            //DevTrails settings is just a wrapper for a bunch of EditorPref calls. That's why it is being created.
            DevTrailSettings settings = new();

            Application.logMessageReceivedThreaded -= OnLogMessageReceived;
            if (settings.TrackConsoleLogs)
            {
                //Application.logMessageReceived += OnLogMessageReceived; //This event only ever triggers on the main thread.
                Application.logMessageReceivedThreaded += OnLogMessageReceived; //This event will be triggered regardless of whether the message comes in on the main thread or not.
            }

            EditorSceneManager.sceneOpened -= SceneOpened;
            if (settings.TrackSceneOpen)
                EditorSceneManager.sceneOpened += SceneOpened;

            EditorSceneManager.sceneClosed -= SceneClosed;
            if (settings.TrackSceneClose)
                EditorSceneManager.sceneClosed += SceneClosed;

            EditorSceneManager.sceneSaved -= SceneSaved;
            if (settings.TrackSceneSave)
                EditorSceneManager.sceneSaved += SceneSaved;

            EditorApplication.playModeStateChanged -= PlayModeStateChanged;
            if (settings.TrackPlayMode)
                EditorApplication.playModeStateChanged += PlayModeStateChanged;
            //EditorApplicationUtility.onEnterPlayMode += EnterPlayMode; //Doesn't work for some reason

            EditorApplication.wantsToQuit -= WantsToQuit;
            if (settings.TrackTime)
            {
                EditorApplication.wantsToQuit += WantsToQuit;
                //EditorApplication.quitting += Quitting; //Session info resets by the time this is called.
            }

            if (settings.TrackEditorCrashes)
            {
                UserStats_Project projectStat = UserStats_Project.instance;

                //IDCache gets set to zero when it quits properly. So, if it's not zero, the editor probably didn't exit correctly.
                if (projectStat.currentSessionIDCache != EditorAnalyticsSessionInfo.id && projectStat.currentSessionIDCache != 0)
                {
                    //While using the test editor crash code, it only crashed when the scene was unsaved.
                    //Debug.Log("Editor crashed probably");
                    projectStat.probablyCrashes++;
                    UserStats_Global.instance.ProbableCrashes++;
                    UserStats_Today.instance.ProbableCrashes++;
                }

                projectStat.currentSessionIDCache = EditorAnalyticsSessionInfo.id;
                projectStat.Save();
            }

            CompilationPipeline.compilationStarted -= OnCompilationStarted;
            CompilationPipeline.compilationFinished -= OnCompilationFinished;
            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeReload;
            AssemblyReloadEvents.afterAssemblyReload -= OnAfterReload;

            if (settings.TrackCompilation)
            {
                CompilationPipeline.compilationStarted += OnCompilationStarted;
                CompilationPipeline.compilationFinished += OnCompilationFinished;
                AssemblyReloadEvents.beforeAssemblyReload += OnBeforeReload;
                AssemblyReloadEvents.afterAssemblyReload += OnAfterReload;
            }

            Undo.undoRedoPerformed -= UndoRedoPerformed;
            if (settings.TrackUndoRedo) Undo.undoRedoPerformed += UndoRedoPerformed;
        }

        static ConcurrentQueue<LogType> _logTypes = new();

        private static void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            _logTypes ??= new();

            if (_logTypes.Count == 0)
                EditorApplication.update += OnEditorUpdate;

            _logTypes.Enqueue(type);
        }

        private static void OnEditorUpdate()
        {
            if (_logTypes.Count == 0)
            {
                EditorApplication.update -= OnEditorUpdate;
                return;
            }

            int i = 0;
            while (i < 10 && _logTypes.TryDequeue(out var item))
            {
                i++;
                ProcessLog(item);
            }

            if (_logTypes.Count == 0)
                EditorApplication.update -= OnEditorUpdate;
        }

        private static void ProcessLog(LogType type)
        {
            switch (type)
            {
                case LogType.Log:
                    if (Application.isPlaying)
                    {
                        UserStats_Project.instance.LogCounter_playMode++;
                        UserStats_Today.instance.LogCounter_playMode++;
                        UserStats_Global.instance.LogCounterPlayMode++;
                    }
                    else
                    {
                        UserStats_Project.instance.LogCounter_editor++;
                        UserStats_Today.instance.LogCounter_editor++;
                        UserStats_Global.instance.LogCounterEditor++;
                    }

                    break;

                case LogType.Warning:
                    if (Application.isPlaying)
                    {
                        UserStats_Project.instance.WarningLogCounter_playMode++;
                        UserStats_Today.instance.WarningLogCounter_playMode++;
                        UserStats_Global.instance.WarningLogCounterPlayMode++;
                    }
                    else
                    {
                        UserStats_Project.instance.WarningLogCounter_editor++;
                        UserStats_Today.instance.WarningLogCounter_editor++;
                        UserStats_Global.instance.WarningLogCounterEditor++;
                    }

                    break;

                case LogType.Exception:
                    if (Application.isPlaying)
                    {
                        UserStats_Project.instance.ExceptionLogCounter_playMode++;
                        UserStats_Today.instance.ExceptionLogCounter_playMode++;
                        UserStats_Global.instance.ExceptionLogCounterPlayMode++;
                    }
                    else
                    {
                        UserStats_Project.instance.ExceptionLogCounter_editor++;
                        UserStats_Today.instance.ExceptionLogCounter_editor++;
                        UserStats_Global.instance.ExceptionLogCounterEditor++;
                    }

                    break;

                case LogType.Error:
                    if (Application.isPlaying)
                    {
                        UserStats_Project.instance.ErrorLogCounter_playMode++;
                        UserStats_Today.instance.ErrorLogCounter_playMode++;
                        UserStats_Global.instance.ErrorLogCounterPlayMode++;
                    }
                    else
                    {
                        UserStats_Project.instance.ErrorLogCounter_editor++;
                        UserStats_Today.instance.ErrorLogCounter_editor++;
                        UserStats_Global.instance.ErrorLogCounterEditor++;
                    }

                    break;
            }
        }

        private static void UndoRedoPerformed()
        {
            UserStats_Project.instance.UndoRedoCounter++;
            UserStats_Today.instance.UndoRedoCounter++;
            UserStats_Global.instance.UndoRedoCounter++;
        }

        private static void OnCompilationStarted(object obj)
        {
            string key = GetCompileKey(obj);
            SessionState.SetFloat(key, (float)EditorApplication.timeSinceStartup);
        }

        static void OnCompilationFinished(object context)
        {
            string key = GetCompileKey(context);

            if (!TryGetSessionFloat(key, out float startTime))
                return;

            UserStats_Project.instance.CompileCounter++;
            UserStats_Today.instance.CompileCounter++;
            UserStats_Global.instance.CompileCounter++;

            double compileTime = EditorApplication.timeSinceStartup - startTime;
            SessionState.EraseFloat(key);

            UserStats_Project.instance.TimeSpentCompiling += (float)compileTime;
            UserStats_Today.instance.TimeSpentCompiling += (float)compileTime;
            UserStats_Global.instance.TimeSpentCompiling += (float)compileTime;
        }

        static void OnBeforeReload()
        {
            SessionState.SetFloat(ReloadKey, (float)EditorApplication.timeSinceStartup);
        }


        static void OnAfterReload()
        {
            if (TryGetSessionFloat(ReloadKey, out float startTime))
            {
                double elapsed = EditorApplication.timeSinceStartup - startTime;
                SessionState.EraseFloat(ReloadKey);
                
                UserStats_Project.instance.TimeSpentInDomainReload += (float)elapsed;
                UserStats_Today.instance.TimeSpentInDomainReload += (float)elapsed;
                UserStats_Global.instance.TimeSpentInDomainReload += (float)elapsed;
            }
        }

        static string GetCompileKey(object context)
        {
            return CompileKeyPrefix + (context?.GetHashCode().ToString() ?? "Default");
        }

        static bool TryGetSessionFloat(string key, out float value)
        {
            value = SessionState.GetFloat(key, float.NaN); // default NaN = “not set”
            return !float.IsNaN(value);
        }
        //private static void FocusChanged(bool obj)
        //{
        //    Debug.Log("Focus changed to " + obj);
        //}

        //private static void Update()
        //{
        //    //Debug.Log("Update");
        //}

        private static bool WantsToQuit()
        {
            DevTrailSettings settings = new DevTrailSettings();

            settings.PauseTimeTracking = false;

            UserStats_Project project = UserStats_Project.instance;
            UserStats_Global global = UserStats_Global.instance;
            UserStats_Today today = UserStats_Today.instance;

            int timeSinceStartup = (int)EditorApplication.timeSinceStartup;
            int focusedElapsedTime = (int)TimeSpan.FromMilliseconds(EditorAnalyticsSessionInfo.focusedElapsedTime).TotalSeconds;
            int activeElapsedTime = (int)TimeSpan.FromMilliseconds(EditorAnalyticsSessionInfo.activeElapsedTime).TotalSeconds;

            if (project.PauseRecords.sessionID == EditorAnalyticsSessionInfo.id)
            {
                for (int i = 0; i < project.PauseRecords.sessions.Count; i++)
                {
                    timeSinceStartup -= project.PauseRecords.sessions[i].usageTime;
                    focusedElapsedTime -= project.PauseRecords.sessions[i].focusedTime;
                    activeElapsedTime -= project.PauseRecords.sessions[i].activeTime;
                }

                //Unnecessary precaution. But, better safe than sorry.
                timeSinceStartup = Math.Abs(timeSinceStartup);
                focusedElapsedTime = Math.Abs(focusedElapsedTime);
                activeElapsedTime = Math.Abs(activeElapsedTime);
            }

            project.activeUseTime += activeElapsedTime;
            global.activeUseTime += activeElapsedTime;

            project.focusedUseTime += focusedElapsedTime;
            global.focusedUseTime += focusedElapsedTime;

            project.totalUseTime += timeSinceStartup;
            global.totalUseTime += timeSinceStartup;

            global.EditorSessionEnded(EditorAnalyticsSessionInfo.id, timeSinceStartup, focusedElapsedTime, activeElapsedTime);

            //project.Save(); //ApplicationExited method saves project stats
            global.SaveToDisk();
            today.SaveToDisk();

            project.currentSessionIDCache = 0;
            if (project.PauseRecords != null)
            {
                project.PauseRecords.sessions?.Clear();
                project.PauseRecords.sessionID = 0;
            }

            project.Save();

            return true;
        }

        private static double _timeOfEnteringPlaymode;

        private static void PlayModeStateChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredPlayMode)
            {
                _timeOfEnteringPlaymode = EditorApplication.timeSinceStartup;

                UserStats_Project.instance.EnteredPlayMode++;
                UserStats_Today.instance.EnteredPlayMode++;
                UserStats_Global.instance.EnteredPlayMode++;
            }
            else if (change == PlayModeStateChange.ExitingPlayMode)
            {
                if (_timeOfEnteringPlaymode == 0)
                    return;

                int playModeUseTimeThisSession = (int)(EditorApplication.timeSinceStartup - _timeOfEnteringPlaymode);

                UserStats_Project.instance.PlayModeUseTime += playModeUseTimeThisSession;
                UserStats_Today.instance.PlayModeUseTime += playModeUseTimeThisSession;
                UserStats_Global.instance.PlayModeUseTime += playModeUseTimeThisSession;

                _timeOfEnteringPlaymode = 0;
            }
        }

        private static void SceneSaved(Scene scene)
        {
            UserStats_Project.instance.SceneSaved++;
            UserStats_Today.instance.SceneSaved++;
            UserStats_Global.instance.SceneSaved++;
        }

        private static void SceneOpened(Scene scene, OpenSceneMode mode)
        {
            UserStats_Project.instance.SceneOpened++;
            UserStats_Today.instance.SceneOpened++;
            UserStats_Global.instance.SceneOpened++;
        }

        private static void SceneClosed(Scene scene)
        {
            UserStats_Project.instance.SceneClosed++;
            UserStats_Today.instance.SceneClosed++;
            UserStats_Global.instance.SceneClosed++;
        }

        [SerializeField] private static Queue<SessionInfo> sessionInformations;

        [Serializable]
        private class SessionInfo
        {
            public int sessionID;
            public int focusedTime;
            public int activeTime;
        }
    }
}