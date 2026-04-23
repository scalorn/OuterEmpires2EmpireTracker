using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;
using OE2EmpireTracker.Tests;
using OE2EmpireTracker.ViewModels;

namespace OE2EmpireTracker.Tests.Services
{
    [TestFixture]
    public class DeliveryRouteViewModelTests
    {
        private PlayerContext playerContext;

        [SetUp]
        public void SetUp()
        {
            PlayerContext.Reset();
            TestHelper.SetEmpireFilePath();
            PlayerContext.FilePath = "nonexistent_player_data.json";
            playerContext = PlayerContext.GetInstance();
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
            Assert.That(vm.Stops.Count, Is.EqualTo(1));
            Assert.That(vm.Stops[0].ColonyUUID, Is.EqualTo("colony-1"));
            Assert.That(vm.Stops[0].Sequence, Is.EqualTo(0));
        }

        [Test]
        public void AddStop_MultipleStops_SequentialNumbers()
        {
            var vm = CreateViewModel();
            vm.AddStop("c1");
            vm.AddStop("c2");
            vm.AddStop("c3");
            Assert.That(vm.Stops.Count, Is.EqualTo(3));
            Assert.That(vm.Stops[0].Sequence, Is.EqualTo(0));
            Assert.That(vm.Stops[1].Sequence, Is.EqualTo(1));
            Assert.That(vm.Stops[2].Sequence, Is.EqualTo(2));
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
            Assert.That(vm.Stops.Count, Is.EqualTo(2));
            Assert.That(vm.Stops[0].ColonyUUID, Is.EqualTo("c1"));
            Assert.That(vm.Stops[1].ColonyUUID, Is.EqualTo("c3"));
            Assert.That(vm.Stops[0].Sequence, Is.EqualTo(0));
            Assert.That(vm.Stops[1].Sequence, Is.EqualTo(1));
        }

        [Test]
        public void RemoveStop_InvalidIndex_NoChange()
        {
            var vm = CreateViewModel();
            vm.AddStop("c1");
            vm.RemoveStop(5);
            Assert.That(vm.Stops.Count, Is.EqualTo(1));
        }

        [Test]
        public void RemoveStop_NegativeIndex_NoChange()
        {
            var vm = CreateViewModel();
            vm.AddStop("c1");
            vm.RemoveStop(-1);
            Assert.That(vm.Stops.Count, Is.EqualTo(1));
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
            Assert.That(vm.Stops.Count, Is.EqualTo(2));
            Assert.That(vm.Stops[0].ColonyUUID, Is.EqualTo("c1"));
            Assert.That(vm.Stops[1].ColonyUUID, Is.EqualTo("c3"));
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
            Assert.That(vm.Stops[0].ColonyUUID, Is.EqualTo("c1"));
            Assert.That(vm.Stops[1].ColonyUUID, Is.EqualTo("c3"));
            Assert.That(vm.Stops[2].ColonyUUID, Is.EqualTo("c2"));
        }

        [Test]
        public void MoveStopUp_FirstStop_NoChange()
        {
            var vm = CreateViewModel();
            vm.AddStop("c1");
            vm.AddStop("c2");
            vm.MoveStopUp(0);
            Assert.That(vm.Stops[0].ColonyUUID, Is.EqualTo("c1"));
            Assert.That(vm.Stops[1].ColonyUUID, Is.EqualTo("c2"));
        }

        [Test]
        public void MoveStopDown_MiddleStop_MovesDown()
        {
            var vm = CreateViewModel();
            vm.AddStop("c1");
            vm.AddStop("c2");
            vm.AddStop("c3");
            vm.MoveStopDown(0);
            Assert.That(vm.Stops[0].ColonyUUID, Is.EqualTo("c2"));
            Assert.That(vm.Stops[1].ColonyUUID, Is.EqualTo("c1"));
            Assert.That(vm.Stops[2].ColonyUUID, Is.EqualTo("c3"));
        }

        [Test]
        public void MoveStopDown_LastStop_NoChange()
        {
            var vm = CreateViewModel();
            vm.AddStop("c1");
            vm.AddStop("c2");
            vm.MoveStopDown(1);
            Assert.That(vm.Stops[0].ColonyUUID, Is.EqualTo("c1"));
            Assert.That(vm.Stops[1].ColonyUUID, Is.EqualTo("c2"));
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
            Assert.That(vm.Stops[0].ColonyUUID, Is.EqualTo("c1"));
            Assert.That(vm.Stops[1].ColonyUUID, Is.EqualTo("c3"));
            Assert.That(vm.Stops[2].ColonyUUID, Is.EqualTo("c4"));
            Assert.That(vm.Stops[3].ColonyUUID, Is.EqualTo("c2"));
            Assert.That(newIndices, Does.Contain(1));
            Assert.That(newIndices, Does.Contain(2));
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
            Assert.That(vm.Stops[0].ColonyUUID, Is.EqualTo("c3"));
            Assert.That(vm.Stops[1].ColonyUUID, Is.EqualTo("c1"));
            Assert.That(vm.Stops[2].ColonyUUID, Is.EqualTo("c2"));
            Assert.That(vm.Stops[3].ColonyUUID, Is.EqualTo("c4"));
            Assert.That(newIndices, Does.Contain(1));
            Assert.That(newIndices, Does.Contain(2));
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
            Assert.That(vm.Stops.Count, Is.EqualTo(0));
            Assert.That(vm.Name, Is.EqualTo(string.Empty));
        }

        [Test]
        public void SelectRoute_SwitchesToNewRoute()
        {
            var vm = CreateViewModel();
            vm.AddStop("c1");
            var newRoute = new DeliveryRoute { UUID = "r2", Name = "New Route" };
            newRoute.Stops.Add(new RouteStop { ColonyUUID = "c9", Sequence = 0 });
            vm.SelectRoute(newRoute);
            Assert.That(vm.Name, Is.EqualTo("New Route"));
            Assert.That(vm.Stops.Count, Is.EqualTo(1));
            Assert.That(vm.Stops[0].ColonyUUID, Is.EqualTo("c9"));
        }

        [Test]
        public void SelectRoute_Null_CreatesEmptyRoute()
        {
            var vm = CreateViewModel();
            vm.SelectRoute(null);
            Assert.That(vm.Stops.Count, Is.EqualTo(0));
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
                Assert.That(
                    vm.Stops[i].Sequence,
                    Is.EqualTo(i),
                    $"Stop at index {i} has wrong sequence");
        }
    }
}
