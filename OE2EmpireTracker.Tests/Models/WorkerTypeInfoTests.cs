using NUnit.Framework;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;
using System.Linq;

namespace OE2EmpireTracker.Tests.Models
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
            Assert.That(WorkerDetail.WorkerTypes.Length, Is.EqualTo(3));
        }

        // -----------------------------------------------------------------------
        // Each entry has correct DetailKey, WorkerPrefix, DisplayName
        // -----------------------------------------------------------------------

        [Test]
        public void WorkerTypes_FirstEntry_IsBlueCollar()
        {
            var entry = WorkerDetail.WorkerTypes[0];
            Assert.That(entry.DetailKey, Is.EqualTo(GameConstants.WorkerIdBlueCollar));
            Assert.That(entry.WorkerPrefix, Is.EqualTo("BlueCollar"));
            Assert.That(entry.DisplayName, Is.EqualTo("Blue Collar"));
            Assert.That(entry.PropertyKey, Is.EqualTo(GameConstants.PropBlueCollarDetail));
            Assert.That(entry.UnassignedPropertyKey, Is.EqualTo(GameConstants.PropUnassignedBlueCollarDetail));
        }

        [Test]
        public void WorkerTypes_SecondEntry_IsWhiteCollar()
        {
            var entry = WorkerDetail.WorkerTypes[1];
            Assert.That(entry.DetailKey, Is.EqualTo(GameConstants.WorkerIdWhiteCollar));
            Assert.That(entry.WorkerPrefix, Is.EqualTo("WhiteCollar"));
            Assert.That(entry.DisplayName, Is.EqualTo("White Collar"));
            Assert.That(entry.PropertyKey, Is.EqualTo(GameConstants.PropWhiteCollarDetail));
            Assert.That(entry.UnassignedPropertyKey, Is.EqualTo(GameConstants.PropUnassignedWhiteCollarDetail));
        }

        [Test]
        public void WorkerTypes_ThirdEntry_IsSpecialist()
        {
            var entry = WorkerDetail.WorkerTypes[2];
            Assert.That(entry.DetailKey, Is.EqualTo(GameConstants.WorkerIdSpecialist));
            Assert.That(entry.WorkerPrefix, Is.EqualTo("Specialist"));
            Assert.That(entry.DisplayName, Is.EqualTo("Specialist"));
            Assert.That(entry.PropertyKey, Is.EqualTo(GameConstants.PropSpecialistDetail));
            Assert.That(entry.UnassignedPropertyKey, Is.EqualTo(GameConstants.PropUnassignedSpecialistDetail));
        }

        // -----------------------------------------------------------------------
        // UnassignedKey is "Unassigned" + DetailKey (item type ID, no spaces)
        // -----------------------------------------------------------------------

        [TestCase(0, "UnassignedBlueCollarDetail")]
        [TestCase(1, "UnassignedWhiteCollarDetail")]
        [TestCase(2, "UnassignedSpecialistDetail")]
        public void WorkerTypes_UnassignedKey_IsUnassignedPlusDetailKey(int index, string expected)
        {
            Assert.That(WorkerDetail.WorkerTypes[index].UnassignedKey, Is.EqualTo(expected));
        }

        // -----------------------------------------------------------------------
        // DetailKeys match WorkerDetail IDs
        // -----------------------------------------------------------------------

        [Test]
        public void WorkerTypes_DetailKeys_MatchWorkerDetailIDs()
        {
            foreach (var wt in WorkerDetail.WorkerTypes)
            {
                Assert.That(WorkerDetail.WorkerDetailMapByID.ContainsKey(wt.DetailKey), Is.True,
                    $"DetailKey '{wt.DetailKey}' not found in WorkerDetailMapByID");
            }
        }
    }
}
