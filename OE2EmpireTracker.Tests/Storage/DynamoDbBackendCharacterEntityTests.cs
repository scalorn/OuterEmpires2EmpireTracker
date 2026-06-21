// -----------------------------------------------------------------------
// <copyright file="DynamoDbBackendCharacterEntityTests.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using OE2EmpireTracker.Common.Storage;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Tests.Storage
{
    /// <summary>
    /// Tests for DynamoDbBackend per-character entity CRUD operations:
    /// Colony, Blueprint, Survey, PlayerProfile, DeliveryRoute, Ship.
    /// Satisfies: Req 5, Criteria 1-2.
    /// </summary>
    [TestFixture]
    public class DynamoDbBackendCharacterEntityTests
    {
        private DynamoDbBackend _backend;
        private string _characterUUID;

        [SetUp]
        public async Task SetUp()
        {
            if (!DynamoDbLocalFixture.IsAvailable)
            {
                Assert.Ignore("DynamoDB Local is not available");
            }

            var tablePrefix = "test_" + Guid.NewGuid().ToString("N").Substring(0, 8);
            _backend = new DynamoDbBackend(
                tablePrefix,
                "us-east-1",
                DynamoDbLocalFixture.ServiceUrl);
            await _backend.InitializeAsync();
            _characterUUID = Guid.NewGuid().ToString();
        }

        [Test]
        public async Task Colony_UpsertAndGet()
        {
            // Arrange
            var colony = new Colony
            {
                UUID = Guid.NewGuid().ToString(),
                OwnerUUID = _characterUUID,
                ColonyName = "Test Colony Prime",
                PlanetName = "Terra Nova",
                SystemName = "Alpha Centauri",
                ColonySize = 5,
                ColonyId = 42,
                SystemId = 7,
            };

            // Act
            await _backend.UpsertColonyAsync(_characterUUID, colony);
            var retrieved = await _backend.GetColonyAsync(_characterUUID, colony.UUID);

            // Assert
            Assert.That(retrieved, Is.Not.Null);
            Assert.That(retrieved.UUID, Is.EqualTo(colony.UUID));
            Assert.That(retrieved.OwnerUUID, Is.EqualTo(_characterUUID));
            Assert.That(retrieved.ColonyName, Is.EqualTo("Test Colony Prime"));
            Assert.That(retrieved.PlanetName, Is.EqualTo("Terra Nova"));
            Assert.That(retrieved.SystemName, Is.EqualTo("Alpha Centauri"));
            Assert.That(retrieved.ColonySize, Is.EqualTo(5));
            Assert.That(retrieved.ColonyId, Is.EqualTo(42));
            Assert.That(retrieved.SystemId, Is.EqualTo(7));
        }

        [Test]
        public async Task Blueprint_UpsertAndGetAll()
        {
            // Arrange
            var bp1 = new Blueprint("Laser Mk1")
            {
                UUID = Guid.NewGuid().ToString(),
                OwnerUUID = _characterUUID,
                BluePrintType = "Weapon",
                TechLevel = "TL3",
                Evolution = 2,
            };

            var bp2 = new Blueprint("Shield Mk2")
            {
                UUID = Guid.NewGuid().ToString(),
                OwnerUUID = _characterUUID,
                BluePrintType = "Defense",
                TechLevel = "TL5",
                Evolution = 1,
            };

            // Act
            await _backend.UpsertBlueprintAsync(_characterUUID, bp1);
            await _backend.UpsertBlueprintAsync(_characterUUID, bp2);
            var all = await _backend.GetAllBlueprintsAsync(_characterUUID);

            // Assert
            Assert.That(all.Count, Is.EqualTo(2));
            Assert.That(all.Any(b => b.UUID == bp1.UUID && b.Name == "Laser Mk1"), Is.True);
            Assert.That(all.Any(b => b.UUID == bp2.UUID && b.Name == "Shield Mk2"), Is.True);
        }

        [Test]
        public async Task Survey_UpsertAndDelete()
        {
            // Arrange
            var survey = new Survey("Planet Survey Alpha")
            {
                UUID = Guid.NewGuid().ToString(),
                OwnerUUID = _characterUUID,
                PlanetName = "Kepler-22b",
                SystemName = "Kepler-22",
                SurveyID = "SRV-001",
            };

            // Act — upsert and verify present
            await _backend.UpsertSurveyAsync(_characterUUID, survey);
            var retrieved = await _backend.GetSurveyAsync(_characterUUID, survey.UUID);
            Assert.That(retrieved, Is.Not.Null);
            Assert.That(retrieved.UUID, Is.EqualTo(survey.UUID));
            Assert.That(retrieved.PlanetName, Is.EqualTo("Kepler-22b"));

            // Act — delete and verify gone
            await _backend.DeleteSurveyAsync(_characterUUID, survey.UUID);
            var afterDelete = await _backend.GetSurveyAsync(_characterUUID, survey.UUID);
            Assert.That(afterDelete, Is.Null);
        }

        [Test]
        public async Task PlayerProfile_UpsertAndGet()
        {
            // Arrange
            var profile = new PlayerProfile
            {
                UUID = _characterUUID,
                Name = "Commander Shepard",
                Faction = "Systems Alliance",
                FactionUUID = Guid.NewGuid().ToString(),
                TotalCredits = 500000m,
                SkillPoints = 42,
                CitizenId = "CIT-7729",
                CharacterId = 1001,
                FirstName = "Commander",
                LastName = "Shepard",
            };

            // Act
            await _backend.UpsertPlayerProfileAsync(_characterUUID, profile);
            var retrieved = await _backend.GetPlayerProfileAsync(_characterUUID, profile.UUID);

            // Assert
            Assert.That(retrieved, Is.Not.Null);
            Assert.That(retrieved.UUID, Is.EqualTo(_characterUUID));
            Assert.That(retrieved.Name, Is.EqualTo("Commander Shepard"));
            Assert.That(retrieved.Faction, Is.EqualTo("Systems Alliance"));
            Assert.That(retrieved.TotalCredits, Is.EqualTo(500000m));
            Assert.That(retrieved.SkillPoints, Is.EqualTo(42));
            Assert.That(retrieved.CitizenId, Is.EqualTo("CIT-7729"));
            Assert.That(retrieved.CharacterId, Is.EqualTo(1001));
            Assert.That(retrieved.FirstName, Is.EqualTo("Commander"));
            Assert.That(retrieved.LastName, Is.EqualTo("Shepard"));
        }

        [Test]
        public async Task DeliveryRoute_UpsertAndGet()
        {
            // Arrange
            var route = new DeliveryRoute
            {
                UUID = Guid.NewGuid().ToString(),
                Name = "Mining Run Alpha",
                OwnerUUID = _characterUUID,
                Stops = new List<RouteStop>
                {
                    new RouteStop
                    {
                        ColonyUUID = Guid.NewGuid().ToString(),
                        Sequence = 0,
                        DestinationType = DestinationType.Colony,
                        DestinationUUID = Guid.NewGuid().ToString(),
                        Purpose = RouteStopPurpose.Cargo,
                        FuelEstimate = 12.5m,
                    },
                    new RouteStop
                    {
                        ColonyUUID = Guid.NewGuid().ToString(),
                        Sequence = 1,
                        DestinationType = DestinationType.Station,
                        DestinationUUID = Guid.NewGuid().ToString(),
                        Purpose = RouteStopPurpose.Fuel,
                        FuelEstimate = 8.0m,
                    },
                },
            };

            // Act
            await _backend.UpsertDeliveryRouteAsync(_characterUUID, route);
            var retrieved = await _backend.GetDeliveryRouteAsync(_characterUUID, route.UUID);

            // Assert
            Assert.That(retrieved, Is.Not.Null);
            Assert.That(retrieved.UUID, Is.EqualTo(route.UUID));
            Assert.That(retrieved.Name, Is.EqualTo("Mining Run Alpha"));
            Assert.That(retrieved.OwnerUUID, Is.EqualTo(_characterUUID));
            Assert.That(retrieved.Stops, Has.Count.EqualTo(2));
            Assert.That(retrieved.Stops[0].Sequence, Is.EqualTo(0));
            Assert.That(retrieved.Stops[0].DestinationType, Is.EqualTo(DestinationType.Colony));
            Assert.That(retrieved.Stops[0].FuelEstimate, Is.EqualTo(12.5m));
            Assert.That(retrieved.Stops[1].Sequence, Is.EqualTo(1));
            Assert.That(retrieved.Stops[1].DestinationType, Is.EqualTo(DestinationType.Station));
            Assert.That(retrieved.Stops[1].Purpose, Is.EqualTo(RouteStopPurpose.Fuel));
        }

        [Test]
        public async Task Ship_UpsertAndGet()
        {
            // Arrange
            var ship = new Ship
            {
                UUID = Guid.NewGuid().ToString(),
                Name = "SS Enterprise",
                OwnerUUID = _characterUUID,
                TemplateUUID = Guid.NewGuid().ToString(),
                HullBlueprintUUID = Guid.NewGuid().ToString(),
                LocationType = DestinationType.Station,
                LocationUUID = Guid.NewGuid().ToString(),
                HullCurrentHP = 800,
                HullMaxHP = 1000,
                HullMaxRepairPercent = 0.95m,
                Components = new List<ShipComponentSlot>
                {
                    new ShipComponentSlot
                    {
                        SlotType = "Weapon",
                        SlotIndex = 0,
                        BlueprintUUID = Guid.NewGuid().ToString(),
                        CurrentHP = 100,
                        MaxHP = 100,
                        MaxRepairPercent = 1.0m,
                    },
                    new ShipComponentSlot
                    {
                        SlotType = "Shield",
                        SlotIndex = 1,
                        BlueprintUUID = Guid.NewGuid().ToString(),
                        CurrentHP = 75,
                        MaxHP = 120,
                        MaxRepairPercent = 0.9m,
                    },
                },
            };

            // Act
            await _backend.UpsertShipAsync(_characterUUID, ship);
            var retrieved = await _backend.GetShipAsync(_characterUUID, ship.UUID);

            // Assert
            Assert.That(retrieved, Is.Not.Null);
            Assert.That(retrieved.UUID, Is.EqualTo(ship.UUID));
            Assert.That(retrieved.Name, Is.EqualTo("SS Enterprise"));
            Assert.That(retrieved.OwnerUUID, Is.EqualTo(_characterUUID));
            Assert.That(retrieved.HullCurrentHP, Is.EqualTo(800));
            Assert.That(retrieved.HullMaxHP, Is.EqualTo(1000));
            Assert.That(retrieved.HullMaxRepairPercent, Is.EqualTo(0.95m));
            Assert.That(retrieved.Components, Has.Count.EqualTo(2));
            Assert.That(retrieved.Components[0].SlotType, Is.EqualTo("Weapon"));
            Assert.That(retrieved.Components[0].CurrentHP, Is.EqualTo(100));
            Assert.That(retrieved.Components[1].SlotType, Is.EqualTo("Shield"));
            Assert.That(retrieved.Components[1].CurrentHP, Is.EqualTo(75));
            Assert.That(retrieved.Components[1].MaxHP, Is.EqualTo(120));
        }
    }
}
