using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OE2EmpireTracker.Data
{
    public class CountDownTime
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        [Newtonsoft.Json.JsonIgnore]
        public long TimeRemaining
        {
            get
            {
                return (long)(EndTime - DateTime.Now).TotalSeconds;
            }
            set
            {
                EndTime = DateTime.Now.AddSeconds(value);
            }
        }

        public string TimeRemainingString
        {
            get
            {
                TimeSpan timeSpan = EndTime - DateTime.Now;
                string timeString = "";
                if (timeSpan.Days > 0) { 
                    timeString = $"{(int)timeSpan.Days}d";
                }
                if (timeSpan.Hours < 24 && timeSpan.Hours > 0)
                {
                    if (timeString.Length > 0)
                    {
                        timeString += " ";
                    }
                    timeString += $"{(int)timeSpan.Hours}h";
                }
                if (timeSpan.Minutes < 60 && timeSpan.Minutes > 0)
                {
                    if (timeString.Length > 0)
                    {
                        timeString += " ";
                    }
                    timeString += $"{(int)timeSpan.Minutes}m";
                }
                if (timeSpan.Seconds < 60 && timeSpan.Seconds > 0)
                {
                    if (timeString.Length > 0)
                    {
                        timeString += " ";
                    }
                    timeString += $"{(int)timeSpan.Seconds}s";
                }
                return timeString;
            }
            set
            {
                var match = Regex.Match(value, @"(?:(\d+)d\s*)?(?:(\d+)h\s*)?(?:(\d+)m\s*)?(?:(\d+)s)?");

                int days = match.Groups[1].Success ? int.Parse(match.Groups[1].Value) : 0;
                int hours = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : 0;
                int minutes = match.Groups[3].Success ? int.Parse(match.Groups[3].Value) : 0;
                int seconds = match.Groups[4].Success ? int.Parse(match.Groups[4].Value) : 0;

                int totalSeconds = ((days * 24 + hours) * 60 + minutes) * 60 + seconds;
                TimeRemaining = totalSeconds;
            }
        }

        public CountDownTime()
        {
            StartTime = new DateTime();
            EndTime = StartTime;
        }
    }
}
