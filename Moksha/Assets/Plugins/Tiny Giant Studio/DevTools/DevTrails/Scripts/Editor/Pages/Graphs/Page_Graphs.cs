using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace TinyGiantStudio.DevTools.DevTrails
{
    public class Page_Graphs : TabPage
    {
        // Inherit base class constructor
        public Page_Graphs(DevToolsWindow projectManagerWindow, VisualElement container) : base(projectManagerWindow, container)
        {
            tabName = "Global Graphs";
            tabShortName = "Graphs";
            tabIcon = "graphs-icon";
            tooltip = "This excludes current session";
            priority = 4; //Determines the order in tabs list
        }

        VisualTreeAsset _graphTemplate;
        Label _weekLabel;
        Label _monthLabel;

        public override void SetupPage(DevToolsWindow newDevToolsWindow, VisualElement container)
        {
            devToolsWindow = newDevToolsWindow;

            VisualTreeAsset pageAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Plugins/Tiny Giant Studio/DevTools/DevTrails/Scripts/Editor/Pages/Graphs/PageGraphs.uxml");
            //If the asset isn't found in the correct location,
            if (pageAsset == null)
                //Check the testing location
                pageAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/DevTrails/Scripts/Editor/Pages/Stats/PageUserStats.uxml");

            _graphTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Plugins/Tiny Giant Studio/DevTools/DevTrails/Scripts/Editor/Pages/Graphs/DayGraph.uxml");

            pageContainer = new();
            pageContainer.AddToClassList("flatContainer");
            pageAsset.CloneTree(pageContainer);
            container.Q<GroupBox>("Content").Add(pageContainer);

            SetupGraph();
        }

        public override void OpenPage()
        {
            pageContainer.style.display = DisplayStyle.Flex;
        }

        public override void ClosePage()
        {
            pageContainer.style.display = DisplayStyle.None;
        }

        public override void UpdatePage()
        {
        }

        void SetupGraph()
        {
            UserStats_Global userStats_Global = UserStats_Global.instance;
            VisualElement weeklyGraphContainer = pageContainer.Q<VisualElement>("WeeklyGraph");
            VisualElement monthlyGraphContainer = pageContainer.Q<VisualElement>("MonthlyGraph");
            Label label = pageContainer.Q<Label>("DataLength");
            Label weeklyLabel = pageContainer.Q<Label>("WeeklyLabel");

            if (userStats_Global.DayRecords == null)
            {
                pageContainer.Add(new HelpBox("There is not enough data. Please check back after a few days.", HelpBoxMessageType.Info));
                weeklyGraphContainer.style.display = DisplayStyle.None;
                monthlyGraphContainer.style.display = DisplayStyle.None;
                label.text = "No data available.";

                return;
            }

            pageContainer.Q<Label>("DataLength").text = "Data available for " + userStats_Global.DayRecords.Count + " days.";

            SetupWeeklyGraph(userStats_Global, weeklyGraphContainer, weeklyLabel);
            SetupMonthlyGraph(userStats_Global, monthlyGraphContainer);
        }

        void SetupWeeklyGraph(UserStats_Global userStats_Global, VisualElement weeklyGraphContainer, Label label)
        {
            _weekLabel = weeklyGraphContainer.Q<Label>("WeekLabel");
            int weekIndex = 0;
            weeklyGraphContainer.Q<Button>("PreviousWeekButton").clicked += () =>
            {
                weekIndex++;
                CreateThisWeeksGraphs(userStats_Global, weeklyGraphContainer.Q<VisualElement>("Graphs"), weekIndex, label);
            };
            weeklyGraphContainer.Q<Button>("NextWeekButton").clicked += () =>
            {
                weekIndex--;
                if (weekIndex < 0)
                    weekIndex = 0;
                CreateThisWeeksGraphs(userStats_Global, weeklyGraphContainer.Q<VisualElement>("Graphs"), weekIndex, label);
            };

            CreateThisWeeksGraphs(userStats_Global, weeklyGraphContainer.Q<VisualElement>("Graphs"), weekIndex, label);
        }

        void SetupMonthlyGraph(UserStats_Global userStats_Global, VisualElement container)
        {
            _monthLabel = container.Q<Label>("Label");
            int targetIndex = 0;
            container.Q<Button>("PreviousIndexButton").clicked += () =>
            {
                targetIndex++;
                CreateThisMonthsGraphs(userStats_Global, container.Q<VisualElement>("Graphs"), targetIndex);
            };
            container.Q<Button>("NextIndexButton").clicked += () =>
            {
                targetIndex--;
                if (targetIndex < 0)
                    targetIndex = 0;
                CreateThisMonthsGraphs(userStats_Global, container.Q<VisualElement>("Graphs"), targetIndex);
            };

            CreateThisMonthsGraphs(userStats_Global, container.Q<VisualElement>("Graphs"), targetIndex);
        }

        void CreateThisMonthsGraphs(UserStats_Global userStats_Global, VisualElement graphsHolder, int monthNumber)
        {
            graphsHolder.Clear();

            //List<UserStats_Global.DayRecord> monthRecords = GetMonthEntries(userStats_Global, monthNumber);
            DateTime firstOfThisMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            DateTime monthStart = firstOfThisMonth.AddMonths(-monthNumber);
            DateTime monthEnd = monthStart.AddMonths(1); // exclusive

            // Step 2: Get all records from that month
            var monthRecords = userStats_Global.DayRecords
                .Where(e => e.date.dateTime().Date >= monthStart && e.date.dateTime().Date < monthEnd)
                .ToDictionary(e => e.date.dateTime().Date); // Dictionary for fast lookup

            _monthLabel.text = GetMonth(monthStart.Month) + ", " + monthStart.Year;

            if (monthNumber == 0)
                _monthLabel.text += " (This month)";
            else if (monthNumber == 1)
                _monthLabel.text += " (Last month)";
            else
                _monthLabel.text += " (" + monthNumber + " months ago)";

            // Step 3: Loop through each day of the month
            for (DateTime day = monthStart; day < monthEnd; day = day.AddDays(1))
            {
                VisualElement holder = new VisualElement();
                graphsHolder.Add(holder);
                _graphTemplate.CloneTree(holder);
                holder.style.flexGrow = 1;

                if (monthRecords.TryGetValue(day, out var record))
                {
                    //SetGraph(holder, monthRecords[day], day.Day.ToString());
                    SetGraph(holder, MonthlyGraphLabel(day), record.ActiveTime(), record.FocusedTime(), record.TotalTime());
                }
                else
                {
                    SetGraph(holder, MonthlyGraphLabel(day), 0, 0, 0);
                }
            }
        }

        void CreateThisWeeksGraphs(UserStats_Global userStatsGlobal, VisualElement graphsHolder, int weeksAgo, Label label)
        {
            graphsHolder.Clear();

            // Step 1: Calculate the start (Sunday) and end (Saturday) of the target week
            DateTime today = DateTime.Today;
            int daysSinceSunday = (int)today.DayOfWeek;
            DateTime thisWeekSunday = today.AddDays(-daysSinceSunday);

            DateTime weekStart = thisWeekSunday.AddDays(-7 * weeksAgo);
            DateTime weekEnd = weekStart.AddDays(6); // inclusive

            _weekLabel.text = string.Empty;

            if (weekStart.Year != today.Year && weekEnd.Year != today.Year)
            {
                _weekLabel.text = weekStart.Day + "-" + weekEnd.Day;

                //If it isn't this year, always show month
                _weekLabel.text += ", " + GetMonth(weekStart.Month);

                _weekLabel.text += ", " + weekStart.Year;
                if (weekStart.Year != weekEnd.Year)
                {
                    _weekLabel.text += "-" + weekEnd.Year;
                }
            }
            else //If it is this year
            {
                if (weekEnd.Month != weekStart.Month)
                {
                    _weekLabel.text += weekStart.Day + GetSuffixWithoutDay(weekStart.Day) + " " + GetMonth(weekStart.Month)
                                       + " - "
                                       + weekEnd.Day + GetSuffixWithoutDay(weekEnd.Day) + " " + GetMonth(weekEnd.Month);
                }
                else
                {
                    _weekLabel.text = weekStart.Day + "-" + weekEnd.Day;
                    _weekLabel.text += ", " + GetMonth(weekStart.Month);
                }
            }

            if (weeksAgo == 0)
                _weekLabel.text += " (This week)";
            else if (weeksAgo == 1)
                _weekLabel.text += " (Last week)";
            else
                _weekLabel.text += " (" + weeksAgo + " weeks ago)";

            // Step 2: Get records from that week and index by date for fast lookup
            Dictionary<DateTime, UserStats_Global.DayRecord> weekRecords = userStatsGlobal.DayRecords
                .Where(e => e.date.dateTime().Date >= weekStart && e.date.dateTime().Date <= weekEnd)
                .ToDictionary(e => e.date.dateTime().Date);

            int totalActiveTime = 0;
            int totalFocusedTime = 0;
            int totalUsageTime = 0;
            int recordCount = 0;

            // Step 3: Loop through each day of the week
            for (DateTime day = weekStart; day <= weekEnd; day = day.AddDays(1))
            {
                VisualElement holder = new VisualElement();
                graphsHolder.Add(holder);
                _graphTemplate.CloneTree(holder);
                holder.style.flexGrow = 1;

                if (weekRecords.TryGetValue(day, out var record))
                {
                    recordCount++;
                    int activeTime = record.ActiveTime();
                    totalActiveTime += activeTime;
                    int focusedTime = record.FocusedTime();
                    totalFocusedTime += focusedTime;
                    int usageTime = record.TotalTime();
                    totalUsageTime += usageTime;
                    SetGraph(holder, WeekGraphLabel(day), activeTime, focusedTime, usageTime);
                }
                else
                {
                    SetGraph(holder, WeekGraphLabel(day), 0, 0, 0);
                }
            }

            label.text = "During the selected week, unity was <b>used</b> for <b>" + FulltringTime(totalUsageTime) + "</b>. " +
                         "\n\nWithin that time, the Unity Editor Window was <b>focused</b> for <b>" + FulltringTime(totalFocusedTime) + "</b>," +
                         "\n\n" +
                         "and you <b>active</b>ly interacted with the editor for <b>" + FulltringTime(totalActiveTime) + "</b>.";
            if (recordCount > 0)
                label.text += "\n\n\n<b>On average</b>, your daily Unity <b>usage</b> during that period is <b>" + FulltringTime(totalUsageTime / recordCount) + "</b>," +
                              "\n\nwith the editor <b>focused</b> for <b>" +
                              FulltringTime(totalFocusedTime / recordCount) + "</b> " +
                              "\n\nand <b>interacted</b> with for <b>" + FulltringTime(totalActiveTime / recordCount) + "</b>.";
        }

        string WeekGraphLabel(DateTime day)
        {
            string label = "<b>" + day.DayOfWeek.ToString() + "</b>";
            // label += "\n<i>" + day.Day + "-" + day.Month + "-" + day.Year + "</i>";
            return label;
        }

        string MonthlyGraphLabel(DateTime day)
        {
            string label = "<b>" + day.Day.ToString() + "</b>";
            label += "\n<i>" + day.DayOfWeek.ToString().Substring(0, 2) + "</i>";
            return label;
        }

        void SetGraph(VisualElement graphs, string label, int activeTime, int focusedTime, int totalTime)
        {
            graphs.Q<Label>("Label").text = label;

            const float divider = 225;
            const float maximum = 86400; //Total time in a day

            VisualElement activeTimeVe = graphs.Q<VisualElement>("ActiveTime");
            activeTimeVe.style.height = Mathf.Clamp(activeTime, 0, maximum) / divider;
            activeTimeVe.tooltip = "Active use time was " + SmallStringTime(activeTime);

            VisualElement focusedTimeVe = graphs.Q<VisualElement>("FocusedTime");
            focusedTimeVe.style.height = Mathf.Clamp(focusedTime, 0, maximum) / divider;
            focusedTimeVe.tooltip = "Focused use time was " + SmallStringTime(focusedTime);

            VisualElement totalTimeVe = graphs.Q<VisualElement>("TotalTime");
            totalTimeVe.style.height = Mathf.Clamp(totalTime, 0, maximum) / divider;
            totalTimeVe.tooltip = "Total use time was " + SmallStringTime(totalTime);
        }

        //This method is in different scripts. Bad practice to do this. Will fix it later
        string SmallStringTime(double time)
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

        //This method is in different scripts. Bad practice to do this. Will fix it later
        string FulltringTime(double time)
        {
            TimeSpan t = TimeSpan.FromSeconds(time);

            if (t.Days > 1)
                return $"{t.Days:D1} days, {t.Hours:D1} hours and {t.Minutes:D2} minutes";
            if (t.Days > 0)
                return $"{t.Days:D1} day, {t.Hours:D1} hours and {t.Minutes:D2} minutes";
            if (t.Hours > 0)
                return string.Format("{0:D1} hours and {1:D2} minutes", t.Hours, t.Minutes);
            if (t.Minutes > 0) //hour haven't reached
            {
                return string.Format("{0:D2} minutes and {1:D2} seconds", t.Minutes, t.Seconds);
            }
            else //minute haven't reached
            {
                if (t.Seconds > 0)
                    return string.Format("{0:D2} seconds", t.Seconds);
                else
                    return string.Format("{0:D2} milliseconds", t.Milliseconds);
            }
            //return string.Format("{0:D1}h {1:D2}m {2:D1}s", t.Hours, t.Minutes, t.Seconds);
        }

        //There MUST be cleaner way to do this
        string GetMonth(int index)
        {
            switch (index)
            {
                case 1:
                    return "January";

                case 2:
                    return "February";

                case 3:
                    return "March";

                case 4:
                    return "April";

                case 5:
                    return "May";

                case 6:
                    return "June";

                case 7:
                    return "July";

                case 8:
                    return "August";

                case 9:
                    return "September";

                case 10:
                    return "October";

                case 11:
                    return "November";

                case 12:
                    return "December";
            }

            return "Unknown month";
        }

        public string GetSuffixWithoutDay(int targetDay)
        {
            if (targetDay is >= 11 and <= 13)
                return "th";

            return (targetDay % 10) switch
            {
                1 => "st",
                2 => "nd",
                3 => "rd",
                _ => "th"
            };
        }
    }
}