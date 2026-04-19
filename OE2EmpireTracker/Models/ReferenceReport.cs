namespace OE2EmpireTracker.Models
{
    public class ReferenceReport
    {
        public int TotalCount { get; }
        public int FlatpackCount { get; }
        public int ResearchingCount { get; }
        public int ManufacturingCount { get; }
        public int BaseBlueprintCount { get; }
        public int ScannerCount { get; }
        public int BuildItemCount { get; }

        public ReferenceReport(
            int flatpackCount,
            int researchingCount,
            int manufacturingCount,
            int baseBlueprintCount,
            int scannerCount,
            int buildItemCount = 0)
        {
            FlatpackCount = flatpackCount;
            ResearchingCount = researchingCount;
            ManufacturingCount = manufacturingCount;
            BaseBlueprintCount = baseBlueprintCount;
            ScannerCount = scannerCount;
            BuildItemCount = buildItemCount;
            TotalCount = flatpackCount + researchingCount + manufacturingCount
                       + baseBlueprintCount + scannerCount + buildItemCount;
        }

        public static readonly ReferenceReport Empty = new ReferenceReport(0, 0, 0, 0, 0, 0);
    }
}
