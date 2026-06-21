// -----------------------------------------------------------------------
// <copyright file="AtomicWritePropertyTests.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.IO;
using FsCheck;
using Microsoft.Data.Sqlite;
using NUnit.Framework;
using OE2EmpireTracker.Common.Storage;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Tests.Storage
{
    /// <summary>
    /// Property tests validating Atomic Write Safety (Correctness Property 3):
    /// After a successful write (upsert) operation, the data file/database is in
    /// a consistent state that can be loaded by a fresh backend instance and
    /// returns the expected data.
    /// **Validates: Requirements 10, Criteria 3-4**
    /// </summary>
    [TestFixture]
    public class AtomicWritePropertyTests
    {
        /// <summary>
        /// Property: After N upsert operations on JsonSingleFileBackend, a fresh
        /// backend instance loading from the same directory reads all N entities.
        /// This proves atomic writes completed and no data was lost.
        /// **Validates: Requirements 2.3, 10.4**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 30)]
        public Property JsonSingleFile_AfterUpsert_NewInstanceLoadsData()
        {
            var gen = Gen.Choose(1, 10);

            return Prop.ForAll(gen.ToArbitrary(), count =>
            {
                var tempDir = CreateTempDir("JsonAtomicWrite");
                try
                {
                    var config1 = new StorageBackendConfig { ConnectionString = tempDir };
                    var backend1 = new JsonSingleFileBackend(config1);
                    backend1.InitializeAsync().GetAwaiter().GetResult();

                    for (int i = 0; i < count; i++)
                    {
                        var colony = new Colony
                        {
                            UUID = "col-" + i,
                            OwnerUUID = "char-1",
                            ColonyName = "Colony" + i,
                            SystemName = "System" + i,
                        };

                        backend1.UpsertColonyAsync("char-1", colony).GetAwaiter().GetResult();
                    }

                    var config2 = new StorageBackendConfig { ConnectionString = tempDir };
                    var backend2 = new JsonSingleFileBackend(config2);
                    backend2.InitializeAsync().GetAwaiter().GetResult();

                    var loaded = backend2.GetAllColoniesAsync("char-1").GetAwaiter().GetResult();
                    return (loaded.Count == count)
                        .Label($"Expected {count} colonies, got {loaded.Count}");
                }
                finally
                {
                    DeleteTempDir(tempDir);
                }
            });
        }

        /// <summary>
        /// Property: After N upsert operations on SqliteBackend, a fresh
        /// backend instance opening the same database reads all N entities.
        /// This proves transactional writes completed and the DB is consistent.
        /// **Validates: Requirements 4.8, 10.3**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 30)]
        public Property Sqlite_AfterUpsert_NewInstanceLoadsData()
        {
            var gen = Gen.Choose(1, 10);

            return Prop.ForAll(gen.ToArbitrary(), count =>
            {
                var dbPath = CreateTempDbPath("SqliteAtomicWrite");
                try
                {
                    var config1 = new StorageBackendConfig { ConnectionString = dbPath };
                    var backend1 = new SqliteBackend(config1);
                    backend1.InitializeAsync().GetAwaiter().GetResult();

                    for (int i = 0; i < count; i++)
                    {
                        var colony = new Colony
                        {
                            UUID = "col-" + i,
                            OwnerUUID = "char-1",
                            ColonyName = "Colony" + i,
                            SystemName = "System" + i,
                        };

                        backend1.UpsertColonyAsync("char-1", colony).GetAwaiter().GetResult();
                    }

                    var config2 = new StorageBackendConfig { ConnectionString = dbPath };
                    var backend2 = new SqliteBackend(config2);
                    backend2.InitializeAsync().GetAwaiter().GetResult();

                    var loaded = backend2.GetAllColoniesAsync("char-1").GetAwaiter().GetResult();
                    return (loaded.Count == count)
                        .Label($"Expected {count} colonies, got {loaded.Count}");
                }
                finally
                {
                    SqliteConnection.ClearAllPools();
                    DeleteTempDb(dbPath);
                }
            });
        }

        /// <summary>
        /// Property: After multiple writes to JsonSingleFileBackend, a .bak backup
        /// file exists (SafeFileWriter creates it on every overwrite). This ensures
        /// the atomic write strategy preserves a recovery point.
        /// **Validates: Requirements 2.3, 10.4**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 20)]
        public Property JsonSingleFile_BackupFileExists_AfterMultipleWrites()
        {
            var gen = Gen.Choose(2, 8);

            return Prop.ForAll(gen.ToArbitrary(), writeCount =>
            {
                var tempDir = CreateTempDir("JsonBackupFile");
                try
                {
                    var config = new StorageBackendConfig { ConnectionString = tempDir };
                    var backend = new JsonSingleFileBackend(config);
                    backend.InitializeAsync().GetAwaiter().GetResult();

                    for (int i = 0; i < writeCount; i++)
                    {
                        var colony = new Colony
                        {
                            UUID = "col-bak-" + i,
                            OwnerUUID = "char-1",
                            ColonyName = "Backup" + i,
                            SystemName = "S" + i,
                        };

                        backend.UpsertColonyAsync("char-1", colony).GetAwaiter().GetResult();
                    }

                    string bakPath = Path.Combine(tempDir, "PlayerData.json.bak");
                    return File.Exists(bakPath)
                        .Label($"Expected .bak file at {bakPath} after {writeCount} writes");
                }
                finally
                {
                    DeleteTempDir(tempDir);
                }
            });
        }

        private static string CreateTempDir(string prefix)
        {
            string dir = Path.Combine(Path.GetTempPath(), prefix + "_" + Path.GetRandomFileName());
            Directory.CreateDirectory(dir);
            return dir;
        }

        private static void DeleteTempDir(string dir)
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, true);
            }
        }

        private static string CreateTempDbPath(string prefix)
        {
            return Path.Combine(Path.GetTempPath(), prefix + "_" + Path.GetRandomFileName() + ".db");
        }

        private static void DeleteTempDb(string dbPath)
        {
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
            }

            string walPath = dbPath + "-wal";
            if (File.Exists(walPath))
            {
                File.Delete(walPath);
            }

            string shmPath = dbPath + "-shm";
            if (File.Exists(shmPath))
            {
                File.Delete(shmPath);
            }
        }
    }
}
