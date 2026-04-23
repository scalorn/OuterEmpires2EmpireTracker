using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Parsers;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Parsers
{
    /// <summary>
    /// Unit tests for RefinerySetupHelper.SetupRefineries.
    /// Validates: Requirements 8.1, 8.2, 8.3, 8.4, 8.5
    /// </summary>
    [TestFixture]
    public class RefinerySetupHelperTests
    {
        private EmpireContext _empireContext;

        [SetUp]
        public void SetUp()
        {
            EmpireContext.Reset();
            TestHelper.SetAllFilePaths();
            _empireContext = EmpireContext.GetInstance();
        }

        [TearDown]
        public void TearDown()
        {
            EmpireContext.Reset();
        }

        /// <summary>
        /// Helper to create a colony structure configured as a refinery.
        /// </summary>
        private ColonyStructure CreateRefinery(string uuid, string resource, string purity, bool built, bool online)
        {
            var structure = new ColonyStructure
            {
                UUID = uuid,
                RefiningResource = resource,
                RefiningResourcePurity = purity
            };

            structure.Properties.SetProperty(GameConstants.PropBuilt, built);
            structure.Properties.SetProperty(GameConstants.PropOnline, online);
            return structure;
        }

        /// <summary>
        /// Validates: Requirements 8.1, 8.2
        /// When a refinery has a resource and purity, and no matching warehouse resource exists,
        /// SetupRefineries creates a warehouse resource with quantity 0.
        /// </summary>
        [Test]
        public void SetupRefineries_CreatesWarehouseResource_WhenMissing()
        {
            // Arrange
            var colony = new Colony
            {
                UUID = "colony-1",
                PlanetName = "TestPlanet",
                SystemName = "TestSystem",
                OwnerUUID = "owner-1"
            };

            colony.Structures.Add(CreateRefinery("ref-1", "Iron", "Medium", true, true));

            // Act
            RefinerySetupHelper.SetupRefineries(colony, _empireContext);

            // Assert
            var resources = colony.Items.FindResource("Iron", "Medium");
            Assert.That(resources.Count, Is.EqualTo(1));
            Assert.That(resources[0].Quantity, Is.EqualTo(0));
            Assert.That(resources[0].BaseItemTypeID, Is.EqualTo("Iron"));
            Assert.That(resources[0].ResourcePurity, Is.EqualTo("Medium"));
        }

        /// <summary>
        /// Validates: Requirements 8.1, 8.2
        /// When a matching warehouse resource already exists, SetupRefineries does not create a duplicate.
        /// </summary>
        [Test]
        public void SetupRefineries_DoesNotDuplicateWarehouseResource_WhenAlreadyExists()
        {
            // Arrange
            var colony = new Colony
            {
                UUID = "colony-2",
                PlanetName = "TestPlanet",
                SystemName = "TestSystem",
                OwnerUUID = "owner-1"
            };

            var existingItem = new Item(ItemType.ItemTypeEnum.Resource, "Iron")
            {
                UUID = Guid.NewGuid().ToString(),
                BaseItemTypeID = "Iron",
                ResourcePurity = "Medium",
                Quantity = 50
            };

            colony.Items.AddItem(existingItem);
            colony.Structures.Add(CreateRefinery("ref-1", "Iron", "Medium", true, true));

            // Act
            RefinerySetupHelper.SetupRefineries(colony, _empireContext);

            // Assert: still only one resource, quantity unchanged
            var resources = colony.Items.FindResource("Iron", "Medium");
            Assert.That(resources.Count, Is.EqualTo(1));
            Assert.That(resources[0].Quantity, Is.EqualTo(50));
        }

        /// <summary>
        /// Validates: Requirements 8.3
        /// When refinery is built and online with no existing timer,
        /// SetupRefineries creates a repeating timer aligned to next hour boundary.
        /// </summary>
        [Test]
        public void SetupRefineries_CreatesRepeatingTimer_WhenBuiltAndOnline()
        {
            // Arrange
            var colony = new Colony
            {
                UUID = "colony-3",
                PlanetName = "TestPlanet",
                SystemName = "TestSystem",
                OwnerUUID = "owner-1"
            };

            colony.Structures.Add(CreateRefinery("ref-1", "Iron", "Medium", true, true));

            // Act
            RefinerySetupHelper.SetupRefineries(colony, _empireContext);

            // Assert
            var structure = colony.Structures[0];
            Assert.That(structure.ProcessCompletionTime, Is.Not.Null);
            Assert.That(structure.ProcessCompletionTime.IsRepeating, Is.True);
            Assert.That(structure.ProcessCompletionTime.RepeatIntervalSeconds,
                Is.EqualTo(GameConstants.SecondsPerHour));

            // Timer should be aligned to next hour boundary
            long remaining = structure.ProcessCompletionTime.TimeRemaining;
            Assert.That(remaining, Is.GreaterThan(0));
            Assert.That(remaining, Is.LessThanOrEqualTo(GameConstants.SecondsPerHour));
        }

        /// <summary>
        /// Validates: Requirements 8.4
        /// When refinery already has an active repeating timer, SetupRefineries preserves it.
        /// </summary>
        [Test]
        public void SetupRefineries_PreservesExistingTimer_WhenActiveRepeatingTimerExists()
        {
            // Arrange
            var colony = new Colony
            {
                UUID = "colony-4",
                PlanetName = "TestPlanet",
                SystemName = "TestSystem",
                OwnerUUID = "owner-1"
            };

            var structure = CreateRefinery("ref-1", "Iron", "Medium", true, true);
            var existingTimer = new CountDownTime();
            existingTimer.StartRepeating(GameConstants.SecondsPerHour, 1800);
            structure.ProcessCompletionTime = existingTimer;
            DateTime originalEndTime = existingTimer.EndTime;
            colony.Structures.Add(structure);

            // Act
            RefinerySetupHelper.SetupRefineries(colony, _empireContext);

            // Assert: timer should be the same object, not replaced
            Assert.That(structure.ProcessCompletionTime, Is.SameAs(existingTimer));
            Assert.That(structure.ProcessCompletionTime.EndTime, Is.EqualTo(originalEndTime));
        }

        /// <summary>
        /// Validates: Requirements 8.3
        /// When refinery is not built, SetupRefineries skips timer creation.
        /// </summary>
        [Test]
        public void SetupRefineries_SkipsTimer_WhenNotBuilt()
        {
            // Arrange
            var colony = new Colony
            {
                UUID = "colony-5",
                PlanetName = "TestPlanet",
                SystemName = "TestSystem",
                OwnerUUID = "owner-1"
            };

            colony.Structures.Add(CreateRefinery("ref-1", "Iron", "Medium", false, true));

            // Act
            RefinerySetupHelper.SetupRefineries(colony, _empireContext);

            // Assert: no timer created, but warehouse resource should still be created
            var structure = colony.Structures[0];
            Assert.That(structure.ProcessCompletionTime, Is.Null);
            var resources = colony.Items.FindResource("Iron", "Medium");
            Assert.That(resources.Count, Is.EqualTo(1));
        }

        /// <summary>
        /// Validates: Requirements 8.3
        /// When refinery is not online, SetupRefineries skips timer creation.
        /// </summary>
        [Test]
        public void SetupRefineries_SkipsTimer_WhenNotOnline()
        {
            // Arrange
            var colony = new Colony
            {
                UUID = "colony-6",
                PlanetName = "TestPlanet",
                SystemName = "TestSystem",
                OwnerUUID = "owner-1"
            };

            colony.Structures.Add(CreateRefinery("ref-1", "Iron", "Medium", true, false));

            // Act
            RefinerySetupHelper.SetupRefineries(colony, _empireContext);

            // Assert: no timer created, but warehouse resource should still be created
            var structure = colony.Structures[0];
            Assert.That(structure.ProcessCompletionTime, Is.Null);
            var resources = colony.Items.FindResource("Iron", "Medium");
            Assert.That(resources.Count, Is.EqualTo(1));
        }

        /// <summary>
        /// Validates: Requirements 8.1, 8.3
        /// Structures without RefiningResource are skipped entirely.
        /// </summary>
        [Test]
        public void SetupRefineries_SkipsStructure_WhenNoRefiningResource()
        {
            // Arrange
            var colony = new Colony
            {
                UUID = "colony-7",
                PlanetName = "TestPlanet",
                SystemName = "TestSystem",
                OwnerUUID = "owner-1"
            };

            var structure = new ColonyStructure
            {
                UUID = "non-refinery-1",
                RefiningResource = null,
                RefiningResourcePurity = null
            };

            structure.Properties.SetProperty(GameConstants.PropBuilt, true);
            structure.Properties.SetProperty(GameConstants.PropOnline, true);
            colony.Structures.Add(structure);

            // Act
            RefinerySetupHelper.SetupRefineries(colony, _empireContext);

            // Assert: no timer, no warehouse resource
            Assert.That(structure.ProcessCompletionTime, Is.Null);
            Assert.That(colony.Items.Count(), Is.EqualTo(0));
        }

        /// <summary>
        /// Validates: Requirements 8.5
        /// SetupRefineries handles multiple refineries in a single colony.
        /// </summary>
        [Test]
        public void SetupRefineries_HandlesMultipleRefineries()
        {
            // Arrange
            var colony = new Colony
            {
                UUID = "colony-8",
                PlanetName = "TestPlanet",
                SystemName = "TestSystem",
                OwnerUUID = "owner-1"
            };

            colony.Structures.Add(CreateRefinery("ref-1", "Iron", "Medium", true, true));
            colony.Structures.Add(CreateRefinery("ref-2", "Copper", "High", true, true));
            colony.Structures.Add(CreateRefinery("ref-3", "Gold", "Low", false, false)); // not built/online

            // Act
            RefinerySetupHelper.SetupRefineries(colony, _empireContext);

            // Assert: two timers started, three warehouse resources created
            Assert.That(colony.Structures[0].ProcessCompletionTime, Is.Not.Null);
            Assert.That(colony.Structures[0].ProcessCompletionTime.IsRepeating, Is.True);

            Assert.That(colony.Structures[1].ProcessCompletionTime, Is.Not.Null);
            Assert.That(colony.Structures[1].ProcessCompletionTime.IsRepeating, Is.True);

            Assert.That(colony.Structures[2].ProcessCompletionTime, Is.Null);

            // All three should have warehouse resources
            Assert.That(colony.Items.FindResource("Iron", "Medium").Count, Is.EqualTo(1));
            Assert.That(colony.Items.FindResource("Copper", "High").Count, Is.EqualTo(1));
            Assert.That(colony.Items.FindResource("Gold", "Low").Count, Is.EqualTo(1));
        }
    }
}
