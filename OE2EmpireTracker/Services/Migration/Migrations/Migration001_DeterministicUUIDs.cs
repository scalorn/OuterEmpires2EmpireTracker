using System.Linq;

namespace OE2EmpireTracker.Services.Migration
{
    public static class Migration001_DeterministicUUIDs
    {
        public static void Run(EmpireContext ec, PlayerContext pc)
        {
            foreach (var bp in ec.GlobalBlueprintList.ToList())
            {
                string deterministicUUID = DeterministicUUID.Generate(bp);
                if (bp.UUID == deterministicUUID) continue;

                if (string.IsNullOrEmpty(bp.LegacyUUID))
                    bp.LegacyUUID = bp.UUID;

                RemapUUID.Remap(ec, pc, bp.UUID, deterministicUUID);
            }
        }
    }
}
