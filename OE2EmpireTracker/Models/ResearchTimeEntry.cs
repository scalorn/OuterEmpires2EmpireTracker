namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// Maps an evolution level to its research duration in seconds.
    /// Serialized into the ResearchTime array in BaselineData.json.
    /// </summary>
    public class ResearchTimeEntry
    {
        public int Evolution { get; set; }
        public long ResearchTimeSeconds { get; set; }
    }
}
