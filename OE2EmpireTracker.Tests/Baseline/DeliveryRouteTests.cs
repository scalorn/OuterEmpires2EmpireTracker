using Newtonsoft.Json;
using NUnit.Framework;
using OE2EmpireTracker.Baseline;
using System.Collections.Generic;

namespace OE2EmpireTracker.Tests.Baseline
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
            Assert.IsNotNull(route.Stops);
        }

        [Test]
        public void DefaultConstructor_StopsIsEmpty()
        {
            var route = new DeliveryRoute();
            Assert.AreEqual(0, route.Stops.Count);
        }

        [Test]
        public void DefaultConstructor_NameIsEmpty()
        {
            var route = new DeliveryRoute();
            Assert.AreEqual(string.Empty, route.Name);
        }

        [Test]
        public void DefaultConstructor_OwnerUUIDIsEmpty()
        {
            var route = new DeliveryRoute();
            Assert.AreEqual(string.Empty, route.OwnerUUID);
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
            Assert.AreEqual("route-1", route.UUID);
            Assert.AreEqual("Main Route", route.Name);
            Assert.AreEqual("player-1", route.OwnerUUID);
        }

        // -----------------------------------------------------------------------
        // RouteStop
        // -----------------------------------------------------------------------

        [Test]
        public void RouteStop_PropertiesCanBeSetAndRead()
        {
            var stop = new RouteStop { ColonyUUID = "colony-1", Sequence = 3 };
            Assert.AreEqual("colony-1", stop.ColonyUUID);
            Assert.AreEqual(3, stop.Sequence);
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
            Assert.AreEqual("r1", restored.UUID);
            Assert.AreEqual("Test", restored.Name);
            Assert.AreEqual("p1", restored.OwnerUUID);
            Assert.AreEqual(0, restored.Stops.Count);
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
            Assert.AreEqual(2, restored.Stops.Count);
            Assert.AreEqual("c1", restored.Stops[0].ColonyUUID);
            Assert.AreEqual(0, restored.Stops[0].Sequence);
            Assert.AreEqual("c2", restored.Stops[1].ColonyUUID);
            Assert.AreEqual(1, restored.Stops[1].Sequence);
        }
    }
}
