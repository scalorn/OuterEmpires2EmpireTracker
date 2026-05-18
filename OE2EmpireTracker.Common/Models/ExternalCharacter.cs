namespace OE2EmpireTracker.Models
{
    public class ExternalCharacter
    {
        public string UUID { get; internal set; }
        public string Name { get; internal set; } = string.Empty;
        public string FactionUUID { get; internal set; } = string.Empty;
    }
}
