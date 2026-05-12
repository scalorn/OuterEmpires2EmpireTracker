namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// DTO for creating a new colony.
    /// No Original snapshot (it doesn't exist yet). No UUID (the service assigns it).
    /// No OwnerUUID (the service sets it from the current player).
    /// </summary>
    public class ColonyCreateRequest
    {
        public string PlanetName { get; set; }

        public string ColonyName { get; set; }

        public string SystemName { get; set; }
    }
}