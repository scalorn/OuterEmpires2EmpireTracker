using System;
using System.Collections.Generic;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Unit tests for SupplyChainMutationService.
    /// Feature: bl-120-supplychain-readonly
    /// </summary>
    [TestFixture]
    public class SupplyChainMutationServiceTests
    {
        private PlayerContext playerContext;
        private SupplyChainMutationService service;

        [SetUp]
        public void SetUp()
        {
            TestHelper.ResetWithCachedData();
            playerContext = PlayerContext.GetInstance();
            playerContext.CurrentPlayerUUID = "test-player";
            service = new SupplyChainMutationService(playerContext);
        }

        [Test]
        public void Update_NonExistentUUID_ThrowsInvalidOperationException()
        {
            var req = new SupplyChainUpdateRequest { Name = "X", IsActive = true, Stages = new List<SupplyChainStage>() };
            Assert.Throws<InvalidOperationException>(() => service.Update("nonexistent", req));
        }

        [Test]
        public void Delete_EmptyUUID_ReturnsWithoutError()
        {
            Assert.DoesNotThrow(() => service.Delete(string.Empty));
        }

        [Test]
        public void Delete_NonExistentUUID_ReturnsWithoutError()
        {
            Assert.DoesNotThrow(() => service.Delete("nonexistent"));
        }

        [Test]
        public void Create_AssignsNonEmptyUUID()
        {
            var req = new SupplyChainCreateRequest { Name = "Test", IsActive = true, Stages = new List<SupplyChainStage>() };
            var result = service.Create(req);
            Assert.That(result.UUID, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void Create_SetsOwnerUUID_ToCurrentPlayerUUID()
        {
            var req = new SupplyChainCreateRequest { Name = "Test", IsActive = true, Stages = new List<SupplyChainStage>() };
            var result = service.Create(req);
            Assert.That(result.OwnerUUID, Is.EqualTo("test-player"));
        }

        [Test]
        public void Update_FiresSupplyChainDataChangedEvent()
        {
            var chain = new SupplyChain { UUID = "sc1", OwnerUUID = "test-player", Name = "C" };
            playerContext.AddSupplyChain(chain);
            bool fired = false;
            playerContext.SupplyChainDataChanged += (s, e) => fired = true;
            var req = new SupplyChainUpdateRequest { Name = "Updated", IsActive = true, Stages = new List<SupplyChainStage>() };
            service.Update("sc1", req);
            Assert.That(fired, Is.True);
        }

        [Test]
        public void Create_FiresSupplyChainDataChangedEvent()
        {
            bool fired = false;
            playerContext.SupplyChainDataChanged += (s, e) => fired = true;
            var req = new SupplyChainCreateRequest { Name = "New", IsActive = true, Stages = new List<SupplyChainStage>() };
            service.Create(req);
            Assert.That(fired, Is.True);
        }

        [Test]
        public void Delete_FiresSupplyChainDataChangedEvent()
        {
            var chain = new SupplyChain { UUID = "sc1", OwnerUUID = "test-player", Name = "C" };
            playerContext.AddSupplyChain(chain);
            bool fired = false;
            playerContext.SupplyChainDataChanged += (s, e) => fired = true;
            service.Delete("sc1");
            Assert.That(fired, Is.True);
        }

        [Test]
        public void Update_ReplacesStagesList()
        {
            var chain = new SupplyChain { UUID = "sc1", OwnerUUID = "test-player", Name = "C" };
            chain.Stages.Add(new SupplyChainStage { Sequence = 1, ResourceName = "Old" });
            playerContext.AddSupplyChain(chain);
            var newStages = new List<SupplyChainStage> { new SupplyChainStage { Sequence = 1, ResourceName = "New" } };
            var req = new SupplyChainUpdateRequest { Name = "C", IsActive = true, Stages = newStages };
            var result = service.Update("sc1", req);
            Assert.That(result.Stages.Count, Is.EqualTo(1));
            Assert.That(result.Stages[0].ResourceName, Is.EqualTo("New"));
        }

        [Test]
        public void Create_PopulatesStagesFromRequest()
        {
            var stages = new List<SupplyChainStage> { new SupplyChainStage { Sequence = 1, ResourceName = "Iron" } };
            var req = new SupplyChainCreateRequest { Name = "C", IsActive = true, Stages = stages };
            var result = service.Create(req);
            Assert.That(result.Stages.Count, Is.EqualTo(1));
            Assert.That(result.Stages[0].ResourceName, Is.EqualTo("Iron"));
        }

        [Test]
        public void Update_RenumbersStages()
        {
            var chain = new SupplyChain { UUID = "sc1", OwnerUUID = "test-player", Name = "C" };
            playerContext.AddSupplyChain(chain);
            var stages = new List<SupplyChainStage>
            {
                new SupplyChainStage { Sequence = 5, ResourceName = "A" },
                new SupplyChainStage { Sequence = 10, ResourceName = "B" },
            };
            var req = new SupplyChainUpdateRequest { Name = "C", IsActive = true, Stages = stages };
            var result = service.Update("sc1", req);
            Assert.That(result.Stages[0].Sequence, Is.EqualTo(1));
            Assert.That(result.Stages[1].Sequence, Is.EqualTo(2));
        }
    }
}