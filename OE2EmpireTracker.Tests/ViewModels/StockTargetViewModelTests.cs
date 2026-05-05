using System;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.ViewModels;

namespace OE2EmpireTracker.Tests.ViewModels
{
    [TestFixture]
    public class StockTargetViewModelTests
    {
        [Test]
        public void IsNew_TrueAfterReset()
        {
            var vm = new StockTargetViewModel();
            vm.ResetPlan();
            Assert.That(vm.IsPlanNew, Is.True);
        }

        [Test]
        public void IsNew_FalseAfterLoadFrom()
        {
            var plan = new StockPlan { UUID = "p1", OwnerUUID = "o1", Name = "Test" };
            var vm = new StockTargetViewModel();
            vm.LoadPlanFrom(new ReadOnlyStockPlan(plan));
            Assert.That(vm.IsPlanNew, Is.False);
        }

        [Test]
        public void AddTarget_IncreasesCount()
        {
            var vm = new StockTargetViewModel();
            vm.AddTarget(new StockTarget { UUID = "t1", ItemName = "Item" });
            Assert.That(vm.Targets.Count, Is.EqualTo(1));
        }

        [Test]
        public void RemoveTarget_DecreasesCount()
        {
            var vm = new StockTargetViewModel();
            vm.AddTarget(new StockTarget { UUID = "t1", ItemName = "Item" });
            vm.RemoveTarget("t1");
            Assert.That(vm.Targets.Count, Is.EqualTo(0));
        }

        [Test]
        public void BuildPlanCreateRequest_CopiesAllFields()
        {
            var vm = new StockTargetViewModel();
            vm.PlanName = "MyPlan";
            vm.PlanIsActive = false;
            vm.ReplenishmentBuildPlanUUID = "bp1";
            var req = vm.BuildPlanCreateRequest();
            Assert.That(req.Name, Is.EqualTo("MyPlan"));
            Assert.That(req.IsActive, Is.False);
            Assert.That(req.ReplenishmentBuildPlanUUID, Is.EqualTo("bp1"));
        }

        [Test]
        public void BuildPlanUpdateRequest_CopiesAllFields()
        {
            var plan = new StockPlan { UUID = "p1", OwnerUUID = "o1", Name = "Old" };
            var vm = new StockTargetViewModel();
            vm.LoadPlanFrom(new ReadOnlyStockPlan(plan));
            vm.PlanName = "New";
            var req = vm.BuildPlanUpdateRequest();
            Assert.That(req.Name, Is.EqualTo("New"));
            Assert.That(req.Original, Is.Not.Null);
        }

        [Test]
        public void ProfileIsNew_TrueAfterReset()
        {
            var vm = new StockTargetViewModel();
            vm.ResetProfile();
            Assert.That(vm.IsProfileNew, Is.True);
        }

        [Test]
        public void AddEntry_IncreasesCount()
        {
            var vm = new StockTargetViewModel();
            vm.AddEntry(new StockProfileEntry { GroupID = "A", StockPlanUUID = "p1" });
            Assert.That(vm.Entries.Count, Is.EqualTo(1));
        }

        [Test]
        public void RemoveEntry_DecreasesCount()
        {
            var entry = new StockProfileEntry { GroupID = "A", StockPlanUUID = "p1" };
            var vm = new StockTargetViewModel();
            vm.AddEntry(entry);
            vm.RemoveEntry(entry);
            Assert.That(vm.Entries.Count, Is.EqualTo(0));
        }
    }
}
