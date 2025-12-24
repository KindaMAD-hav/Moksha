using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace TinyGiantStudio.DevTools.DevTrails
{
    // ReSharper disable once InconsistentNaming
    public class Page_UserStats : TabPage
    {
        // Inherit base class constructor
        public Page_UserStats(DevToolsWindow projectManagerWindow, VisualElement container) : base(projectManagerWindow, container)
        {
            tabName = "DevTrails";
            tabShortName = "Trails";
            tabIcon = "devTrails-icon";
            priority = 3; //Determines the order in tabs list
        }

        UserStats_Today _userStatsToday;
        UserStatsTodayUI _userStatsTodayUI;
        UserStats_Project_UI _userStatsProjectUI;
        UserStats_Global _userStatsGlobal;
        UserStats_Global_UI _userStatsGlobalUI;

        Label _usageGoalLabel;

        //The schedule to refresh data is tied to this.
        //So, it's easier control schedule like stopping it and restarting
        VisualElement _scheduleContainer;

        DevTrailSettings _settings;

        public override void SetupPage(DevToolsWindow newDevToolsWindow, VisualElement container)
        {
            base.devToolsWindow = newDevToolsWindow;

            VisualTreeAsset pageAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Plugins/Tiny Giant Studio/DevTools/DevTrails/Scripts/Editor/Pages/Stats/PageUserStats.uxml");
            //If the asset isn't found in the correct location,
            if (pageAsset == null)
                //Check the testing location
                pageAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/DevTrails/Scripts/Editor/Pages/Stats/PageUserStats.uxml");

            pageContainer = new();
            pageContainer.AddToClassList("flatContainer");
            pageAsset.CloneTree(pageContainer);
            container.Q<GroupBox>("Content").Add(pageContainer);

            _userStatsProjectUI = new UserStats_Project_UI(pageContainer.Q<GroupBox>("ProjectStatsSummary"));
            _userStatsTodayUI = new UserStatsTodayUI(pageContainer.Q<GroupBox>("TodaysUnityStatsSummary"));
            _userStatsGlobalUI = new UserStats_Global_UI(pageContainer.Q<GroupBox>("GlobalStats"));

            _userStatsToday = UserStats_Today.instance;
            UserStats_Today.instance.DevToolsEditorWindowOpened++;
            UserStats_Project.instance.DevToolsEditorWindowOpened++;
            _userStatsGlobal = UserStats_Global.instance;
            UserStats_Global.instance.DevToolsEditorWindowOpened++;

            _settings = new DevTrailSettings();

            Button pauseTimeButton = pageContainer.Q<Button>("PauseTrackingTime");
            Button resumeTimeButton = pageContainer.Q<Button>("ResumeTrackingTime");

            //If time tracking is stopped without keeping track of the session, editor crashed and start tracking again.
            if (_settings.PauseTimeTracking && !EditorPrefs.HasKey("PauseStartUsageTime"))
                _settings.PauseTimeTracking = false;

            pauseTimeButton.clicked += () =>
        {
            _settings.PauseTimeTracking = true;
            UpdateTimePauseResumeButton(pauseTimeButton, resumeTimeButton);
        };
            resumeTimeButton.clicked += () =>
            {
                _settings.PauseTimeTracking = false;
                UpdateTimePauseResumeButton(pauseTimeButton, resumeTimeButton);
            };
            UpdateTimePauseResumeButton(pauseTimeButton, resumeTimeButton);

            Button startAlarmButton = pageContainer.Q<Button>("StartAlarmButton");
            Button stopAlarmButton = pageContainer.Q<Button>("StopAlarmButton");
            _usageGoalLabel = stopAlarmButton.Q<Label>("UsageGoalLabel");
            startAlarmButton.clicked += () =>
            {
                _settings.EnabledUsageGoal = true;
                UpdateUsageGoalButton(startAlarmButton, stopAlarmButton);
            };
            stopAlarmButton.clicked += () =>
            {
                _settings.EnabledUsageGoal = false;
                UpdateUsageGoalButton(startAlarmButton, stopAlarmButton);
            };
            UpdateUsageGoalButton(startAlarmButton, stopAlarmButton);

            UpdateUsageGoal();
        }

        void UpdateUsageGoalButton(Button startAlarmButton, Button stopAlarmButton)
        {
            if (!_settings.TrackTime)
            {
                startAlarmButton.style.display = DisplayStyle.None;
                stopAlarmButton.style.display = DisplayStyle.None;

                return;
            }

            if (_settings.EnabledUsageGoal)
            {
                startAlarmButton.style.display = DisplayStyle.None;
                stopAlarmButton.style.display = DisplayStyle.Flex;
            }
            else
            {
                startAlarmButton.style.display = DisplayStyle.Flex;
                stopAlarmButton.style.display = DisplayStyle.None;
            }
        }

        void UpdateTimePauseResumeButton(Button pauseTimeButton, Button resumeTimeButton)
        {
            if (!_settings.TrackTime)
            {
                pauseTimeButton.style.display = DisplayStyle.None;
                resumeTimeButton.style.display = DisplayStyle.None;

                return;
            }

            if (_settings.PauseTimeTracking)
            {
                pauseTimeButton.style.display = DisplayStyle.None;
                resumeTimeButton.style.display = DisplayStyle.Flex;
            }
            else
            {
                pauseTimeButton.style.display = DisplayStyle.Flex;
                resumeTimeButton.style.display = DisplayStyle.None;
            }
        }

        public override void OpenPage()
        {
            pageContainer.style.display = DisplayStyle.Flex;

            HideUntrackedData();
        }

        public override void ClosePage()
        {
            pageContainer.style.display = DisplayStyle.None;

            if (_scheduleContainer != null)
            {
                pageContainer.Remove(_scheduleContainer);
                _scheduleContainer = null;
            }
        }

        public override void UpdatePage()
        {
            if (_scheduleContainer != null)
            {
                pageContainer.Remove(_scheduleContainer);
                _scheduleContainer = null;
            }

            _scheduleContainer = new VisualElement();
            pageContainer.Add(_scheduleContainer);

            _scheduleContainer.schedule.Execute(() =>
            {
                UpdateInfo();
                //}).Every(120000).ExecuteLater(60000);
            }).Every(1000).ExecuteLater(1000); //todo control update rate by settings file

            UpdateInfo();
        }

        void HideUntrackedData()
        {
            _userStatsProjectUI.HideUntrackedData();
            _userStatsGlobalUI.HideUntrackedData();
            _userStatsTodayUI.HideUntrackedData();
        }

        void UpdateInfo()
        {
            _userStatsProjectUI.UpdateInfo();
            _userStatsGlobalUI.UpdateInfo();
            _userStatsTodayUI.UpdateInfo();

            UpdateUsageGoal();
        }

        void UpdateUsageGoal()
        {
            if (_settings.TrackTime)
            {
                if (_settings.EnabledUsageGoal)
                {
                    int usageGoal = _settings.UsageGoal;
                    int elapsedTime = TimeSpentInUnityToday() + (int)_userStatsTodayUI.CurrentSessionUsageTime();

                    if (usageGoal > elapsedTime)
                    {
                        int timeLeft = usageGoal - elapsedTime;
                        _usageGoalLabel.text = "Only " + SmallStringTime(timeLeft) + " until you have reached your goal.";
                    }
                    else
                    {
                        if (_settings.UsageGoalPopUp && !Application.isPlaying)
                        {
                            if (!_userStatsToday.showedUsagePopUpToday)
                            {
                                _userStatsToday.showedUsagePopUpToday = true;
                                _userStatsToday.SaveToDisk();

                                EditorUtility.DisplayDialog("Usage goal reached!", "You have used unity for " + SmallStringTime(elapsedTime) + " today and reached your usage goal of " + SmallStringTime(usageGoal) + ".", "Ok");
                                //DisplayDialog()
                            }
                        }

                        int timeExtra = elapsedTime - usageGoal;
                        _usageGoalLabel.text = "Congratulations! You have worked " + SmallStringTime(timeExtra) + " more than your goal today.";
                    }
                }
            }
        }

        int TimeSpentInUnityToday()
        {
            if (_userStatsGlobal.DayRecords != null)
            {
                if (_userStatsGlobal.DayRecords.Count > 0)
                {
                    UserStats_Global.DayRecord lastRecord = _userStatsGlobal.DayRecords[_userStatsGlobal.DayRecords.Count - 1];
                    if (lastRecord.date.SameDay(DateTime.Now))
                    {
                        int timeSpent = 0;
                        for (int i = 0; i < lastRecord.Sessions.Count; i++)
                        {
                            timeSpent += lastRecord.Sessions[i].totalTime;
                        }
                        return timeSpent;
                    }
                }
            }

            return 0;
        }

        string SmallStringTime(int time)
        {
            TimeSpan t = TimeSpan.FromSeconds(time);

            if (t.Days > 0)
                return string.Format("{0:D1}d {1:D1}h {2:D2}m", t.Days, t.Hours, t.Minutes);
            else if (t.Hours > 0)
            {
                return string.Format("{0:D1}h {1:D2}m", t.Hours, t.Minutes);
            }
            else
            {
                if (t.Minutes > 0) //hour haven't reached
                {
                    return string.Format("{0:D2}m {1:D2}s", t.Minutes, t.Seconds);
                }
                else //minute haven't reached
                {
                    if (t.Seconds > 0)
                        return string.Format("{0:D2}s", t.Seconds);
                    else
                        return string.Format("{0:D2}ms", t.Milliseconds);
                }
            }
            //return string.Format("{0:D1}h {1:D2}m {2:D1}s", t.Hours, t.Minutes, t.Seconds);
        }
    }
}