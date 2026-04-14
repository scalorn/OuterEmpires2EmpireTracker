using NLog;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace OE2EmpireTracker.Services.Migration
{
    public static class MigrationRunner
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public const int CurrentVersion = 6;

        /// <summary>
        /// Set to true when a migration fails. Prevents saving data in a
        /// partially-migrated state.
        /// </summary>
        public static bool MigrationFailed { get; private set; }

        private static readonly Dictionary<int, Action<EmpireContext, PlayerContext>>
            Migrations = new Dictionary<int, Action<EmpireContext, PlayerContext>>
        {
            { 1, Migration001_DeterministicUUIDs.Run },
            { 2, Migration002_ColonyDeterministicUUIDs.Run },
            { 3, Migration003_SurveyDateTimeNormalization.Run },
            { 4, Migration004_ColonyImportTimestampBackfill.Run },
            { 5, Migration005_PropertyKeyCleanup.Run },
            { 6, Migration006_DisplaySequenceJsonKey.Run },
        };

        public static void Run(EmpireContext ec, PlayerContext pc)
        {
            MigrationFailed = false;

            try
            {
                RenameTable.Apply(ec, pc);
            }
            catch (Exception ex)
            {
                HandleFailure("rename table (pre-migration)", ex);
                return;
            }

            int baselineVersion = ec.DataVersion;
            int playerVersion = pc.DataVersion;
            Log.Info("Migration check: baseline v{0}, player v{1}, target v{2}",
                baselineVersion, playerVersion, CurrentVersion);

            for (int v = Math.Min(baselineVersion, playerVersion) + 1;
                 v <= CurrentVersion; v++)
            {
                if (Migrations.TryGetValue(v, out var migration))
                {
                    try
                    {
                        Log.Info("Running migration v{0}", v);
                        migration(ec, pc);
                        Log.Info("Migration v{0} completed", v);
                    }
                    catch (Exception ex)
                    {
                        HandleFailure($"migration v{v}", ex);
                        return;
                    }
                }
            }

            ec.DataVersion = CurrentVersion;
            pc.DataVersion = CurrentVersion;

            try
            {
                RenameTable.Apply(ec, pc);
            }
            catch (Exception ex)
            {
                HandleFailure("rename table (post-migration)", ex);
                return;
            }

            Log.Info("All migrations complete, DataVersion set to {0}", CurrentVersion);
        }

        private static void HandleFailure(string phase, Exception ex)
        {
            MigrationFailed = true;
            Log.Error(ex, "Migration failed during {0}", phase);
            MessageBox.Show(
                $"Data migration failed during {phase}.\n\n" +
                $"{ex.Message}\n\n" +
                "Your data files have NOT been modified. " +
                "Saving is disabled to prevent data corruption. " +
                "Please report this error.",
                "Migration Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        /// <summary>
        /// Resets the failure flag. Used by tests and after a successful
        /// File -> New / File -> Open that reloads clean data.
        /// </summary>
        public static void ResetFailureState()
        {
            MigrationFailed = false;
        }
    }
}
