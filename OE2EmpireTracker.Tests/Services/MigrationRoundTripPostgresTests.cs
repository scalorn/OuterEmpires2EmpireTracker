// -----------------------------------------------------------------------
// <copyright file="MigrationRoundTripPostgresTests.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FsCheck;
using Newtonsoft.Json;
using NUnit.Framework;
using OE2EmpireTracker.Common.Interfaces;
using OE2EmpireTracker.Common.Storage;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Round-trip fidelity property tests for the Sqlite↔Postgres backend pair.
    /// Specifically validates the REAL→TEXT (Sqlite) and DOUBLE PRECISION→NUMERIC
    /// (Postgres) decimal precision fixes for BankingTransaction and MarketTransaction.
    /// **Validates: Requirements 7.1, 7.2, 7.8**
    /// </summary>
    [TestFixture]
    public class MigrationRoundTripPostgresTests : MigrationRoundTripPropertyTests
    {
        private string _postgresConnString;

        /// <summary>
        /// Checks Postgres availability before each test.
        /// Skips the test if OE2_TEST_POSTGRES_CONN is not set.
        /// </summary>
        [SetUp]
        public void CheckPostgresAvailability()
        {
            _postgresConnString = Environment.GetEnvironmentVariable("OE2_TEST_POSTGRES_CONN");
            if (string.IsNullOrWhiteSpace(_postgresConnString))
            {
                Assert.Ignore("Postgres not available (OE2_TEST_POSTGRES_CONN not set)");
            }
        }

        /// <summary>
        /// Creates an initialized PostgresBackend using the test connection string.
        /// Uses a unique schema per test run to avoid conflicts.
        /// </summary>
        /// <returns>An initialized PostgresBackend instance.</returns>
        private IStorageBackend CreatePostgresBackend()
        {
            var config = new StorageBackendConfig
            {
                ConnectionString = _postgresConnString,
            };
            var backend = new PostgresBackend(config);
            Task.Run(() => backend.InitializeAsync()).GetAwaiter().GetResult();
            return backend;
        }

        /// <summary>
        /// Asserts round-trip fidelity via Sqlite → Postgres → Sqlite path.
        /// </summary>
        /// <typeparam name="T">The entity type being round-tripped.</typeparam>
        /// <param name="original">The original entity instance.</param>
        /// <param name="characterUuid">The character UUID to associate.</param>
        /// <param name="write">Delegate to write the entity to a backend.</param>
        /// <param name="read">Delegate to read entities from a backend.</param>
        /// <returns>True if round-trip produces identical JSON.</returns>
        private bool AssertPostgresRoundTrip<T>(
            T original,
            string characterUuid,
            Func<IStorageBackend, string, T, Task> write,
            Func<IStorageBackend, string, Task<IReadOnlyList<T>>> read)
        {
            var source = CreateSqliteBackend();
            var postgres = CreatePostgresBackend();
            var freshSource = CreateSqliteBackend();

            var migrationService = new MigrationService();

            Task.Run(() => write(source, characterUuid, original))
                .GetAwaiter().GetResult();

            Task.Run(() => migrationService.MigrateAsync(source, postgres))
                .GetAwaiter().GetResult();

            Task.Run(() => migrationService.MigrateAsync(postgres, freshSource))
                .GetAwaiter().GetResult();

            var roundTripped = Task.Run(() => read(freshSource, characterUuid))
                .GetAwaiter().GetResult();

            if (roundTripped == null || roundTripped.Count == 0)
            {
                return false;
            }

            string originalJson = JsonConvert.SerializeObject(
                original, JsonSettings.SerializerSettings);
            string roundTrippedJson = JsonConvert.SerializeObject(
                roundTripped[0], JsonSettings.SerializerSettings);

            return originalJson == roundTrippedJson;
        }

        /// <summary>
        /// Colony round-trip: Sqlite → Postgres → Sqlite.
        /// **Validates: Requirements 7.1, 7.2, 7.8**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property Colony_RoundTrip_Sqlite_Postgres()
        {
            return Prop.ForAll(EntityGenerators.ArbColony(), colony =>
            {
                colony.OwnerUUID = "test-char";
                return AssertPostgresRoundTrip(
                    colony,
                    "test-char",
                    (b, c, e) => b.UpsertColonyAsync(c, e),
                    (b, c) => b.GetAllColoniesAsync(c));
            });
        }

        /// <summary>
        /// Blueprint round-trip: Sqlite → Postgres → Sqlite.
        /// **Validates: Requirements 7.1, 7.2, 7.8**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property Blueprint_RoundTrip_Sqlite_Postgres()
        {
            return Prop.ForAll(EntityGenerators.ArbBlueprint(), blueprint =>
            {
                blueprint.OwnerUUID = "test-char";
                return AssertPostgresRoundTrip(
                    blueprint,
                    "test-char",
                    (b, c, e) => b.UpsertBlueprintAsync(c, e),
                    (b, c) => b.GetAllBlueprintsAsync(c));
            });
        }

        /// <summary>
        /// Survey round-trip: Sqlite → Postgres → Sqlite.
        /// **Validates: Requirements 7.1, 7.2, 7.8**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property Survey_RoundTrip_Sqlite_Postgres()
        {
            return Prop.ForAll(EntityGenerators.ArbSurvey(), survey =>
            {
                survey.OwnerUUID = "test-char";
                return AssertPostgresRoundTrip(
                    survey,
                    "test-char",
                    (b, c, e) => b.UpsertSurveyAsync(c, e),
                    (b, c) => b.GetAllSurveysAsync(c));
            });
        }

        /// <summary>
        /// PlayerProfile round-trip: Sqlite → Postgres → Sqlite.
        /// **Validates: Requirements 7.1, 7.2, 7.8**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property PlayerProfile_RoundTrip_Sqlite_Postgres()
        {
            return Prop.ForAll(EntityGenerators.ArbPlayerProfile(), profile =>
            {
                profile.UUID = "test-char";
                return AssertPostgresRoundTrip(
                    profile,
                    "test-char",
                    (b, c, e) => b.UpsertPlayerProfileAsync(c, e),
                    (b, c) => b.GetAllPlayerProfilesAsync(c));
            });
        }

        /// <summary>
        /// DeliveryRoute round-trip: Sqlite → Postgres → Sqlite.
        /// **Validates: Requirements 7.1, 7.2, 7.8**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property DeliveryRoute_RoundTrip_Sqlite_Postgres()
        {
            return Prop.ForAll(EntityGenerators.ArbDeliveryRoute(), route =>
            {
                route.OwnerUUID = "test-char";
                return AssertPostgresRoundTrip(
                    route,
                    "test-char",
                    (b, c, e) => b.UpsertDeliveryRouteAsync(c, e),
                    (b, c) => b.GetAllDeliveryRoutesAsync(c));
            });
        }

        /// <summary>
        /// DeliveryPlan round-trip: Sqlite → Postgres → Sqlite.
        /// **Validates: Requirements 7.1, 7.2, 7.8**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property DeliveryPlan_RoundTrip_Sqlite_Postgres()
        {
            return Prop.ForAll(EntityGenerators.ArbDeliveryPlan(), plan =>
            {
                plan.OwnerUUID = "test-char";
                return AssertPostgresRoundTrip(
                    plan,
                    "test-char",
                    (b, c, e) => b.UpsertDeliveryPlanAsync(c, e),
                    (b, c) => b.GetAllDeliveryPlansAsync(c));
            });
        }

        /// <summary>
        /// PricingPlan round-trip: Sqlite → Postgres → Sqlite.
        /// **Validates: Requirements 7.1, 7.2, 7.8**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property PricingPlan_RoundTrip_Sqlite_Postgres()
        {
            return Prop.ForAll(EntityGenerators.ArbPricingPlan(), plan =>
            {
                plan.OwnerUUID = "test-char";
                return AssertPostgresRoundTrip(
                    plan,
                    "test-char",
                    (b, c, e) => b.UpsertPricingPlanAsync(c, e),
                    (b, c) => b.GetAllPricingPlansAsync(c));
            });
        }

        /// <summary>
        /// BuildPlan round-trip: Sqlite → Postgres → Sqlite.
        /// **Validates: Requirements 7.1, 7.2, 7.8**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property BuildPlan_RoundTrip_Sqlite_Postgres()
        {
            return Prop.ForAll(EntityGenerators.ArbBuildPlan(), plan =>
            {
                plan.OwnerUUID = "test-char";
                return AssertPostgresRoundTrip(
                    plan,
                    "test-char",
                    (b, c, e) => b.UpsertBuildPlanAsync(c, e),
                    (b, c) => b.GetAllBuildPlansAsync(c));
            });
        }

        /// <summary>
        /// ShipTemplate round-trip: Sqlite → Postgres → Sqlite.
        /// **Validates: Requirements 7.1, 7.2, 7.8**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property ShipTemplate_RoundTrip_Sqlite_Postgres()
        {
            return Prop.ForAll(EntityGenerators.ArbShipTemplate(), template =>
            {
                template.OwnerUUID = "test-char";
                return AssertPostgresRoundTrip(
                    template,
                    "test-char",
                    (b, c, e) => b.UpsertShipTemplateAsync(c, e),
                    (b, c) => b.GetAllShipTemplatesAsync(c));
            });
        }

        /// <summary>
        /// Ship round-trip: Sqlite → Postgres → Sqlite.
        /// **Validates: Requirements 7.1, 7.2, 7.8**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property Ship_RoundTrip_Sqlite_Postgres()
        {
            return Prop.ForAll(EntityGenerators.ArbShip(), ship =>
            {
                ship.OwnerUUID = "test-char";
                return AssertPostgresRoundTrip(
                    ship,
                    "test-char",
                    (b, c, e) => b.UpsertShipAsync(c, e),
                    (b, c) => b.GetAllShipsAsync(c));
            });
        }

        /// <summary>
        /// Station round-trip: Sqlite → Postgres → Sqlite.
        /// **Validates: Requirements 7.1, 7.2, 7.8**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property Station_RoundTrip_Sqlite_Postgres()
        {
            return Prop.ForAll(EntityGenerators.ArbStation(), station =>
            {
                station.OwnerUUID = "test-char";
                return AssertPostgresRoundTrip(
                    station,
                    "test-char",
                    (b, c, e) => b.UpsertStationAsync(c, e),
                    (b, c) => b.GetAllStationsAsync(c));
            });
        }

        /// <summary>
        /// MarketListing round-trip: Sqlite → Postgres → Sqlite.
        /// **Validates: Requirements 7.1, 7.2, 7.8**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property MarketListing_RoundTrip_Sqlite_Postgres()
        {
            return Prop.ForAll(EntityGenerators.ArbMarketListing(), entity =>
            {
                entity.OwnerUUID = "test-char";
                return AssertPostgresRoundTrip(
                    entity,
                    "test-char",
                    (b, c, e) => b.UpsertMarketListingAsync(c, e),
                    (b, c) => b.GetAllMarketListingsAsync(c));
            });
        }

        /// <summary>
        /// MarketTransaction round-trip: Sqlite → Postgres → Sqlite.
        /// Special focus on decimal precision (PricePerUnit, TotalPrice).
        /// **Validates: Requirements 7.1, 7.2, 7.8**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property MarketTransaction_RoundTrip_Sqlite_Postgres()
        {
            return Prop.ForAll(EntityGenerators.ArbMarketTransaction(), entity =>
            {
                entity.OwnerUUID = "test-char";
                return AssertPostgresRoundTrip(
                    entity,
                    "test-char",
                    (b, c, e) => b.UpsertMarketTransactionAsync(c, e),
                    (b, c) => b.GetAllMarketTransactionsAsync(c));
            });
        }

        /// <summary>
        /// BankingTransaction round-trip: Sqlite → Postgres → Sqlite.
        /// Special focus on decimal precision (CreditChange, OldBalance, NewBalance).
        /// **Validates: Requirements 7.1, 7.2, 7.8**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property BankingTransaction_RoundTrip_Sqlite_Postgres()
        {
            return Prop.ForAll(EntityGenerators.ArbBankingTransaction(), entity =>
            {
                entity.OwnerUUID = "test-char";
                return AssertPostgresRoundTrip(
                    entity,
                    "test-char",
                    (b, c, e) => b.UpsertBankingTransactionAsync(c, e),
                    (b, c) => b.GetAllBankingTransactionsAsync(c));
            });
        }

        /// <summary>
        /// StockPlan round-trip: Sqlite → Postgres → Sqlite.
        /// **Validates: Requirements 7.1, 7.2, 7.8**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property StockPlan_RoundTrip_Sqlite_Postgres()
        {
            return Prop.ForAll(EntityGenerators.ArbStockPlan(), entity =>
            {
                entity.OwnerUUID = "test-char";
                return AssertPostgresRoundTrip(
                    entity,
                    "test-char",
                    (b, c, e) => b.UpsertStockPlanAsync(c, e),
                    (b, c) => b.GetAllStockPlansAsync(c));
            });
        }

        /// <summary>
        /// StockProfile round-trip: Sqlite → Postgres → Sqlite.
        /// **Validates: Requirements 7.1, 7.2, 7.8**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property StockProfile_RoundTrip_Sqlite_Postgres()
        {
            return Prop.ForAll(EntityGenerators.ArbStockProfile(), entity =>
            {
                entity.OwnerUUID = "test-char";
                return AssertPostgresRoundTrip(
                    entity,
                    "test-char",
                    (b, c, e) => b.UpsertStockProfileAsync(c, e),
                    (b, c) => b.GetAllStockProfilesAsync(c));
            });
        }

        /// <summary>
        /// SupplyChain round-trip: Sqlite → Postgres → Sqlite.
        /// **Validates: Requirements 7.1, 7.2, 7.8**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property SupplyChain_RoundTrip_Sqlite_Postgres()
        {
            return Prop.ForAll(EntityGenerators.ArbSupplyChain(), entity =>
            {
                entity.OwnerUUID = "test-char";
                return AssertPostgresRoundTrip(
                    entity,
                    "test-char",
                    (b, c, e) => b.UpsertSupplyChainAsync(c, e),
                    (b, c) => b.GetAllSupplyChainsAsync(c));
            });
        }

        /// <summary>
        /// WarehouseOverflowRule round-trip: Sqlite → Postgres → Sqlite.
        /// **Validates: Requirements 7.1, 7.2, 7.8**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property WarehouseOverflowRule_RoundTrip_Sqlite_Postgres()
        {
            return Prop.ForAll(EntityGenerators.ArbWarehouseOverflowRule(), entity =>
            {
                entity.OwnerUUID = "test-char";
                return AssertPostgresRoundTrip(
                    entity,
                    "test-char",
                    (b, c, e) => b.UpsertWarehouseOverflowRuleAsync(c, e),
                    (b, c) => b.GetAllWarehouseOverflowRulesAsync(c));
            });
        }

        /// <summary>
        /// Asteroid round-trip: Sqlite → Postgres → Sqlite.
        /// **Validates: Requirements 7.1, 7.2, 7.8**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property Asteroid_RoundTrip_Sqlite_Postgres()
        {
            return Prop.ForAll(EntityGenerators.ArbAsteroid(), entity =>
            {
                entity.OwnerUUID = "test-char";
                return AssertPostgresRoundTrip(
                    entity,
                    "test-char",
                    (b, c, e) => b.UpsertAsteroidAsync(c, e),
                    (b, c) => b.GetAllAsteroidsAsync(c));
            });
        }

        /// <summary>
        /// MailMessage round-trip: Sqlite → Postgres → Sqlite.
        /// **Validates: Requirements 7.1, 7.2, 7.8**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property MailMessage_RoundTrip_Sqlite_Postgres()
        {
            return Prop.ForAll(EntityGenerators.ArbMailMessage(), entity =>
            {
                entity.OwnerUUID = "test-char";
                return AssertPostgresRoundTrip(
                    entity,
                    "test-char",
                    (b, c, e) => b.UpsertMailMessageAsync(c, e),
                    (b, c) => b.GetAllMailMessagesAsync(c));
            });
        }

        /// <summary>
        /// Faction (contacts) round-trip: Sqlite → Postgres → Sqlite.
        /// **Validates: Requirements 7.1, 7.2, 7.8**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property Faction_RoundTrip_Sqlite_Postgres()
        {
            return Prop.ForAll(EntityGenerators.ArbFaction(), entity =>
            {
                entity.OwnerUUID = "test-char";
                return AssertPostgresRoundTrip(
                    entity,
                    "test-char",
                    (b, c, e) => b.UpsertFactionForCharacterAsync(c, e),
                    (b, c) => b.GetAllFactionsForCharacterAsync(c));
            });
        }

        /// <summary>
        /// ExternalCharacter round-trip: Sqlite → Postgres → Sqlite.
        /// **Validates: Requirements 7.1, 7.2, 7.8**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property ExternalCharacter_RoundTrip_Sqlite_Postgres()
        {
            return Prop.ForAll(EntityGenerators.ArbExternalCharacter(), entity =>
            {
                entity.OwnerUUID = "test-char";
                return AssertPostgresRoundTrip(
                    entity,
                    "test-char",
                    (b, c, e) => b.UpsertExternalCharacterAsync(c, e),
                    (b, c) => b.GetAllExternalCharactersAsync(c));
            });
        }
    }
}
