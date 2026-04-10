using System.Collections.Generic;
using System.Linq;

namespace OE2EmpireTracker.Services.Migration
{
    public static class RenameTable
    {
        private static readonly List<RenameEntry> _entries = new List<RenameEntry>
        {
            // Append new renames here. Never remove old entries.
        };

        public static void Apply(EmpireContext ec, PlayerContext pc)
        {
            foreach (var entry in _entries)
            {
                string oldUUID = DeterministicUUID.Generate(
                    entry.OldName, entry.Evolution,
                    entry.BluePrintType, entry.Class, entry.TechLevel);
                string newUUID = DeterministicUUID.Generate(
                    entry.NewName, entry.Evolution,
                    entry.BluePrintType, entry.Class, entry.TechLevel);

                var bp = ec.globalBlueprintList
                    .FirstOrDefault(b => b.UUID == oldUUID);
                if (bp == null) continue;

                bp.Name = entry.NewName;
                bp.UUID = newUUID;
                RemapUUID.Remap(ec, pc, oldUUID, newUUID);
            }
        }
    }

    public class RenameEntry
    {
        public string OldName { get; }
        public string NewName { get; }
        public int Evolution { get; }
        public string BluePrintType { get; }
        public int Class { get; }
        public string TechLevel { get; }

        public RenameEntry(string oldName, string newName,
            int evolution, string blueprintType, int cls, string techLevel)
        {
            OldName = oldName;
            NewName = newName;
            Evolution = evolution;
            BluePrintType = blueprintType;
            Class = cls;
            TechLevel = techLevel;
        }
    }
}
