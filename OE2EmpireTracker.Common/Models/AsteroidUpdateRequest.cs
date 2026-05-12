using System.Collections.Generic;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// DTO carrying the original snapshot and the current local state for updating an existing asteroid.
    /// The service can diff against the original for field-level change detection if needed.
    /// </summary>
    public class AsteroidUpdateRequest
    {
        /// <summary>
        /// The original snapshot the edit was based on.
        /// Enables field-level dirty detection and optimistic concurrency.
        /// </summary>
        public ReadOnlyAsteroid Original { get; set; }

        public string Name { get; set; }

        public string SystemName { get; set; }

        public List<AsteroidReserve> Reserves { get; set; }
    }
}