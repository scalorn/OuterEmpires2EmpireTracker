namespace OE2EmpireTracker.ViewModels
{
    /// <summary>
    /// Structured filter criteria for blueprint list filtering.
    /// Each field is nullable — null means "no filter on this dimension."
    /// </summary>
    public class BlueprintFilterCriteria
    {
        public string BlueprintTypeId { get; set; }   // null = no filter
        public int? ShipClassId { get; set; }          // null = no filter
        public string TechLevelName { get; set; }      // null = no filter
        public int? Evolution { get; set; }            // null = no filter
        public bool EvolutionAndAbove { get; set; }    // when true, filter Evolution >= value
    }
}
