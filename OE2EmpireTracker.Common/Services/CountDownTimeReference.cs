using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Services
{
    public class CountDownTimeReference
    {
        public enum SourceType
        {
            None,
            Player,
            Colony
        }

        public SourceType Source { get; set; }

        public string SourceUUID { get; set; }

        public string InternalUUID { get; set; }

        public CountDownTime CountDownTime { get; set; }
    }
}
