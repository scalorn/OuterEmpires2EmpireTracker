// -----------------------------------------------------------------------
// <copyright file="MigrationRoundTripPropertyTests.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using NUnit.Framework;
using OE2EmpireTracker.Common.Interfaces;
using OE2EmpireTracker.Common.Storage;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Round-trip fidelity property tests for migration between backend pairs.
    /// Verifies that migrating data source→destination→freshSource produces
    /// identical serialized JSON output via JsonSettings and SerializationSorter.
    /// **Validates: Requirements 7.1, 7.8**
    /// </summary>
    [TestFixture]
    public class MigrationRoundTripPropertyTests
    {
        private readonly List<string> _tempDirs = new List<string>();
        private readonly List<string> _tempDbPaths = new List<string>();

        /// <summary>
        /// Cleans up all temporary directories and database files created during tests.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            foreach (var dir in _tempDirs)
            {
                try
                {
                    if (Directory.Exists(dir))
                    {
                        Directory.Delete(dir, true);
                    }
                }
                catch (IOException)
                {
                    // Best-effort cleanup; temp files will be cleaned by OS.
                }
            }

            foreach (var dbPath in _tempDbPaths)
            {
                SqliteConnection.ClearAllPools();
                try
                {
                    if (File.Exists(dbPath))
                    {
                        File.Delete(dbPath);
                    }
                }
                catch (IOException)
                {
                    // Best-effort cleanup.
                }
            }

            _tempDirs.Clear();
            _tempDbPaths.Clear();
        }

        /// <summary>
        /// Asserts round-trip fidelity for a single entity through a backend pair.
        /// 1. Write entity to source backend.
        /// 2. Migrate source → destination via MigrationService.
        /// 3. Migrate destination → fresh source via MigrationService.
        /// 4. Read entity from fresh source.
        /// 5. Serialize original and round-tripped with JsonSettings + SerializationSorter ordering.
        /// 6. Assert JSON strings are identical.
        /// </summary>
        /// <typeparam name="T">The entity type being round-tripped.</typeparam>
        /// <param name="original">The original entity instance.</param>
        /// <param name="characterUuid">The character UUID to associate with the entity.</param>
        /// <param name="write">Delegate to write the entity to a backend.</param>
        /// <param name="read">Delegate to read entities of this type from a backend.</param>
        /// <param name="sort">
        /// Optional delegate to apply deterministic sorting before serialization.
        /// If null, the entity is serialized as-is.
        /// </param>
        /// <returns>True if the round-trip produces identical JSON; false otherwise.</returns>
        protected bool AssertRoundTrip<T>(
            T original,
            string characterUuid,
            Func<IStorageBackend, string, T, Task> write,
            Func<IStorageBackend, string, Task<IReadOnlyList<T>>> read,
            Func<T, T> sort = null)
        {
            var source = CreateJsonSingleFileBackend();
            var destination = CreateSqliteBackend();
            var freshSource = CreateJsonSingleFileBackend();

            var migrationService = new MigrationService();

            // Step 1: Write original entity to source.
            Task.Run(() => write(source, characterUuid, original))
                .GetAwaiter().GetResult();

            // Step 2: Migrate source → destination.
            Task.Run(() => migrationService.MigrateAsync(source, destination))
                .GetAwaiter().GetResult();

            // Step 3: Migrate destination → fresh source.
            Task.Run(() => migrationService.MigrateAsync(destination, freshSource))
                .GetAwaiter().GetResult();

            // Step 4: Read entity from fresh source.
            var roundTripped = Task.Run(() => read(freshSource, characterUuid))
                .GetAwaiter().GetResult();

            if (roundTripped == null || roundTripped.Count == 0)
            {
                return false;
            }

            // Step 5: Serialize both with JsonSettings and compare.
            T originalForCompare = sort != null ? sort(original) : original;
            T roundTrippedForCompare = sort != null ? sort(roundTripped[0]) : roundTripped[0];

            string originalJson = JsonConvert.SerializeObject(
                originalForCompare, JsonSettings.SerializerSettings);
            string roundTrippedJson = JsonConvert.SerializeObject(
                roundTrippedForCompare, JsonSettings.SerializerSettings);

            return originalJson == roundTrippedJson;
        }

        /// <summary>
        /// Creates an initialized JsonSingleFileBackend in a temporary directory.
        /// </summary>
        /// <returns>An initialized JsonSingleFileBackend instance.</returns>
        protected IStorageBackend CreateJsonSingleFileBackend()
        {
            string tempDir = Path.Combine(
                Path.GetTempPath(),
                "MigRoundTrip_Json_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            _tempDirs.Add(tempDir);

            var config = new StorageBackendConfig { ConnectionString = tempDir };
            var backend = new JsonSingleFileBackend(config);
            Task.Run(() => backend.InitializeAsync()).GetAwaiter().GetResult();
            return backend;
        }

        /// <summary>
        /// Creates an initialized SqliteBackend using a temporary database file.
        /// </summary>
        /// <returns>An initialized SqliteBackend instance.</returns>
        protected IStorageBackend CreateSqliteBackend()
        {
            string dbPath = Path.Combine(
                Path.GetTempPath(),
                "MigRoundTrip_Sqlite_" + Guid.NewGuid().ToString("N") + ".db");
            _tempDbPaths.Add(dbPath);

            var config = new StorageBackendConfig { ConnectionString = dbPath };
            var backend = new SqliteBackend(config);
            Task.Run(() => backend.InitializeAsync()).GetAwaiter().GetResult();
            return backend;
        }
    }
}
