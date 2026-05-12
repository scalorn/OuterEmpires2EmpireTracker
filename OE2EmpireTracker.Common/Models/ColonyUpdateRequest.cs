namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// DTO carrying the original snapshot and the current local state for updating an existing colony.
    /// The service can diff against the original for field-level change detection if needed.
    /// </summary>
    public class ColonyUpdateRequest
    {
        /// <summary>
        /// The original snapshot the edit was based on.
        /// Enables field-level dirty detection and optimistic concurrency.
        /// </summary>
        public ReadOnlyColony Original { get; set; }

        public string PlanetName { get; set; }

        public string ColonyName { get; set; }

        public string SystemName { get; set; }
    }
}