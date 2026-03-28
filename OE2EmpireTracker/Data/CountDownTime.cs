using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public CountDownTime()
        {
            StartTime = new DateTime();
            EndTime = StartTime;
        }
    }
}
