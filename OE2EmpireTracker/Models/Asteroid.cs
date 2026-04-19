using System.Collections.Generic;

namespace OE2EmpireTracker.Models
{
    public class Asteroid
    {
        public string UUID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string SystemName { get; set; } = string.Empty;
        public List<AsteroidReserve> Reserves { get; set; } = new List<AsteroidReserve>();
    }

    public class AsteroidReserve
    {
        public string ResourceName { get; set; } = string.Empty;
        public string Purity { get; set; } = string.Empty;
        public int MaxReserve { get; set; } = 0;
        public int CurrentReserve { get; set; } = 0;
        public string ResetTimestamp { get; set; } = string.Empty;
    }
}
