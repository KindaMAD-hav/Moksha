using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TinyGiantStudio.DevTools.DevTrails
{
    // ReSharper disable once InconsistentNaming
    public class UserStats_Global : ScriptableSingleton<UserStats_Global>
    {
        #region Variables

        public int totalUseTime;
        public int activeUseTime;
        public int focusedUseTime;

        [SerializeField] int _enteredPlayMode;

        public int EnteredPlayMode
        {
            get => _enteredPlayMode;
            set
            {
                _enteredPlayMode = value;
                SaveToDisk();
                //Save();
            }
        }

        [SerializeField] int _playModeUseTime;

        public int PlayModeUseTime
        {
            get => _playModeUseTime;
            set
            {
                _playModeUseTime = value;
                SaveToDisk();
                //Save();
            }
        }

        [SerializeField] int _sceneSaved;

        public int SceneSaved
        {
            get => _sceneSaved;
            set
            {
                _sceneSaved = value;
                SaveToDisk();
                //Save();
            }
        }

        [SerializeField] int _sceneOpened;

        public int SceneOpened
        {
            get => _sceneOpened;
            set
            {
                _sceneOpened = value;
                SaveToDisk();
                //Save();
            }
        }

        [SerializeField] int _sceneClosed;

        public int SceneClosed
        {
            get => _sceneClosed;
            set
            {
                _sceneClosed = value;
                SaveToDisk();
                //Save();
            }
        }

        [SerializeField] int _undoRedoCounter;

        public int UndoRedoCounter
        {
            get => _undoRedoCounter;
            set
            {
                _undoRedoCounter = value;
                SaveToDisk();
                //Save();
            }
        }

        [SerializeField] int _compileCounter;

        public int CompileCounter
        {
            get => _compileCounter;
            set
            {
                _compileCounter = value;
                SaveToDisk();
                //Save();
            }
        }

        [SerializeField] int _totalTimeSpentCompiling;

        public int TotalTimeSpentCompiling
        {
            get => _totalTimeSpentCompiling;
            set
            {
                _totalTimeSpentCompiling = value;
                SaveToDisk();
                //Save();
            }
        }

        [SerializeField] float _timeSpentCompiling;

        public float TimeSpentCompiling
        {
            get => _timeSpentCompiling;
            set
            {
                _timeSpentCompiling = value;
                SaveToDisk();
                //Save();
            }
        }


        [SerializeField] int _totalTimeSpentInDomainReload;

        public int TotalTimeSpentInDomainReload
        {
            get => _totalTimeSpentInDomainReload;
            set
            {
                _totalTimeSpentInDomainReload = value;
                SaveToDisk();
            }
        }

        [SerializeField] float timeSpentInDomainReload;

        public float TimeSpentInDomainReload
        {
            get => timeSpentInDomainReload;
            set
            {
                timeSpentInDomainReload = value;
                SaveToDisk();
            }
        }

        #region Console Logs

        [SerializeField] int _logCounterEditor;

        public int LogCounterEditor
        {
            get => _logCounterEditor;
            set
            {
                _logCounterEditor = value;
                SaveToDisk();
                //Save();
            }
        }

        [SerializeField] int _logCounterPlayMode;

        public int LogCounterPlayMode
        {
            get => _logCounterPlayMode;
            set
            {
                _logCounterPlayMode = value;
                SaveToDisk();
                //Save();
            }
        }

        [SerializeField] int _warningLogCounterEditor;

        public int WarningLogCounterEditor
        {
            get => _warningLogCounterEditor;
            set
            {
                _warningLogCounterEditor = value;
                SaveToDisk();
                //Save();
            }
        }

        [SerializeField] int _warningLogCounterPlayMode;

        public int WarningLogCounterPlayMode
        {
            get => _warningLogCounterPlayMode;
            set
            {
                _warningLogCounterPlayMode = value;
                SaveToDisk();
                //Save();
            }
        }

        [SerializeField] int _errorLogCounterEditor;

        public int ErrorLogCounterEditor
        {
            get => _errorLogCounterEditor;
            set
            {
                _errorLogCounterEditor = value;
                SaveToDisk();
                //Save();
            }
        }

        [SerializeField] int _errorLogCounterPlayMode;

        public int ErrorLogCounterPlayMode
        {
            get => _errorLogCounterPlayMode;
            set
            {
                _errorLogCounterPlayMode = value;
                SaveToDisk();
                //Save();
            }
        }

        [SerializeField] int _exceptionLogCounterEditor;

        public int ExceptionLogCounterEditor
        {
            get => _exceptionLogCounterEditor;
            set
            {
                _exceptionLogCounterEditor = value;
                SaveToDisk();
                //Save();
            }
        }

        [SerializeField] int _exceptionLogCounterPlayMode;

        public int ExceptionLogCounterPlayMode
        {
            get => _exceptionLogCounterPlayMode;
            set
            {
                _exceptionLogCounterPlayMode = value;
                SaveToDisk();
                //Save();
            }
        }

        #endregion Console Logs

        [SerializeField] int _devToolsEditorWindowOpened;

        public int DevToolsEditorWindowOpened
        {
            get { return _devToolsEditorWindowOpened; }
            set
            {
                _devToolsEditorWindowOpened = value;
                SaveToDisk();
            }
        }

        public List<DayRecord> DayRecords = new();

        [SerializeField] int _probableCrashes;

        public int ProbableCrashes
        {
            get => _probableCrashes;
            set
            {
                _probableCrashes = value;
                SaveToDisk();
            }
        }

        [SerializeField] int _projectOpenCounter;

        public int ProjectOpenCounter
        {
            get => _projectOpenCounter;
            set
            {
                _projectOpenCounter = value;
                SaveToDisk();
            }
        }

        [SerializeField] int _totalTimeSpentOpeningProject;

        public int TotalTimeSpentOpeningProject
        {
            get => _totalTimeSpentOpeningProject;
            set
            {
                _totalTimeSpentOpeningProject = value;
                SaveToDisk();
            }
        }
        public List<BuildRecord> BuildRecords
        {
            get => UserStats_Global_BuildStats.instance != null ? UserStats_Global_BuildStats.instance.buildRecords : null;
            set
            {
                UserStats_Global_BuildStats buildStats = UserStats_Global_BuildStats.instance;
                if (buildStats == null) return;
                buildStats.buildRecords = value;
                buildStats.Save();
            }
        }
        #endregion Variables

        #region Methods

        static string GetCustomSavePath()
        {
#if UNITY_EDITOR_WIN
            string myPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
#elif UNITY_EDITOR_OSX
            string myPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
#elif UNITY_EDITOR_LINUX
            string myPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
#else
            throw new System.NotSupportedException("Unsupported platform");
#endif
            return Path.Combine(myPath, "Tiny Giant Studio/DevTrails", "Global Stats.json");
        }

        // Load from file
        public void LoadFromDisk()
        {
            if(UserStats_Global_BuildStats.instance!=null)
                UserStats_Global_BuildStats.instance.LoadFromDisk();
            
            string path = GetCustomSavePath();
            if (File.Exists(path))
            {
                try
                {
                    string json = File.ReadAllText(path);
                    if (!string.IsNullOrEmpty(json))
                        JsonUtility.FromJsonOverwrite(json, this);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError("Failed to load global stats. Please contact support(FerdowsurAsif@gmail.com) with details, if you have time. Exception: " + ex);
                }
            }
            else
            {
                Debug.Log("No existing Global Stats found at " + path + " for DevTrails. A new one will be created the next time something is saved.");
            }
        }

        static readonly object FileLock = new object();

        // Save to file
        public void SaveToDisk()
        {
            if(UserStats_Global_BuildStats.instance!=null)
                UserStats_Global_BuildStats.instance.SaveToDisk();
            
            try
            {
                string path = GetCustomSavePath();
                if (string.IsNullOrWhiteSpace(path))
                    throw new Exception("Invalid or empty path.");

                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);

                string json = JsonUtility.ToJson(this, true);

                // Attempt to write with retry mechanism
                const int maxAttempts = 3;
                for (int attempt = 1; attempt <= maxAttempts; attempt++)
                {
                    try
                    {
                        lock (FileLock)
                        {
                            using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
                            using (var writer = new StreamWriter(stream))
                            {
                                writer.Write(json);
                            }
                        }

                        // Success � break out of retry loop
                        break;
                    }
                    //catch (IOException e) when (attempt < maxAttempts)
                    catch (IOException) when (attempt < maxAttempts)
                    {
                        // If file is in use, wait and retry
                        System.Threading.Thread.Sleep(100); // Wait 100ms before retrying
                        continue;
                    }
                }
                //Debug.Log($"Settings saved to {path}");
            }
            catch (UnauthorizedAccessException e)
            {
                Debug.LogError($"Couldn't save stats. Access denied: {e.Message}");
            }
            catch (IOException e)
            {
                Debug.LogError($"Couldn't save stats. IO error while saving: {e.Message}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Couldn't save stats. Unexpected error: {e.Message}");
            }
        }


        public void Reset()
        {
            totalUseTime = 0;
            activeUseTime = 0;
            focusedUseTime = 0;

            _enteredPlayMode = 0;
            _playModeUseTime = 0;
            _sceneSaved = 0;
            _sceneOpened = 0;
            _sceneClosed = 0;
            _undoRedoCounter = 0;
            _compileCounter = 0;
            _totalTimeSpentCompiling = 0;
            _timeSpentCompiling = 0;
            _totalTimeSpentInDomainReload = 0;
            timeSpentInDomainReload = 0;

            _logCounterEditor = 0;
            _logCounterPlayMode = 0;
            _warningLogCounterEditor = 0;
            _warningLogCounterPlayMode = 0;
            _exceptionLogCounterEditor = 0;
            _exceptionLogCounterPlayMode = 0;
            _errorLogCounterEditor = 0;
            _errorLogCounterPlayMode = 0;

            _devToolsEditorWindowOpened = 0;
            _probableCrashes = 0;

            DayRecords.Clear();

            if(UserStats_Global_BuildStats.instance!=null)
                UserStats_Global_BuildStats.instance.Reset();
            
            Save();
        }

        public void Save()
        {
            //Debug.Log("Saved");
            ////Save(true);
            //SaveToDisk();
        }

        #endregion Methods

        #region Day Records

        public void NewDayHappenedInTheMiddleOfSession()
        {
            int activeElapsedTime = (int)TimeSpan.FromMilliseconds(EditorAnalyticsSessionInfo.activeElapsedTime).TotalSeconds;
            int focusedElapsedTime = (int)TimeSpan.FromMilliseconds(EditorAnalyticsSessionInfo.focusedElapsedTime).TotalSeconds;
            int timeSinceStartup = (int)EditorApplication.timeSinceStartup;

            if (ShouldBeAddedToLastDayRecord())
            {
                var lastRecord = DayRecords[DayRecords.Count - 1];
                lastRecord.Sessions ??= new();

                SessionRecord sessionRecord = CreateSessionRecord(timeSinceStartup, focusedElapsedTime, activeElapsedTime);

                lastRecord.Sessions.Add(sessionRecord);
            }
            else
            {
                AddNewDayRecord(timeSinceStartup, focusedElapsedTime, activeElapsedTime);
            }

            SaveToDisk();
        }

        static SessionRecord CreateSessionRecord(int totalTime, int focusedElapsedTime, int activeElapsedTime)
        {
            return new()
            {
                sessionID = EditorAnalyticsSessionInfo.id,
                totalTime = totalTime,
                focusedUseTime = focusedElapsedTime,
                activeUseTime = activeElapsedTime,
            };
        }

        /// <summary>
        /// When Unity exits
        /// </summary>
        /// <param name="sessionID"></param>
        /// <param name="timeSinceStartup"></param>
        /// <param name="focusedElapsedTime"></param>
        /// <param name="activeElapsedTime"></param>
        public void EditorSessionEnded(long sessionID, int timeSinceStartup, int focusedElapsedTime, int activeElapsedTime)
        {
            // If there was no record before, create it
            if (DayRecords == null || DayRecords.Count == 0)
            {
                AddNewDayRecord(timeSinceStartup, focusedElapsedTime, activeElapsedTime);
                return;
            }

            //This block of code checks if a new day happened in the middle of a session
            //and if this session's record got split into two days
            //If that happened, remove the last record's values from this one before adding it.
            var lastRecord = DayRecords[DayRecords.Count - 1];
            if (!lastRecord.date.SameDay(DateTime.Now)) //Session record doesn't get split if it's in the same day
            {
                if (lastRecord.Sessions != null) //This should never happen but being careful
                {
                    foreach (var session in lastRecord.Sessions)
                    {
                        if (session.sessionID == sessionID) //This session's record got split
                        {
                            AddNewDayRecord(timeSinceStartup - session.totalTime, focusedElapsedTime - session.focusedUseTime, activeElapsedTime - session.activeUseTime);
                            return;
                        }
                    }
                }

                //If it's a new day and the session wasn't split, add it to a new day
                AddNewDayRecord(timeSinceStartup, focusedElapsedTime, activeElapsedTime);
                return;
            }

            //Had other sessions today. So
            lastRecord.Sessions.Add(CreateSessionRecord(timeSinceStartup, focusedElapsedTime, activeElapsedTime));
        }

        void AddNewDayRecord(int timeSinceStartup, int focusedElapsedTime, int activeElapsedTime)
        {
            DayRecords ??= new List<DayRecord>();
            DayRecord dayRecord = new DayRecord
            {
                date = new TGSTime(DateTime.Now),
                Sessions = new()
                {
                    CreateSessionRecord(timeSinceStartup, focusedElapsedTime, activeElapsedTime)
                }
            };

            DayRecords.Add(dayRecord);
        }

        public void AddTestNewDayRecord(DateTime dateTime, int timeSinceStartup, int focusedElapsedTime, int activeElapsedTime)
        {
            DayRecords ??= new List<DayRecord>();
            DayRecord dayRecord = new DayRecord
            {
                date = new TGSTime(dateTime),
                Sessions = new()
                {
                    CreateSessionRecord(timeSinceStartup, focusedElapsedTime, activeElapsedTime)
                }
            };

            DayRecords.Add(dayRecord);
        }

        bool ShouldBeAddedToLastDayRecord()
        {
            if (DayRecords == null || DayRecords.Count == 0)
                return false;

            var lastRecord = DayRecords[DayRecords.Count - 1];
            //This is same day as last record
            if (lastRecord.date.SameDay(DateTime.Now))
                return true;

            return false;
        }

        [System.Serializable]
        public class DayRecord
        {
            public TGSTime date;
            public List<SessionRecord> Sessions;

            public int TotalTime()
            {
                if (Sessions == null || Sessions.Count == 0)
                    return 0;

                int totalTime = 0;
                for (int i = 0; i < Sessions.Count; i++)
                {
                    totalTime += Sessions[i].totalTime;
                }

                return totalTime;
            }

            public int FocusedTime()
            {
                if (Sessions == null || Sessions.Count == 0)
                    return 0;

                int focusedUseTime = 0;
                for (int i = 0; i < Sessions.Count; i++)
                {
                    focusedUseTime += Sessions[i].focusedUseTime;
                }

                return focusedUseTime;
            }

            public int ActiveTime()
            {
                if (Sessions == null || Sessions.Count == 0)
                    return 0;

                int activeUseTime = 0;
                for (int i = 0; i < Sessions.Count; i++)
                {
                    activeUseTime += Sessions[i].activeUseTime;
                }

                return activeUseTime;
            }
        }

        [System.Serializable]
        public struct SessionRecord
        {
            public long sessionID;
            public int totalTime;
            public int focusedUseTime;
            public int activeUseTime;
        }

        #endregion Day Records
    }
}