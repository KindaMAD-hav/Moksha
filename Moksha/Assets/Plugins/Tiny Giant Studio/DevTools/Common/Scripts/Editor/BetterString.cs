using System;
using System.Globalization;

namespace TinyGiantStudio.DevTools
{
    public static class BetterString
    {
        public static string Number(int number) => number.ToString("N0", CultureInfo.InvariantCulture);
        
       public static string SmallStringTime(double time)
        {
            TimeSpan t = TimeSpan.FromSeconds(time);

            if (t.Days > 0)
                return $"{t.Days:D1}d {t.Hours:D1}h {t.Minutes:D2}m";
            else if (t.Hours > 0)
            {
                return $"{t.Hours:D1}h {t.Minutes:D2}m";
            }
            else
            {
                if (t.Minutes > 0) //hour haven't reached
                    return $"{t.Minutes:D2}m {t.Seconds:D2}s";

                //minute haven't reached
                return t.Seconds > 0 ? $"{t.Seconds:D2}s" : $"{t.Milliseconds:D2}ms";
            }
            //return string.Format("{0:D1}h {1:D2}m {2:D1}s", t.Hours, t.Minutes, t.Seconds);
        }
    }
}