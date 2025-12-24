using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TinyGiantStudio.DevTools.DevTrails
{
    [FilePath("UserSettings/UserStats_project.asset", FilePathAttribute.Location.ProjectFolder)]
    // ReSharper disable once InconsistentNaming
    public class UserStats_Project : ScriptableSingleton<UserStats_Project>
    {
        public long currentSessionIDCache = -1;
        public int probablyCrashes;

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
                Save();
            }
        }

        [SerializeField] int _playModeUseTime;

        public int PlayModeUseTime
        {
            get => _playModeUseTime;
            set
            {
                _playModeUseTime = value;
                Save();
            }
        }

        [SerializeField] int _sceneSaved;

        public int SceneSaved
        {
            get => _sceneSaved;
            set
            {
                _sceneSaved = value;
                Save();
            }
        }

        [SerializeField] int _sceneOpened;

        public int SceneOpened
        {
            get => _sceneOpened;
            set
            {
                _sceneOpened = value;
                Save();
            }
        }

        [SerializeField] int _sceneClosed;

        public int SceneClosed
        {
            get => _sceneClosed;
            set
            {
                _sceneClosed = value;
                Save();
            }
        }

        [SerializeField] int _undoRedoCounter;

        public int UndoRedoCounter
        {
            get => _undoRedoCounter;
            set
            {
                _undoRedoCounter = value;
                Save();
            }
        }

        [SerializeField] int _compileCounter;

        public int CompileCounter
        {
            get => _compileCounter;
            set
            {
                _compileCounter = value;
                Save();
            }
        }
        

        //Legacy support.
        [SerializeField] int _totalTimeSpentCompiling;

        public int TotalTimeSpentCompiling
        {
            get => _totalTimeSpentCompiling;
            set
            {
                _totalTimeSpentCompiling = value;
                Save();
            }
        }

        [SerializeField] float _timeSpentCompiling;

        public float TimeSpentCompiling
        {
            get => _timeSpentCompiling;
            set
            {
                _timeSpentCompiling = value;
                Save();
            }
        }

        [SerializeField] int _totalTimeSpentInDomainReload;

        public int TotalTimeSpentInDomainReload
        {
            get => _totalTimeSpentInDomainReload;
            set
            {
                _totalTimeSpentInDomainReload = value;
                Save();
            }
        }

        [SerializeField] float _timeSpentInDomainReload;

        public float TimeSpentInDomainReload
        {
            get => _timeSpentInDomainReload;
            set
            {
                _timeSpentInDomainReload = value;
                Save();
            }
        }

        #region Console Logs

        [SerializeField] int _logCounter_editor;

        public int LogCounter_editor
        {
            get => _logCounter_editor;
            set
            {
                _logCounter_editor = value;
                Save();
            }
        }

        [SerializeField] int _logCounter_playMode;

        public int LogCounter_playMode
        {
            get => _logCounter_playMode;
            set
            {
                _logCounter_playMode = value;
                Save();
            }
        }

        [SerializeField] int _warningLogCounter_editor;

        public int WarningLogCounter_editor
        {
            get => _warningLogCounter_editor;
            set
            {
                _warningLogCounter_editor = value;
                Save();
            }
        }

        [SerializeField] int _warningLogCounter_playMode;

        public int WarningLogCounter_playMode
        {
            get => _warningLogCounter_playMode;
            set
            {
                _warningLogCounter_playMode = value;
                Save();
            }
        }

        [SerializeField] int _errorLogCounter_editor;

        public int ErrorLogCounter_editor
        {
            get => _errorLogCounter_editor;
            set
            {
                _errorLogCounter_editor = value;
                Save();
            }
        }

        [SerializeField] int _errorLogCounter_playMode;

        public int ErrorLogCounter_playMode
        {
            get => _errorLogCounter_playMode;
            set
            {
                _errorLogCounter_playMode = value;
                Save();
            }
        }

        [SerializeField] int _exceptionLogCounter_editor;

        public int ExceptionLogCounter_editor
        {
            get => _exceptionLogCounter_editor;
            set
            {
                _exceptionLogCounter_editor = value;
                Save();
            }
        }

        [SerializeField] int _exceptionLogCounter_playMode;

        public int ExceptionLogCounter_playMode
        {
            get => _exceptionLogCounter_playMode;
            set
            {
                _exceptionLogCounter_playMode = value;
                Save();
            }
        }

        #endregion Console Logs

        [SerializeField] int _projectOpenCounter;

        public int ProjectOpenCounter
        {
            get => _projectOpenCounter;
            set
            {
                _projectOpenCounter = value;
                Save();
            }
        }

        [SerializeField] int _totalTimeSpentOpeningProject;

        public int TotalTimeSpentOpeningProject
        {
            get => _totalTimeSpentOpeningProject;
            set
            {
                _totalTimeSpentOpeningProject = value;
                Save();
            }
        }

        [SerializeField] int _devToolsEditorWindowOpened;

        public int DevToolsEditorWindowOpened
        {
            get => _devToolsEditorWindowOpened;
            set
            {
                _devToolsEditorWindowOpened = value;
                Save();
            }
        }
        public List<BuildRecord> BuildRecords
        {
            get => UserStats_Project_BuildStats.instance != null ? UserStats_Project_BuildStats.instance.buildRecords : null;
            set
            {
                UserStats_Project_BuildStats buildStats = UserStats_Project_BuildStats.instance;
                if (buildStats == null) return;
                buildStats.buildRecords = value;
                buildStats.Save();
            }
        }
        [SerializeField] PauseRecord _pauseRecords;

        public PauseRecord PauseRecords
        {
            get => _pauseRecords;
            set
            {
                _pauseRecords = value;
                Save();
            }
        }

        [System.Serializable]
        public class PauseRecord
        {
            //Although sessions are always cleared before exit,
            //having id record helps in edge cases where the application crashed and couldn't exit properly
            public long sessionID;

            public List<PauseSession> sessions;
        }

        [System.Serializable]
        public class PauseSession
        {
            public int usageTime;
            public int focusedTime;
            public int activeTime;
        }

        #region Methods

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
            _totalTimeSpentInDomainReload = 0;
            _timeSpentCompiling = 0;
            _timeSpentInDomainReload = 0;
            
            _logCounter_editor = 0;
            _logCounter_playMode = 0;
            _warningLogCounter_editor = 0;
            _warningLogCounter_playMode = 0;
            _exceptionLogCounter_editor = 0;
            _exceptionLogCounter_playMode = 0;
            _errorLogCounter_editor = 0;
            _errorLogCounter_playMode = 0;

            _devToolsEditorWindowOpened = 0;
            probablyCrashes = 0;

            if(UserStats_Project_BuildStats.instance != null)
                UserStats_Project_BuildStats.instance.Reset();
            
            Save();
        }

        public void Save()
        {
            Save(true);
            
            if(UserStats_Project_BuildStats.instance != null)
                UserStats_Project_BuildStats.instance.Save();
        }

        #endregion Methods
    }
}