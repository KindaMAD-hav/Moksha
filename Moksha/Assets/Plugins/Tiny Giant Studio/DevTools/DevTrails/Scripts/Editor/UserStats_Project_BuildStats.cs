using System.Collections.Generic;
using UnityEditor;

namespace TinyGiantStudio.DevTools.DevTrails
{
    [FilePath("UserSettings/UserStats_project Build Stats.asset", FilePathAttribute.Location.ProjectFolder)]
    // ReSharper disable once InconsistentNaming
    public class UserStats_Project_BuildStats : ScriptableSingleton<UserStats_Project_BuildStats>
    {
        public List<BuildRecord> buildRecords = new List<BuildRecord>();
        
        #region Methods

        public void Reset()
        {
            buildRecords.Clear();
        }

        public void Save()
        {
            Save(true);
        }

        #endregion Methods
    }
}