using OE2EmpireTracker.Models;
using System.Collections.Generic;
using System.Linq;

namespace OE2EmpireTracker.Services
{
    public class BlueprintReferenceCounter
    {
        private readonly IEnumerable<Colony> _colonies;
        private readonly IEnumerable<Blueprint> _allBlueprints;
        private readonly IEnumerable<Survey> _surveys;

        public BlueprintReferenceCounter(
            IEnumerable<Colony> colonies,
            IEnumerable<Blueprint> allBlueprints,
            IEnumerable<Survey> surveys)
        {
            _colonies = colonies ?? Enumerable.Empty<Colony>();
            _allBlueprints = allBlueprints ?? Enumerable.Empty<Blueprint>();
            _surveys = surveys ?? Enumerable.Empty<Survey>();
        }

        public ReferenceReport CountReferences(string blueprintUUID)
        {
            if (string.IsNullOrEmpty(blueprintUUID))
                return ReferenceReport.Empty;

            var allStructures = _colonies
                .Where(c => c.Structures != null)
                .SelectMany(c => c.Structures);

            int flatpackCount = allStructures
                .Count(s => s.FlatpackBlueprintUUID == blueprintUUID);

            int researchingCount = allStructures
                .Count(s => s.ResearchingBlueprintUUID == blueprintUUID);

            int manufacturingCount = allStructures
                .Count(s => s.ManufacturingBlueprintUUID == blueprintUUID);

            int baseBlueprintCount = _allBlueprints
                .Count(b => b.BaseBlueprintUUID == blueprintUUID && b.UUID != blueprintUUID);

            int scannerCount = _surveys
                .Count(s => s.ScannerBlueprintUUID == blueprintUUID);

            return new ReferenceReport(
                flatpackCount,
                researchingCount,
                manufacturingCount,
                baseBlueprintCount,
                scannerCount);
        }
    }
}
