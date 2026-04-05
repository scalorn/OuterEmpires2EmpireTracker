using NUnit.Framework;
using OE2EmpireTracker.Data;
using System.Linq;

namespace OE2EmpireTracker.Tests.Data
{
    [TestFixture]
    public class WorkerTypeInfoTests
    {
        // -----------------------------------------------------------------------
        // WorkerTypes array structure
        // -----------------------------------------------------------------------

        [Test]
        public void WorkerTypes_HasThreeEntries()
        {
            Assert.AreEqual(3, WorkerDetail.WorkerTypes.Length);
        }

        // -----------------------------------------------------------------------
        // Each entry has correct DetailKey, WorkerPrefix, DisplayName
        // -----------------------------------------------------------------------

        [Test]
        public void WorkerTypes_FirstEntry_IsBlueCollar()
        {
            var entry = WorkerDetail.WorkerTypes[0];
            Assert.AreEqual("BlueCollarDetail", entry.DetailKey);
            Assert.AreEqual("BlueCollar", entry.WorkerPrefix);
            Assert.AreEqual("Blue Collar", entry.DisplayName);
        }

        [Test]
        public void WorkerTypes_SecondEntry_IsWhiteCollar()
        {
            var entry = WorkerDetail.WorkerTypes[1];
            Assert.AreEqual("WhiteCollarDetail", entry.DetailKey);
            Assert.AreEqual("WhiteCollar", entry.WorkerPrefix);
            Assert.AreEqual("White Collar", entry.DisplayName);
        }

        [Test]
        public void WorkerTypes_ThirdEntry_IsSpecialist()
        {
            var entry = WorkerDetail.WorkerTypes[2];
            Assert.AreEqual("SpecialistDetail", entry.DetailKey);
            Assert.AreEqual("Specialist", entry.WorkerPrefix);
            Assert.AreEqual("Specialist", entry.DisplayName);
        }

        // -----------------------------------------------------------------------
        // UnassignedKey is "Unassigned" + DetailKey
        // -----------------------------------------------------------------------

        [TestCase(0, "UnassignedBlueCollarDetail")]
        [TestCase(1, "UnassignedWhiteCollarDetail")]
        [TestCase(2, "UnassignedSpecialistDetail")]
        public void WorkerTypes_UnassignedKey_IsUnassignedPlusDetailKey(int index, string expected)
        {
            Assert.AreEqual(expected, WorkerDetail.WorkerTypes[index].UnassignedKey);
        }

        // -----------------------------------------------------------------------
        // DetailKeys match WorkerDetail IDs
        // -----------------------------------------------------------------------

        [Test]
        public void WorkerTypes_DetailKeys_MatchWorkerDetailIDs()
        {
            foreach (var wt in WorkerDetail.WorkerTypes)
            {
                Assert.IsTrue(WorkerDetail.WorkerDetailMapByID.ContainsKey(wt.DetailKey),
                    $"DetailKey '{wt.DetailKey}' not found in WorkerDetailMapByID");
            }
        }
    }
}
