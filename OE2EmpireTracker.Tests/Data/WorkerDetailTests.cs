using NUnit.Framework;
using NUnit.Framework.Legacy;
using OE2EmpireTracker.Data;
using System.Linq;

namespace OE2EmpireTracker.Tests.Data
{
    [TestFixture]
    public class WorkerDetailTests
    {
        // -----------------------------------------------------------------------
        // WorkerDetails static list
        // -----------------------------------------------------------------------

        [Test]
        public void WorkerDetails_ListIsNotEmpty()
        {
            Assert.IsTrue(WorkerDetail.WorkerDetails.Count > 0);
        }

        [Test]
        public void WorkerDetails_ContainsThreeNamedEntries()
        {
            // Blank entry + 3 named = 4 total
            var named = WorkerDetail.WorkerDetails.Where(w => !string.IsNullOrEmpty(w.ID)).ToList();
            Assert.AreEqual(3, named.Count);
        }

        [Test]
        public void WorkerDetails_ContainsBlueCollarDetail()
        {
            Assert.IsTrue(WorkerDetail.WorkerDetails.Any(w => w.ID == "BlueCollarDetail"));
        }

        [Test]
        public void WorkerDetails_ContainsWhiteCollarDetail()
        {
            Assert.IsTrue(WorkerDetail.WorkerDetails.Any(w => w.ID == "WhiteCollarDetail"));
        }

        [Test]
        public void WorkerDetails_ContainsSpecialistDetail()
        {
            Assert.IsTrue(WorkerDetail.WorkerDetails.Any(w => w.ID == "SpecialistDetail"));
        }

        [Test]
        public void WorkerDetails_AllNamedEntriesHaveNonEmptyName()
        {
            foreach (var w in WorkerDetail.WorkerDetails.Where(w => !string.IsNullOrEmpty(w.ID)))
                Assert.IsFalse(string.IsNullOrEmpty(w.Name),
                    $"WorkerDetail with ID '{w.ID}' has empty Name");
        }

        [Test]
        public void WorkerDetails_NoDuplicateIDs()
        {
            var ids = WorkerDetail.WorkerDetails.Select(w => w.ID).ToList();
            Assert.AreEqual(ids.Distinct().Count(), ids.Count, "Duplicate WorkerDetail IDs found");
        }

        [Test]
        public void WorkerDetails_NoDuplicateNames()
        {
            var names = WorkerDetail.WorkerDetails.Select(w => w.Name).ToList();
            Assert.AreEqual(names.Distinct().Count(), names.Count, "Duplicate WorkerDetail Names found");
        }

        // -----------------------------------------------------------------------
        // WorkerDetailMapByID
        // -----------------------------------------------------------------------

        [Test]
        public void WorkerDetailMapByID_ContainsAllIDs()
        {
            foreach (var w in WorkerDetail.WorkerDetails)
                Assert.IsTrue(WorkerDetail.WorkerDetailMapByID.ContainsKey(w.ID),
                    $"WorkerDetailMapByID missing key '{w.ID}'");
        }

        [Test]
        public void WorkerDetailMapByID_LookupReturnsCorrectName()
        {
            Assert.AreEqual("Blue Collar Detail",  WorkerDetail.WorkerDetailMapByID["BlueCollarDetail"].Name);
            Assert.AreEqual("White Collar Detail", WorkerDetail.WorkerDetailMapByID["WhiteCollarDetail"].Name);
            Assert.AreEqual("Specialist Detail",   WorkerDetail.WorkerDetailMapByID["SpecialistDetail"].Name);
        }

        [Test]
        public void WorkerDetailMapByID_EmptyKeyReturnsBlankEntry()
        {
            Assert.AreEqual(string.Empty, WorkerDetail.WorkerDetailMapByID[string.Empty].Name);
        }

        // -----------------------------------------------------------------------
        // WorkerDetailMapByName
        // -----------------------------------------------------------------------

        [Test]
        public void WorkerDetailMapByName_ContainsAllNames()
        {
            foreach (var w in WorkerDetail.WorkerDetails)
                Assert.IsTrue(WorkerDetail.WorkerDetailMapByName.ContainsKey(w.Name),
                    $"WorkerDetailMapByName missing key '{w.Name}'");
        }

        [Test]
        public void WorkerDetailMapByName_LookupReturnsCorrectID()
        {
            Assert.AreEqual("BlueCollarDetail",  WorkerDetail.WorkerDetailMapByName["Blue Collar Detail"].ID);
            Assert.AreEqual("WhiteCollarDetail", WorkerDetail.WorkerDetailMapByName["White Collar Detail"].ID);
            Assert.AreEqual("SpecialistDetail",  WorkerDetail.WorkerDetailMapByName["Specialist Detail"].ID);
        }

        [Test]
        public void WorkerDetailMapByName_EmptyKeyReturnsBlankEntry()
        {
            Assert.AreEqual(string.Empty, WorkerDetail.WorkerDetailMapByName[string.Empty].ID);
        }

        // -----------------------------------------------------------------------
        // Round-trip: ID -> Name -> ID
        // -----------------------------------------------------------------------

        [Test]
        public void RoundTrip_IDToNameToID_AllEntries()
        {
            foreach (var w in WorkerDetail.WorkerDetails)
            {
                string name = WorkerDetail.WorkerDetailMapByID[w.ID].Name;
                string roundTrippedID = WorkerDetail.WorkerDetailMapByName[name].ID;
                Assert.AreEqual(w.ID, roundTrippedID, $"Round-trip failed for ID '{w.ID}'");
            }
        }

        // -----------------------------------------------------------------------
        // IDs match ColonyStatusCalculator property bag keys
        // -----------------------------------------------------------------------

        [Test]
        public void WorkerDetailIDs_MatchExpectedPropertyBagKeys()
        {
            // These IDs are used as property bag keys in ColonyStatusCalculator
            // for worker slot parsing. Verify they haven't drifted.
            Assert.IsTrue(WorkerDetail.WorkerDetailMapByID.ContainsKey("BlueCollarDetail"));
            Assert.IsTrue(WorkerDetail.WorkerDetailMapByID.ContainsKey("WhiteCollarDetail"));
            Assert.IsTrue(WorkerDetail.WorkerDetailMapByID.ContainsKey("SpecialistDetail"));
        }
    }
}
