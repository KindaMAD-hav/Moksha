using System;
using UnityEditor;

namespace TinyGiantStudio.DevTools.DevTrails
{
    //Note to self: Do not convert it back to scriptableInstance. When the static scripts checked this, it would bug out and reset
    //But EditorPrefs are global per user, per machine, not per project
    //TODO Consider converting it into a static class since it's just a wrapper to use editor prefs easily
    /// <summary>
    ///
    /// </summary>
    public class DevTrailSettings
    {
        public bool TrackTime
        {
            get { return EditorPrefs.GetBool("devTrack_trackTime", true); }
            set { EditorPrefs.SetBool("devTrack_trackTime", value); }
        }

        public bool PauseTimeTracking
        {
            get { return EditorPrefs.GetBool("devTrails_pauseTimeTracking", false); }
            set
            {
                EditorPrefs.SetBool("devTrails_pauseTimeTracking", value);
                if (value)
                {
                    EditorPrefs.SetInt("PauseStartUsageTime", (int)EditorApplication.timeSinceStartup);
                    EditorPrefs.SetInt("PauseStartFocusedTime", (int)TimeSpan.FromMilliseconds(EditorAnalyticsSessionInfo.focusedElapsedTime).TotalSeconds);
                    EditorPrefs.SetInt("PauseStartActiveTime", (int)TimeSpan.FromMilliseconds(EditorAnalyticsSessionInfo.activeElapsedTime).TotalSeconds);
                }
                else
                {
                    if (EditorPrefs.HasKey("PauseStartUsageTime"))
                    {
                        UserStats_Project project = UserStats_Project.instance;

                        if (project.PauseRecords.sessionID != EditorAnalyticsSessionInfo.id)
                        {
                            project.PauseRecords.sessionID = EditorAnalyticsSessionInfo.id;
                            project.PauseRecords.sessions.Clear();
                        }

                        UserStats_Project.PauseSession pauseSession = new();
                        pauseSession.usageTime = (int)EditorApplication.timeSinceStartup - EditorPrefs.GetInt("PauseStartUsageTime");
                        pauseSession.focusedTime = (int)TimeSpan.FromMilliseconds(EditorAnalyticsSessionInfo.focusedElapsedTime).TotalSeconds - EditorPrefs.GetInt("PauseStartFocusedTime");
                        pauseSession.activeTime = (int)TimeSpan.FromMilliseconds(EditorAnalyticsSessionInfo.activeElapsedTime).TotalSeconds - EditorPrefs.GetInt("PauseStartActiveTime");

                        project.PauseRecords.sessions.Add(pauseSession);
                        project.Save();

                        EditorPrefs.DeleteKey("PauseStartUsageTime");
                        EditorPrefs.DeleteKey("PauseStartFocusedTime");
                        EditorPrefs.DeleteKey("PauseStartActiveTime");

                        //for (int i = 0; i < project.PauseRecords.sessions.Count; i++)
                        //{
                        //    //Debug.Log("Usage time:" + project.PauseRecords.sessions[i].usageTime + " Focused time:" + project.PauseRecords.sessions[i].focusedTime + " Active time:" + project.PauseRecords.sessions[i].activeTime);
                        //}
                    }
                }
            }
        }

        public bool EnabledUsageGoal
        {
            get { return EditorPrefs.GetBool("devTrails_enabledUsageGoal", false); }
            set { EditorPrefs.SetBool("devTrails_enabledUsageGoal", value); }
        }

        public int UsageGoal
        {
            get { return EditorPrefs.GetInt("devTrails_usageGoal", 21600); }
            set { EditorPrefs.SetInt("devTrails_usageGoal", value); }
        }

        public bool UsageGoalPopUp
        {
            get { return EditorPrefs.GetBool("devTrails_usageGoalPopUp", false); }
            set { EditorPrefs.SetBool("devTrails_usageGoalPopUp", value); }
        }

        public bool TrackPlayMode
        {
            get { return EditorPrefs.GetBool("devTrack_trackPlayMode", true); }
            set { EditorPrefs.SetBool("devTrack_trackPlayMode", value); }
        }

        public bool TrackSceneOpen
        {
            get { return EditorPrefs.GetBool("devTrack_trackConsoleLogs", true); }
            set { EditorPrefs.SetBool("devTrack_trackConsoleLogs", value); }
        }

        public bool TrackSceneClose
        {
            get { return EditorPrefs.GetBool("devTrack_trackConsoleLogs", true); }
            set { EditorPrefs.SetBool("devTracktrackConsoleLogs_", value); }
        }

        public bool TrackSceneSave
        {
            get { return EditorPrefs.GetBool("devTrack_trackConsoleLogs", true); }
            set { EditorPrefs.SetBool("devTrack_trackConsoleLogs", value); }
        }

        public bool TrackCompilation
        {
            get { return EditorPrefs.GetBool("devTrack_trackConsoleLogs", true); }
            set { EditorPrefs.SetBool("devTrack_trackConsoleLogs", value); }
        }

        public bool TrackUndoRedo
        {
            get { return EditorPrefs.GetBool("devTrack_trackConsoleLogs", true); }
            set { EditorPrefs.SetBool("devTrack_trackConsoleLogs", value); }
        }

        public bool TrackConsoleLogs
        {
            get { return EditorPrefs.GetBool("devTrack_trackConsoleLogs", true); }
            set { EditorPrefs.SetBool("devTrack_trackConsoleLogs", value); }
        }

        public bool ShowDevToolsEditorWindowTrack
        {
            get { return EditorPrefs.GetBool("devTrack_showDevToolsEditorWindowTrack", false); }
            set { EditorPrefs.SetBool("devTrack_showDevToolsEditorWindowTrack", value); }
        }

        public bool TrackEditorCrashes
        {
            get { return EditorPrefs.GetBool("devTrack_editorCrashesTrack", false); }
            set { EditorPrefs.SetBool("devTrack_editorCrashesTrack", value); }
        }

        public void Reset()
        {
            TrackTime = true;
            TrackPlayMode = true;
            TrackSceneOpen = true;
            TrackSceneClose = true;
            TrackSceneSave = true;
            TrackCompilation = true;
            TrackUndoRedo = true;
            TrackConsoleLogs = true;
            ShowDevToolsEditorWindowTrack = false;
        }
    }
}