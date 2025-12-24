using System;
using System.Collections.Generic;
using System.Linq;
using TinyGiantStudio.DevTools.DevTasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace TinyGiantStudio.DevTools
{
    /// <summary>
    ///
    /// </summary>
    public class DevToolsWindow : EditorWindow
    {
        #region Variables

        [SerializeField] private VisualTreeAsset visualTreeAsset;

        public Header header;


        private SearchManger _searchManger;

        public SearchManger SearchManger
        {
            get
            {
                _searchManger ??= new SearchManger();
                return _searchManger;
            }
        }
        private AnimationHandler _uiAnimationHandler; //In the name, just capitalizing the I feels wrong for some reason

        public AnimationHandler UIAnimationHandler
        {
            get
            {
                _uiAnimationHandler ??= new AnimationHandler();
                return _uiAnimationHandler;
            }
        }

        /// <summary>
        /// When the window first loads, FindAllTabPages() method creates a list of all tabs 
        /// </summary>
        public List<TabPage> tabPages = new();

        #endregion Variables

        #region Unity Stuff

        //[MenuItem("Tools/Tiny Giant Studio/Project Manager", false, 0)]
        [MenuItem("Tools/Tiny Giant Studio/Dev Tools %#t", false, 0)]
        public static void ShowWindow()
        {
            DevToolsWindow window = GetWindow<DevToolsWindow>();
            Texture2D icon = EditorGUIUtility.Load("Assets/Plugins/Tiny Giant Studio/DevTools/Common/Artworks/TGS_DevTools_CompanyIcon_Thumbnail.png") as Texture2D;
            if (icon == null)
                window.titleContent = new GUIContent("DevTools");
            else
                window.titleContent = new GUIContent("DevTools", icon);
            window.minSize = new Vector2(600, 650);
        }

        private void OnEnable()
        {
            UpdatePages();
            Undo.undoRedoPerformed += HandleUndo;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= HandleUndo;
        }

        public void CreateGUI()
        {
            visualTreeAsset.CloneTree(rootVisualElement);

            FindAllTabPages();

            //This also handles setting up all the tab pages via constructor
            header = new Header(rootVisualElement, devToolsWindow: this);

            OpenDefaultTab();

            UpdatePages();
        }

        #endregion Unity Stuff

        /// <summary>
        /// finds all the pages that should be tab by their class.
        /// Then rearranges them according to their priority int variable.
        /// </summary>
        private void FindAllTabPages()
        {
            tabPages ??= new List<TabPage>();
            tabPages.Clear();

            var derivedClasses = DerivedClassFinder.GetDerivedClasses<TabPage>();
            foreach (Type derivedClass in derivedClasses)
            {
                // Create an instance of the derived class
                if (Activator.CreateInstance(derivedClass, this, rootVisualElement) is TabPage instance)
                {
                    if (!string.IsNullOrEmpty(instance.tabName))
                        tabPages.Add(instance);
                }
            }

            tabPages = tabPages.OrderBy(tabPage => tabPage.priority).ToList();
        }

        /// <summary>
        /// This is called when the window is first opened
        /// </summary>
        private void OpenDefaultTab()
        {
            TabPage selectedTab = Settings.instance.selectedTab;
            if (Settings.instance.selectedTab != null)
            {
                for (int i = 0; i < tabPages.Count; i++)
                {
                    if (tabPages[i].tabName == selectedTab.tabName)
                    {
                        header.ResumeTab(tabPages[i]);
                        return;
                    }
                }
            }
            //Open the first tab by default
            header.SelectTab(tabPages[0]); //If there are no tab pages, the asset won't show anything and this SHOULD print an error
        }

        private void HandleUndo()
        {
            UpdatePages();
        }

        /// <summary>
        /// This handles switching pages in the editor window according to the CurrentPage variable.
        /// </summary>
        public void UpdatePages()
        {
            var settings = Settings.instance;

            if (tabPages.Contains(settings.selectedTab))
                settings.selectedTab?.UpdatePage();
            else //During domain reload, sometimes settings has a different version of the script instance saved, in that case, reconnect
            {
                for (int i = 0; i < tabPages.Count; i++)
                {
                    if (tabPages[i].tabName == settings.selectedTab.tabName)
                    {
                        settings.selectedTab = tabPages[i];
                        tabPages[i].UpdatePage();
                        break;
                    }
                }
            }
        }
    }
}