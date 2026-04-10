using System;
using System.Collections.Generic;

namespace OE2EmpireTracker.Services.Migration
{
    public static class MigrationRunner
    {
        public const int CurrentVersion = 1;

        private static readonly Dictionary<int, Action<EmpireContext, PlayerContext>>
            Migrations = new Dictionary<int, Action<EmpireContext, PlayerContext>>
        {
            { 1, Migration001_DeterministicUUIDs.Run },
        };

        public static void Run(EmpireContext ec, PlayerContext pc)
        {
            RenameTable.Apply(ec, pc);

            int baselineVersion = ec.DataVersion;
            int playerVersion = pc.DataVersion;

            for (int v = Math.Min(baselineVersion, playerVersion) + 1;
                 v <= CurrentVersion; v++)
            {
                if (Migrations.TryGetValue(v, out var migration))
                    migration(ec, pc);
            }

            ec.DataVersion = CurrentVersion;
            pc.DataVersion = CurrentVersion;

            RenameTable.Apply(ec, pc);
        }
    }
}
