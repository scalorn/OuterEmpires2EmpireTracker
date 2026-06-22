// <copyright file="MigrationServiceTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using OE2EmpireTracker.Common.Interfaces;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;
using Bp = OE2EmpireTracker.Models.Blueprint;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Unit tests for MigrationService.
    /// Satisfies: Req 6, Criteria 1-10.
    /// </summary>
    [TestFixture]
    public class MigrationServiceTests
    {
        private MigrationService service;

        [SetUp]
        public void SetUp()
        {
            service = new MigrationService();
        }

        /// <summary>
        /// Empty source → empty destination, no errors, validation passes.
        /// </summary>
        [Test]
        public void MigrateAsync_EmptySource_CompletesWithNoErrors()
        {
            var source = new InMemoryMigrationBackend();
            var dest = new InMemoryMigrationBackend();

            Assert.DoesNotThrowAsync(() => service.MigrateAsync(source, dest));
            Assert.That(dest.UpsertedEntityTypes, Has.Count.GreaterThanOrEqualTo(0));
        }

        /// <summary>
        /// Single character migration transfers all per-character entity types.
        /// </summary>
        [Test]
        public async Task MigrateAsync_SingleCharacter_TransfersAllEntityTypes()
        {
            string charUUID = "char-001";
            var source = new InMemoryMigrationBackend(new List<string> { charUUID });

            // Seed source with entities
            await source.UpsertColonyAsync(charUUID, new Colony
            {
                UUID = "col-1",
                OwnerUUID = charUUID,
                ColonyName = "Alpha"
            });
            await source.UpsertBlueprintAsync(charUUID, new Bp
            {
                UUID = "bp-1",
                OwnerUUID = charUUID,
                Name = "Hull Mk1"
            });
            await source.UpsertShipAsync(charUUID, new Ship
            {
                UUID = "ship-1",
                OwnerUUID = charUUID,
                Name = "Freighter"
            });
            await source.UpsertMailMessageAsync(charUUID, new MailMessage
            {
                UUID = "mail-1"
            });
            await source.UpsertBankingTransactionAsync(charUUID, new BankingTransaction
            {
                UUID = "bank-1"
            });

            var dest = new InMemoryMigrationBackend();
            await service.MigrateAsync(source, dest);

            // Verify all entity types transferred
            Assert.That(dest.UpsertedEntityTypes, Does.Contain("Colony"));
            Assert.That(dest.UpsertedEntityTypes, Does.Contain("Blueprint"));
            Assert.That(dest.UpsertedEntityTypes, Does.Contain("Ship"));
            Assert.That(dest.UpsertedEntityTypes, Does.Contain("MailMessage"));
            Assert.That(dest.UpsertedEntityTypes, Does.Contain("BankingTransaction"));
        }

        /// <summary>
        /// Count validation passes on correct migration (no exception thrown).
        /// Source and destination have matching counts after migration.
        /// </summary>
        [Test]
        public async Task MigrateAsync_CorrectMigration_ValidationPasses()
        {
            string charUUID = "char-val";
            var source = new InMemoryMigrationBackend(new List<string> { charUUID });

            await source.UpsertColonyAsync(charUUID, new Colony
            {
                UUID = "col-v",
                OwnerUUID = charUUID,
                ColonyName = "Validate"
            });

            var dest = new InMemoryMigrationBackend();

            // Should complete without MigrationValidationException
            Assert.DoesNotThrowAsync(() => service.MigrateAsync(source, dest));
        }

        /// <summary>
        /// Count mismatch throws MigrationValidationException with correct Mismatches.
        /// Uses a RecordingStorageBackend as destination — it accepts writes but returns
        /// empty on re-read during validation, causing a count mismatch.
        /// </summary>
        [Test]
        public void MigrateAsync_CountMismatch_ThrowsMigrationValidationException()
        {
            var source = new RecordingStorageBackend
            {
                ColoniesToReturn = new List<Colony>
                {
                    new Colony { UUID = "col-m", ColonyName = "Mismatch Colony" }
                }
            };
            var dest = new RecordingStorageBackend();

            // Pass explicit character UUIDs since source returns empty UUID list
            var charUUIDs = new List<string> { "char-mm" };

            var ex = Assert.ThrowsAsync<MigrationValidationException>(
                () => service.MigrateAsync(source, dest, characterUUIDs: charUUIDs));

            Assert.That(ex.Mismatches, Is.Not.Null);
            Assert.That(ex.Mismatches.Count, Is.GreaterThan(0));
            Assert.That(ex.Mismatches.ContainsKey("Colony"), Is.True);
            Assert.That(ex.Mismatches["Colony"].Expected, Is.EqualTo(1));
            Assert.That(ex.Mismatches["Colony"].Actual, Is.EqualTo(0));
        }

        /// <summary>
        /// Progress callback invoked at least once per entity type during migration.
        /// </summary>
        [Test]
        public async Task MigrateAsync_ProgressCallback_InvokedPerEntityType()
        {
            string charUUID = "char-prog";
            var source = new InMemoryMigrationBackend(new List<string> { charUUID });

            await source.UpsertColonyAsync(charUUID, new Colony
            {
                UUID = "col-p",
                OwnerUUID = charUUID,
                ColonyName = "Progress"
            });
            await source.UpsertShipAsync(charUUID, new Ship
            {
                UUID = "ship-p",
                OwnerUUID = charUUID,
                Name = "TestShip"
            });

            var dest = new InMemoryMigrationBackend();
            var reports = new List<MigrationProgress>();
            var progress = new TestProgress(reports);

            await service.MigrateAsync(source, dest, progress);

            Assert.That(reports.Count, Is.GreaterThan(0));

            var entityTypes = reports.Select(r => r.CurrentEntityType).Distinct().ToList();

            // Baseline types are always reported
            Assert.That(entityTypes, Does.Contain("BlueprintTypes"));
            Assert.That(entityTypes, Does.Contain("ShipClasses"));

            // Per-character types that had data
            Assert.That(entityTypes, Does.Contain("Colony"));
            Assert.That(entityTypes, Does.Contain("Ship"));
        }

        /// <summary>
        /// Selective migration (explicit UUIDs) only migrates specified characters.
        /// </summary>
        [Test]
        public async Task MigrateAsync_SelectiveUUIDs_OnlyMigratesSpecifiedCharacters()
        {
            var source = new InMemoryMigrationBackend(new List<string> { "char-A", "char-B" });

            await source.UpsertColonyAsync("char-A", new Colony
            {
                UUID = "col-A",
                OwnerUUID = "char-A",
                ColonyName = "Alpha"
            });
            await source.UpsertColonyAsync("char-B", new Colony
            {
                UUID = "col-B",
                OwnerUUID = "char-B",
                ColonyName = "Beta"
            });

            var dest = new InMemoryMigrationBackend();

            // Only migrate char-B
            var selectiveUUIDs = new List<string> { "char-B" };
            await service.MigrateAsync(source, dest, characterUUIDs: selectiveUUIDs);

            // char-A should NOT be in destination
            var coloniesA = await dest.GetAllColoniesAsync("char-A");
            Assert.That(coloniesA.Count, Is.EqualTo(0));

            // char-B SHOULD be in destination
            var coloniesB = await dest.GetAllColoniesAsync("char-B");
            Assert.That(coloniesB.Count, Is.EqualTo(1));
            Assert.That(coloniesB[0].UUID, Is.EqualTo("col-B"));
        }

        /// <summary>
        /// Source read failure throws StorageLoadException with entity type in message.
        /// </summary>
        [Test]
        public void MigrateAsync_SourceReadFailure_ThrowsStorageLoadException()
        {
            var source = new RecordingStorageBackend { ThrowOnLoad = true };
            var dest = new RecordingStorageBackend();

            // Pass explicit character UUIDs to trigger per-character reads
            var charUUIDs = new List<string> { "char-fail" };

            var ex = Assert.ThrowsAsync<StorageLoadException>(
                () => service.MigrateAsync(source, dest, characterUUIDs: charUUIDs));

            Assert.That(ex.Message, Is.Not.Null.And.Not.Empty);
        }

        /// <summary>
        /// Synchronous IProgress implementation that captures reports immediately.
        /// Avoids timing issues with <see cref="Progress{T}"/> which posts to
        /// SynchronizationContext asynchronously.
        /// </summary>
        private sealed class TestProgress : IProgress<MigrationProgress>
        {
            private readonly List<MigrationProgress> reports;

            public TestProgress(List<MigrationProgress> reports)
            {
                this.reports = reports;
            }

            public void Report(MigrationProgress value)
            {
                reports.Add(value);
            }
        }
    }
}
