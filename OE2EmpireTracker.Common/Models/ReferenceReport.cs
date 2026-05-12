namespace OE2EmpireTracker.Models
{
    public class ReferenceReport
    {
        public static readonly ReferenceReport Empty = new ReferenceReport(0, 0, 0, 0, 0, 0, 0, 0);

        public ReferenceReport(
            int flatpackCount,
            int researchingCount,
            int manufacturingCount,
            int baseBlueprintCount,
            int scannerCount,
            int buildItemCount = 0,
            int shipComponentCount = 0,
            int stockTargetCount = 0)
        {
            FlatpackCount = flatpackCount;
            ResearchingCount = researchingCount;
            ManufacturingCount = manufacturingCount;
            BaseBlueprintCount = baseBlueprintCount;
            ScannerCount = scannerCount;
            BuildItemCount = buildItemCount;
            ShipComponentCount = shipComponentCount;
            StockTargetCount = stockTargetCount;
            TotalCount = flatpackCount + researchingCount + manufacturingCount
                       + baseBlueprintCount + scannerCount + buildItemCount + shipComponentCount
                       + stockTargetCount;
        }

        public int TotalCount { get; }

        public int FlatpackCount { get; }

        public int ResearchingCount { get; }

        public int ManufacturingCount { get; }

        public int BaseBlueprintCount { get; }

        public int ScannerCount { get; }

        public int BuildItemCount { get; }

        public int ShipComponentCount { get; }

        public int StockTargetCount { get; }
    }
}
