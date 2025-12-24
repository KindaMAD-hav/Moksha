using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace TinyGiantStudio.DevTools.DevTrails
{
    public class DevTrailsSettingsPage : SettingsContent
    {
        private DevTrailSettings devTrailSettings;
        private readonly string assetLink = "https://assetstore.unity.com/packages/tools/utilities/devtrails-developer-statistics-made-easy-291626?aid=1011ljxWe#releases";
        private readonly string documentationLink = "https://tiny-giant-studio.gitbook.io/devtrail";

        public override void Setup(VisualElement visualElement)
        {
            devTrailSettings = new();

            VisualTreeAsset asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Plugins/Tiny Giant Studio/DevTools/DevTrails/Scripts/Editor/Settings/DevTrails Settings.uxml");
            VisualElement pageContainer = new();
            asset.CloneTree(pageContainer);
            visualElement.Add(pageContainer);

            pageContainer.Q<Button>("VersionButton").clicked += () => { Application.OpenURL(assetLink); };
            pageContainer.Q<Button>("DocumentationButton").clicked += () => { Application.OpenURL(documentationLink); };

            Toggle timeTrackToggle = pageContainer.Q<Toggle>("TimeTrackToggle");
            timeTrackToggle.value = devTrailSettings.TrackTime;
            timeTrackToggle.RegisterValueChangedCallback(e => { devTrailSettings.TrackTime = e.newValue; });

            var usageGoalField = pageContainer.Q<IntegerField>("UsageGoalField");
            usageGoalField.value = (devTrailSettings.UsageGoal) / 3600;
            usageGoalField.RegisterValueChangedCallback(e => { devTrailSettings.UsageGoal = e.newValue * 3600; });

            Toggle usageGoalPopUp = pageContainer.Q<Toggle>("UsageGoalPopUp");
            usageGoalPopUp.value = devTrailSettings.UsageGoalPopUp;
            usageGoalPopUp.RegisterValueChangedCallback(e => { devTrailSettings.UsageGoalPopUp = e.newValue; });

            Toggle sceneSaveTrackToggle = pageContainer.Q<Toggle>("SceneSaveTrackToggle");
            sceneSaveTrackToggle.value = devTrailSettings.TrackSceneSave;
            sceneSaveTrackToggle.RegisterValueChangedCallback(e => { devTrailSettings.TrackSceneSave = e.newValue; });

            Toggle sceneOpenTrackToggle = pageContainer.Q<Toggle>("SceneOpenTrackToggle");
            sceneOpenTrackToggle.value = devTrailSettings.TrackSceneOpen;
            sceneOpenTrackToggle.RegisterValueChangedCallback(e => { devTrailSettings.TrackSceneOpen = e.newValue; });

            Toggle playModeEnterTrack = pageContainer.Q<Toggle>("PlayModeEnterTrack");
            playModeEnterTrack.value = devTrailSettings.TrackPlayMode;
            playModeEnterTrack.RegisterValueChangedCallback(e => { devTrailSettings.TrackPlayMode = e.newValue; });

            Toggle compile = pageContainer.Q<Toggle>("CompileTrackToggle");
            compile.value = devTrailSettings.TrackCompilation;
            compile.RegisterValueChangedCallback(e => { devTrailSettings.TrackCompilation = e.newValue; });

            Toggle undoRedoTrackToggle = pageContainer.Q<Toggle>("UndoRedoTrackToggle");
            undoRedoTrackToggle.value = devTrailSettings.TrackUndoRedo;
            undoRedoTrackToggle.RegisterValueChangedCallback(e => { devTrailSettings.TrackUndoRedo = e.newValue; });

            Toggle consoleTrackToggle = pageContainer.Q<Toggle>("ConsoleTrackToggle");
            consoleTrackToggle.value = devTrailSettings.TrackConsoleLogs;
            consoleTrackToggle.RegisterValueChangedCallback(e => { devTrailSettings.TrackConsoleLogs = e.newValue; });

            Toggle devToolsWindowTrack = pageContainer.Q<Toggle>("DevToolsWindowTrack");
            devToolsWindowTrack.value = devTrailSettings.ShowDevToolsEditorWindowTrack;
            devToolsWindowTrack.RegisterValueChangedCallback(e => { devTrailSettings.ShowDevToolsEditorWindowTrack = e.newValue; });

            Toggle editorCrashes = pageContainer.Q<Toggle>("EditorCrashes");
            editorCrashes.value = devTrailSettings.TrackEditorCrashes;
            editorCrashes.RegisterValueChangedCallback(e => { devTrailSettings.TrackEditorCrashes = e.newValue; });

            //This is the daily usage's pop up button
            var popUpRepeat = pageContainer.Q<Button>("PopUpRepeat");

            if (UserStats_Today.instance.showedUsagePopUpToday == false)
                popUpRepeat.style.display = DisplayStyle.None;

            popUpRepeat.clicked += () =>
            {
                UserStats_Today.instance.showedUsagePopUpToday = false;
                popUpRepeat.style.display = DisplayStyle.None;
            };

            var ResetThisProjectStats = pageContainer.Q<Button>("ResetThisProjectStats");
            ResetThisProjectStats.clicked += () =>
            {
                if (EditorUtility.DisplayDialog
                (
                "Reset This Project's Stats?",
                "This will reset this project's stats completely. " +
                "\nUndo can't be used here." +
                "\nNote that Time Spent will still include this session's timers even though previous times are reset to zero.",
                "Reset",
                "Cancel"
                ))
                {
                    if (EditorUtility.DisplayDialog
                     (
                     "Definitely Reset This Project's Stats?",
                     "Undo can't be used after this. This will reset this project's stats permanently. \nPlease restart the editor window to reflect changes.",
                     "Reset",
                     "Cancel"
                     ))
                    {
                        UserStats_Project.instance.Reset();
                    }
                }
            };

            var ResetLifeTimeStats = pageContainer.Q<Button>("ResetLifeTimeStats");
            ResetLifeTimeStats.clicked += () =>
            {
                if (EditorUtility.DisplayDialog
                (
                "Reset Lifetime Unity Stats?",
                "This will reset this lifetime completely and will impact other projects this asset is included in." +
                "\nUndo can't be used here." +
                "\nNote that Time Spent will still include this session's timers even though previous times are reset to zero. Also, session count can't be reset.",
                "Reset",
                "Cancel"
                ))
                {
                    if (EditorUtility.DisplayDialog
                     (
                     "Definitely Reset Lifetime Stats?",
                     "Undo can't be used after this. This will reset this lifetime unity stats tracked by this asset, permanently. \nPlease restart the editor window to reflect changes.",
                     "Reset",
                     "Cancel"
                     ))
                    {
                        UserStats_Global.instance.Reset();
                    }
                }
            };

            var ResetTodaysStats = pageContainer.Q<Button>("ResetTodaysStats");
            ResetTodaysStats.clicked += () =>
            {
                if (EditorUtility.DisplayDialog
                (
                "Reset Todays Stats?",
                "This will reset stats of today." +
                "\nUndo can't be used here. \nPlease restart the editor window to reflect changes.",
                "Reset",
                "Cancel"
                ))
                {
                    UserStats_Today.instance.Reset(true);
                }
            };
        }

        public override void Reset()
        {
            devTrailSettings.Reset();
        }
    }
}