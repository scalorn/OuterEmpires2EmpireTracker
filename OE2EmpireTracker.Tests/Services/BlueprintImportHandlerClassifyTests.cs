using System.Collections.Generic;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;
using BpModel = OE2EmpireTracker.Models.Blueprint;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Unit tests for BlueprintImportHandler.ClassifyImport.
    /// Validates: Requirement 6.2 (ClassifyImport routing logic)
    /// </summary>
    [TestFixture]
    public class BlueprintImportHandlerClassifyTests
    {
        /// <summary>
        /// ResourcesOnly: blueprint with resources but no type/class/tech
        /// triggers the resources-only import path.
        /// </summary>
        [Test]
        public void ClassifyImport_ResourcesOnly_ZeroProperties_NResources()
        {
            var bp = new BpModel();
            bp.Name = "Some Hull";
            bp.Resources = new Dictionary<string, string>
            {
                { "Iron", "500" },
                { "Copper", "200" }
            };
            bp.BluePrintType = null;
            bp.Class = 0;
            bp.TechLevel = null;
            bp.Properties = new PropertyBag();

            var result = BlueprintImportHandler.ClassifyImport(bp);

            Assert.That(result, Is.EqualTo(BlueprintImportHandler.ImportType.ResourcesOnly));
        }

        /// <summary>
        /// Full: blueprint with a name, properties, and type info
        /// triggers the full import path.
        /// </summary>
        [Test]
        public void ClassifyImport_Full_HasNameAndProperties()
        {
            var bp = new BpModel("Test Reactor");
            bp.BluePrintType = "Reactor";
            bp.Class = 3;
            bp.TechLevel = "MilSpec";
            bp.Evolution = 2;
            bp.Properties = new PropertyBag();
            bp.Properties.setProperty("Power Output", "1200");
            bp.Resources = new Dictionary<string, string> { { "Iron", "100" } };

            var result = BlueprintImportHandler.ClassifyImport(bp);

            Assert.That(result, Is.EqualTo(BlueprintImportHandler.ImportType.Full));
        }

        /// <summary>
        /// NoName: blueprint with empty name and non-resources-only shape
        /// triggers the no-name fallback path.
        /// </summary>
        [Test]
        public void ClassifyImport_NoName_EmptyName()
        {
            var bp = new BpModel();
            bp.Name = "";
            bp.BluePrintType = "Hull";
            bp.Class = 1;
            bp.TechLevel = "Standard";
            bp.Properties = new PropertyBag();
            bp.Properties.setProperty("Health", "500");
            bp.Resources = new Dictionary<string, string>();

            var result = BlueprintImportHandler.ClassifyImport(bp);

            Assert.That(result, Is.EqualTo(BlueprintImportHandler.ImportType.NoName));
        }

        /// <summary>
        /// NoName: blueprint with null name triggers the no-name path.
        /// </summary>
        [Test]
        public void ClassifyImport_NoName_NullName()
        {
            var bp = new BpModel();
            bp.Name = null;
            bp.BluePrintType = "Shield";
            bp.Class = 2;
            bp.TechLevel = "MilSpec";
            bp.Properties = new PropertyBag();
            bp.Resources = new Dictionary<string, string>();

            var result = BlueprintImportHandler.ClassifyImport(bp);

            Assert.That(result, Is.EqualTo(BlueprintImportHandler.ImportType.NoName));
        }
    }
}
