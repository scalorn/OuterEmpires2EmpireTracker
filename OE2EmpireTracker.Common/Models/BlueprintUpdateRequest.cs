using System.Collections.Generic;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// DTO carrying the original snapshot and the current local state for updating an existing blueprint.
    /// The service can diff against the original for field-level change detection if needed.
    /// </summary>
    public class BlueprintUpdateRequest
    {
        /// <summary>
        /// The original snapshot the edit was based on.
        /// Enables field-level dirty detection and optimistic concurrency.
        /// </summary>
        public ReadOnlyBlueprint Original { get; set; }

        public string Name { get; set; }
        public string NickName { get; set; }
        public string Description { get; set; }
        public string BluePrintType { get; set; }
        public int Evolution { get; set; }
        public string TechLevel { get; set; }
        public int Class { get; set; }
        public int CopyCost { get; set; }
        public string BaseBlueprintUUID { get; set; }
        public Dictionary<string, string> Properties { get; set; }
        public Dictionary<string, string> Resources { get; set; }
    }
}
