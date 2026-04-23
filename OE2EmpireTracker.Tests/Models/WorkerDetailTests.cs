using System.Linq;
using NUnit.Framework;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Tests.Models
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
            Assert.That(WorkerDetail.WorkerDetails.Count > 0, Is.True);
        }

        [Test]
        public void WorkerDetails_ContainsThreeNamedEntries()
        {
            // Blank entry + 3 named = 4 total
            var named = WorkerDetail.WorkerDetails.Where(w => !string.IsNullOrEmpty(w.ID)).ToList();
            Assert.That(named.Count, Is.EqualTo(3));
        }

        [Test]
        public void WorkerDetails_ContainsBlueCollarDetail()
        {
            Assert.That(WorkerDetail.WorkerDetails.Any(w => w.ID == "BlueCollarDetail"), Is.True);
        }

        [Test]
        public void WorkerDetails_ContainsWhiteCollarDetail()
        {
            Assert.That(WorkerDetail.WorkerDetails.Any(w => w.ID == "WhiteCollarDetail"), Is.True);
        }

        [Test]
        public void WorkerDetails_ContainsSpecialistDetail()
        {
            Assert.That(WorkerDetail.WorkerDetails.Any(w => w.ID == "SpecialistDetail"), Is.True);
        }

        [Test]
        public void WorkerDetails_AllNamedEntriesHaveNonEmptyName()
        {
            foreach (var w in WorkerDetail.WorkerDetails.Where(w => !string.IsNullOrEmpty(w.ID)))
                Assert.That(
                    string.IsNullOrEmpty(w.Name),
                    Is.False,
                    $"WorkerDetail with ID '{w.ID}' has empty Name");
        }

        [Test]
        public void WorkerDetails_NoDuplicateIDs()
        {
            var ids = WorkerDetail.WorkerDetails.Select(w => w.ID).ToList();
            Assert.That(
                ids.Count,
                Is.EqualTo(ids.Distinct().Count()),
                "Duplicate WorkerDetail IDs found");
        }

        [Test]
        public void WorkerDetails_NoDuplicateNames()
        {
            var names = WorkerDetail.WorkerDetails.Select(w => w.Name).ToList();
            Assert.That(
                names.Count,
                Is.EqualTo(names.Distinct().Count()),
                "Duplicate WorkerDetail Names found");
        }

        // -----------------------------------------------------------------------
        // WorkerDetailMapByID
        // -----------------------------------------------------------------------

        [Test]
        public void WorkerDetailMapByID_ContainsAllIDs()
        {
            foreach (var w in WorkerDetail.WorkerDetails)
                Assert.That(
                    WorkerDetail.WorkerDetailMapByID.ContainsKey(w.ID),
                    Is.True,
                    $"WorkerDetailMapByID missing key '{w.ID}'");
        }

        [Test]
        public void WorkerDetailMapByID_LookupReturnsCorrectName()
        {
            Assert.That(WorkerDetail.WorkerDetailMapByID["BlueCollarDetail"].Name, Is.EqualTo("Blue Collar Detail"));
            Assert.That(WorkerDetail.WorkerDetailMapByID["WhiteCollarDetail"].Name, Is.EqualTo("White Collar Detail"));
            Assert.That(WorkerDetail.WorkerDetailMapByID["SpecialistDetail"].Name, Is.EqualTo("Specialist Detail"));
        }

        [Test]
        public void WorkerDetailMapByID_EmptyKeyReturnsBlankEntry()
        {
            Assert.That(WorkerDetail.WorkerDetailMapByID[string.Empty].Name, Is.EqualTo(string.Empty));
        }

        // -----------------------------------------------------------------------
        // WorkerDetailMapByName
        // -----------------------------------------------------------------------

        [Test]
        public void WorkerDetailMapByName_ContainsAllNames()
        {
            foreach (var w in WorkerDetail.WorkerDetails)
                Assert.That(
                    WorkerDetail.WorkerDetailMapByName.ContainsKey(w.Name),
                    Is.True,
                    $"WorkerDetailMapByName missing key '{w.Name}'");
        }

        [Test]
        public void WorkerDetailMapByName_LookupReturnsCorrectID()
        {
            Assert.That(WorkerDetail.WorkerDetailMapByName["Blue Collar Detail"].ID, Is.EqualTo("BlueCollarDetail"));
            Assert.That(WorkerDetail.WorkerDetailMapByName["White Collar Detail"].ID, Is.EqualTo("WhiteCollarDetail"));
            Assert.That(WorkerDetail.WorkerDetailMapByName["Specialist Detail"].ID, Is.EqualTo("SpecialistDetail"));
        }

        [Test]
        public void WorkerDetailMapByName_EmptyKeyReturnsBlankEntry()
        {
            Assert.That(WorkerDetail.WorkerDetailMapByName[string.Empty].ID, Is.EqualTo(string.Empty));
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
                Assert.That(
                    roundTrippedID,
                    Is.EqualTo(w.ID),
                    $"Round-trip failed for ID '{w.ID}'");
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
            Assert.That(WorkerDetail.WorkerDetailMapByID.ContainsKey("BlueCollarDetail"), Is.True);
            Assert.That(WorkerDetail.WorkerDetailMapByID.ContainsKey("WhiteCollarDetail"), Is.True);
            Assert.That(WorkerDetail.WorkerDetailMapByID.ContainsKey("SpecialistDetail"), Is.True);
        }
    }
}
