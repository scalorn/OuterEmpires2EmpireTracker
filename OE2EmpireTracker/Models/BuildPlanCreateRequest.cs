using System.Collections.Generic;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// DTO for creating a new build plan.
    /// No Original snapshot (it doesn't exist yet). No UUID (the service assigns it).
    /// No OwnerUUID (the service sets it from the current player).
    /// </summary>
    public class BuildPlanCreateRequest
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public bool IsActive { get; set; }

        public List<BuildItem> Items { get; set; }
    }
}
