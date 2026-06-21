namespace OE2EmpireTracker.Models
{
    public class ExternalCharacter
    {
        public string UUID { get; set; }
        public string OwnerUUID { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string FactionUUID { get; set; } = string.Empty;
    }
}
