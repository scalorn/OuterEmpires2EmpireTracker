namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// Lightweight DTO carrying the destination identity for finding or creating a stop.
    /// Used by AddDropOffItem, AddPickUpItem, RemoveDropOffItems, RemovePickUpItems service methods.
    /// </summary>
    public class StopDestinationInfo
    {
        public string ColonyUUID { get; set; }

        public int Sequence { get; set; }

        public DestinationType DestinationType { get; set; }

        public string DestinationUUID { get; set; }
    }
}