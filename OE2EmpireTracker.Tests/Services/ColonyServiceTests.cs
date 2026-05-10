using System;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Unit tests for ColonyService edge cases and event firing.
    /// Feature: bl-109-colony-readonly
    /// Validates: Requirements 13.7, 13.8, 14.2, 14.3, 14.7, 14.8, 15.4, 15.5,
    ///            17.3, 17.4, 18.3, 19.3, 20.3, 21.3, 22.3, 23.3, 24.3
    /// </summary>
    [TestFixture]
    public class ColonyServiceTests
    {
        private PlayerContext playerContext;
        private ColonyService service;

        [SetUp]
        public void SetUp()
        {
            TestHelper.ResetWithCachedData();
            playerContext = PlayerContext.GetInstance();
            playerContext.CurrentPlayerUUID = "test-player-uuid";
            service = new ColonyService(playerContext);
        }

        [TearDown]
        public void TearDown()
        {
            EmpireContext.Reset();
        }

        // -------------------------------------------------------------------
        // Requirement 13.8: Update throws InvalidOperationException on unknown UUID
        // -------------------------------------------------------------------

        [Test]
        public void Update_NonExistentUUID_ThrowsInvalidOperationException()
        {
            var request = new ColonyUpdateRequest
            {
                PlanetName = "Test",
                ColonyName = "TestColony",
                SystemName = "TestSystem",
            };

            Assert.Throws<InvalidOperationException>(
                () => service.Update("nonexistent-uuid", request));
        }

        // -------------------------------------------------------------------
        // Requirement 15.5: Delete with empty UUID returns without error
        // -------------------------------------------------------------------

        [Test]
        public void Delete_EmptyUUID_ReturnsWithoutError()
        {
            Assert.DoesNotThrow(() => service.Delete(string.Empty));
        }

        // -------------------------------------------------------------------
        // Requirement 15.5: Delete with non-existent UUID returns without error
        // -------------------------------------------------------------------

        [Test]
        public void Delete_NonExistentUUID_ReturnsWithoutError()
        {
            Assert.DoesNotThrow(() => service.Delete("nonexistent-uuid"));
        }

        // -------------------------------------------------------------------
        // Requirement 14.2: Create assigns non-empty UUID
        // -------------------------------------------------------------------

        [Test]
        public void Create_AssignsNonEmptyUUID()
        {
            var request = new ColonyCreateRequest
            {
                PlanetName = "NewPlanet",
                ColonyName = "NewColony",
                SystemName = "NewSystem",
            };

            var result = service.Create(request);

            Assert.That(result.UUID, Is.Not.Null.And.Not.Empty);
        }

        // -------------------------------------------------------------------
        // Requirement 14.3: Create sets OwnerUUID to current player UUID
        // -------------------------------------------------------------------

        [Test]
        public void Create_SetsOwnerUUID_ToCurrentPlayerUUID()
        {
            var request = new ColonyCreateRequest
            {
                PlanetName = "OwnerTest",
                ColonyName = "OwnerColony",
                SystemName = "OwnerSystem",
            };

            var result = service.Create(request);

            Assert.That(result.OwnerUUID, Is.EqualTo("test-player-uuid"));
        }

        // -------------------------------------------------------------------
        // Requirement 13.7: Update fires ColonyDataChanged event
        // -------------------------------------------------------------------

        [Test]
        public void Update_FiresColonyDataChangedEvent()
        {
            var colony = new Colony
            {
                UUID = Guid.NewGuid().ToString(),
                OwnerUUID = "test-player-uuid",
                PlanetName = "EventTest",
                ColonyName = "EventColony",
                SystemName = "EventSystem",
            };
            playerContext.AddColony(colony);

            bool eventFired = false;
            playerContext.ColonyDataChanged += (s, e) => eventFired = true;

            var request = new ColonyUpdateRequest
            {
                PlanetName = "Updated",
                ColonyName = "UpdatedColony",
                SystemName = "UpdatedSystem",
            };
            service.Update(colony.UUID, request);

            Assert.That(eventFired, Is.True);
        }

        // -------------------------------------------------------------------
        // Requirement 14.7: Create fires ColonyDataChanged event
        // -------------------------------------------------------------------

        [Test]
        public void Create_FiresColonyDataChangedEvent()
        {
            bool eventFired = false;
            playerContext.ColonyDataChanged += (s, e) => eventFired = true;

            var request = new ColonyCreateRequest
            {
                PlanetName = "NewPlan",
                ColonyName = "NewCol",
                SystemName = "NewSys",
            };
            service.Create(request);

            Assert.That(eventFired, Is.True);
        }

        // -------------------------------------------------------------------
        // Requirement 15.4: Delete fires ColonyDataChanged event
        // -------------------------------------------------------------------

        [Test]
        public void Delete_FiresColonyDataChangedEvent()
        {
            var colony = new Colony
            {
                UUID = Guid.NewGuid().ToString(),
                OwnerUUID = "test-player-uuid",
                PlanetName = "DeleteEventTest",
                ColonyName = "DeleteColony",
                SystemName = "DeleteSystem",
            };
            playerContext.AddColony(colony);

            bool eventFired = false;
            playerContext.ColonyDataChanged += (s, e) => eventFired = true;

            service.Delete(colony.UUID);

            Assert.That(eventFired, Is.True);
        }

        // -------------------------------------------------------------------
        // Requirement 17.3: AddStructure creates structure with correct flatpack UUID
        // -------------------------------------------------------------------

        [Test]
        public void AddStructure_CreatesStructureWithCorrectFlatpackUUID()
        {
            var colony = new Colony
            {
                UUID = Guid.NewGuid().ToString(),
                OwnerUUID = "test-player-uuid",
                PlanetName = "StructTest",
                ColonyName = "StructColony",
                SystemName = "StructSystem",
            };
            playerContext.AddColony(colony);

            string flatpackUUID = "flatpack-bp-001";
            service.AddStructure(colony.UUID, flatpackUUID);

            Assert.That(colony.Structures.Count, Is.EqualTo(1));
            Assert.That(colony.Structures[0].FlatpackBlueprintUUID, Is.EqualTo(flatpackUUID));
            Assert.That(colony.Structures[0].UUID, Is.Not.Null.And.Not.Empty);
        }

        // -------------------------------------------------------------------
        // Requirement 17.4: AddStructure assigns next DisplaySequence for type
        // -------------------------------------------------------------------

        [Test]
        public void AddStructure_AssignsNextDisplaySequenceForType()
        {
            var colony = new Colony
            {
                UUID = Guid.NewGuid().ToString(),
                OwnerUUID = "test-player-uuid",
                PlanetName = "SeqTest",
                ColonyName = "SeqColony",
                SystemName = "SeqSystem",
            };
            playerContext.AddColony(colony);

            string flatpackUUID = "flatpack-bp-002";
            service.AddStructure(colony.UUID, flatpackUUID);
            service.AddStructure(colony.UUID, flatpackUUID);
            service.AddStructure(colony.UUID, "flatpack-bp-other");

            // Look up by flatpack UUID  WriteContext sorts structures by UUID,
            // so insertion order is not preserved in the list.
            var sameType = colony.Structures
                .Where(s => s.FlatpackBlueprintUUID == flatpackUUID)
                .OrderBy(s => s.DisplaySequence)
                .ToList();
            var otherType = colony.Structures
                .Where(s => s.FlatpackBlueprintUUID == "flatpack-bp-other")
                .ToList();

            Assert.That(sameType.Count, Is.EqualTo(2));
            Assert.That(sameType[0].DisplaySequence, Is.EqualTo(1));
            Assert.That(sameType[1].DisplaySequence, Is.EqualTo(2));
            Assert.That(otherType.Count, Is.EqualTo(1));
            Assert.That(otherType[0].DisplaySequence, Is.EqualTo(1));
        }

        // -------------------------------------------------------------------
        // Requirement 18.3: RemoveStructure removes correct structure by UUID
        // -------------------------------------------------------------------

        [Test]
        public void RemoveStructure_RemovesCorrectStructureByUUID()
        {
            var colony = new Colony
            {
                UUID = Guid.NewGuid().ToString(),
                OwnerUUID = "test-player-uuid",
                PlanetName = "RemoveTest",
                ColonyName = "RemoveColony",
                SystemName = "RemoveSystem",
            };
            playerContext.AddColony(colony);

            service.AddStructure(colony.UUID, "flatpack-a");
            service.AddStructure(colony.UUID, "flatpack-b");

            Assert.That(colony.Structures.Count, Is.EqualTo(2), "Should have 2 structures before remove");

            // Find the structure with flatpack-a by its FlatpackBlueprintUUID
            var structureA = colony.Structures.First(s => s.FlatpackBlueprintUUID == "flatpack-a");
            service.RemoveStructure(colony.UUID, structureA.UUID);

            Assert.That(colony.Structures.Count, Is.EqualTo(1));
            Assert.That(colony.Structures[0].FlatpackBlueprintUUID, Is.EqualTo("flatpack-b"));
        }

        // -------------------------------------------------------------------
        // Requirement 19.3: AddItem adds item to colony ItemBag
        // -------------------------------------------------------------------

        [Test]
        public void AddItem_AddsItemToColonyItemBag()
        {
            var colony = new Colony
            {
                UUID = Guid.NewGuid().ToString(),
                OwnerUUID = "test-player-uuid",
                PlanetName = "ItemTest",
                ColonyName = "ItemColony",
                SystemName = "ItemSystem",
            };
            playerContext.AddColony(colony);

            var item = new Item
            {
                UUID = Guid.NewGuid().ToString(),
                ItemType = ItemType.ItemTypeEnum.Resource,
                Name = "TestResource",
                Quantity = 100,
            };
            service.AddItem(colony.UUID, item);

            Assert.That(colony.Items.ContainsKey(item.UUID), Is.True);
        }

        // -------------------------------------------------------------------
        // Requirement 20.3: RemoveItem removes item from colony ItemBag
        // -------------------------------------------------------------------

        [Test]
        public void RemoveItem_RemovesItemFromColonyItemBag()
        {
            var colony = new Colony
            {
                UUID = Guid.NewGuid().ToString(),
                OwnerUUID = "test-player-uuid",
                PlanetName = "RemItemTest",
                ColonyName = "RemItemColony",
                SystemName = "RemItemSystem",
            };
            playerContext.AddColony(colony);

            var item = new Item
            {
                UUID = Guid.NewGuid().ToString(),
                ItemType = ItemType.ItemTypeEnum.Resource,
                Name = "TestResource",
                Quantity = 50,
            };
            service.AddItem(colony.UUID, item);
            service.RemoveItem(colony.UUID, item.UUID);

            Assert.That(colony.Items.ContainsKey(item.UUID), Is.False);
        }

        // -------------------------------------------------------------------
        // Requirement 21.3: UpdateItem changes item quantity
        // -------------------------------------------------------------------

        [Test]
        public void UpdateItem_ChangesItemQuantity()
        {
            var colony = new Colony
            {
                UUID = Guid.NewGuid().ToString(),
                OwnerUUID = "test-player-uuid",
                PlanetName = "UpdItemTest",
                ColonyName = "UpdItemColony",
                SystemName = "UpdItemSystem",
            };
            playerContext.AddColony(colony);

            var item = new Item
            {
                UUID = Guid.NewGuid().ToString(),
                ItemType = ItemType.ItemTypeEnum.Resource,
                Name = "TestResource",
                Quantity = 50,
            };
            service.AddItem(colony.UUID, item);
            service.UpdateItem(colony.UUID, item.UUID, 200);

            Assert.That(item.Quantity, Is.EqualTo(200));
        }

        // -------------------------------------------------------------------
        // Requirement 22.3: AddCommodityRequest adds commodity to colony
        // -------------------------------------------------------------------

        [Test]
        public void AddCommodityRequest_AddsCommodityToColony()
        {
            var colony = new Colony
            {
                UUID = Guid.NewGuid().ToString(),
                OwnerUUID = "test-player-uuid",
                PlanetName = "CommTest",
                ColonyName = "CommColony",
                SystemName = "CommSystem",
            };
            playerContext.AddColony(colony);

            service.AddCommodityRequest(colony.UUID, "Steel", 100, null);

            Assert.That(colony.Commodities.Count, Is.EqualTo(1));
            Assert.That(colony.Commodities[0].Name, Is.EqualTo("Steel"));
            Assert.That(colony.Commodities[0].Requested, Is.EqualTo(100));
        }

        // -------------------------------------------------------------------
        // Requirement 23.3: RemoveCommodityRequest removes commodity from colony
        // -------------------------------------------------------------------

        [Test]
        public void RemoveCommodityRequest_RemovesCommodityFromColony()
        {
            var colony = new Colony
            {
                UUID = Guid.NewGuid().ToString(),
                OwnerUUID = "test-player-uuid",
                PlanetName = "RemCommTest",
                ColonyName = "RemCommColony",
                SystemName = "RemCommSystem",
            };
            playerContext.AddColony(colony);

            service.AddCommodityRequest(colony.UUID, "Steel", 100, null);
            service.RemoveCommodityRequest(colony.UUID, "Steel");

            Assert.That(colony.Commodities.Count, Is.EqualTo(0));
        }

        // -------------------------------------------------------------------
        // Requirement 24.3: UpdateCommodityRequest updates commodity fields
        // -------------------------------------------------------------------

        [Test]
        public void UpdateCommodityRequest_UpdatesCommodityFields()
        {
            var colony = new Colony
            {
                UUID = Guid.NewGuid().ToString(),
                OwnerUUID = "test-player-uuid",
                PlanetName = "UpdCommTest",
                ColonyName = "UpdCommColony",
                SystemName = "UpdCommSystem",
            };
            playerContext.AddColony(colony);

            service.AddCommodityRequest(colony.UUID, "Steel", 100, null);

            var needBy = new DateTime(2025, 12, 31);
            service.UpdateCommodityRequest(colony.UUID, "Steel", 200, 50, needBy, false);

            Assert.That(colony.Commodities[0].Requested, Is.EqualTo(200));
            Assert.That(colony.Commodities[0].Delivered, Is.EqualTo(50));
            Assert.That(colony.Commodities[0].NeedBy, Is.EqualTo(needBy));
        }

        // -------------------------------------------------------------------
        // Bug condition: Fulfilled checkbox not persisted
        // Validates: Requirements 1.1, 1.2, 2.1, 2.2
        // -------------------------------------------------------------------

        [Test]
        public void UpdateCommodityRequest_SetsFulfilledTrue_WhenFulfilledParameterIsTrue()
        {
            var colony = new Colony
            {
                UUID = Guid.NewGuid().ToString(),
                OwnerUUID = "test-player-uuid",
                PlanetName = "FulfilledTest",
                ColonyName = "FulfilledColony",
                SystemName = "FulfilledSystem",
            };
            playerContext.AddColony(colony);
            service.AddCommodityRequest(colony.UUID, "Steel", 100, null);

            var needBy = new DateTime(2025, 12, 31);
            service.UpdateCommodityRequest(colony.UUID, "Steel", 100, 100, needBy, true);

            Assert.That(colony.Commodities[0].Fulfilled, Is.True);
        }

        [Test]
        public void UpdateCommodityRequest_SetsFulfilledFalse_WhenFulfilledParameterIsFalse()
        {
            var colony = new Colony
            {
                UUID = Guid.NewGuid().ToString(),
                OwnerUUID = "test-player-uuid",
                PlanetName = "UnfulfillTest",
                ColonyName = "UnfulfillColony",
                SystemName = "UnfulfillSystem",
            };
            playerContext.AddColony(colony);
            service.AddCommodityRequest(colony.UUID, "Steel", 100, null);
            colony.Commodities[0].Fulfilled = true; // Pre-set to true

            var needBy = new DateTime(2025, 12, 31);
            service.UpdateCommodityRequest(colony.UUID, "Steel", 100, 0, needBy, false);

            Assert.That(colony.Commodities[0].Fulfilled, Is.False);
        }

        // -------------------------------------------------------------------
        // Preservation: Non-Fulfilled column edits unchanged
        // Validates: Requirements 3.1, 3.2, 3.3
        // -------------------------------------------------------------------

        [Test]
        public void UpdateCommodityRequest_PreservesFulfilled_WhenPassingExistingValue()
        {
            var colony = new Colony
            {
                UUID = Guid.NewGuid().ToString(),
                OwnerUUID = "test-player-uuid",
                PlanetName = "PreserveTest",
                ColonyName = "PreserveColony",
                SystemName = "PreserveSystem",
            };
            playerContext.AddColony(colony);
            service.AddCommodityRequest(colony.UUID, "Steel", 100, null);
            colony.Commodities[0].Fulfilled = true; // Pre-set to true

            // Simulate Amount edit (column 1) — passes existing Fulfilled value
            var needBy = new DateTime(2025, 12, 31);
            service.UpdateCommodityRequest(colony.UUID, "Steel", 200, 100, needBy, true);

            Assert.That(colony.Commodities[0].Fulfilled, Is.True, "Fulfilled should be preserved when passing existing value");
            Assert.That(colony.Commodities[0].Requested, Is.EqualTo(200), "Requested should be updated");
        }

        // -------------------------------------------------------------------
        // Write lock timeout throws TimeoutException
        // -------------------------------------------------------------------

        [Test]
        public void Update_WriteLockTimeout_ThrowsTimeoutException()
        {
            var colony = new Colony
            {
                UUID = Guid.NewGuid().ToString(),
                OwnerUUID = "test-player-uuid",
                PlanetName = "LockTest",
                ColonyName = "LockColony",
                SystemName = "LockSystem",
            };
            playerContext.AddColony(colony);

            // Hold write lock on a background thread so the main thread's
            // service.Update() sees a genuine timeout (not LockRecursionException).
            using (var held = new System.Threading.ManualResetEventSlim(false))
            using (var release = new System.Threading.ManualResetEventSlim(false))
            {
                var lockThread = new System.Threading.Thread(() =>
                {
                    colony.ColonyLock.EnterWriteLock();
                    held.Set();
                    release.Wait();
                    colony.ColonyLock.ExitWriteLock();
                });
                lockThread.Start();
                held.Wait();

                try
                {
                    var request = new ColonyUpdateRequest
                    {
                        PlanetName = "Blocked",
                        ColonyName = "BlockedColony",
                        SystemName = "BlockedSystem",
                    };

                    Assert.Throws<TimeoutException>(
                        () => service.Update(colony.UUID, request));
                }
                finally
                {
                    release.Set();
                    lockThread.Join(5000);
                }
            }
        }
    }
}