using UnityEngine.UIElements;

namespace TinyGiantStudio.DevTools
{
    /// <summary>
    /// If tab page contains any sub-pages, that needs to be closed when this is closed.
    /// </summary>
    [System.Serializable]
    public abstract class TabPage : Page
    {
        /// <summary>
        /// Leave it empty if this page shouldn't be a tab
        /// </summary>
        public string tabName = "";
        public string tabShortName = "";
        public string tooltip = "";

        public string tabIcon = "";
        public VisualElement tab;
        public int priority = 1000;

        protected TabPage(DevToolsWindow projectManagerWindow, VisualElement container) : base(projectManagerWindow, container)
        {
        }
    }
}