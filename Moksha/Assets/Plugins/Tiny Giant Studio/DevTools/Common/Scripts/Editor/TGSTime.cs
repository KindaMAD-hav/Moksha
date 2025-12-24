using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace TinyGiantStudio.DevTools
{
    /// <summary>
    /// Custom serialized date-time utility tailored for Unity.
    /// It focuses on providing different formats of time for UX
    /// </summary>
    [Serializable]
    // ReSharper disable once InconsistentNaming //Can not rename without breaking saving breaking when updating with multiple projects
    public class TGSTime
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public TGSTime(DateTime time)
        {
            Year = time.Year;
            Month = time.Month;
            Day = time.Day;
            Minute = time.Minute;
            Hour = time.Hour;
        }

        [FormerlySerializedAs("year"), SerializeField]
        // ReSharper disable once InconsistentNaming //Can not rename without breaking saving breaking when updating with multiple projects
        int _year = 2024;

        public int Year
        {
            get => _year;
            set
            {
                _year = value switch
                {
                    <= 0 => 1800,
                    > 9000 => 9000,
                    _ => value
                };
            }
        }

        [FormerlySerializedAs("month"), SerializeField]
        // ReSharper disable once InconsistentNaming //Can not rename without breaking saving breaking when updating with multiple projects
        int _month = 1;

        public int Month
        {
            get => _month;
            set
            {
                _month = value switch
                {
                    < 1 => 1,
                    > 12 => 12,
                    _ => value
                };

                int daysInMonth = DateTime.DaysInMonth(Year, Month);
                if (Day > daysInMonth) Day = DateTime.DaysInMonth(Year, Month);
            }
        }

        [FormerlySerializedAs("day"), SerializeField]
        // ReSharper disable once InconsistentNaming //Can not rename without breaking saving breaking when updating with multiple projects
        int _day = 1;

        /// <summary>
        /// When setting the value, it checks maximum amount of days according to month and year.
        /// So, if you are setting all three, set year, month and then set day
        /// Ranges from 1
        /// </summary>
        public int Day
        {
            get => _day;
            set
            {
                int daysInMonth = DateTime.DaysInMonth(Year, Month);

                if (value < 1) _day = 1;
                else if (value > daysInMonth) _day = daysInMonth;
                else _day = value;
            }
        }

        public string DayOfTheWeek() => new DateTime(Year, Month, Day).DayOfWeek.ToString();

        [FormerlySerializedAs("hour"), SerializeField]
        // ReSharper disable once InconsistentNaming //Can not rename without breaking saving breaking when updating with multiple projects
        int _hour;

        /// <summary>
        /// In 24hour format.
        /// HourFormatted() for 12hour
        /// IsAM() to get if it's AM or PM.
        ///
        /// This Ranges from 0 to 23
        /// </summary>
        public int Hour
        {
            get => _hour;
            set
            {
                if (value < 0) _hour = 0;
                _hour = value > 23 ? 23 : value;
            }
        }

        /// <summary>
        /// in 12hour format
        /// </summary>
        /// <returns></returns>
        public int HourFormatted()
        {
            if (Hour == 0) return 12;
            else if (Hour < 12) return Hour;
            else if (Hour == 12) return 12;
            else return Hour - 12;
        }

        public bool IsAM() => Hour < 12;

        /// <summary>
        /// Goes from 0 to 59
        /// </summary>
        [FormerlySerializedAs("minute"), SerializeField]
        int _minute;

        public int Minute
        {
            get { return _minute; }
            set
            {
                _minute = value switch
                {
                    < 0 => 0,
                    > 59 => 59,
                    _ => value
                };
            }
        }

        /// <summary>
        /// Can be used to compare against system time
        /// </summary>
        public DateTime dateTime()
        {
            VerifyCorrectValues();
            return new DateTime(Year, Month, Day, Hour, Minute, 0);
        }

        public bool IsToday() => SameDay(DateTime.Today);

        /// <summary>
        /// Doesn't compare hour, minute and second
        /// </summary>
        public bool SameDay(DateTime time) => (time.Year == Year && time.Month == Month && time.Day == Day);

        public bool NotTimeYet() => dateTime() > DateTime.Now;

        public TimeSpan FullTimeSpanFromCurrentTime()
        {
            if (Year == 0) return TimeSpan.Zero;

            var now = DateTime.Now;
            TimeSpan timeSpan = new DateTime(Year, Month, Day) - now;
            return timeSpan;
        }

        //days from current time only checks 24hour difference. doesn't compare 10pm today vs 2am tomorrow as different day
        public int DaysFromCurrentTime()
        {
            if (Year == 0) return 0;

            var now = DateTime.Now;
            //TimeSpan timeSpan = new DateTime(Year, Month, Day) - now;
            //return timeSpan.Days;
            var targetDate = new DateTime(Year, Month, Day);
            TimeSpan timeSpan = targetDate.Date - now;
            return timeSpan.Days;
        }

        #region Get strings

        public string GetClock()
        {
            if (Settings.instance.useTwelveHoursFormat)
            {
                string myTime = HourFormatted().ToString("00") + ":" + Minute.ToString("00");
                if (IsAM()) myTime += " AM";
                else myTime += " PM";

                return myTime;
            }
            else
            {
                string myTime = Hour.ToString("00") + ":" + Minute.ToString("00");

                return myTime;
            }
        }

        public string GetDate()
        {
            var date = new DateTime(Year, Month, Day);
            string monthName = date.ToString("MMMM"); // e.g., "March"
            string dayWithSuffix = GetDayWithSuffix(Day);
            string yearStr = Year.ToString();

            return $"{monthName} {dayWithSuffix}, {yearStr}";
        }

        string GetDayWithSuffix(int day)
        {
            if (day >= 11 && day <= 13)
                return day + "th";

            switch (day % 10)
            {
                case 1: return day + "st";
                case 2: return day + "nd";
                case 3: return day + "rd";
                default: return day + "th";
            }
        }

        /// <summary>
        /// Returns "st" when 1 is passed as parameter for 1st day.
        /// </summary>
        /// <param name="day"></param>
        /// <returns></returns>
        public string GetSuffixWithoutDay(int day)
        {
            if (day >= 11 && day <= 13)
                return "th";

            switch (day % 10)
            {
                case 1: return "st";
                case 2: return "nd";
                case 3: return "rd";
                default: return "th";
            }
        }

        /// <summary>
        /// This includes a "This" or "On a" before the week
        /// </summary>
        /// <returns></returns>
        public string GetFullTime()
        {
            if (Year == 0) return "Unset time.";

            int daysDiff = DaysFromCurrentTime();

            string myTime;
            if (daysDiff == 0)
                myTime = "today ";
            else if (daysDiff >= 0 && daysDiff < 8)
                myTime = "this ";
            else if (daysDiff < 0 && daysDiff >= -7)
                myTime = "last ";
            else
                myTime = "a ";

            if (daysDiff != 0)
                myTime += DayOfTheWeek() + ", ";

            myTime += "at " + GetClock() + " on " + GetDate();

            return myTime;
        }

        public string GetFullTimeOnly()
        {
            string myTime = string.Empty;

            myTime += DayOfTheWeek() + ", ";

            myTime += GetClock() + " " + GetDate();

            return myTime;
        }

        public string GetDueTime()
        {
            //string myTime = string.Empty;
            var now = DateTime.Now;
            if (SameDay(now)) //today
            {
                if (NotTimeYet())
                    return "Due today at " + GetClock();
                else
                    return "Was due today at " + WrittenTime();
            }

            return WrittenDateMonth();
        }

        public string GetShortDueTime()
        {
            //string myTime = string.Empty;
            var now = DateTime.Now;
            if (SameDay(now)) //today
            {
                if (NotTimeYet())
                    return "Due at " + GetClock();
                else
                    return "Was due at " + WrittenTime();
            }

            return ShortWrittenDateMonth();
        }

        /// <summary>
        /// Returns dd/mm/year
        /// </summary>
        /// <returns></returns>
        string ShortWrittenDateMonth() => Day.ToString("00") + "/" + Month.ToString("00") + "/" + (Year % 100).ToString("00");

        string WrittenDateMonth() => new DateTime(Year, Month, Day, Hour, Minute, 0).ToLongDateString();

        string WrittenTime() => new DateTime(Year, Month, Day, Hour, Minute, 0).ToShortTimeString();

        public string GetComparison(DateTime target)
        {
            var myTime = dateTime();

            if (myTime < target)
            {
                TimeSpan timeRemaining = target - myTime;
                return GetImportantValuesOnly(timeRemaining);
            }
            else
            {
                TimeSpan timeRemaining = myTime - target;
                return GetImportantValuesOnly(timeRemaining);
            }
        }

        string GetImportantValuesOnly(TimeSpan span)
        {
            if (span.Days > 7)
                return span.Days + " days";
            else if (span.Days < 1)
                return span.Hours + " hours " + span.Minutes + " minutes";
            else
                return span.Days + " days " + span.Hours + " hours " + span.Minutes + " minutes";
        }

        #endregion Get strings

        #region For Import and Export

        public override string ToString() => $"{Year}:{Month}::{Hour}:{Minute}";

        /// <summary>
        /// Example: 2023:8:9:4:43:Wednesday
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static TGSTime Parse(string value)
        {
            try
            {
                TGSTime time = new TGSTime(new DateTime());
                var parts = value.Split(':');
                int.TryParse(parts[0], out int year);
                int.TryParse(parts[1], out int month);
                int.TryParse(parts[2], out int day);
                int.TryParse(parts[3], out int hour);
                int.TryParse(parts[4], out int minute);

                time.Year = year;
                time.Month = month;
                time.Day = day;
                time.Hour = hour;
                time.Minute = minute;

                return time;
            }
            catch
            {
                Debug.Log(value + " parsing failed.");
                return null;
            }
        }

        #endregion For Import and Export

        void VerifyCorrectValues()
        {
            Year = Year switch
            {
                <= 0 => 1800,
                > 9000 => 9000,
                _ => Year
            };

            if (Month < 1) Month = 1;
            if (Month > 12) Month = 12;

            int daysInMonth = DateTime.DaysInMonth(Year, Month);

            if (Day < 1) Day = 1;
            else if (Day > daysInMonth) Day = daysInMonth;
        }
    }
}