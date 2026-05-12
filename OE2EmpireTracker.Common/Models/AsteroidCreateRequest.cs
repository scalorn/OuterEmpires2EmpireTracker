using System.Collections.Generic;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// DTO for creating a new asteroid.
    /// No Original snapshot (it doesn't exist yet). No UUID (the service assigns it).
    /// </summary>
    public class AsteroidCreateRequest
    {
        public string Name { get; set; }

        public string SystemName { get; set; }

        public List<AsteroidReserve> Reserves { get; set; }
    }
}