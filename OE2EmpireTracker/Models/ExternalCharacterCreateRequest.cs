namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// DTO for creating a new external character.
    /// No Original snapshot (it doesn't exist yet). No UUID (the service assigns it).
    /// </summary>
    public class ExternalCharacterCreateRequest
    {
        public string Name { get; set; }

        public string FactionUUID { get; set; }
    }
}
