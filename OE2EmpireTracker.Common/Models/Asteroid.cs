using System.Collections.Generic;

namespace OE2EmpireTracker.Models
{
    public class Asteroid
    {
        public string UUID { get; internal set; }
        public string Name { get; internal set; } = string.Empty;
        public string SystemName { get; internal set; } = string.Empty;
        public List<AsteroidReserve> Reserves { get; internal set; } = new List<AsteroidReserve>();
    }

    public class AsteroidReserve
    {
        public string ResourceName { get; internal set; } = string.Empty;
        public string Purity { get; internal set; } = string.Empty;
        public int MaxReserve { get; internal set; } = 0;
        public int CurrentReserve { get; internal set; } = 0;
        public string ResetTimestamp { get; internal set; } = string.Empty;
    }
}
