using System;
using System.Collections.Generic;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Unit tests for DeliveryRouteService edge cases and event firing.
    /// Feature: bl-112-deliveryroute-readonly
    /// Validates: Requirements 12.7, 12.8, 13.2, 13.3, 13.7, 13.8, 14.4, 14.5
    /// </summary>
    [TestFixture]
    public class DeliveryRouteServiceTests
    {
        private PlayerContext playerContext;
        private DeliveryRouteService service;

        [SetUp]
        public void SetUp()
        {
            TestHelper.ResetWithCachedData();
            playerContext = PlayerContext.GetInstance();
            playerContext.CurrentPlayerUUID = "test-player-uuid";
            service = new DeliveryRouteService(playerContext);
        }

        // -------------------------------------------------------------------
        // Requirement 12.8: Update throws InvalidOperationException on unknown UUID
        // -------------------------------------------------------------------

        [Test]
        public void Update_NonExistentUUID_ThrowsInvalidOperationException()
        {
            var request = new DeliveryRouteUpdateRequest
            {
                Name = "Test",
                Stops = new List<RouteStop>(),
            };

            Assert.Throws<InvalidOperationException>(
                () => service.Update("nonexistent-uuid", request));
        }

        // -------------------------------------------------------------------
        // Requirement 14.5: Delete with empty UUID returns without error
        // -------------------------------------------------------------------

        [Test]
        public void Delete_EmptyUUID_ReturnsWithoutError()
        {
            Assert.DoesNotThrow(() => service.Delete(string.Empty));
        }

        // -------------------------------------------------------------------
        // Requirement 14.5: Delete with non-existent UUID returns without error
        // -------------------------------------------------------------------

        [Test]
        public void Delete_NonExistentUUID_ReturnsWithoutError()
        {
            Assert.DoesNotThrow(() => service.Delete("nonexistent-uuid"));
        }

        // -------------------------------------------------------------------
        // Requirement 13.2: Create assigns non-empty UUID
        // -------------------------------------------------------------------

        [Test]
        public void Create_AssignsNonEmptyUUID()
        {
            var request = new DeliveryRouteCreateRequest
            {
                Name = "NewRoute",
                Stops = new List<RouteStop>(),
            };

            var result = service.Create(request);

            Assert.That(result.UUID, Is.Not.Null.And.Not.Empty);
        }

        // -------------------------------------------------------------------
        // Requirement 13.3: Create sets OwnerUUID to current player UUID
        // -------------------------------------------------------------------

        [Test]
        public void Create_SetsOwnerUUID_ToCurrentPlayerUUID()
        {
            var request = new DeliveryRouteCreateRequest
            {
                Name = "OwnerTest",
                Stops = new List<RouteStop>(),
            };

            var result = service.Create(request);

            Assert.That(result.OwnerUUID, Is.EqualTo("test-player-uuid"));
        }

        // -------------------------------------------------------------------
        // Requirement 12.7: Update fires DeliveryDataChanged event
        // -------------------------------------------------------------------

        [Test]
        public void Update_FiresDeliveryDataChangedEvent()
        {
            var route = new DeliveryRoute
            {
                UUID = Guid.NewGuid().ToString(),
                OwnerUUID = "test-player-uuid",
                Name = "EventTest",
            };
            playerContext.AddDeliveryRoute(route);

            bool eventFired = false;
            playerContext.DeliveryDataChanged += (s, e) => eventFired = true;

            var request = new DeliveryRouteUpdateRequest
            {
                Name = "Updated",
                Stops = new List<RouteStop>(),
            };
            service.Update(route.UUID, request);

            Assert.That(eventFired, Is.True);
        }

        // -------------------------------------------------------------------
        // Requirement 13.7: Create fires DeliveryDataChanged event
        // -------------------------------------------------------------------

        [Test]
        public void Create_FiresDeliveryDataChangedEvent()
        {
            bool eventFired = false;
            playerContext.DeliveryDataChanged += (s, e) => eventFired = true;

            var request = new DeliveryRouteCreateRequest
            {
                Name = "NewRoute",
                Stops = new List<RouteStop>(),
            };
            service.Create(request);

            Assert.That(eventFired, Is.True);
        }

        // -------------------------------------------------------------------
        // Requirement 14.4: Delete fires DeliveryDataChanged event
        // -------------------------------------------------------------------

        [Test]
        public void Delete_FiresDeliveryDataChangedEvent()
        {
            var route = new DeliveryRoute
            {
                UUID = Guid.NewGuid().ToString(),
                OwnerUUID = "test-player-uuid",
                Name = "DeleteEventTest",
            };
            playerContext.AddDeliveryRoute(route);

            bool eventFired = false;
            playerContext.DeliveryDataChanged += (s, e) => eventFired = true;

            service.Delete(route.UUID);

            Assert.That(eventFired, Is.True);
        }

        // -------------------------------------------------------------------
        // Requirement 12.4: Update replaces Stops list with correct Sequence numbering
        // -------------------------------------------------------------------

        [Test]
        public void Update_ReplacesStops_WithCorrectSequenceNumbering()
        {
            var route = new DeliveryRoute
            {
                UUID = Guid.NewGuid().ToString(),
                OwnerUUID = "test-player-uuid",
                Name = "StopsTest",
            };
            playerContext.AddDeliveryRoute(route);

            var request = new DeliveryRouteUpdateRequest
            {
                Name = "StopsTest",
                Stops = new List<RouteStop>
                {
                    new RouteStop
                    {
                        ColonyUUID = "colony-a",
                        Sequence = 99,
                        DestinationType = DestinationType.Colony,
                        DestinationUUID = "dest-a",
                        Purpose = RouteStopPurpose.Cargo,
                        FuelEstimate = 10.5m,
                    },
                    new RouteStop
                    {
                        ColonyUUID = "colony-b",
                        Sequence = 77,
                        DestinationType = DestinationType.Station,
                        DestinationUUID = "dest-b",
                        Purpose = RouteStopPurpose.Refuel,
                        FuelEstimate = 20.0m,
                    },
                    new RouteStop
                    {
                        ColonyUUID = "colony-c",
                        Sequence = 55,
                        DestinationType = DestinationType.Asteroid,
                        DestinationUUID = "dest-c",
                        Purpose = RouteStopPurpose.CargoAndRefuel,
                        FuelEstimate = 30.0m,
                    },
                },
            };

            var result = service.Update(route.UUID, request);

            Assert.That(result.Stops.Count, Is.EqualTo(3));
            Assert.That(result.Stops[0].Sequence, Is.EqualTo(1));
            Assert.That(result.Stops[1].Sequence, Is.EqualTo(2));
            Assert.That(result.Stops[2].Sequence, Is.EqualTo(3));
            Assert.That(result.Stops[0].ColonyUUID, Is.EqualTo("colony-a"));
            Assert.That(result.Stops[1].ColonyUUID, Is.EqualTo("colony-b"));
            Assert.That(result.Stops[2].ColonyUUID, Is.EqualTo("colony-c"));
        }

        // -------------------------------------------------------------------
        // Requirement 13.4: Create populates Stops list from request
        // -------------------------------------------------------------------

        [Test]
        public void Create_PopulatesStopsFromRequest()
        {
            var request = new DeliveryRouteCreateRequest
            {
                Name = "StopsCreateTest",
                Stops = new List<RouteStop>
                {
                    new RouteStop
                    {
                        ColonyUUID = "colony-x",
                        Sequence = 42,
                        DestinationType = DestinationType.Colony,
                        DestinationUUID = "dest-x",
                        Purpose = RouteStopPurpose.Cargo,
                        FuelEstimate = 5.0m,
                    },
                    new RouteStop
                    {
                        ColonyUUID = "colony-y",
                        Sequence = 43,
                        DestinationType = DestinationType.Station,
                        DestinationUUID = "dest-y",
                        Purpose = RouteStopPurpose.Refuel,
                        FuelEstimate = 15.0m,
                    },
                },
            };

            var result = service.Create(request);

            Assert.That(result.Stops.Count, Is.EqualTo(2));
            Assert.That(result.Stops[0].ColonyUUID, Is.EqualTo("colony-x"));
            Assert.That(result.Stops[0].DestinationUUID, Is.EqualTo("dest-x"));
            Assert.That(result.Stops[0].Sequence, Is.EqualTo(1));
            Assert.That(result.Stops[1].ColonyUUID, Is.EqualTo("colony-y"));
            Assert.That(result.Stops[1].DestinationUUID, Is.EqualTo("dest-y"));
            Assert.That(result.Stops[1].Sequence, Is.EqualTo(2));
        }
    }
}