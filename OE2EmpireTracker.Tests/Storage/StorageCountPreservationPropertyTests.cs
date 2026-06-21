// -----------------------------------------------------------------------
// <copyright file="StorageCountPreservationPropertyTests.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.IO;
using FsCheck;
using Microsoft.Data.Sqlite;
using NUnit.Framework;
using OE2EmpireTracker.Common.Storage;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Tests.Storage
{
    /// <summary>
    /// Property-based tests verifying Entity Count Preservation across storage backends.
    /// For any sequence of N upsert operations on a backend, GetAll returns exactly N entities
    /// (no data loss, no duplication).
    /// **Validates: Requirements 2.1, 2.8, 3.1, 4.1, 4.2, 5.1, 6.1**
    /// </summary>
    [TestFixture]
    public class StorageCountPreservationPropertyTests
    {
        // -----------------------------------------------------------------
        // Property 1: JsonSingleFile — Upsert N distinct colonies, GetAll returns N
        // **Validates: Requirements 2.1, 2.8**
        // -----------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 50)]
        public Property JsonSingleFile_UpsertN_GetAllReturnsN()
        {
            var countGen = Gen.Choose(1, 20);

            return Prop.ForAll(Arb.From(countGen), count =>
            {
                string tempDir = CreateTempDir();
                try
                {
                    var config = new StorageBackendConfig { ConnectionString = tempDir };
                    var backend = new JsonSingleFileBackend(config);
                    backend.InitializeAsync().GetAwaiter().GetResult();

                    string charUuid = "char-" + Guid.NewGuid().ToString("N");

                    for (int i = 0; i < count; i++)
                    {
                        var colony = new Colony
                        {
                            UUID = Guid.NewGuid().ToString(),
                            OwnerUUID = charUuid,
                            ColonyName = "Colony " + i,
                            SystemName = "System-1",
                        };

                        backend.UpsertColonyAsync(charUuid, colony).GetAwaiter().GetResult();
                    }

                    var all = backend.GetAllColoniesAsync(charUuid).GetAwaiter().GetResult();

                    return (all.Count == count)
                        .Label("Expected " + count + " colonies, got " + all.Count);
                }
                finally
                {
                    DeleteTempDir(tempDir);
                }
            });
        }

        // -----------------------------------------------------------------
        // Property 1: SQLite — Upsert N distinct colonies, GetAll returns N
        // **Validates: Requirements 4.1, 4.2**
        // -----------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 50)]
        public Property Sqlite_UpsertN_GetAllReturnsN()
        {
            var countGen = Gen.Choose(1, 20);

            return Prop.ForAll(Arb.From(countGen), count =>
            {
                string dbPath = Path.Combine(
                    Path.GetTempPath(),
                    "PBT_SqliteCount_" + Path.GetRandomFileName() + ".db");
                try
                {
                    var config = new StorageBackendConfig { ConnectionString = dbPath };
                    var backend = new SqliteBackend(config);
                    backend.InitializeAsync().GetAwaiter().GetResult();

                    string charUuid = "char-" + Guid.NewGuid().ToString("N");

                    for (int i = 0; i < count; i++)
                    {
                        var colony = new Colony
                        {
                            UUID = Guid.NewGuid().ToString(),
                            OwnerUUID = charUuid,
                            ColonyName = "Colony " + i,
                            SystemName = "System-1",
                        };

                        backend.UpsertColonyAsync(charUuid, colony).GetAwaiter().GetResult();
                    }

                    var all = backend.GetAllColoniesAsync(charUuid).GetAwaiter().GetResult();

                    return (all.Count == count)
                        .Label("Expected " + count + " colonies, got " + all.Count);
                }
                finally
                {
                    SqliteConnection.ClearAllPools();
                    if (File.Exists(dbPath))
                    {
                        File.Delete(dbPath);
                    }
                }
            });
        }

        // -----------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------

        private static string CreateTempDir()
        {
            string path = Path.Combine(Path.GetTempPath(), "PBT_JsonCount_" + Path.GetRandomFileName());
            Directory.CreateDirectory(path);
            return path;
        }

        private static void DeleteTempDir(string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, recursive: true);
                }
            }
            catch (IOException)
            {
                // Best effort cleanup in tests.
            }
        }
    }
}
