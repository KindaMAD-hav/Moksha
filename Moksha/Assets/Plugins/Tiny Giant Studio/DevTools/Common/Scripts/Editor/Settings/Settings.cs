using UnityEditor;
using UnityEngine;

namespace TinyGiantStudio.DevTools
{
    /// <summary>
    /// This handles all the settings for the asset.
    ///
    /// Although the scriptable instance save most of them,
    /// some settings are saved using editorPrefs to avoid being shared in version control
    /// but, still received from here through getter/setter to simply handling settings.
    /// </summary>
    [FilePath("ProjectSettings/DevTools Settings.asset", FilePathAttribute.Location.ProjectFolder)]
    public class Settings : ScriptableSingleton<Settings>
    {
        public TabPage selectedTab;

        /// <summary>
        /// Subpage of the tab
        /// </summary>
        public string selectedPage;


        public bool enableIconAnimations = true;

        public bool useTwelveHoursFormat = true;




        public void Save() => Save(true);

        public void Reset()
        {
            enableIconAnimations = true;

            //Debug.Log("<color=green>Settings have been reset successfully.</color>");
        }
    }
}