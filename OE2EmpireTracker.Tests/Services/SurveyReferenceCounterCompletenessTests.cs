using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Completeness tests for SurveyReferenceCounter.
    /// Verifies that every source listed in spec/design/reference-counting.md
    /// is actually counted by the counter.
    /// Sources: ColonyStructure.MiningSurvey, BuildItem.MiningSurveyUUID
    /// </summary>
    [TestFixture]
    public class SurveyReferenceCounterCompletenessTests
    {
        private const string TargetUUID = "survey-completeness-uuid";

        [Test]
        public void CountReferences_ColonyStructureMiningSurvey_Counted()
        {
            var colony = new Colony();
            colony.Structures = new List<ColonyStructure>
            {
                new ColonyStructure { MiningSurvey = TargetUUID }
            };

            var counter = new SurveyReferenceCounter(new[] { colony });

            var report = counter.CountReferences(TargetUUID);

            Assert.That(report.TotalCount, Is.GreaterThan(0));
            Assert.That(report.MinerCount, Is.EqualTo(1));
        }

        [Test]
        public void CountReferences_BuildItemMiningSurveyUUID_Counted()
        {
            var buildPlan = new BuildPlan
            {
                UUID = "plan-1",
                Items = new List<BuildItem>
                {
                    new BuildItem { UUID = "bi-1", MiningSurveyUUID = TargetUUID }
                }
            };

            var counter = new SurveyReferenceCounter(
                Enumerable.Empty<Colony>(),
                new[] { buildPlan });

            var report = counter.CountReferences(TargetUUID);

            Assert.That(report.TotalCount, Is.GreaterThan(0));
            Assert.That(report.BuildItemCount, Is.EqualTo(1));
        }
    }
}
