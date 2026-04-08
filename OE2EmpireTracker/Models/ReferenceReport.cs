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

        public ReferenceReport(
            int flatpackCount,
            int researchingCount,
            int manufacturingCount,
            int baseBlueprintCount,
            int scannerCount)
        {
            FlatpackCount = flatpackCount;
            ResearchingCount = researchingCount;
            ManufacturingCount = manufacturingCount;
            BaseBlueprintCount = baseBlueprintCount;
            ScannerCount = scannerCount;
            TotalCount = flatpackCount + researchingCount + manufacturingCount
                       + baseBlueprintCount + scannerCount;
        }

        public static readonly ReferenceReport Empty = new ReferenceReport(0, 0, 0, 0, 0);
    }
}
