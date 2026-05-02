using System;
using System.Linq;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Parsers;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Parsers
{
    /// <summary>
    /// Unit tests for MinerSetupHelper.EnsureWarehouseResource.
    /// Validates: Requirements 7.1, 7.2, 7.3
    /// </summary>
    [TestFixture]
    public class EnsureWarehouseResourceTests
    {
        [SetUp]
        public void SetUp()
        {
            TestHelper.ResetWithCachedData();
        }

        [TearDown]
        public void TearDown()
        {
            EmpireContext.Reset();
        }

        /// <summary>
        /// Validates: Requirement 7.2
        /// When no matching resource exists, creates one with quantity 0.
        /// </summary>
        [Test]
        public void CreatesMissingResource_WithQuantityZero()
        {
            var colony = new Colony { PlanetName = "TestPlanet" };

            MinerSetupHelper.EnsureWarehouseResource(colony, "Iron", "Medium");

            var items = colony.Items.FindResource("Iron", "Medium");
            Assert.That(items.Count, Is.EqualTo(1));
            Assert.That(items[0].Quantity, Is.EqualTo(0));
            Assert.That(items[0].ItemType, Is.EqualTo(ItemType.ItemTypeEnum.Resource));
            Assert.That(items[0].BaseItemTypeID, Is.EqualTo("Iron"));
            Assert.That(items[0].ResourcePurity, Is.EqualTo("Medium"));
            Assert.That(items[0].UUID, Is.Not.Null.And.Not.Empty);
        }

        /// <summary>
        /// Validates: Requirement 7.3
        /// Does NOT overwrite or modify existing warehouse resource records.
        /// </summary>
        [Test]
        public void DoesNotOverwriteExistingResource()
        {
            var colony = new Colony { PlanetName = "TestPlanet" };

            // Pre-populate with an existing resource
            var existing = new Item(ItemType.ItemTypeEnum.Resource, "Iron");
            existing.UUID = Guid.NewGuid().ToString();
            existing.BaseItemTypeID = "Iron";
            existing.ResourcePurity = "Medium";
            existing.Quantity = 42;
            colony.Items.AddItem(existing);

            MinerSetupHelper.EnsureWarehouseResource(colony, "Iron", "Medium");

            var items = colony.Items.FindResource("Iron", "Medium");
            Assert.That(items.Count, Is.EqualTo(1));
            Assert.That(items[0].Quantity, Is.EqualTo(42), "Existing quantity must not be modified");
            Assert.That(items[0].UUID, Is.EqualTo(existing.UUID), "Existing item must not be replaced");
        }

        /// <summary>
        /// Validates: Requirement 7.1
        /// Checks warehouse for matching resource by name and purity.
        /// Different purity should not match -- creates a new record.
        /// </summary>
        [Test]
        public void CreatesResource_WhenSameNameButDifferentPurityExists()
        {
            var colony = new Colony { PlanetName = "TestPlanet" };

            var existing = new Item(ItemType.ItemTypeEnum.Resource, "Iron");
            existing.UUID = Guid.NewGuid().ToString();
            existing.BaseItemTypeID = "Iron";
            existing.ResourcePurity = "Low";
            existing.Quantity = 10;
            colony.Items.AddItem(existing);

            MinerSetupHelper.EnsureWarehouseResource(colony, "Iron", "High");

            var lowItems = colony.Items.FindResource("Iron", "Low");
            var highItems = colony.Items.FindResource("Iron", "High");
            Assert.That(lowItems.Count, Is.EqualTo(1));
            Assert.That(lowItems[0].Quantity, Is.EqualTo(10));
            Assert.That(highItems.Count, Is.EqualTo(1));
            Assert.That(highItems[0].Quantity, Is.EqualTo(0));
        }

        /// <summary>
        /// Handles null colony gracefully -- no exception.
        /// </summary>
        [Test]
        public void HandlesNullColony_Gracefully()
        {
            Assert.DoesNotThrow(() =>
                MinerSetupHelper.EnsureWarehouseResource(null, "Iron", "Medium"));
        }

        /// <summary>
        /// Handles null/empty resourceName gracefully -- no exception, no item created.
        /// </summary>
        [Test]
        public void HandlesNullResourceName_Gracefully()
        {
            var colony = new Colony { PlanetName = "TestPlanet" };

            MinerSetupHelper.EnsureWarehouseResource(colony, null, "Medium");

            Assert.That(colony.Items.Count(), Is.EqualTo(0));
        }

        /// <summary>
        /// Handles empty resourceName gracefully -- no exception, no item created.
        /// </summary>
        [Test]
        public void HandlesEmptyResourceName_Gracefully()
        {
            var colony = new Colony { PlanetName = "TestPlanet" };

            MinerSetupHelper.EnsureWarehouseResource(colony, string.Empty, "Medium");

            Assert.That(colony.Items.Count(), Is.EqualTo(0));
        }

        /// <summary>
        /// Handles null/empty purity gracefully -- no exception, no item created.
        /// </summary>
        [Test]
        public void HandlesNullPurity_Gracefully()
        {
            var colony = new Colony { PlanetName = "TestPlanet" };

            MinerSetupHelper.EnsureWarehouseResource(colony, "Iron", null);

            Assert.That(colony.Items.Count(), Is.EqualTo(0));
        }

        /// <summary>
        /// Handles empty purity gracefully -- no exception, no item created.
        /// </summary>
        [Test]
        public void HandlesEmptyPurity_Gracefully()
        {
            var colony = new Colony { PlanetName = "TestPlanet" };

            MinerSetupHelper.EnsureWarehouseResource(colony, "Iron", string.Empty);

            Assert.That(colony.Items.Count(), Is.EqualTo(0));
        }
    }
}
