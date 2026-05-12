namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// DTO carrying the original snapshot and the current local state for updating an existing faction.
    /// </summary>
    public class FactionUpdateRequest
    {
        /// <summary>
        /// The original snapshot the edit was based on.
        /// </summary>
        public ReadOnlyFaction Original { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }
    }
}
