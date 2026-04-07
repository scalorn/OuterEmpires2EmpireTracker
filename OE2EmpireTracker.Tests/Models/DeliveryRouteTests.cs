using Newtonsoft.Json;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using System.Collections.Generic;

namespace OE2EmpireTracker.Tests.Models
{
    [TestFixture]
    public class DeliveryRouteTests
    {
        // -----------------------------------------------------------------------
        // Default Constructor
        // -----------------------------------------------------------------------

        [Test]
        public void DefaultConstructor_StopsIsNotNull()
        {
            var route = new DeliveryRoute();
            Assert.That(route.Stops, Is.Not.Null);
        }

        [Test]
        public void DefaultConstructor_StopsIsEmpty()
        {
            var route = new DeliveryRoute();
            Assert.That(route.Stops.Count, Is.EqualTo(0));
        }

        [Test]
        public void DefaultConstructor_NameIsEmpty()
        {
            var route = new DeliveryRoute();
            Assert.That(route.Name, Is.EqualTo(string.Empty));
        }

        [Test]
        public void DefaultConstructor_OwnerUUIDIsEmpty()
        {
            var route = new DeliveryRoute();
            Assert.That(route.OwnerUUID, Is.EqualTo(string.Empty));
        }

        // -----------------------------------------------------------------------
        // Properties
        // -----------------------------------------------------------------------

        [Test]
        public void Properties_CanBeSetAndRead()
        {
            var route = new DeliveryRoute
            {
                UUID = "route-1",
                Name = "Main Route",
                OwnerUUID = "player-1"
            };
            Assert.That(route.UUID, Is.EqualTo("route-1"));
            Assert.That(route.Name, Is.EqualTo("Main Route"));
            Assert.That(route.OwnerUUID, Is.EqualTo("player-1"));
        }

        // -----------------------------------------------------------------------
        // RouteStop
        // -----------------------------------------------------------------------

        [Test]
        public void RouteStop_PropertiesCanBeSetAndRead()
        {
            var stop = new RouteStop { ColonyUUID = "colony-1", Sequence = 3 };
            Assert.That(stop.ColonyUUID, Is.EqualTo("colony-1"));
            Assert.That(stop.Sequence, Is.EqualTo(3));
        }

        // -----------------------------------------------------------------------
        // JSON Round-Trip
        // -----------------------------------------------------------------------

        [Test]
        public void JsonRoundTrip_EmptyRoute_Preserved()
        {
            var route = new DeliveryRoute { UUID = "r1", Name = "Test", OwnerUUID = "p1" };
            string json = JsonConvert.SerializeObject(route);
            var restored = JsonConvert.DeserializeObject<DeliveryRoute>(json);
            Assert.That(restored.UUID, Is.EqualTo("r1"));
            Assert.That(restored.Name, Is.EqualTo("Test"));
            Assert.That(restored.OwnerUUID, Is.EqualTo("p1"));
            Assert.That(restored.Stops.Count, Is.EqualTo(0));
        }

        [Test]
        public void JsonRoundTrip_WithStops_Preserved()
        {
            var route = new DeliveryRoute
            {
                UUID = "r1",
                Name = "Route A",
                OwnerUUID = "p1",
                Stops = new List<RouteStop>
                {
                    new RouteStop { ColonyUUID = "c1", Sequence = 0 },
                    new RouteStop { ColonyUUID = "c2", Sequence = 1 }
                }
            };
            string json = JsonConvert.SerializeObject(route);
            var restored = JsonConvert.DeserializeObject<DeliveryRoute>(json);
            Assert.That(restored.Stops.Count, Is.EqualTo(2));
            Assert.That(restored.Stops[0].ColonyUUID, Is.EqualTo("c1"));
            Assert.That(restored.Stops[0].Sequence, Is.EqualTo(0));
            Assert.That(restored.Stops[1].ColonyUUID, Is.EqualTo("c2"));
            Assert.That(restored.Stops[1].Sequence, Is.EqualTo(1));
        }
    }
}
