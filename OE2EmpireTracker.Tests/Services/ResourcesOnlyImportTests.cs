using System.Collections.Generic;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;
using BpModel = OE2EmpireTracker.Models.Blueprint;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Unit tests for resources-only import edge cases.
    /// Validates: Requirements 1.2, 1.3, 3.1, 3.2, 3.3
    /// </summary>
    [TestFixture]
    public class ResourcesOnlyImportTests
    {
        /// <summary>
        /// Req 1.3 edge case: empty resources + missing dedup fields -> NOT resources-only.
        /// </summary>
        [Test]
        public void IsResourcesOnlyImport_EmptyResources_MissingDedupFields_ReturnsFalse()
        {
            var bp = new BpModel();
            bp.Resources = new Dictionary<string, string>(); // empty
            bp.BluePrintType = null;
            bp.Class = 0;
            bp.TechLevel = null;

            Assert.That(MarketBlueprintImporter.IsResourcesOnlyImport(bp), Is.False);
        }

        /// <summary>
        /// Req 1.2 edge case: has resources but BluePrintType is non-empty -> NOT resources-only.
        /// </summary>
        [Test]
        public void IsResourcesOnlyImport_HasResources_NonEmptyBluePrintType_ReturnsFalse()
        {
            var bp = new BpModel();
            bp.Resources = new Dictionary<string, string> { { "Iron", "500" } };
            bp.BluePrintType = "Reactor";
            bp.Class = 0;
            bp.TechLevel = null;

            Assert.That(MarketBlueprintImporter.IsResourcesOnlyImport(bp), Is.False);
        }

        /// <summary>
        /// Req 3.2: MergeResourcesOnly preserves protected properties when incoming
        /// has different values for "Manufacture Run Time" and "Power Required".
        /// </summary>
        [Test]
        public void MergeResourcesOnly_PreservesProtectedProperties_WhenIncomingDiffers()
        {
            var target = new BpModel("Test Hull");
            target.UUID = "target-uuid";
            target.BluePrintType = "Hull";
            target.Class = 3;
            target.TechLevel = "MilSpec";
            target.Evolution = 2;
            target.Properties = new PropertyBag();
            target.Properties.setProperty("Manufacture Run Time", "7200");
            target.Properties.setProperty("Power Required", "250");
            target.Properties.setProperty("Health", "100");
            target.Resources = new Dictionary<string, string> { { "OldRes", "1" } };

            var incoming = new BpModel();
            incoming.Resources = new Dictionary<string, string> { { "Iron", "500" } };
            incoming.Properties = new PropertyBag();
            incoming.Properties.setProperty("Manufacture Run Time", "9999");
            incoming.Properties.setProperty("Power Required", "9999");
            incoming.Properties.setProperty("Damage", "50");

            MarketBlueprintImporter.MergeResourcesOnly(target, incoming);

            // Protected properties must retain original values
            string mrt;
            target.Properties.getString("Manufacture Run Time", null, out mrt);
            Assert.That(mrt, Is.EqualTo("7200"), "Manufacture Run Time must be preserved");

            string pwr;
            target.Properties.getString("Power Required", null, out pwr);
            Assert.That(pwr, Is.EqualTo("250"), "Power Required must be preserved");

            // Non-protected incoming property should be merged
            string dmg;
            target.Properties.getString("Damage", null, out dmg);
            Assert.That(dmg, Is.EqualTo("50"), "Non-protected incoming property should be present");
        }

        /// <summary>
        /// Req 3.1, 3.3: MergeResourcesOnly replaces resources completely --
        /// no leftover keys from the target's original resources.
        /// </summary>
        [Test]
        public void MergeResourcesOnly_ReplacesResourcesCompletely_NoLeftoverKeys()
        {
            var target = new BpModel("Test Reactor");
            target.UUID = "target-uuid";
            target.BluePrintType = "Reactor";
            target.Class = 1;
            target.TechLevel = "Standard";
            target.Evolution = 0;
            target.Properties = new PropertyBag();
            target.Resources = new Dictionary<string, string>
            {
                { "OldIron", "100" },
                { "OldCopper", "200" },
                { "OldGold", "300" }
            };

            var incoming = new BpModel();
            incoming.Resources = new Dictionary<string, string>
            {
                { "NewSteel", "400" },
                { "NewTitanium", "500" }
            };

            incoming.Properties = new PropertyBag();

            MarketBlueprintImporter.MergeResourcesOnly(target, incoming);

            // Resources should be exactly the incoming set
            Assert.That(target.Resources.Count, Is.EqualTo(2));
            Assert.That(target.Resources.ContainsKey("NewSteel"), Is.True);
            Assert.That(target.Resources.ContainsKey("NewTitanium"), Is.True);
            Assert.That(target.Resources["NewSteel"], Is.EqualTo("400"));
            Assert.That(target.Resources["NewTitanium"], Is.EqualTo("500"));

            // Old keys must be gone
            Assert.That(target.Resources.ContainsKey("OldIron"), Is.False);
            Assert.That(target.Resources.ContainsKey("OldCopper"), Is.False);
            Assert.That(target.Resources.ContainsKey("OldGold"), Is.False);
        }
    }
}
