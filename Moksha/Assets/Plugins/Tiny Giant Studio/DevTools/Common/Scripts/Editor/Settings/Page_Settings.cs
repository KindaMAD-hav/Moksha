using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.UIElements;

namespace TinyGiantStudio.DevTools.DevTasks
{
    public class Page_Settings : TabPage
    {
        // Inherit base class constructor
        public Page_Settings(DevToolsWindow projectManagerWindow, VisualElement container) : base(projectManagerWindow, container)
        {
            tabName = "Settings";
            tabShortName = "Config";
            tabIcon = "settings-icon";
            priority = 9999;
        }

        Settings settings;

        /// <summary>
        /// When the window first loads, FindAllTabPages() method creates a list of all tabs
        /// </summary>
        public List<SettingsContent> settingsContents = new();

        VisualElement content;

        public override void SetupPage(DevToolsWindow newDevToolsWindow, VisualElement container)
        {
            devToolsWindow = newDevToolsWindow;
            content = container;

            settings = Settings.instance;

            VisualTreeAsset pageAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Plugins/Tiny Giant Studio/DevTools/Common/Scripts/Editor/Settings/PageSettings.uxml");
            pageContainer = new();
            pageContainer.AddToClassList("flatContainer");
            pageAsset.CloneTree(pageContainer);
            container.Q<GroupBox>("Content").Add(pageContainer);



            var IconAnimation = pageContainer.Q<Toggle>("IconAnimation");
            IconAnimation.value = Settings.instance.enableIconAnimations;
            IconAnimation.RegisterValueChangedCallback(e =>
            {
                Settings.instance.enableIconAnimations = e.newValue;
                settings.Save();
            });

            var clockFormat = pageContainer.Q<DropdownField>("ClockFormat");
            if (settings.useTwelveHoursFormat) clockFormat.SetValueWithoutNotify("12 Hours with AM-PM");
            else clockFormat.SetValueWithoutNotify("24 Hours");
            clockFormat.RegisterValueChangedCallback(e =>
            {
                if (clockFormat.index == 0) settings.useTwelveHoursFormat = true;
                else settings.useTwelveHoursFormat = false;

                settings.Save();
            });

            FindAllSettingsContents();
            VisualElement parent = pageContainer.Q<VisualElement>("Contents");
            foreach (SettingsContent assetSettings in settingsContents)
            {
                assetSettings.Setup(parent);
            }


            var resetSettingsButton = pageContainer.Q<Button>("ResetSettingsButton");
            resetSettingsButton.clicked += () =>
            {
                if (EditorUtility.DisplayDialog
                (
                "Reset all settings?",
                "This will reset all settings to default. Undo can't be used here. \nUser stats will be unaffected.",
                "Reset",
                "Cancel"
                ))
                {
                    settings.Reset();

                    foreach (SettingsContent setting in settingsContents)
                    {
                        setting.Reset();
                    }

                    devToolsWindow.Close();
                    DevToolsWindow.ShowWindow();
                }
            };
        }

        public override void OpenPage()
        {
            pageContainer.style.display = DisplayStyle.Flex;
        }

        public override void UpdatePage()

        {
        }

        public override void ClosePage()
        {
            pageContainer.style.display = DisplayStyle.None;
        }

        /// <summary>
        /// finds all the pages that should be tab by their class.
        /// Then rearranges them according to their priority int variable.
        /// </summary>
        void FindAllSettingsContents()
        {
            settingsContents ??= new List<SettingsContent>();
            settingsContents.Clear();

            var derivedClasses = DerivedClassFinder.GetDerivedClasses<SettingsContent>();
            foreach (Type derivedClass in derivedClasses)
            {
                // Create an instance of the derived class
                if (Activator.CreateInstance(derivedClass) is SettingsContent instance)
                {
                    settingsContents.Add(instance);
                }
            }

            settingsContents = settingsContents.OrderBy(settingsContent => settingsContent.priority).ToList();
        }
    }
}