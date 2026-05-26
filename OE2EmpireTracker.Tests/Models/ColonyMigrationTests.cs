#pragma warning disable CS0618 // Tests intentionally exercise deprecated members
using Newtonsoft.Json;
using NUnit.Framework;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Tests.Models
{
    [TestFixture]
    public class ColonyMigrationTests
    {
        [Test]
        public void Deserialize_LegacyCurrentAttitudeOnStructure_MigratesToColonyWorkerCurrentAttitude()
        {
            string json = @"{
                ""UUID"": ""col-1"",
                ""Structures"": [
                    { ""UUID"": ""s1"", ""currentAttitude"": ""75"", ""contentmentIndex"": 0 }
                ]
            }";

            var colony = JsonConvert.DeserializeObject<Colony>(json);

            Assert.That(colony.WorkerCurrentAttitude, Is.EqualTo(75));
        }

        [Test]
        public void Deserialize_LegacyContentmentIndexOnStructure_MigratesToColonyContentmentIndex()
        {
            string json = @"{
                ""UUID"": ""col-1"",
                ""Structures"": [
                    { ""UUID"": ""s1"", ""currentAttitude"": """", ""contentmentIndex"": 80 }
                ]
            }";

            var colony = JsonConvert.DeserializeObject<Colony>(json);

            Assert.That(colony.ContentmentIndex, Is.EqualTo(80));
        }

        [Test]
        public void Deserialize_DeprecatedFieldsClearedAfterMigration()
        {
            string json = @"{
                ""UUID"": ""col-1"",
                ""Structures"": [
                    { ""UUID"": ""s1"", ""currentAttitude"": ""75"", ""contentmentIndex"": 80 },
                    { ""UUID"": ""s2"", ""currentAttitude"": ""60"", ""contentmentIndex"": 50 }
                ]
            }";

            var colony = JsonConvert.DeserializeObject<Colony>(json);

            foreach (var structure in colony.Structures)
            {
                Assert.That(structure.CurrentAttitude, Is.EqualTo(string.Empty));
                Assert.That(structure.ContentmentIndex, Is.EqualTo(0));
            }
        }

        [Test]
        public void Deserialize_MigrationIsIdempotent()
        {
            string json = @"{
                ""UUID"": ""col-1"",
                ""Structures"": [
                    { ""UUID"": ""s1"", ""currentAttitude"": ""75"", ""contentmentIndex"": 80 }
                ]
            }";

            var colony = JsonConvert.DeserializeObject<Colony>(json);

            // Serialize and deserialize again
            string reserialized = JsonConvert.SerializeObject(colony);
            var colony2 = JsonConvert.DeserializeObject<Colony>(reserialized);

            Assert.That(colony2.WorkerCurrentAttitude, Is.EqualTo(75));
            Assert.That(colony2.ContentmentIndex, Is.EqualTo(80));
            Assert.That(colony2.Structures[0].CurrentAttitude, Is.EqualTo(string.Empty));
            Assert.That(colony2.Structures[0].ContentmentIndex, Is.EqualTo(0));
        }

        [Test]
        public void Deserialize_InvalidCurrentAttitudeString_DefaultsToZero()
        {
            string json = @"{
                ""UUID"": ""col-1"",
                ""Structures"": [
                    { ""UUID"": ""s1"", ""currentAttitude"": ""not-a-number"", ""contentmentIndex"": 0 }
                ]
            }";

            var colony = JsonConvert.DeserializeObject<Colony>(json);

            Assert.That(colony.WorkerCurrentAttitude, Is.EqualTo(0));
        }

        [Test]
        public void Deserialize_AlreadyMigratedData_SkipsReprocessing()
        {
            string json = @"{
                ""UUID"": ""col-1"",
                ""workerCurrentAttitude"": 50,
                ""contentmentIndex"": 90,
                ""Structures"": [
                    { ""UUID"": ""s1"", ""currentAttitude"": ""99"", ""contentmentIndex"": 99 }
                ]
            }";

            var colony = JsonConvert.DeserializeObject<Colony>(json);

            // Colony already had non-zero values, so migration should NOT overwrite
            Assert.That(colony.WorkerCurrentAttitude, Is.EqualTo(50));
            Assert.That(colony.ContentmentIndex, Is.EqualTo(90));

            // Deprecated fields are still cleared regardless
            Assert.That(colony.Structures[0].CurrentAttitude, Is.EqualTo(string.Empty));
            Assert.That(colony.Structures[0].ContentmentIndex, Is.EqualTo(0));
        }
    }
}
#pragma warning restore CS0618
