using System.Linq;

namespace OE2EmpireTracker.Services.Migration
{
    public static class Migration002_ColonyDeterministicUUIDs
    {
        public static void Run(EmpireContext ec, PlayerContext pc)
        {
            // Phase 1: Fix stale FlatpackBlueprintUUIDs
            var legacyLookup = ec.GlobalBlueprintList
                .Where(bp => !string.IsNullOrEmpty(bp.LegacyUUID))
                .ToDictionary(bp => bp.LegacyUUID, bp => bp.UUID);

            foreach (var colony in pc.ColonyList)
                foreach (var s in colony.Structures)
                {
                    if (!string.IsNullOrEmpty(s.FlatpackBlueprintUUID) &&
                        legacyLookup.TryGetValue(s.FlatpackBlueprintUUID, out string currentUUID))
                    {
                        s.FlatpackBlueprintUUID = currentUUID;
                    }
                }

            // Phase 2: Deterministic colony UUIDs
            foreach (var colony in pc.ColonyList.ToList())
            {
                string deterministicUUID = DeterministicUUID.Generate(colony);
                if (colony.UUID == deterministicUUID) continue;

                if (string.IsNullOrEmpty(colony.LegacyUUID))
                    colony.LegacyUUID = colony.UUID;

                RemapUUID.Remap(ec, pc, colony.UUID, deterministicUUID);
            }
        }
    }
}
