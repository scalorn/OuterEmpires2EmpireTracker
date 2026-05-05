namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// DTO for creating a new faction.
    /// No Original snapshot (it doesn't exist yet). No UUID (the service assigns it).
    /// </summary>
    public class FactionCreateRequest
    {
        public string Name { get; set; }

        public string Description { get; set; }
    }
}
