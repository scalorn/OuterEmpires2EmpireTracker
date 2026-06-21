// -----------------------------------------------------------------------
// <copyright file="SqliteBackendColonyTests.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using NUnit.Framework;
using OE2EmpireTracker.Common.Storage;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Tests.Storage
{
    /// <summary>
    /// Tests for SqliteBackend Colony CRUD with child tables (Structures, Properties, Workers, Items).
    /// </summary>
    [TestFixture]
    public class SqliteBackendColonyTests
    {
        private string _dbPath;
        private SqliteBackend _backend;

        [SetUp]
        public async Task SetUp()
        {
            _dbPath = Path.Combine(Path.GetTempPath(), "SqliteColonyTest_" + Path.GetRandomFileName() + ".db");
            var config = new StorageBackendConfig { ConnectionString = _dbPath };
            _backend = new SqliteBackend(config);
            await _backend.InitializeAsync();
        }

        [TearDown]
        public void TearDown()
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (File.Exists(_dbPath))
            {
                File.Delete(_dbPath);
            }
        }

        [Test]
        public async Task UpsertColony_BasicFields_RoundTrips()
        {
            var colony = new Colony
            {
                UUID = "col-1",
                OwnerUUID = "char-1",
                ColonyName = "Test Colony",
                PlanetName = "Terra",
                SystemName = "Sol",
                ColonySize = 10,
                Distance = 1.5m,
                SurfaceVariation = 3,
                AtmosVariation = 2,
                HexValue = "#FF0000",
                WorkerCurrentAttitude = 75,
                ContentmentIndex = 80,
                WageLevel = 5,
            };

            await _backend.UpsertColonyAsync("char-1", colony);
            var loaded = await _backend.GetColonyAsync("char-1", "col-1");

            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded.UUID, Is.EqualTo("col-1"));
            Assert.That(loaded.OwnerUUID, Is.EqualTo("char-1"));
            Assert.That(loaded.ColonyName, Is.EqualTo("Test Colony"));
            Assert.That(loaded.PlanetName, Is.EqualTo("Terra"));
            Assert.That(loaded.SystemName, Is.EqualTo("Sol"));
            Assert.That(loaded.ColonySize, Is.EqualTo(10));
            Assert.That(loaded.Distance, Is.EqualTo(1.5m));
            Assert.That(loaded.SurfaceVariation, Is.EqualTo(3));
            Assert.That(loaded.AtmosVariation, Is.EqualTo(2));
            Assert.That(loaded.HexValue, Is.EqualTo("#FF0000"));
            Assert.That(loaded.WorkerCurrentAttitude, Is.EqualTo(75));
            Assert.That(loaded.ContentmentIndex, Is.EqualTo(80));
            Assert.That(loaded.WageLevel, Is.EqualTo(5));
        }

        [Test]
        public async Task UpsertColony_WithStructures_PersistsChildren()
        {
            var colony = new Colony
            {
                UUID = "col-2",
                OwnerUUID = "char-1",
                ColonyName = "Structured Colony",
                SystemName = "Alpha",
            };

            colony.Structures.Add(new ColonyStructure
            {
                UUID = "struct-1",
                FlatpackBlueprintUUID = "bp-001",
                DisplaySequence = 1,
                BuildingID = 100,
                BuildQueueSequence = 1,
                ManufacturingQuantity = 5,
                StagingResources = true,
            });

            colony.Structures.Add(new ColonyStructure
            {
                UUID = "struct-2",
                FlatpackBlueprintUUID = "bp-002",
                DisplaySequence = 2,
                BuildingID = 200,
                BuildQueueSequence = 2,
                RefiningResource = "Iron",
                RefiningResourcePurity = "High",
            });

            await _backend.UpsertColonyAsync("char-1", colony);
            var loaded = await _backend.GetColonyAsync("char-1", "col-2");

            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded.Structures, Has.Count.EqualTo(2));
            Assert.That(loaded.Structures[0].UUID, Is.EqualTo("struct-1"));
            Assert.That(loaded.Structures[0].FlatpackBlueprintUUID, Is.EqualTo("bp-001"));
            Assert.That(loaded.Structures[0].BuildingID, Is.EqualTo(100));
            Assert.That(loaded.Structures[0].ManufacturingQuantity, Is.EqualTo(5));
            Assert.That(loaded.Structures[0].StagingResources, Is.True);
            Assert.That(loaded.Structures[1].UUID, Is.EqualTo("struct-2"));
            Assert.That(loaded.Structures[1].RefiningResource, Is.EqualTo("Iron"));
            Assert.That(loaded.Structures[1].RefiningResourcePurity, Is.EqualTo("High"));
        }

        [Test]
        public async Task UpsertColony_WithStructureProperties_PersistsPropertyBag()
        {
            var colony = new Colony
            {
                UUID = "col-3",
                OwnerUUID = "char-1",
                ColonyName = "Props Colony",
                SystemName = "Beta",
            };

            var structure = new ColonyStructure
            {
                UUID = "struct-prop-1",
                FlatpackBlueprintUUID = "bp-010",
                DisplaySequence = 1,
                BuildingID = 300,
            };

            structure.Properties.SetProperty("Built", true);
            structure.Properties.SetProperty("Online", true);
            structure.Properties.SetProperty("Efficiency", "85");
            colony.Structures.Add(structure);

            await _backend.UpsertColonyAsync("char-1", colony);
            var loaded = await _backend.GetColonyAsync("char-1", "col-3");

            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded.Structures, Has.Count.EqualTo(1));

            var loadedStruct = loaded.Structures[0];
            Assert.That(loadedStruct.Properties.ContainsKey("Built"), Is.True);
            Assert.That(loadedStruct.Properties.ContainsKey("Online"), Is.True);
            Assert.That(loadedStruct.Properties.ContainsKey("Efficiency"), Is.True);
            Assert.That(loadedStruct.Properties.Properties["Built"], Is.EqualTo("True"));
            Assert.That(loadedStruct.Properties.Properties["Online"], Is.EqualTo("True"));
            Assert.That(loadedStruct.Properties.Properties["Efficiency"], Is.EqualTo("85"));
        }

        [Test]
        public async Task UpsertColony_WithStructureWorkers_PersistsWorkersBag()
        {
            var colony = new Colony
            {
                UUID = "col-4",
                OwnerUUID = "char-1",
                ColonyName = "Workers Colony",
                SystemName = "Gamma",
            };

            var structure = new ColonyStructure
            {
                UUID = "struct-workers-1",
                FlatpackBlueprintUUID = "bp-020",
                DisplaySequence = 1,
                BuildingID = 400,
            };

            structure.AssignedWorkers.SetProperty("BlueCollar", "3");
            structure.AssignedWorkers.SetProperty("WhiteCollar", "1");
            structure.AssignedWorkers.SetProperty("Specialist", "0");
            colony.Structures.Add(structure);

            await _backend.UpsertColonyAsync("char-1", colony);
            var loaded = await _backend.GetColonyAsync("char-1", "col-4");

            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded.Structures, Has.Count.EqualTo(1));

            var loadedStruct = loaded.Structures[0];
            Assert.That(loadedStruct.AssignedWorkers.ContainsKey("BlueCollar"), Is.True);
            Assert.That(loadedStruct.AssignedWorkers.ContainsKey("WhiteCollar"), Is.True);
            Assert.That(loadedStruct.AssignedWorkers.ContainsKey("Specialist"), Is.True);
            Assert.That(loadedStruct.AssignedWorkers.Properties["BlueCollar"], Is.EqualTo("3"));
            Assert.That(loadedStruct.AssignedWorkers.Properties["WhiteCollar"], Is.EqualTo("1"));
            Assert.That(loadedStruct.AssignedWorkers.Properties["Specialist"], Is.EqualTo("0"));
        }

        [Test]
        public async Task UpsertColony_WithItems_PersistsItemBag()
        {
            var colony = new Colony
            {
                UUID = "col-5",
                OwnerUUID = "char-1",
                ColonyName = "Items Colony",
                SystemName = "Delta",
            };

            colony.Items.AddItem(new Item(ItemType.ItemTypeEnum.Resource, "Iron")
            {
                UUID = "item-1",
                BaseItemTypeID = "Iron",
                Quantity = 500,
                ResourcePurity = "High",
                Volume = 1m,
            });

            colony.Items.AddItem(new Item(ItemType.ItemTypeEnum.Blueprint, "Flatpack A")
            {
                UUID = "item-2",
                BaseItemTypeID = "bp-flatpack-a",
                Quantity = 1,
                Volume = 0m,
            });

            await _backend.UpsertColonyAsync("char-1", colony);
            var loaded = await _backend.GetColonyAsync("char-1", "col-5");

            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded.Items.Count(), Is.EqualTo(2));
            Assert.That(loaded.Items.ContainsKey("item-1"), Is.True);
            Assert.That(loaded.Items.ContainsKey("item-2"), Is.True);

            var iron = loaded.Items.Items["item-1"];
            Assert.That(iron.Name, Is.EqualTo("Iron"));
            Assert.That(iron.Quantity, Is.EqualTo(500));
            Assert.That(iron.ResourcePurity, Is.EqualTo("High"));
            Assert.That(iron.ItemType, Is.EqualTo(ItemType.ItemTypeEnum.Resource));

            var bp = loaded.Items.Items["item-2"];
            Assert.That(bp.Name, Is.EqualTo("Flatpack A"));
            Assert.That(bp.BaseItemTypeID, Is.EqualTo("bp-flatpack-a"));
            Assert.That(bp.Quantity, Is.EqualTo(1));
        }

        [Test]
        public async Task DeleteColony_CascadesChildren()
        {
            var colony = new Colony
            {
                UUID = "col-del",
                OwnerUUID = "char-1",
                ColonyName = "Doomed Colony",
                SystemName = "Epsilon",
            };

            var structure = new ColonyStructure
            {
                UUID = "struct-doomed",
                FlatpackBlueprintUUID = "bp-030",
                DisplaySequence = 1,
                BuildingID = 500,
            };

            structure.Properties.SetProperty("Built", true);
            structure.AssignedWorkers.SetProperty("BlueCollar", "2");
            colony.Structures.Add(structure);

            colony.Items.AddItem(new Item(ItemType.ItemTypeEnum.Resource, "Gold")
            {
                UUID = "item-doomed",
                BaseItemTypeID = "Gold",
                Quantity = 100,
                ResourcePurity = "Medium",
                Volume = 1m,
            });

            await _backend.UpsertColonyAsync("char-1", colony);

            // Verify it exists first
            var exists = await _backend.GetColonyAsync("char-1", "col-del");
            Assert.That(exists, Is.Not.Null);

            // Delete and verify cascade
            await _backend.DeleteColonyAsync("char-1", "col-del");

            var deleted = await _backend.GetColonyAsync("char-1", "col-del");
            Assert.That(deleted, Is.Null);

            // Verify no orphaned children by re-inserting the same UUID
            // If children leaked, this would cause unique constraint violations
            var newColony = new Colony
            {
                UUID = "col-del",
                OwnerUUID = "char-1",
                ColonyName = "Reborn Colony",
                SystemName = "Epsilon",
            };

            await _backend.UpsertColonyAsync("char-1", newColony);
            var reborn = await _backend.GetColonyAsync("char-1", "col-del");
            Assert.That(reborn, Is.Not.Null);
            Assert.That(reborn.ColonyName, Is.EqualTo("Reborn Colony"));
            Assert.That(reborn.Structures, Has.Count.EqualTo(0));
            Assert.That(reborn.Items.Count(), Is.EqualTo(0));
        }

        [Test]
        public async Task GetAllColonies_FiltersByOwnerUUID()
        {
            var colony1 = new Colony
            {
                UUID = "col-owner-a1",
                OwnerUUID = "owner-a",
                ColonyName = "Alpha Colony 1",
                SystemName = "Zeta",
            };

            var colony2 = new Colony
            {
                UUID = "col-owner-a2",
                OwnerUUID = "owner-a",
                ColonyName = "Alpha Colony 2",
                SystemName = "Zeta",
            };

            var colony3 = new Colony
            {
                UUID = "col-owner-b1",
                OwnerUUID = "owner-b",
                ColonyName = "Beta Colony 1",
                SystemName = "Eta",
            };

            await _backend.UpsertColonyAsync("owner-a", colony1);
            await _backend.UpsertColonyAsync("owner-a", colony2);
            await _backend.UpsertColonyAsync("owner-b", colony3);

            var ownerAColonies = await _backend.GetAllColoniesAsync("owner-a");
            Assert.That(ownerAColonies, Has.Count.EqualTo(2));
            Assert.That(ownerAColonies[0].OwnerUUID, Is.EqualTo("owner-a"));
            Assert.That(ownerAColonies[1].OwnerUUID, Is.EqualTo("owner-a"));

            var ownerBColonies = await _backend.GetAllColoniesAsync("owner-b");
            Assert.That(ownerBColonies, Has.Count.EqualTo(1));
            Assert.That(ownerBColonies[0].UUID, Is.EqualTo("col-owner-b1"));

            var ownerCColonies = await _backend.GetAllColoniesAsync("owner-c");
            Assert.That(ownerCColonies, Has.Count.EqualTo(0));
        }

        [Test]
        public async Task UpsertColony_UpdateExisting_ReplacesData()
        {
            var colony = new Colony
            {
                UUID = "col-update",
                OwnerUUID = "char-1",
                ColonyName = "Original Name",
                SystemName = "Theta",
                ColonySize = 5,
            };

            colony.Structures.Add(new ColonyStructure
            {
                UUID = "struct-orig",
                FlatpackBlueprintUUID = "bp-040",
                DisplaySequence = 1,
                BuildingID = 600,
            });

            colony.Items.AddItem(new Item(ItemType.ItemTypeEnum.Resource, "Copper")
            {
                UUID = "item-orig",
                BaseItemTypeID = "Copper",
                Quantity = 200,
                ResourcePurity = "Low",
                Volume = 1m,
            });

            await _backend.UpsertColonyAsync("char-1", colony);

            // Second upsert with different data
            var updated = new Colony
            {
                UUID = "col-update",
                OwnerUUID = "char-1",
                ColonyName = "Updated Name",
                SystemName = "Theta Prime",
                ColonySize = 12,
            };

            updated.Structures.Add(new ColonyStructure
            {
                UUID = "struct-new",
                FlatpackBlueprintUUID = "bp-050",
                DisplaySequence = 1,
                BuildingID = 700,
            });

            // No items in the updated version

            await _backend.UpsertColonyAsync("char-1", updated);
            var loaded = await _backend.GetColonyAsync("char-1", "col-update");

            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded.ColonyName, Is.EqualTo("Updated Name"));
            Assert.That(loaded.SystemName, Is.EqualTo("Theta Prime"));
            Assert.That(loaded.ColonySize, Is.EqualTo(12));
            Assert.That(loaded.Structures, Has.Count.EqualTo(1));
            Assert.That(loaded.Structures[0].UUID, Is.EqualTo("struct-new"));
            Assert.That(loaded.Structures[0].BuildingID, Is.EqualTo(700));
            Assert.That(loaded.Items.Count(), Is.EqualTo(0));
        }
    }
}
