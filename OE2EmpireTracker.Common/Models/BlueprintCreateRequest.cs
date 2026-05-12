using System.Collections.Generic;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// DTO for creating a new blueprint.
    /// No Original snapshot (it doesn't exist yet). No UUID (the service assigns it).
    /// </summary>
    public class BlueprintCreateRequest
    {
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
