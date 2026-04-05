using NUnit.Framework;
using OE2EmpireTracker.Baseline;
using OE2EmpireTracker.ViewModels;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OE2EmpireTracker.Tests.Baseline
{
    [TestFixture]
    public class DeliveryRouteViewModelTests
    {
        private PlayerContext playerContext;

        [SetUp]
        public void SetUp()
        {
            PlayerContext.Reset();
            EmpireContext.FilePath = Path.Combine(TestContext.CurrentContext.TestDirectory, @"..\..\..\..\OE2EmpireTracker\BaselineData.json");
            PlayerContext.FilePath = "nonexistent_player_data.json";
            playerContext = PlayerContext.getInstance();
        }

        private DeliveryRouteViewModel CreateViewModel()
        {
            var route = new DeliveryRoute { UUID = "r1", Name = "Test Route", OwnerUUID = "p1" };
            return new DeliveryRouteViewModel(route, playerContext);
        }

        // -----------------------------------------------------------------------
        // AddStop
        // -----------------------------------------------------------------------

        [Test]
        public void AddStop_AddsStopWithCorrectSequence()
        {
            var vm = CreateViewModel();
            vm.AddStop("colony-1");
            Assert.AreEqual(1, vm.Stops.Count);
            Assert.AreEqual("colony-1", vm.Stops[0].ColonyUUID);
            Assert.AreEqual(0, vm.Stops[0].Sequence);
        }

        [Test]
        public void AddStop_MultipleStops_SequentialNumbers()
        {
            var vm = CreateViewModel();
            vm.AddStop("c1");
            vm.AddStop("c2");
            vm.AddStop("c3");
            Assert.AreEqual(3, vm.Stops.Count);
            Assert.AreEqual(0, vm.Stops[0].Sequence);
            Assert.AreEqual(1, vm.Stops[1].Sequence);
            Assert.AreEqual(2, vm.Stops[2].Sequence);
        }

        // -----------------------------------------------------------------------
        // RemoveStop
        // -----------------------------------------------------------------------

        [Test]
        public void RemoveStop_ValidIndex_RemovesAndRenumbers()
        {
            var vm = CreateViewModel();
            vm.AddStop("c1");
            vm.AddStop("c2");
            vm.AddStop("c3");
            vm.RemoveStop(1);
            Assert.AreEqual(2, vm.Stops.Count);
            Assert.AreEqual("c1", vm.Stops[0].ColonyUUID);
            Assert.AreEqual("c3", vm.Stops[1].ColonyUUID);
            Assert.AreEqual(0, vm.Stops[0].Sequence);
            Assert.AreEqual(1, vm.Stops[1].Sequence);
        }

        [Test]
        public void RemoveStop_InvalidIndex_NoChange()
        {
            var vm = CreateViewModel();
            vm.AddStop("c1");
            vm.RemoveStop(5);
            Assert.AreEqual(1, vm.Stops.Count);
        }

        [Test]
        public void RemoveStop_NegativeIndex_NoChange()
        {
            var vm = CreateViewModel();
            vm.AddStop("c1");
            vm.RemoveStop(-1);
            Assert.AreEqual(1, vm.Stops.Count);
        }

        [Test]
        public void RemoveStops_MultipleIndices_RemovesCorrectly()
        {
            var vm = CreateViewModel();
            vm.AddStop("c1");
            vm.AddStop("c2");
            vm.AddStop("c3");
            vm.AddStop("c4");
            vm.RemoveStops(new[] { 1, 3 });
            Assert.AreEqual(2, vm.Stops.Count);
            Assert.AreEqual("c1", vm.Stops[0].ColonyUUID);
            Assert.AreEqual("c3", vm.Stops[1].ColonyUUID);
        }

        // -----------------------------------------------------------------------
        // MoveStopUp / MoveStopDown
        // -----------------------------------------------------------------------

        [Test]
        public void MoveStopUp_MiddleStop_MovesUp()
        {
            var vm = CreateViewModel();
            vm.AddStop("c1");
            vm.AddStop("c2");
            vm.AddStop("c3");
            vm.MoveStopUp(2);
            Assert.AreEqual("c1", vm.Stops[0].ColonyUUID);
            Assert.AreEqual("c3", vm.Stops[1].ColonyUUID);
            Assert.AreEqual("c2", vm.Stops[2].ColonyUUID);
        }

        [Test]
        public void MoveStopUp_FirstStop_NoChange()
        {
            var vm = CreateViewModel();
            vm.AddStop("c1");
            vm.AddStop("c2");
            vm.MoveStopUp(0);
            Assert.AreEqual("c1", vm.Stops[0].ColonyUUID);
            Assert.AreEqual("c2", vm.Stops[1].ColonyUUID);
        }

        [Test]
        public void MoveStopDown_MiddleStop_MovesDown()
        {
            var vm = CreateViewModel();
            vm.AddStop("c1");
            vm.AddStop("c2");
            vm.AddStop("c3");
            vm.MoveStopDown(0);
            Assert.AreEqual("c2", vm.Stops[0].ColonyUUID);
            Assert.AreEqual("c1", vm.Stops[1].ColonyUUID);
            Assert.AreEqual("c3", vm.Stops[2].ColonyUUID);
        }

        [Test]
        public void MoveStopDown_LastStop_NoChange()
        {
            var vm = CreateViewModel();
            vm.AddStop("c1");
            vm.AddStop("c2");
            vm.MoveStopDown(1);
            Assert.AreEqual("c1", vm.Stops[0].ColonyUUID);
            Assert.AreEqual("c2", vm.Stops[1].ColonyUUID);
        }

        // -----------------------------------------------------------------------
        // Multi-Select Move
        // -----------------------------------------------------------------------

        [Test]
        public void MoveStopsUp_TwoSelected_BothMoveUp()
        {
            var vm = CreateViewModel();
            vm.AddStop("c1");
            vm.AddStop("c2");
            vm.AddStop("c3");
            vm.AddStop("c4");
            var newIndices = vm.MoveStopsUp(new[] { 2, 3 });
            Assert.AreEqual("c1", vm.Stops[0].ColonyUUID);
            Assert.AreEqual("c3", vm.Stops[1].ColonyUUID);
            Assert.AreEqual("c4", vm.Stops[2].ColonyUUID);
            Assert.AreEqual("c2", vm.Stops[3].ColonyUUID);
            Assert.Contains(1, newIndices);
            Assert.Contains(2, newIndices);
        }

        [Test]
        public void MoveStopsDown_TwoSelected_BothMoveDown()
        {
            var vm = CreateViewModel();
            vm.AddStop("c1");
            vm.AddStop("c2");
            vm.AddStop("c3");
            vm.AddStop("c4");
            var newIndices = vm.MoveStopsDown(new[] { 0, 1 });
            Assert.AreEqual("c3", vm.Stops[0].ColonyUUID);
            Assert.AreEqual("c1", vm.Stops[1].ColonyUUID);
            Assert.AreEqual("c2", vm.Stops[2].ColonyUUID);
            Assert.AreEqual("c4", vm.Stops[3].ColonyUUID);
            Assert.Contains(1, newIndices);
            Assert.Contains(2, newIndices);
        }

        // -----------------------------------------------------------------------
        // Reset / SelectRoute
        // -----------------------------------------------------------------------

        [Test]
        public void Reset_ClearsRoute()
        {
            var vm = CreateViewModel();
            vm.AddStop("c1");
            vm.Reset();
            Assert.AreEqual(0, vm.Stops.Count);
            Assert.AreEqual(string.Empty, vm.Name);
        }

        [Test]
        public void SelectRoute_SwitchesToNewRoute()
        {
            var vm = CreateViewModel();
            vm.AddStop("c1");
            var newRoute = new DeliveryRoute { UUID = "r2", Name = "New Route" };
            newRoute.Stops.Add(new RouteStop { ColonyUUID = "c9", Sequence = 0 });
            vm.SelectRoute(newRoute);
            Assert.AreEqual("New Route", vm.Name);
            Assert.AreEqual(1, vm.Stops.Count);
            Assert.AreEqual("c9", vm.Stops[0].ColonyUUID);
        }

        [Test]
        public void SelectRoute_Null_CreatesEmptyRoute()
        {
            var vm = CreateViewModel();
            vm.SelectRoute(null);
            Assert.AreEqual(0, vm.Stops.Count);
        }

        // -----------------------------------------------------------------------
        // Renumbering
        // -----------------------------------------------------------------------

        [Test]
        public void AllOperations_MaintainSequentialNumbering()
        {
            var vm = CreateViewModel();
            vm.AddStop("c1");
            vm.AddStop("c2");
            vm.AddStop("c3");
            vm.AddStop("c4");
            vm.MoveStopUp(3);
            vm.RemoveStop(0);
            for (int i = 0; i < vm.Stops.Count; i++)
                Assert.AreEqual(i, vm.Stops[i].Sequence, $"Stop at index {i} has wrong sequence");
        }
    }
}
