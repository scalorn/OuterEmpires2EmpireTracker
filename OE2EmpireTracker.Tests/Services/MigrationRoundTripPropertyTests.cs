// -----------------------------------------------------------------------
// <copyright file="MigrationRoundTripPropertyTests.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FsCheck;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using NUnit.Framework;
using OE2EmpireTracker.Common.Interfaces;
using OE2EmpireTracker.Common.Storage;
using OE2EmpireTracker.Models;
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

        /// <summary>
        /// Colony round-trip: JsonSingleFile → Sqlite → JsonSingleFile.
        /// **Validates: Requirements 7.1, 7.8**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property Colony_RoundTrip_JsonSingleFile_Sqlite()
        {
            return Prop.ForAll(EntityGenerators.ArbColony(), colony =>
            {
                colony.OwnerUUID = "test-char";
                return AssertRoundTrip(
                    colony,
                    "test-char",
                    (backend, charUuid, entity) => backend.UpsertColonyAsync(charUuid, entity),
                    (backend, charUuid) => backend.GetAllColoniesAsync(charUuid));
            });
        }

        /// <summary>
        /// Blueprint round-trip: JsonSingleFile → Sqlite → JsonSingleFile.
        /// **Validates: Requirements 7.1, 7.8**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property Blueprint_RoundTrip_JsonSingleFile_Sqlite()
        {
            return Prop.ForAll(EntityGenerators.ArbBlueprint(), blueprint =>
            {
                blueprint.OwnerUUID = "test-char";
                return AssertRoundTrip(
                    blueprint,
                    "test-char",
                    (backend, charUuid, entity) => backend.UpsertBlueprintAsync(charUuid, entity),
                    (backend, charUuid) => backend.GetAllBlueprintsAsync(charUuid));
            });
        }

        /// <summary>
        /// Survey round-trip: JsonSingleFile → Sqlite → JsonSingleFile.
        /// **Validates: Requirements 7.1, 7.8**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property Survey_RoundTrip_JsonSingleFile_Sqlite()
        {
            return Prop.ForAll(EntityGenerators.ArbSurvey(), survey =>
            {
                survey.OwnerUUID = "test-char";
                return AssertRoundTrip(
                    survey,
                    "test-char",
                    (backend, charUuid, entity) => backend.UpsertSurveyAsync(charUuid, entity),
                    (backend, charUuid) => backend.GetAllSurveysAsync(charUuid));
            });
        }

        /// <summary>
        /// PlayerProfile round-trip: JsonSingleFile → Sqlite → JsonSingleFile.
        /// **Validates: Requirements 7.1, 7.8**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property PlayerProfile_RoundTrip_JsonSingleFile_Sqlite()
        {
            return Prop.ForAll(EntityGenerators.ArbPlayerProfile(), profile =>
            {
                profile.UUID = "test-char";
                return AssertRoundTrip(
                    profile,
                    "test-char",
                    (backend, charUuid, entity) => backend.UpsertPlayerProfileAsync(charUuid, entity),
                    (backend, charUuid) => backend.GetAllPlayerProfilesAsync(charUuid));
            });
        }

        /// <summary>
        /// DeliveryRoute round-trip: JsonSingleFile → Sqlite → JsonSingleFile.
        /// **Validates: Requirements 7.1, 7.8**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property DeliveryRoute_RoundTrip_JsonSingleFile_Sqlite()
        {
            return Prop.ForAll(EntityGenerators.ArbDeliveryRoute(), route =>
            {
                route.OwnerUUID = "test-char";
                return AssertRoundTrip(
                    route,
                    "test-char",
                    (backend, charUuid, entity) => backend.UpsertDeliveryRouteAsync(charUuid, entity),
                    (backend, charUuid) => backend.GetAllDeliveryRoutesAsync(charUuid));
            });
        }

        /// <summary>
        /// DeliveryPlan round-trip: JsonSingleFile → Sqlite → JsonSingleFile.
        /// **Validates: Requirements 7.1, 7.8**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property DeliveryPlan_RoundTrip_JsonSingleFile_Sqlite()
        {
            return Prop.ForAll(EntityGenerators.ArbDeliveryPlan(), plan =>
            {
                plan.OwnerUUID = "test-char";
                return AssertRoundTrip(
                    plan,
                    "test-char",
                    (backend, charUuid, entity) => backend.UpsertDeliveryPlanAsync(charUuid, entity),
                    (backend, charUuid) => backend.GetAllDeliveryPlansAsync(charUuid));
            });
        }

        /// <summary>
        /// PricingPlan round-trip: JsonSingleFile → Sqlite → JsonSingleFile.
        /// **Validates: Requirements 7.1, 7.8**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property PricingPlan_RoundTrip_JsonSingleFile_Sqlite()
        {
            return Prop.ForAll(EntityGenerators.ArbPricingPlan(), plan =>
            {
                plan.OwnerUUID = "test-char";
                return AssertRoundTrip(
                    plan,
                    "test-char",
                    (backend, charUuid, entity) => backend.UpsertPricingPlanAsync(charUuid, entity),
                    (backend, charUuid) => backend.GetAllPricingPlansAsync(charUuid));
            });
        }

        /// <summary>
        /// BuildPlan round-trip: JsonSingleFile → Sqlite → JsonSingleFile.
        /// **Validates: Requirements 7.1, 7.8**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property BuildPlan_RoundTrip_JsonSingleFile_Sqlite()
        {
            return Prop.ForAll(EntityGenerators.ArbBuildPlan(), plan =>
            {
                plan.OwnerUUID = "test-char";
                return AssertRoundTrip(
                    plan,
                    "test-char",
                    (backend, charUuid, entity) => backend.UpsertBuildPlanAsync(charUuid, entity),
                    (backend, charUuid) => backend.GetAllBuildPlansAsync(charUuid));
            });
        }

        /// <summary>
        /// ShipTemplate round-trip: JsonSingleFile → Sqlite → JsonSingleFile.
        /// **Validates: Requirements 7.1, 7.8**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property ShipTemplate_RoundTrip_JsonSingleFile_Sqlite()
        {
            return Prop.ForAll(EntityGenerators.ArbShipTemplate(), template =>
            {
                template.OwnerUUID = "test-char";
                return AssertRoundTrip(
                    template,
                    "test-char",
                    (backend, charUuid, entity) => backend.UpsertShipTemplateAsync(charUuid, entity),
                    (backend, charUuid) => backend.GetAllShipTemplatesAsync(charUuid));
            });
        }

        /// <summary>
        /// Ship round-trip: JsonSingleFile → Sqlite → JsonSingleFile.
        /// **Validates: Requirements 7.1, 7.8**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property Ship_RoundTrip_JsonSingleFile_Sqlite()
        {
            return Prop.ForAll(EntityGenerators.ArbShip(), ship =>
            {
                ship.OwnerUUID = "test-char";
                return AssertRoundTrip(
                    ship,
                    "test-char",
                    (backend, charUuid, entity) => backend.UpsertShipAsync(charUuid, entity),
                    (backend, charUuid) => backend.GetAllShipsAsync(charUuid));
            });
        }

        /// <summary>
        /// Station round-trip: JsonSingleFile → Sqlite → JsonSingleFile.
        /// **Validates: Requirements 7.1, 7.8**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property Station_RoundTrip_JsonSingleFile_Sqlite()
        {
            return Prop.ForAll(EntityGenerators.ArbStation(), station =>
            {
                station.OwnerUUID = "test-char";
                return AssertRoundTrip(
                    station,
                    "test-char",
                    (backend, charUuid, entity) => backend.UpsertStationAsync(charUuid, entity),
                    (backend, charUuid) => backend.GetAllStationsAsync(charUuid));
            });
        }
    }
}

    /// <summary>
    /// Round-trip property tests for the remaining 11 per-character entity types
    /// across JsonSingleFile↔Sqlite backend pair.
    /// **Validates: Requirements 7.1, 7.2, 7.8**
    /// </summary>
    [TestFixture]
    public class MigrationRoundTripEntityTests : MigrationRoundTripPropertyTests
    {
        /// <summary>
        /// Verifies MarketListing round-trip fidelity through JsonSingleFile↔Sqlite.
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property MarketListing_RoundTrip_JsonSingleFile_Sqlite()
        {
            return Prop.ForAll(EntityGenerators.ArbMarketListing(), entity =>
            {
                entity.OwnerUUID = "test-char";
                return AssertRoundTrip(
                    entity,
                    "test-char",
                    (backend, charUuid, e) => backend.UpsertMarketListingAsync(charUuid, e),
                    (backend, charUuid) => backend.GetAllMarketListingsAsync(charUuid));
            });
        }

        /// <summary>
        /// Verifies MarketTransaction round-trip fidelity including decimal precision.
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property MarketTransaction_RoundTrip_JsonSingleFile_Sqlite()
        {
            return Prop.ForAll(EntityGenerators.ArbMarketTransaction(), entity =>
            {
                entity.OwnerUUID = "test-char";
                return AssertRoundTrip(
                    entity,
                    "test-char",
                    (backend, charUuid, e) => backend.UpsertMarketTransactionAsync(charUuid, e),
                    (backend, charUuid) => backend.GetAllMarketTransactionsAsync(charUuid));
            });
        }

        /// <summary>
        /// Verifies StockPlan round-trip fidelity through JsonSingleFile↔Sqlite.
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property StockPlan_RoundTrip_JsonSingleFile_Sqlite()
        {
            return Prop.ForAll(EntityGenerators.ArbStockPlan(), entity =>
            {
                entity.OwnerUUID = "test-char";
                return AssertRoundTrip(
                    entity,
                    "test-char",
                    (backend, charUuid, e) => backend.UpsertStockPlanAsync(charUuid, e),
                    (backend, charUuid) => backend.GetAllStockPlansAsync(charUuid));
            });
        }

        /// <summary>
        /// Verifies StockProfile round-trip fidelity through JsonSingleFile↔Sqlite.
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property StockProfile_RoundTrip_JsonSingleFile_Sqlite()
        {
            return Prop.ForAll(EntityGenerators.ArbStockProfile(), entity =>
            {
                entity.OwnerUUID = "test-char";
                return AssertRoundTrip(
                    entity,
                    "test-char",
                    (backend, charUuid, e) => backend.UpsertStockProfileAsync(charUuid, e),
                    (backend, charUuid) => backend.GetAllStockProfilesAsync(charUuid));
            });
        }

        /// <summary>
        /// Verifies SupplyChain round-trip fidelity through JsonSingleFile↔Sqlite.
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property SupplyChain_RoundTrip_JsonSingleFile_Sqlite()
        {
            return Prop.ForAll(EntityGenerators.ArbSupplyChain(), entity =>
            {
                entity.OwnerUUID = "test-char";
                return AssertRoundTrip(
                    entity,
                    "test-char",
                    (backend, charUuid, e) => backend.UpsertSupplyChainAsync(charUuid, e),
                    (backend, charUuid) => backend.GetAllSupplyChainsAsync(charUuid));
            });
        }

        /// <summary>
        /// Verifies WarehouseOverflowRule round-trip fidelity through JsonSingleFile↔Sqlite.
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property WarehouseOverflowRule_RoundTrip_JsonSingleFile_Sqlite()
        {
            return Prop.ForAll(EntityGenerators.ArbWarehouseOverflowRule(), entity =>
            {
                entity.OwnerUUID = "test-char";
                return AssertRoundTrip(
                    entity,
                    "test-char",
                    (backend, charUuid, e) => backend.UpsertWarehouseOverflowRuleAsync(charUuid, e),
                    (backend, charUuid) => backend.GetAllWarehouseOverflowRulesAsync(charUuid));
            });
        }

        /// <summary>
        /// Verifies Asteroid round-trip fidelity through JsonSingleFile↔Sqlite.
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property Asteroid_RoundTrip_JsonSingleFile_Sqlite()
        {
            return Prop.ForAll(EntityGenerators.ArbAsteroid(), entity =>
            {
                entity.OwnerUUID = "test-char";
                return AssertRoundTrip(
                    entity,
                    "test-char",
                    (backend, charUuid, e) => backend.UpsertAsteroidAsync(charUuid, e),
                    (backend, charUuid) => backend.GetAllAsteroidsAsync(charUuid));
            });
        }

        /// <summary>
        /// Verifies BankingTransaction round-trip fidelity including decimal precision.
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property BankingTransaction_RoundTrip_JsonSingleFile_Sqlite()
        {
            return Prop.ForAll(EntityGenerators.ArbBankingTransaction(), entity =>
            {
                entity.OwnerUUID = "test-char";
                return AssertRoundTrip(
                    entity,
                    "test-char",
                    (backend, charUuid, e) => backend.UpsertBankingTransactionAsync(charUuid, e),
                    (backend, charUuid) => backend.GetAllBankingTransactionsAsync(charUuid));
            });
        }

        /// <summary>
        /// Verifies MailMessage round-trip fidelity through JsonSingleFile↔Sqlite.
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property MailMessage_RoundTrip_JsonSingleFile_Sqlite()
        {
            return Prop.ForAll(EntityGenerators.ArbMailMessage(), entity =>
            {
                entity.OwnerUUID = "test-char";
                return AssertRoundTrip(
                    entity,
                    "test-char",
                    (backend, charUuid, e) => backend.UpsertMailMessageAsync(charUuid, e),
                    (backend, charUuid) => backend.GetAllMailMessagesAsync(charUuid));
            });
        }

        /// <summary>
        /// Verifies Faction (per-character contacts) round-trip fidelity through JsonSingleFile↔Sqlite.
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property Faction_RoundTrip_JsonSingleFile_Sqlite()
        {
            return Prop.ForAll(EntityGenerators.ArbFaction(), entity =>
            {
                entity.OwnerUUID = "test-char";
                return AssertRoundTrip(
                    entity,
                    "test-char",
                    (backend, charUuid, e) => backend.UpsertFactionForCharacterAsync(charUuid, e),
                    (backend, charUuid) => backend.GetAllFactionsForCharacterAsync(charUuid));
            });
        }

        /// <summary>
        /// Verifies ExternalCharacter round-trip fidelity through JsonSingleFile↔Sqlite.
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property ExternalCharacter_RoundTrip_JsonSingleFile_Sqlite()
        {
            return Prop.ForAll(EntityGenerators.ArbExternalCharacter(), entity =>
            {
                entity.OwnerUUID = "test-char";
                return AssertRoundTrip(
                    entity,
                    "test-char",
                    (backend, charUuid, e) => backend.UpsertExternalCharacterAsync(charUuid, e),
                    (backend, charUuid) => backend.GetAllExternalCharactersAsync(charUuid));
            });
        }
    }
}
