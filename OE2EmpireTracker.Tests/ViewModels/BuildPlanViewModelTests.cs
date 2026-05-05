using System;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.ViewModels;

namespace OE2EmpireTracker.Tests.ViewModels
{
    /// <summary>
    /// Unit tests for BuildPlanViewModel.
    /// Feature: bl-118-buildplan-readonly
    /// </summary>
    [TestFixture]
    public class BuildPlanViewModelTests
    {
        [Test]
        public void Reset_ClearsAllFieldsToDefaults()
        {
            var vm = new BuildPlanViewModel();
            vm.Name = "test";
            vm.Description = "desc";
            vm.IsActive = false;
            vm.Reset();
            Assert.That(vm.Name, Is.EqualTo(string.Empty));
            Assert.That(vm.Description, Is.EqualTo(string.Empty));
            Assert.That(vm.IsActive, Is.True);
            Assert.That(vm.Items, Is.Empty);
        }

        [Test]
        public void IsNew_TrueAfterReset()
        {
            var vm = new BuildPlanViewModel();
            vm.Reset();
            Assert.That(vm.IsNew, Is.True);
        }

        [Test]
        public void IsNew_FalseAfterLoadFrom()
        {
            var plan = new BuildPlan { UUID = "u1", Name = "P", OwnerUUID = "o1" };
            var vm = new BuildPlanViewModel();
            vm.LoadFrom(new ReadOnlyBuildPlan(plan));
            Assert.That(vm.IsNew, Is.False);
        }

        [Test]
        public void IsDirty_TrueForNewPlanWithNonEmptyName()
        {
            var vm = new BuildPlanViewModel();
            vm.Reset();
            vm.Name = "New Plan";
            Assert.That(vm.IsDirty, Is.True);
        }

        [Test]
        public void BuildUpdateRequest_CopiesAllFields()
        {
            var plan = new BuildPlan { UUID = "u1", Name = "P", OwnerUUID = "o1", Description = "D", IsActive = true };
            var vm = new BuildPlanViewModel();
            vm.LoadFrom(new ReadOnlyBuildPlan(plan));
            vm.Name = "Updated";
            var req = vm.BuildUpdateRequest();
            Assert.That(req.Name, Is.EqualTo("Updated"));
            Assert.That(req.Description, Is.EqualTo("D"));
            Assert.That(req.IsActive, Is.True);
        }

        [Test]
        public void BuildCreateRequest_CopiesAllFields()
        {
            var vm = new BuildPlanViewModel();
            vm.Reset();
            vm.Name = "New";
            vm.Description = "Desc";
            vm.IsActive = false;
            var req = vm.BuildCreateRequest();
            Assert.That(req.Name, Is.EqualTo("New"));
            Assert.That(req.Description, Is.EqualTo("Desc"));
            Assert.That(req.IsActive, Is.False);
        }

        [Test]
        public void UUID_And_OwnerUUID_PreservedFromLoadFrom()
        {
            var plan = new BuildPlan { UUID = "u1", OwnerUUID = "o1", Name = "P" };
            var vm = new BuildPlanViewModel();
            vm.LoadFrom(new ReadOnlyBuildPlan(plan));
            Assert.That(vm.UUID, Is.EqualTo("u1"));
            Assert.That(vm.OwnerUUID, Is.EqualTo("o1"));
        }

        [Test]
        public void AddItem_IncreasesCount()
        {
            var vm = new BuildPlanViewModel();
            vm.Reset();
            vm.AddItem(new BuildItem { UUID = "i1", ItemName = "Item1" });
            Assert.That(vm.Items.Count, Is.EqualTo(1));
        }

        [Test]
        public void RemoveItem_DecreasesCount()
        {
            var vm = new BuildPlanViewModel();
            vm.Reset();
            vm.AddItem(new BuildItem { UUID = "i1", ItemName = "Item1" });
            vm.RemoveItem("i1");
            Assert.That(vm.Items.Count, Is.EqualTo(0));
        }

        [Test]
        public void FindItem_ReturnsCorrectItem()
        {
            var vm = new BuildPlanViewModel();
            vm.Reset();
            vm.AddItem(new BuildItem { UUID = "i1", ItemName = "Item1" });
            vm.AddItem(new BuildItem { UUID = "i2", ItemName = "Item2" });
            var found = vm.FindItem("i2");
            Assert.That(found, Is.Not.Null);
            Assert.That(found.ItemName, Is.EqualTo("Item2"));
        }
    }
}
