using Newtonsoft.Json;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;
using OE2EmpireTracker.Services.Migration;

namespace OE2EmpireTracker.Tests.Services.Migration
{
    [TestFixture]
    public class Migration006Tests
    {
        [SetUp]
        public void SetUp()
        {
            EmpireContext.Reset();
            TestHelper.SetAllFilePaths();
        }

        [TearDown]
        public void TearDown()
        {
            EmpireContext.Reset();
        }

        [Test]
        public void OldJson_GameSequenceKey_DeserializesIntoDisplaySequence()
        {
            string json = @"{ ""UUID"": ""abc"", ""gameSequence"": 42 }";
            var structure = JsonConvert.DeserializeObject<ColonyStructure>(json);
            Assert.That(structure.displaySequence, Is.EqualTo(42));
        }

        [Test]
        public void NewJson_SerializesAsDisplaySequence_NotGameSequence()
        {
            var structure = new ColonyStructure();
            structure.displaySequence = 7;
            string json = JsonConvert.SerializeObject(structure);
            Assert.That(json, Does.Contain("\"displaySequence\":7"));
            Assert.That(json, Does.Not.Contain("\"gameSequence\""));
        }

        [Test]
        public void RoundTrip_OldJsonToNewJson()
        {
            // Deserialize from old format
            string oldJson = @"{ ""UUID"": ""test"", ""gameSequence"": 15 }";
            var structure = JsonConvert.DeserializeObject<ColonyStructure>(oldJson);
            Assert.That(structure.displaySequence, Is.EqualTo(15));

            // Serialize to new format
            string newJson = JsonConvert.SerializeObject(structure);
            Assert.That(newJson, Does.Contain("\"displaySequence\":15"));

            // Deserialize from new format
            var structure2 = JsonConvert.DeserializeObject<ColonyStructure>(newJson);
            Assert.That(structure2.displaySequence, Is.EqualTo(15));
        }

        [Test]
        public void MigrationRunner_BumpsVersionFromV5()
        {
            var ec = EmpireContext.GetInstance();
            var pc = PlayerContext.GetInstance();
            ec.DataVersion = 5;
            pc.DataVersion = 5;

            MigrationRunner.Run(ec, pc);

            Assert.That(ec.DataVersion, Is.EqualTo(MigrationRunner.CurrentVersion));
            Assert.That(pc.DataVersion, Is.EqualTo(MigrationRunner.CurrentVersion));
        }
    }
}
