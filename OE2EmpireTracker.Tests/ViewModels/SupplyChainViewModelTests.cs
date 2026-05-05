using System;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.ViewModels;

namespace OE2EmpireTracker.Tests.ViewModels
{
    /// <summary>
    /// Unit tests for SupplyChainViewModel.
    /// Feature: bl-120-supplychain-readonly
    /// </summary>
    [TestFixture]
    public class SupplyChainViewModelTests
    {
        [Test]
        public void Reset_ClearsAllFieldsToDefaults()
        {
            var vm = new SupplyChainViewModel();
            vm.Name = "test";
            vm.IsActive = false;
            vm.Reset();
            Assert.That(vm.Name, Is.EqualTo(string.Empty));
            Assert.That(vm.IsActive, Is.True);
            Assert.That(vm.Stages, Is.Empty);
        }

        [Test]
        public void IsNew_TrueAfterReset()
        {
            var vm = new SupplyChainViewModel();
            vm.Reset();
            Assert.That(vm.IsNew, Is.True);
        }

        [Test]
        public void IsNew_FalseAfterLoadFrom()
        {
            var chain = new SupplyChain { UUID = "u1", Name = "C", OwnerUUID = "o1" };
            var vm = new SupplyChainViewModel();
            vm.LoadFrom(new ReadOnlySupplyChain(chain));
            Assert.That(vm.IsNew, Is.False);
        }

        [Test]
        public void IsDirty_TrueForNewChainWithNonEmptyName()
        {
            var vm = new SupplyChainViewModel();
            vm.Reset();
            vm.Name = "New Chain";
            Assert.That(vm.IsDirty, Is.True);
        }

        [Test]
        public void BuildUpdateRequest_CopiesAllFields()
        {
            var chain = new SupplyChain { UUID = "u1", Name = "C", OwnerUUID = "o1", IsActive = true };
            var vm = new SupplyChainViewModel();
            vm.LoadFrom(new ReadOnlySupplyChain(chain));
            vm.Name = "Updated";
            var req = vm.BuildUpdateRequest();
            Assert.That(req.Name, Is.EqualTo("Updated"));
            Assert.That(req.IsActive, Is.True);
        }

        [Test]
        public void BuildCreateRequest_CopiesAllFields()
        {
            var vm = new SupplyChainViewModel();
            vm.Reset();
            vm.Name = "New";
            vm.IsActive = false;
            var req = vm.BuildCreateRequest();
            Assert.That(req.Name, Is.EqualTo("New"));
            Assert.That(req.IsActive, Is.False);
        }

        [Test]
        public void UUID_And_OwnerUUID_PreservedFromLoadFrom()
        {
            var chain = new SupplyChain { UUID = "u1", OwnerUUID = "o1", Name = "C" };
            var vm = new SupplyChainViewModel();
            vm.LoadFrom(new ReadOnlySupplyChain(chain));
            Assert.That(vm.UUID, Is.EqualTo("u1"));
            Assert.That(vm.OwnerUUID, Is.EqualTo("o1"));
        }

        [Test]
        public void AddStage_IncreasesCount()
        {
            var vm = new SupplyChainViewModel();
            vm.Reset();
            vm.AddStage(new SupplyChainStage { Sequence = 1, StageType = SupplyChainStageType.Mine });
            Assert.That(vm.Stages.Count, Is.EqualTo(1));
        }

        [Test]
        public void RemoveStage_DecreasesCount()
        {
            var vm = new SupplyChainViewModel();
            vm.Reset();
            vm.AddStage(new SupplyChainStage { Sequence = 1 });
            vm.RemoveStage(0);
            Assert.That(vm.Stages.Count, Is.EqualTo(0));
        }

        [Test]
        public void UpdateStage_ReplacesAtIndex()
        {
            var vm = new SupplyChainViewModel();
            vm.Reset();
            vm.AddStage(new SupplyChainStage { Sequence = 1, ResourceName = "Iron" });
            vm.UpdateStage(0, new SupplyChainStage { Sequence = 1, ResourceName = "Gold" });
            Assert.That(vm.Stages[0].ResourceName, Is.EqualTo("Gold"));
        }

        [Test]
        public void MoveStageUp_SwapsWithPrevious()
        {
            var vm = new SupplyChainViewModel();
            vm.Reset();
            vm.AddStage(new SupplyChainStage { Sequence = 1, ResourceName = "A" });
            vm.AddStage(new SupplyChainStage { Sequence = 2, ResourceName = "B" });
            vm.MoveStageUp(1);
            Assert.That(vm.Stages[0].ResourceName, Is.EqualTo("B"));
            Assert.That(vm.Stages[1].ResourceName, Is.EqualTo("A"));
        }

        [Test]
        public void MoveStageDown_SwapsWithNext()
        {
            var vm = new SupplyChainViewModel();
            vm.Reset();
            vm.AddStage(new SupplyChainStage { Sequence = 1, ResourceName = "A" });
            vm.AddStage(new SupplyChainStage { Sequence = 2, ResourceName = "B" });
            vm.MoveStageDown(0);
            Assert.That(vm.Stages[0].ResourceName, Is.EqualTo("B"));
            Assert.That(vm.Stages[1].ResourceName, Is.EqualTo("A"));
        }

        [Test]
        public void RenumberStages_AssignsSequentialNumbers()
        {
            var vm = new SupplyChainViewModel();
            vm.Reset();
            vm.AddStage(new SupplyChainStage { Sequence = 5 });
            vm.AddStage(new SupplyChainStage { Sequence = 10 });
            vm.AddStage(new SupplyChainStage { Sequence = 3 });
            vm.RenumberStages();
            Assert.That(vm.Stages[0].Sequence, Is.EqualTo(1));
            Assert.That(vm.Stages[1].Sequence, Is.EqualTo(2));
            Assert.That(vm.Stages[2].Sequence, Is.EqualTo(3));
        }
    }
}