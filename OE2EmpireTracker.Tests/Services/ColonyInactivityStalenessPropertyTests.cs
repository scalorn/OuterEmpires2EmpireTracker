using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using FsCheck.NUnit;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Feature: colony-import-timestamp, Property 4: Inactivity collector produces correct ColonyImportStaleness rows
    /// </summary>
    [TestFixture]
    public class ColonyInactivityStalenessPropertyTests
    {
        private PlayerContext _playerContext;
        private string _originalEmpireFilePath;
        private string _originalPlayerFilePath;

        [SetUp]
        public void SetUp()
        {
            _originalEmpireFilePath = EmpireContext.FilePath;
            _originalPlayerFilePath = PlayerContext.FilePath;

            TestHelper.SetEmpireFilePath();
            PlayerContext.FilePath = "nonexistent_player_data.json";

            EmpireContext.Reset();
            EmpireContext.GetInstance();
            PlayerContext.Reset();
            _playerContext = PlayerContext.GetInstance();
            EmpireContext.PlayerContext = _playerContext;
        }

        [TearDown]
        public void TearDown()
        {
            PlayerContext.Reset();
            EmpireContext.Reset();
            EmpireContext.FilePath = _originalEmpireFilePath;
            PlayerContext.FilePath = _originalPlayerFilePath;
        }

        #region Generators

        /// <summary>
        /// Generates valid UTC DateTime values constrained to years 2000-2099.
        /// </summary>
        private static Gen<DateTime> ValidUtcDateTimeGen()
        {
            return from year in Gen.Choose(2000, 2099)
                   from month in Gen.Choose(1, 12)
                   from day in Gen.Choose(1, 28)
                   from hour in Gen.Choose(0, 23)
                   from minute in Gen.Choose(0, 59)
                   select new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Utc);
        }

        /// <summary>
        /// Generates a colony with a LastImportDateTime that is older than 1 day (stale).
        /// </summary>
        private static Gen<Colony> StaleColonyGen()
        {
            return from colonyName in Arb.Default.NonEmptyString().Generator.Select(s => s.Get)
                   from systemName in Arb.Default.NonEmptyString().Generator.Select(s => s.Get)
                   from daysOld in Gen.Choose(2, 365)
                   select new Colony
                   {
                       ColonyName = colonyName,
                       SystemName = systemName,
                       LastImportDateTime = SurveyDateTimeParser.ToIsoString(
                           DateTime.UtcNow.AddDays(-daysOld))
                   };
        }

        /// <summary>
        /// Generates a colony with a LastImportDateTime that is less than 1 day old (fresh).
        /// </summary>
        private static Gen<Colony> FreshColonyGen()
        {
            return from colonyName in Arb.Default.NonEmptyString().Generator.Select(s => s.Get)
                   from systemName in Arb.Default.NonEmptyString().Generator.Select(s => s.Get)
                   from secondsOld in Gen.Choose(0, 86000)
                   select new Colony
                   {
                       ColonyName = colonyName,
                       SystemName = systemName,
                       LastImportDateTime = SurveyDateTimeParser.ToIsoString(
                           DateTime.UtcNow.AddSeconds(-secondsOld))
                   };
        }

        /// <summary>
        /// Generates a colony with null or empty LastImportDateTime (should be treated as stale).
        /// </summary>
        private static Gen<Colony> NullTimestampColonyGen()
        {
            var nullOrEmpty = Gen.OneOf(Gen.Constant((string)null), Gen.Constant(string.Empty));
            return from colonyName in Arb.Default.NonEmptyString().Generator.Select(s => s.Get)
                   from systemName in Arb.Default.NonEmptyString().Generator.Select(s => s.Get)
                   from ts in nullOrEmpty
                   select new Colony
                   {
                       ColonyName = colonyName,
                       SystemName = systemName,
                       LastImportDateTime = ts
                   };
        }

        /// <summary>
        /// Generates a mixed list of stale, fresh, and null-timestamp colonies.
        /// </summary>
        private static Gen<List<Colony>> MixedColonyListGen()
        {
            var colonyGen = Gen.OneOf(StaleColonyGen(), FreshColonyGen(), NullTimestampColonyGen());
            return Gen.ListOf(colonyGen).Select(cs => cs.ToList());
        }

        #endregion

        #region Property 4: Inactivity collector produces correct ColonyImportStaleness rows

        /// <summary>
        /// Feature: colony-import-timestamp, Property 4: Inactivity collector produces correct ColonyImportStaleness rows.
        /// For any set of colonies with various LastImportDateTime values (some > 1 day old,
        /// some &lt; 1 day old, some null), the collector should produce exactly one ActivityRow
        /// with Type == ColonyImportStaleness for each colony whose LastImportDateTime is older
        /// than 1 day or is null/empty. Each row should have ColonyName and SystemName matching
        /// the source colony, SourceName == "Colony Import", and ProcessDetails ending with
        /// " since last import".
        /// **Validates: Requirements 4.2, 4.3, 4.5**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property InactivityCollectorProducesCorrectStalenessRows()
        {
            return Prop.ForAll(MixedColonyListGen().ToArbitrary(), colonies =>
            {
                var rows = ColonyInactivityCollector.CollectInactivities(colonies, _playerContext);

                // Filter to only staleness rows
                var stalenessRows = rows.Where(r => r.Type == ActivityType.ColonyImportStaleness).ToList();

                // Determine which colonies should produce staleness rows
                var expectedStaleColonies = new List<Colony>();
                foreach (var colony in colonies)
                {
                    DateTime parsed;
                    if (!SurveyDateTimeParser.TryParseIso(colony.LastImportDateTime, out parsed))
                    {
                        // Null/empty/unparseable — should produce a row
                        expectedStaleColonies.Add(colony);
                    }
                    else if ((DateTime.UtcNow - parsed).TotalSeconds > 86400)
                    {
                        // Older than 1 day — should produce a row
                        expectedStaleColonies.Add(colony);
                    }
                }

                // Count check: exactly one row per expected stale colony
                if (stalenessRows.Count != expectedStaleColonies.Count)
                    return false.Label(
                        $"Expected {expectedStaleColonies.Count} staleness rows but got {stalenessRows.Count}");

                // Verify each staleness row matches its source colony
                for (int i = 0; i < expectedStaleColonies.Count; i++)
                {
                    var colony = expectedStaleColonies[i];
                    var row = stalenessRows[i];

                    if (row.ColonyName != colony.ColonyName)
                        return false.Label(
                            $"Row {i}: ColonyName mismatch: expected '{colony.ColonyName}', got '{row.ColonyName}'");

                    if (row.SystemName != colony.SystemName)
                        return false.Label(
                            $"Row {i}: SystemName mismatch: expected '{colony.SystemName}', got '{row.SystemName}'");

                    if (row.SourceName != "Colony Import")
                        return false.Label(
                            $"Row {i}: SourceName should be 'Colony Import', got '{row.SourceName}'");

                    if (!row.ProcessDetails.EndsWith(" since last import"))
                        return false.Label(
                            $"Row {i}: ProcessDetails should end with ' since last import', got '{row.ProcessDetails}'");
                }

                return true.Label("All staleness rows correct");
            });
        }

        #endregion
    }
}
