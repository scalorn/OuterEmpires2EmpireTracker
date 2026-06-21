// -----------------------------------------------------------------------
// <copyright file="SqliteBackendEntityTests.cs" company="OE2EmpireTracker">
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
using BlueprintModel = global::OE2EmpireTracker.Models.Blueprint;

namespace OE2EmpireTracker.Tests.Storage
{
    /// <summary>
    /// Tests for SqliteBackend Blueprint, Survey, PlayerProfile, DeliveryRoute,
    /// and DeliveryPlan CRUD with child tables.
    /// </summary>
    [TestFixture]
    public class SqliteBackendEntityTests
    {
        private string _dbPath;
        private SqliteBackend _backend;

        [SetUp]
        public async Task SetUp()
        {
            _dbPath = Path.Combine(Path.GetTempPath(), "SqliteEntityTest_" + Path.GetRandomFileName() + ".db");
            var config = new StorageBackendConfig { ConnectionString = _dbPath };
            _backend = new SqliteBackend(config);
            await _backend.InitializeAsync();
        }

        [TearDown]
        public void TearDown()
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(_dbPath))
            {
                File.Delete(_dbPath);
            }
        }

        [Test]
        public async Task Blueprint_UpsertAndGet_WithProperties()
        {
            var bp = new BlueprintModel("Mining Laser")
            {
                UUID = "bp-1",
                OwnerUUID = "char-1",
                BluePrintType = "Equipment",
                TechLevel = "Advanced",
                Class = 3,
                Evolution = 2,
                CopyCost = 500,
                BaseItemTypeID = "mining-laser-mk2",
                NickName = "LaserNick",
                Description = "A mining laser",
                Quantity = 1,
                Volume = 2.5m,
            };

            bp.Properties.SetProperty("Built", true);
            bp.Properties.SetProperty("Efficiency", "92");
            bp.Properties.SetProperty("Durability", "100");

            await _backend.UpsertBlueprintAsync("char-1", bp);
            var loaded = await _backend.GetBlueprintAsync("char-1", "bp-1");

            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded.UUID, Is.EqualTo("bp-1"));
            Assert.That(loaded.OwnerUUID, Is.EqualTo("char-1"));
            Assert.That(loaded.Name, Is.EqualTo("Mining Laser"));
            Assert.That(loaded.BluePrintType, Is.EqualTo("Equipment"));
            Assert.That(loaded.TechLevel, Is.EqualTo("Advanced"));
            Assert.That(loaded.Class, Is.EqualTo(3));
            Assert.That(loaded.Evolution, Is.EqualTo(2));
            Assert.That(loaded.CopyCost, Is.EqualTo(500));
            Assert.That(loaded.Properties, Is.Not.Null);
            Assert.That(loaded.Properties.Count, Is.EqualTo(3));
            Assert.That(loaded.Properties.Properties["Built"], Is.EqualTo("True"));
            Assert.That(loaded.Properties.Properties["Efficiency"], Is.EqualTo("92"));
            Assert.That(loaded.Properties.Properties["Durability"], Is.EqualTo("100"));
        }

        [Test]
        public async Task Blueprint_UpsertAndGet_WithResources()
        {
            var bp = new BlueprintModel("Hull Plating")
            {
                UUID = "bp-2",
                OwnerUUID = "char-1",
                BluePrintType = "Flatpack",
                TechLevel = "Basic",
                Class = 1,
                Evolution = 0,
                CopyCost = 200,
                BaseItemTypeID = "hull-plating-basic",
            };

            bp.Resources["Iron"] = "100";
            bp.Resources["Copper"] = "50";
            bp.Resources["Silicon"] = "25";

            await _backend.UpsertBlueprintAsync("char-1", bp);
            var loaded = await _backend.GetBlueprintAsync("char-1", "bp-2");

            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded.UUID, Is.EqualTo("bp-2"));
            Assert.That(loaded.Name, Is.EqualTo("Hull Plating"));
            Assert.That(loaded.Resources, Is.Not.Null);
            Assert.That(loaded.Resources.Count, Is.EqualTo(3));
            Assert.That(loaded.Resources["Iron"], Is.EqualTo("100"));
            Assert.That(loaded.Resources["Copper"], Is.EqualTo("50"));
            Assert.That(loaded.Resources["Silicon"], Is.EqualTo("25"));
        }

        [Test]
        public async Task Survey_UpsertAndGet_WithProperties()
        {
            var survey = new Survey("Planet Alpha Survey")
            {
                UUID = "survey-1",
                OwnerUUID = "char-1",
                ScannedBy = "Explorer One",
                DateTime = "2024-01-15T10:30:00",
                PlanetName = "Alpha Prime",
                SystemName = "Alpha Centauri",
                SurveyID = "SURV-001",
                SurveyType = SurveyType.Planet,
                SystemObjectId = 42,
            };

            survey.Properties["Gravity"] = "1.2g";
            survey.Properties["Atmosphere"] = "Nitrogen-Oxygen";
            survey.Properties["Temperature"] = "22C";

            await _backend.UpsertSurveyAsync("char-1", survey);
            var loaded = await _backend.GetSurveyAsync("char-1", "SURV-001");

            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded.UUID, Is.EqualTo("survey-1"));
            Assert.That(loaded.OwnerUUID, Is.EqualTo("char-1"));
            Assert.That(loaded.PlanetName, Is.EqualTo("Alpha Prime"));
            Assert.That(loaded.SystemName, Is.EqualTo("Alpha Centauri"));
            Assert.That(loaded.SurveyID, Is.EqualTo("SURV-001"));
            Assert.That(loaded.ScannedBy, Is.EqualTo("Explorer One"));
            Assert.That(loaded.Properties, Is.Not.Null);
            Assert.That(loaded.Properties.Count, Is.EqualTo(3));
            Assert.That(loaded.Properties["Gravity"], Is.EqualTo("1.2g"));
            Assert.That(loaded.Properties["Atmosphere"], Is.EqualTo("Nitrogen-Oxygen"));
            Assert.That(loaded.Properties["Temperature"], Is.EqualTo("22C"));
        }

        [Test]
        public async Task Survey_UpsertAndGet_WithResources()
        {
            var survey = new Survey("Asteroid Beta Survey")
            {
                UUID = "survey-2",
                OwnerUUID = "char-1",
                ScannedBy = "Scanner Bot",
                PlanetName = "Beta Rock",
                SystemName = "Beta System",
                SurveyID = "SURV-002",
                SurveyType = SurveyType.Asteroid,
                AsteroidUUID = "asteroid-99",
            };

            survey.Resources["res-iron"] = new SurveyResource("Iron", "High", "1200");
            survey.Resources["res-copper"] = new SurveyResource("Copper", "Medium", "800");

            await _backend.UpsertSurveyAsync("char-1", survey);
            var loaded = await _backend.GetSurveyAsync("char-1", "SURV-002");

            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded.UUID, Is.EqualTo("survey-2"));
            Assert.That(loaded.SurveyType, Is.EqualTo(SurveyType.Asteroid));
            Assert.That(loaded.AsteroidUUID, Is.EqualTo("asteroid-99"));
            Assert.That(loaded.Resources, Is.Not.Null);
            Assert.That(loaded.Resources.Count, Is.EqualTo(2));
            Assert.That(loaded.Resources["res-iron"].Resource, Is.EqualTo("Iron"));
            Assert.That(loaded.Resources["res-iron"].Purity, Is.EqualTo("High"));
            Assert.That(loaded.Resources["res-iron"].Amount, Is.EqualTo("1200"));
            Assert.That(loaded.Resources["res-copper"].Resource, Is.EqualTo("Copper"));
            Assert.That(loaded.Resources["res-copper"].Purity, Is.EqualTo("Medium"));
            Assert.That(loaded.Resources["res-copper"].Amount, Is.EqualTo("800"));
        }

        [Test]
        public async Task PlayerProfile_UpsertAndGet_WithSkills()
        {
            var profile = new PlayerProfile
            {
                UUID = "player-1",
                Name = "TestPlayer",
                Faction = "Explorers Guild",
                FactionUUID = "faction-1",
                TotalCredits = 50000.75m,
                SkillPoints = 42,
                CitizenId = "CIT-12345",
                CharacterId = 99,
                FirstName = "John",
                LastName = "Doe",
            };

            profile.Skills["Mining"] = new PlayerSkill
            {
                Level = 5,
                TrainingStarted = true,
                SkillId = 101,
                EffectDescription = "Increases mining yield",
                AmountPerLevel = 10,
                SkillGroupName = "Colony Operations",
                IsUnlocked = true,
                TargetLevel = 7,
                TrainingPercentageComplete = 60,
                RemainingMinutes = 120,
            };

            profile.Skills["Refining"] = new PlayerSkill
            {
                Level = 3,
                TrainingStarted = false,
                SkillId = 102,
                EffectDescription = "Increases refining speed",
                AmountPerLevel = 5,
                SkillGroupName = "Colony Operations",
                IsUnlocked = true,
            };

            await _backend.UpsertPlayerProfileAsync("char-1", profile);
            var loaded = await _backend.GetPlayerProfileAsync("char-1", "player-1");

            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded.UUID, Is.EqualTo("player-1"));
            Assert.That(loaded.Name, Is.EqualTo("TestPlayer"));
            Assert.That(loaded.Faction, Is.EqualTo("Explorers Guild"));
            Assert.That(loaded.TotalCredits, Is.EqualTo(50000.75m));
            Assert.That(loaded.SkillPoints, Is.EqualTo(42));
            Assert.That(loaded.Skills, Is.Not.Null);
            Assert.That(loaded.Skills.Count, Is.EqualTo(2));
            Assert.That(loaded.Skills["Mining"].Level, Is.EqualTo(5));
            Assert.That(loaded.Skills["Mining"].TrainingStarted, Is.True);
            Assert.That(loaded.Skills["Mining"].SkillId, Is.EqualTo(101));
            Assert.That(loaded.Skills["Mining"].TargetLevel, Is.EqualTo(7));
            Assert.That(loaded.Skills["Refining"].Level, Is.EqualTo(3));
            Assert.That(loaded.Skills["Refining"].TrainingStarted, Is.False);
            Assert.That(loaded.Skills["Refining"].SkillId, Is.EqualTo(102));
        }

        [Test]
        public async Task DeliveryRoute_UpsertAndGet_WithStops()
        {
            var route = new DeliveryRoute
            {
                UUID = "route-1",
                Name = "Trade Route Alpha",
                OwnerUUID = "char-1",
            };

            route.Stops.Add(new RouteStop
            {
                ColonyUUID = "col-a",
                Sequence = 0,
                DestinationType = DestinationType.Colony,
                DestinationUUID = "col-a",
                Purpose = RouteStopPurpose.Cargo,
                FuelEstimate = 10.5m,
            });

            route.Stops.Add(new RouteStop
            {
                ColonyUUID = "station-b",
                Sequence = 1,
                DestinationType = DestinationType.Station,
                DestinationUUID = "station-b",
                Purpose = RouteStopPurpose.Refuel,
                FuelEstimate = 5.0m,
            });

            route.Stops.Add(new RouteStop
            {
                ColonyUUID = "col-c",
                Sequence = 2,
                DestinationType = DestinationType.Colony,
                DestinationUUID = "col-c",
                Purpose = RouteStopPurpose.Cargo,
                FuelEstimate = 15.2m,
            });

            await _backend.UpsertDeliveryRouteAsync("char-1", route);
            var loaded = await _backend.GetDeliveryRouteAsync("char-1", "route-1");

            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded.UUID, Is.EqualTo("route-1"));
            Assert.That(loaded.Name, Is.EqualTo("Trade Route Alpha"));
            Assert.That(loaded.OwnerUUID, Is.EqualTo("char-1"));
            Assert.That(loaded.Stops, Has.Count.EqualTo(3));
            Assert.That(loaded.Stops[0].ColonyUUID, Is.EqualTo("col-a"));
            Assert.That(loaded.Stops[0].DestinationType, Is.EqualTo(DestinationType.Colony));
            Assert.That(loaded.Stops[0].FuelEstimate, Is.EqualTo(10.5m));
            Assert.That(loaded.Stops[1].DestinationType, Is.EqualTo(DestinationType.Station));
            Assert.That(loaded.Stops[1].Purpose, Is.EqualTo(RouteStopPurpose.Refuel));
            Assert.That(loaded.Stops[2].Sequence, Is.EqualTo(2));
            Assert.That(loaded.Stops[2].FuelEstimate, Is.EqualTo(15.2m));
        }

        [Test]
        public async Task DeliveryPlan_UpsertAndGet_WithStopsAndItems()
        {
            var plan = new DeliveryPlan
            {
                UUID = "plan-1",
                Name = "Weekly Delivery",
                OwnerUUID = "char-1",
                RouteUUID = "route-1",
                ShipUUID = "ship-1",
                Completed = false,
            };

            var stop1 = new DeliveryPlanStop
            {
                ColonyUUID = "col-a",
                Sequence = 0,
                StopCompleted = false,
                DestinationType = DestinationType.Colony,
                DestinationUUID = "col-a",
            };

            stop1.DropOff.Add(new DeliveryItem
            {
                ItemType = ItemType.ItemTypeEnum.Resource,
                BaseItemTypeID = "Iron",
                Name = "Iron",
                ResourcePurity = "High",
                Quantity = 500,
                Delivered = false,
            });

            stop1.PickUp.Add(new DeliveryItem
            {
                ItemType = ItemType.ItemTypeEnum.Commodity,
                BaseItemTypeID = "Steel",
                Name = "Steel Plates",
                ResourcePurity = string.Empty,
                Quantity = 200,
                Delivered = false,
            });

            var stop2 = new DeliveryPlanStop
            {
                ColonyUUID = "col-b",
                Sequence = 1,
                StopCompleted = true,
                DestinationType = DestinationType.Colony,
                DestinationUUID = "col-b",
            };

            stop2.DropOff.Add(new DeliveryItem
            {
                ItemType = ItemType.ItemTypeEnum.Commodity,
                BaseItemTypeID = "Steel",
                Name = "Steel Plates",
                ResourcePurity = string.Empty,
                Quantity = 100,
                Delivered = true,
            });

            plan.Stops.Add(stop1);
            plan.Stops.Add(stop2);

            await _backend.UpsertDeliveryPlanAsync("char-1", plan);
            var loaded = await _backend.GetDeliveryPlanAsync("char-1", "plan-1");

            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded.UUID, Is.EqualTo("plan-1"));
            Assert.That(loaded.Name, Is.EqualTo("Weekly Delivery"));
            Assert.That(loaded.RouteUUID, Is.EqualTo("route-1"));
            Assert.That(loaded.ShipUUID, Is.EqualTo("ship-1"));
            Assert.That(loaded.Completed, Is.False);
            Assert.That(loaded.Stops, Has.Count.EqualTo(2));
            Assert.That(loaded.Stops[0].DropOff, Has.Count.EqualTo(1));
            Assert.That(loaded.Stops[0].DropOff[0].Name, Is.EqualTo("Iron"));
            Assert.That(loaded.Stops[0].DropOff[0].Quantity, Is.EqualTo(500));
            Assert.That(loaded.Stops[0].DropOff[0].ResourcePurity, Is.EqualTo("High"));
            Assert.That(loaded.Stops[0].PickUp, Has.Count.EqualTo(1));
            Assert.That(loaded.Stops[0].PickUp[0].Name, Is.EqualTo("Steel Plates"));
            Assert.That(loaded.Stops[0].PickUp[0].Quantity, Is.EqualTo(200));
            Assert.That(loaded.Stops[1].StopCompleted, Is.True);
            Assert.That(loaded.Stops[1].DropOff, Has.Count.EqualTo(1));
            Assert.That(loaded.Stops[1].DropOff[0].Delivered, Is.True);
        }

        [Test]
        public async Task Blueprint_Delete_CascadesChildren()
        {
            var bp = new BlueprintModel("Doomed Blueprint")
            {
                UUID = "bp-del",
                OwnerUUID = "char-1",
                BluePrintType = "Equipment",
                TechLevel = "Basic",
                Class = 1,
                BaseItemTypeID = "doomed-item",
            };

            bp.Properties.SetProperty("Quality", "100");
            bp.Properties.SetProperty("Rarity", "Common");
            bp.Resources["Iron"] = "50";
            bp.Resources["Gold"] = "10";

            await _backend.UpsertBlueprintAsync("char-1", bp);

            var exists = await _backend.GetBlueprintAsync("char-1", "bp-del");
            Assert.That(exists, Is.Not.Null);

            await _backend.DeleteBlueprintAsync("char-1", "bp-del");

            var deleted = await _backend.GetBlueprintAsync("char-1", "bp-del");
            Assert.That(deleted, Is.Null);

            // Verify no orphaned children by re-inserting the same UUID
            var reborn = new BlueprintModel("Reborn Blueprint")
            {
                UUID = "bp-del",
                OwnerUUID = "char-1",
                BluePrintType = "Flatpack",
                TechLevel = "Advanced",
                Class = 2,
                BaseItemTypeID = "reborn-item",
            };

            await _backend.UpsertBlueprintAsync("char-1", reborn);
            var loaded = await _backend.GetBlueprintAsync("char-1", "bp-del");

            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded.Name, Is.EqualTo("Reborn Blueprint"));
            Assert.That(loaded.Properties.Count, Is.EqualTo(0));
            Assert.That(loaded.Resources.Count, Is.EqualTo(0));
        }

        [Test]
        public async Task Survey_FilterByOwnerUUID()
        {
            var survey1 = new Survey("Owner A Survey 1")
            {
                UUID = "survey-a1",
                OwnerUUID = "owner-a",
                PlanetName = "Planet X",
                SystemName = "System X",
                SurveyID = "SA1",
            };

            var survey2 = new Survey("Owner A Survey 2")
            {
                UUID = "survey-a2",
                OwnerUUID = "owner-a",
                PlanetName = "Planet Y",
                SystemName = "System Y",
                SurveyID = "SA2",
            };

            var survey3 = new Survey("Owner B Survey 1")
            {
                UUID = "survey-b1",
                OwnerUUID = "owner-b",
                PlanetName = "Planet Z",
                SystemName = "System Z",
                SurveyID = "SB1",
            };

            await _backend.UpsertSurveyAsync("owner-a", survey1);
            await _backend.UpsertSurveyAsync("owner-a", survey2);
            await _backend.UpsertSurveyAsync("owner-b", survey3);

            var ownerASurveys = await _backend.GetAllSurveysAsync("owner-a");
            Assert.That(ownerASurveys, Has.Count.EqualTo(2));
            Assert.That(ownerASurveys[0].OwnerUUID, Is.EqualTo("owner-a"));
            Assert.That(ownerASurveys[1].OwnerUUID, Is.EqualTo("owner-a"));

            var ownerBSurveys = await _backend.GetAllSurveysAsync("owner-b");
            Assert.That(ownerBSurveys, Has.Count.EqualTo(1));
            Assert.That(ownerBSurveys[0].UUID, Is.EqualTo("survey-b1"));

            var ownerCSurveys = await _backend.GetAllSurveysAsync("owner-c");
            Assert.That(ownerCSurveys, Has.Count.EqualTo(0));
        }

        [Test]
        public async Task DeliveryPlan_UpdateExisting_ReplacesStopsAndItems()
        {
            var plan = new DeliveryPlan
            {
                UUID = "plan-update",
                Name = "Original Plan",
                OwnerUUID = "char-1",
                RouteUUID = "route-orig",
                ShipUUID = "ship-orig",
                Completed = false,
            };

            var origStop = new DeliveryPlanStop
            {
                ColonyUUID = "col-orig",
                Sequence = 0,
                StopCompleted = false,
                DestinationType = DestinationType.Colony,
                DestinationUUID = "col-orig",
            };

            origStop.DropOff.Add(new DeliveryItem
            {
                ItemType = ItemType.ItemTypeEnum.Resource,
                BaseItemTypeID = "Copper",
                Name = "Copper",
                ResourcePurity = "Low",
                Quantity = 300,
                Delivered = false,
            });

            plan.Stops.Add(origStop);

            await _backend.UpsertDeliveryPlanAsync("char-1", plan);

            // Second upsert with completely different stops
            var updated = new DeliveryPlan
            {
                UUID = "plan-update",
                Name = "Updated Plan",
                OwnerUUID = "char-1",
                RouteUUID = "route-new",
                ShipUUID = "ship-new",
                Completed = true,
            };

            var newStop1 = new DeliveryPlanStop
            {
                ColonyUUID = "col-new-1",
                Sequence = 0,
                StopCompleted = true,
                DestinationType = DestinationType.Station,
                DestinationUUID = "station-new-1",
            };

            newStop1.PickUp.Add(new DeliveryItem
            {
                ItemType = ItemType.ItemTypeEnum.Resource,
                BaseItemTypeID = "Gold",
                Name = "Gold",
                ResourcePurity = "High",
                Quantity = 50,
                Delivered = true,
            });

            var newStop2 = new DeliveryPlanStop
            {
                ColonyUUID = "col-new-2",
                Sequence = 1,
                StopCompleted = false,
                DestinationType = DestinationType.Colony,
                DestinationUUID = "col-new-2",
            };

            newStop2.DropOff.Add(new DeliveryItem
            {
                ItemType = ItemType.ItemTypeEnum.Resource,
                BaseItemTypeID = "Gold",
                Name = "Gold",
                ResourcePurity = "High",
                Quantity = 50,
                Delivered = false,
            });

            updated.Stops.Add(newStop1);
            updated.Stops.Add(newStop2);

            await _backend.UpsertDeliveryPlanAsync("char-1", updated);
            var loaded = await _backend.GetDeliveryPlanAsync("char-1", "plan-update");

            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded.Name, Is.EqualTo("Updated Plan"));
            Assert.That(loaded.RouteUUID, Is.EqualTo("route-new"));
            Assert.That(loaded.ShipUUID, Is.EqualTo("ship-new"));
            Assert.That(loaded.Completed, Is.True);
            Assert.That(loaded.Stops, Has.Count.EqualTo(2));
            Assert.That(loaded.Stops[0].ColonyUUID, Is.EqualTo("col-new-1"));
            Assert.That(loaded.Stops[0].PickUp, Has.Count.EqualTo(1));
            Assert.That(loaded.Stops[0].PickUp[0].Name, Is.EqualTo("Gold"));
            Assert.That(loaded.Stops[0].DropOff, Has.Count.EqualTo(0));
            Assert.That(loaded.Stops[1].DropOff, Has.Count.EqualTo(1));
            Assert.That(loaded.Stops[1].DropOff[0].Quantity, Is.EqualTo(50));
            Assert.That(loaded.Stops[1].PickUp, Has.Count.EqualTo(0));
        }
    }
}
