using UnityEngine.UIElements;

namespace TinyGiantStudio.DevTools
{
    [System.Serializable]
    public abstract class Page
    {
        public Page(DevToolsWindow projectManagerWindow, VisualElement container)
        {
            SetupPage(projectManagerWindow, container);
        }

        public DevToolsWindow devToolsWindow;
        public VisualElement pageContainer;

        /// <summary> 
        /// This is called when the editor window is opened.
        /// </summary>
        /// <param name="newDevToolsWindow">Keep a reference to the main script to reference later.</param>
        /// <param name="container">The visual element which holds all the contents of the page.</param>
        public abstract void SetupPage(DevToolsWindow newDevToolsWindow, VisualElement container);
        /// <summary>
        /// Switches on the visual element of the page.
        /// </summary>
        public abstract void OpenPage();
        /// <summary>
        /// Updates all the required info.
        /// </summary>
        public abstract void UpdatePage();
        public abstract void ClosePage();
    }
}