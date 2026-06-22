using System;
using System.Linq;
using FsCheck;
using NUnit.Framework;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Verification tests that EntityGenerators can produce instances without exception.
    /// </summary>
    [TestFixture]
    public class EntityGeneratorsSmokeTests
    {
        [Test]
        public void GenColony_Generates10Instances_WithoutException()
        {
            var gen = EntityGenerators.GenColony();
            var samples = Gen.Sample(10, 10, gen).ToList();

            Assert.That(samples.Count, Is.EqualTo(10));
            foreach (var colony in samples)
            {
                Assert.That(colony.UUID, Is.Not.Null.And.Not.Empty);
                Assert.That(colony.OwnerUUID, Is.Not.Null.And.Not.Empty);
                Assert.That(colony.ColonyName, Is.Not.Null.And.Not.Empty);
            }
        }

        [Test]
        public void GenBlueprint_Generates10Instances_WithoutException()
        {
            var gen = EntityGenerators.GenBlueprint();
            var samples = Gen.Sample(10, 10, gen).ToList();

            Assert.That(samples.Count, Is.EqualTo(10));
            foreach (var bp in samples)
            {
                Assert.That(bp.UUID, Is.Not.Null.And.Not.Empty);
                Assert.That(bp.OwnerUUID, Is.Not.Null.And.Not.Empty);
                Assert.That(bp.Name, Is.Not.Null.And.Not.Empty);
                Assert.That(bp.BluePrintType, Is.Not.Null.And.Not.Empty);
            }
        }

        [Test]
        public void GenColonyStructure_Generates10Instances_WithoutException()
        {
            var gen = EntityGenerators.GenColonyStructure();
            var samples = Gen.Sample(10, 10, gen).ToList();

            Assert.That(samples.Count, Is.EqualTo(10));
            foreach (var structure in samples)
            {
                Assert.That(structure.UUID, Is.Not.Null.And.Not.Empty);
                Assert.That(structure.FlatpackBlueprintUUID, Is.Not.Null.And.Not.Empty);
            }
        }
    }
}
