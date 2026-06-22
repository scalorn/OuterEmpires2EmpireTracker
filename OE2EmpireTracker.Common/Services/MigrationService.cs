// <copyright file="MigrationService.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using OE2EmpireTracker.Common.Interfaces;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Migrates all entity data from any source <see cref="IStorageBackend"/> to any destination.
    /// </summary>
    public class MigrationService
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Migrates all data from source to destination.
        /// </summary>
        /// <param name="source">Source backend to read from.</param>
        /// <param name="destination">Destination backend to write to.</param>
        /// <param name="progress">Progress callback (invoked at least once per entity type).</param>
        /// <param name="characterUUIDs">
        /// Optional explicit list of character UUIDs to migrate.
        /// If null, discovers all characters from source via GetAllCharacterUUIDsAsync.
        /// </param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A task representing the asynchronous migration operation.</returns>
        public async Task MigrateAsync(
            IStorageBackend source,
            IStorageBackend destination,
            IProgress<MigrationProgress> progress = null,
            IReadOnlyList<string> characterUUIDs = null,
            CancellationToken ct = default)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            // Discover characters
            var uuids = characterUUIDs ?? await source.GetAllCharacterUUIDsAsync().ConfigureAwait(false);
            Log.Info("Migration starting — {0} character(s) to migrate", uuids.Count);

            int totalProcessed = 0;

#pragma warning disable CS8321 // Local function declared but never used — called by subsequent tasks (18.2-18.7)
            void Report(string phase, string entityType)
            {
                totalProcessed++;
                progress?.Report(new MigrationProgress
                {
                    Phase = phase,
                    CurrentEntityType = entityType,
                    EntitiesProcessed = totalProcessed,
                });
            }
#pragma warning restore CS8321

            // TODO: Task 18.2 — server-global data migration
            // TODO: Task 18.3 — baseline data migration
            // TODO: Task 18.4/18.5 — per-character entity migration
            // TODO: Task 18.6 — permissions/intel/audit migration
            // TODO: Task 18.7 — count validation

            Log.Info("Migration complete — {0} entities processed", totalProcessed);
        }
    }
}
