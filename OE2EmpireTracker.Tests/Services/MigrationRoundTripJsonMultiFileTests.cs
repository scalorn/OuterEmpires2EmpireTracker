// -----------------------------------------------------------------------
// <copyright file="MigrationRoundTripJsonMultiFileTests.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FsCheck;
using Newtonsoft.Json;
using NUnit.Framework;
using OE2EmpireTracker.Common.Interfaces;
using OE2EmpireTracker.Common.Storage;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Round-trip fidelity property tests for JsonSingleFile↔JsonMultiFile backend pair.
    /// Since both backends use JSON serialization, differences are in file layout only.
    /// **Validates: Requirements 7.1, 7.8**
    /// </summary>
    [TestFixture]
    public class MigrationRoundTripJsonMultiFileTests : MigrationRoundTripPropertyTests
    {
        private readonly List<string> _multiFileTempDirs = new List<string>();

        /// <summary>
        /// Cleans up temporary JsonMultiFile directories after each test.
        /// </summary>
        [TearDown]
        public void TearDownMultiFile()
        {
            foreach (var dir in _multiFileTempDirs)
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

            _multiFileTempDirs.Clear();
        }

        /// <summary>
        /// Creates an initialized JsonMultiFileBackend in a temporary directory.
        /// </summary>
        /// <returns>An initialized JsonMultiFileBackend instance.</returns>
        protected IStorageBackend CreateJsonMultiFileBackend()
        {
            string tempDir = Path.Combine(
                Path.GetTempPath(),
                "MigRoundTrip_Multi_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            _multiFileTempDirs.Add(tempDir);

            var backend = new JsonMultiFileBackend(tempDir);
            Task.Run(() => backend.InitializeAsync()).GetAwaiter().GetResult();
            return backend;
        }

        /// <summary>
        /// Asserts round-trip fidelity: JsonSingleFile → JsonMultiFile → JsonSingleFile.
        /// </summary>
        /// <typeparam name="T">The entity type being round-tripped.</typeparam>
        /// <param name="original">The original entity instance.</param>
        /// <param name="characterUuid">The character UUID to associate with the entity.</param>
        /// <param name="write">Delegate to write the entity to a backend.</param>
        /// <param name="read">Delegate to read entities from a backend.</param>
        /// <returns>True if the round-trip produces identical JSON; false otherwise.</returns>
        private bool AssertRoundTripJsonMulti<T>(
            T original,
            string characterUuid,
            Func<IStorageBackend, string, T, Task> write,
            Func<IStorageBackend, string, Task<IReadOnlyList<T>>> read)
        {
            var source = CreateJsonSingleFileBackend();
            var destination = CreateJsonMultiFileBackend();
            var freshSource = CreateJsonSingleFileBackend();

            var migrationService = new MigrationService();

            // Step 1: Write original entity to source.
            Task.Run(() => write(source, characterUuid, original))
                .GetAwaiter().GetResult();

            // Step 2: Migrate source → destination (JsonMultiFile).
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
            string originalJson = JsonConvert.SerializeObject(
                original, JsonSettings.SerializerSettings);
            string roundTrippedJson = JsonConvert.SerializeObject(
                roundTripped[0], JsonSettings.SerializerSettings);

            return originalJson == roundTrippedJson;
        }

        /// <summary>
        /// Colony round-trip: JsonSingleFile → JsonMultiFile → JsonSingleFile.
        /// **Validates: Requirements 7.1, 7.8**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property Colony_RoundTrip_JsonSingleFile_JsonMultiFile()
        {
            return Prop.ForAll(EntityGenerators.ArbColony(), colony =>
            {
                colony.OwnerUUID = "test-char";
                return AssertRoundTripJsonMulti(
                    colony,
                    "test-char",
                    (backend, charUuid, entity) => backend.UpsertColonyAsync(charUuid, entity),
                    (backend, charUuid) => backend.GetAllColoniesAsync(charUuid));
            });
        }

        /// <summary>
        /// Blueprint round-trip: JsonSingleFile → JsonMultiFile → JsonSingleFile.
        /// **Validates: Requirements 7.1, 7.8**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property Blueprint_RoundTrip_JsonSingleFile_JsonMultiFile()
        {
            return Prop.ForAll(EntityGenerators.ArbBlueprint(), blueprint =>
            {
                blueprint.OwnerUUID = "test-char";
                return AssertRoundTripJsonMulti(
                    blueprint,
                    "test-char",
                    (backend, charUuid, entity) => backend.UpsertBlueprintAsync(charUuid, entity),
                    (backend, charUuid) => backend.GetAllBlueprintsAsync(charUuid));
            });
        }

        /// <summary>
        /// Survey round-trip: JsonSingleFile → JsonMultiFile → JsonSingleFile.
        /// **Validates: Requirements 7.1, 7.8**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property Survey_RoundTrip_JsonSingleFile_JsonMultiFile()
        {
            return Prop.ForAll(EntityGenerators.ArbSurvey(), survey =>
            {
                survey.OwnerUUID = "test-char";
                return AssertRoundTripJsonMulti(
                    survey,
                    "test-char",
                    (backend, charUuid, entity) => backend.UpsertSurveyAsync(charUuid, entity),
                    (backend, charUuid) => backend.GetAllSurveysAsync(charUuid));
            });
        }

        /// <summary>
        /// PlayerProfile round-trip: JsonSingleFile → JsonMultiFile → JsonSingleFile.
        /// **Validates: Requirements 7.1, 7.8**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property PlayerProfile_RoundTrip_JsonSingleFile_JsonMultiFile()
        {
            return Prop.ForAll(EntityGenerators.ArbPlayerProfile(), profile =>
            {
                profile.UUID = "test-char";
                return AssertRoundTripJsonMulti(
                    profile,
                    "test-char",
                    (backend, charUuid, entity) => backend.UpsertPlayerProfileAsync(charUuid, entity),
                    (backend, charUuid) => backend.GetAllPlayerProfilesAsync(charUuid));
            });
        }

        /// <summary>
        /// DeliveryRoute round-trip: JsonSingleFile → JsonMultiFile → JsonSingleFile.
        /// **Validates: Requirements 7.1, 7.8**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property DeliveryRoute_RoundTrip_JsonSingleFile_JsonMultiFile()
        {
            return Prop.ForAll(EntityGenerators.ArbDeliveryRoute(), route =>
            {
                route.OwnerUUID = "test-char";
                return AssertRoundTripJsonMulti(
                    route,
                    "test-char",
                    (backend, charUuid, entity) => backend.UpsertDeliveryRouteAsync(charUuid, entity),
                    (backend, charUuid) => backend.GetAllDeliveryRoutesAsync(charUuid));
            });
        }

        /// <summary>
        /// DeliveryPlan round-trip: JsonSingleFile → JsonMultiFile → JsonSingleFile.
        /// **Validates: Requirements 7.1, 7.8**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property DeliveryPlan_RoundTrip_JsonSingleFile_JsonMultiFile()
        {
            return Prop.ForAll(EntityGenerators.ArbDeliveryPlan(), plan =>
            {
                plan.OwnerUUID = "test-char";
                return AssertRoundTripJsonMulti(
                    plan,
                    "test-char",
                    (backend, charUuid, entity) => backend.UpsertDeliveryPlanAsync(charUuid, entity),
                    (backend, charUuid) => backend.GetAllDeliveryPlansAsync(charUuid));
            });
        }

        /// <summary>
        /// PricingPlan round-trip: JsonSingleFile → JsonMultiFile → JsonSingleFile.
        /// **Validates: Requirements 7.1, 7.8**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property PricingPlan_RoundTrip_JsonSingleFile_JsonMultiFile()
        {
            return Prop.ForAll(EntityGenerators.ArbPricingPlan(), plan =>
            {
                plan.OwnerUUID = "test-char";
                return AssertRoundTripJsonMulti(
                    plan,
                    "test-char",
                    (backend, charUuid, entity) => backend.UpsertPricingPlanAsync(charUuid, entity),
                    (backend, charUuid) => backend.GetAllPricingPlansAsync(charUuid));
            });
        }

        /// <summary>
        /// BuildPlan round-trip: JsonSingleFile → JsonMultiFile → JsonSingleFile.
        /// **Validates: Requirements 7.1, 7.8**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property BuildPlan_RoundTrip_JsonSingleFile_JsonMultiFile()
        {
            return Prop.ForAll(EntityGenerators.ArbBuildPlan(), plan =>
            {
                plan.OwnerUUID = "test-char";
                return AssertRoundTripJsonMulti(
                    plan,
                    "test-char",
                    (backend, charUuid, entity) => backend.UpsertBuildPlanAsync(charUuid, entity),
                    (backend, charUuid) => backend.GetAllBuildPlansAsync(charUuid));
            });
        }

        /// <summary>
        /// ShipTemplate round-trip: JsonSingleFile → JsonMultiFile → JsonSingleFile.
        /// **Validates: Requirements 7.1, 7.8**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property ShipTemplate_RoundTrip_JsonSingleFile_JsonMultiFile()
        {
            return Prop.ForAll(EntityGenerators.ArbShipTemplate(), template =>
            {
                template.OwnerUUID = "test-char";
                return AssertRoundTripJsonMulti(
                    template,
                    "test-char",
                    (backend, charUuid, entity) => backend.UpsertShipTemplateAsync(charUuid, entity),
                    (backend, charUuid) => backend.GetAllShipTemplatesAsync(charUuid));
            });
        }

        /// <summary>
        /// Ship round-trip: JsonSingleFile → JsonMultiFile → JsonSingleFile.
        /// **Validates: Requirements 7.1, 7.8**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property Ship_RoundTrip_JsonSingleFile_JsonMultiFile()
        {
            return Prop.ForAll(EntityGenerators.ArbShip(), ship =>
            {
                ship.OwnerUUID = "test-char";
                return AssertRoundTripJsonMulti(
                    ship,
                    "test-char",
                    (backend, charUuid, entity) => backend.UpsertShipAsync(charUuid, entity),
                    (backend, charUuid) => backend.GetAllShipsAsync(charUuid));
            });
        }

        /// <summary>
        /// Station round-trip: JsonSingleFile → JsonMultiFile → JsonSingleFile.
        /// **Validates: Requirements 7.1, 7.8**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property Station_RoundTrip_JsonSingleFile_JsonMultiFile()
        {
            return Prop.ForAll(EntityGenerators.ArbStation(), station =>
            {
                station.OwnerUUID = "test-char";
                return AssertRoundTripJsonMulti(
                    station,
                    "test-char",
                    (backend, charUuid, entity) => backend.UpsertStationAsync(charUuid, entity),
                    (backend, charUuid) => backend.GetAllStationsAsync(charUuid));
            });
        }

        /// <summary>
        /// MarketListing round-trip: JsonSingleFile → JsonMultiFile → JsonSingleFile.
        /// **Validates: Requirements 7.1, 7.8**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property MarketListing_RoundTrip_JsonSingleFile_JsonMultiFile()
        {
            return Prop.ForAll(EntityGenerators.ArbMarketListing(), entity =>
            {
                entity.OwnerUUID = "test-char";
                return AssertRoundTripJsonMulti(
                    entity,
                    "test-char",
                    (backend, charUuid, e) => backend.UpsertMarketListingAsync(charUuid, e),
                    (backend, charUuid) => backend.GetAllMarketListingsAsync(charUuid));
            });
        }

        /// <summary>
        /// MarketTransaction round-trip: JsonSingleFile → JsonMultiFile → JsonSingleFile.
        /// **Validates: Requirements 7.1, 7.8**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property MarketTransaction_RoundTrip_JsonSingleFile_JsonMultiFile()
        {
            return Prop.ForAll(EntityGenerators.ArbMarketTransaction(), entity =>
            {
                entity.OwnerUUID = "test-char";
                return AssertRoundTripJsonMulti(
                    entity,
                    "test-char",
                    (backend, charUuid, e) => backend.UpsertMarketTransactionAsync(charUuid, e),
                    (backend, charUuid) => backend.GetAllMarketTransactionsAsync(charUuid));
            });
        }

        /// <summary>
        /// StockPlan round-trip: JsonSingleFile → JsonMultiFile → JsonSingleFile.
        /// **Validates: Requirements 7.1, 7.8**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property StockPlan_RoundTrip_JsonSingleFile_JsonMultiFile()
        {
            return Prop.ForAll(EntityGenerators.ArbStockPlan(), entity =>
            {
                entity.OwnerUUID = "test-char";
                return AssertRoundTripJsonMulti(
                    entity,
                    "test-char",
                    (backend, charUuid, e) => backend.UpsertStockPlanAsync(charUuid, e),
                    (backend, charUuid) => backend.GetAllStockPlansAsync(charUuid));
            });
        }

        /// <summary>
        /// StockProfile round-trip: JsonSingleFile → JsonMultiFile → JsonSingleFile.
        /// **Validates: Requirements 7.1, 7.8**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property StockProfile_RoundTrip_JsonSingleFile_JsonMultiFile()
        {
            return Prop.ForAll(EntityGenerators.ArbStockProfile(), entity =>
            {
                entity.OwnerUUID = "test-char";
                return AssertRoundTripJsonMulti(
                    entity,
                    "test-char",
                    (backend, charUuid, e) => backend.UpsertStockProfileAsync(charUuid, e),
                    (backend, charUuid) => backend.GetAllStockProfilesAsync(charUuid));
            });
        }

        /// <summary>
        /// SupplyChain round-trip: JsonSingleFile → JsonMultiFile → JsonSingleFile.
        /// **Validates: Requirements 7.1, 7.8**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property SupplyChain_RoundTrip_JsonSingleFile_JsonMultiFile()
        {
            return Prop.ForAll(EntityGenerators.ArbSupplyChain(), entity =>
            {
                entity.OwnerUUID = "test-char";
                return AssertRoundTripJsonMulti(
                    entity,
                    "test-char",
                    (backend, charUuid, e) => backend.UpsertSupplyChainAsync(charUuid, e),
                    (backend, charUuid) => backend.GetAllSupplyChainsAsync(charUuid));
            });
        }

        /// <summary>
        /// WarehouseOverflowRule round-trip: JsonSingleFile → JsonMultiFile → JsonSingleFile.
        /// **Validates: Requirements 7.1, 7.8**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property WarehouseOverflowRule_RoundTrip_JsonSingleFile_JsonMultiFile()
        {
            return Prop.ForAll(EntityGenerators.ArbWarehouseOverflowRule(), entity =>
            {
                entity.OwnerUUID = "test-char";
                return AssertRoundTripJsonMulti(
                    entity,
                    "test-char",
                    (backend, charUuid, e) => backend.UpsertWarehouseOverflowRuleAsync(charUuid, e),
                    (backend, charUuid) => backend.GetAllWarehouseOverflowRulesAsync(charUuid));
            });
        }

        /// <summary>
        /// Asteroid round-trip: JsonSingleFile → JsonMultiFile → JsonSingleFile.
        /// **Validates: Requirements 7.1, 7.8**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property Asteroid_RoundTrip_JsonSingleFile_JsonMultiFile()
        {
            return Prop.ForAll(EntityGenerators.ArbAsteroid(), entity =>
            {
                entity.OwnerUUID = "test-char";
                return AssertRoundTripJsonMulti(
                    entity,
                    "test-char",
                    (backend, charUuid, e) => backend.UpsertAsteroidAsync(charUuid, e),
                    (backend, charUuid) => backend.GetAllAsteroidsAsync(charUuid));
            });
        }

        /// <summary>
        /// BankingTransaction round-trip: JsonSingleFile → JsonMultiFile → JsonSingleFile.
        /// **Validates: Requirements 7.1, 7.8**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property BankingTransaction_RoundTrip_JsonSingleFile_JsonMultiFile()
        {
            return Prop.ForAll(EntityGenerators.ArbBankingTransaction(), entity =>
            {
                entity.OwnerUUID = "test-char";
                return AssertRoundTripJsonMulti(
                    entity,
                    "test-char",
                    (backend, charUuid, e) => backend.UpsertBankingTransactionAsync(charUuid, e),
                    (backend, charUuid) => backend.GetAllBankingTransactionsAsync(charUuid));
            });
        }

        /// <summary>
        /// MailMessage round-trip: JsonSingleFile → JsonMultiFile → JsonSingleFile.
        /// **Validates: Requirements 7.1, 7.8**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property MailMessage_RoundTrip_JsonSingleFile_JsonMultiFile()
        {
            return Prop.ForAll(EntityGenerators.ArbMailMessage(), entity =>
            {
                entity.OwnerUUID = "test-char";
                return AssertRoundTripJsonMulti(
                    entity,
                    "test-char",
                    (backend, charUuid, e) => backend.UpsertMailMessageAsync(charUuid, e),
                    (backend, charUuid) => backend.GetAllMailMessagesAsync(charUuid));
            });
        }

        /// <summary>
        /// Faction round-trip: JsonSingleFile → JsonMultiFile → JsonSingleFile.
        /// **Validates: Requirements 7.1, 7.8**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property Faction_RoundTrip_JsonSingleFile_JsonMultiFile()
        {
            return Prop.ForAll(EntityGenerators.ArbFaction(), entity =>
            {
                entity.OwnerUUID = "test-char";
                return AssertRoundTripJsonMulti(
                    entity,
                    "test-char",
                    (backend, charUuid, e) => backend.UpsertFactionForCharacterAsync(charUuid, e),
                    (backend, charUuid) => backend.GetAllFactionsForCharacterAsync(charUuid));
            });
        }

        /// <summary>
        /// ExternalCharacter round-trip: JsonSingleFile → JsonMultiFile → JsonSingleFile.
        /// **Validates: Requirements 7.1, 7.8**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property ExternalCharacter_RoundTrip_JsonSingleFile_JsonMultiFile()
        {
            return Prop.ForAll(EntityGenerators.ArbExternalCharacter(), entity =>
            {
                entity.OwnerUUID = "test-char";
                return AssertRoundTripJsonMulti(
                    entity,
                    "test-char",
                    (backend, charUuid, e) => backend.UpsertExternalCharacterAsync(charUuid, e),
                    (backend, charUuid) => backend.GetAllExternalCharactersAsync(charUuid));
            });
        }
    }
}
