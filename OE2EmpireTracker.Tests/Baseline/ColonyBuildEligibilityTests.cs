using NUnit.Framework;
using OE2EmpireTracker.Baseline;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;
using System;
using System.IO;
using System.Linq;

namespace OE2EmpireTracker.Tests.Baseline
{
    [TestFixture]
    public class ColonyBuildEligibilityTests
    {
        private PlayerContext playerContext;

        [SetUp]
        public void SetUp()
        {
            PlayerContext.Reset();
            EmpireContext.FilePath = Path.Combine(TestContext.CurrentContext.TestDirectory,
                @"..\..\..\..\OE2EmpireTracker\BaselineData.json");
            PlayerContext.FilePath = "nonexistent_player_data.json";
            playerContext = PlayerContext.getInstance();
        }

        // -----------------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------------

        private static ColonyStructure MakeStructure(bool staged, bool built,
            CountDownTime buildCompletionTime = null)
        {
            var s = new ColonyStructure();
            s.UUID = Guid.NewGuid().ToString();
            s.Properties.setProperty(GameConstants.PropStaged, staged);
            s.Properties.setProperty(GameConstants.PropBuilt, built);
            s.BuildCompletionTime = buildCompletionTime;
            return s;
        }

        private static CountDownTime MakeExpiredTimer()
        {
            var t = new CountDownTime();
            t.TimeRemaining = -10; // already expired
            return t;
        }

        private static CountDownTime MakeActiveTimer()
        {
            var t = new CountDownTime();
            t.TimeRemaining = 3600; // 1 hour remaining
            return t;
        }

        // -----------------------------------------------------------------------
        // Property 2: Structure state predicates are mutually consistent
        // Feature: colony-daily-build, Property 2
        // **Validates: Requirements 3.2, 3.3**
        // -----------------------------------------------------------------------

        [Test]
        public void Property2_StructureStatePredicates_MutuallyConsistent()
        {
            bool[] boolValues = { true, false };
            CountDownTime[] timers = { null, MakeExpiredTimer(), MakeActiveTimer() };

            foreach (bool staged in boolValues)
            {
                foreach (bool built in boolValues)
                {
                    foreach (var timer in timers)
                    {
                        var structure = MakeStructure(staged, built, timer);

                        bool isStaged = ColonyBuildEligibility.IsStagedStructure(structure, playerContext);
                        bool isBuilding = ColonyBuildEligibility.IsBuildingStructure(structure, playerContext);

                        // IsStagedStructure: true iff IsStaged=true AND IsBuilt=false
                        bool expectedStaged = staged && !built;
                        Assert.That(isStaged, Is.EqualTo(expectedStaged),
                    $"IsStagedStructure wrong for staged={staged}, built={built}, timer={TimerDesc(timer)}");

                        // IsBuildingStructure: true iff IsStaged=false AND IsBuilt=false
                        //   AND BuildCompletionTime != null AND TimeRemaining > 0
                        bool expectedBuilding = !staged && !built
                            && timer != null && timer.TimeRemaining > 0;
                        Assert.That(isBuilding, Is.EqualTo(expectedBuilding),
                    $"IsBuildingStructure wrong for staged={staged}, built={built}, timer={TimerDesc(timer)}");

                        // Never both true simultaneously
                        Assert.That(isStaged && isBuilding, Is.False,
                    $"Structure cannot be both staged and building: staged={staged}, built={built}, timer={TimerDesc(timer)}");
                    }
                }
            }
        }

        private static string TimerDesc(CountDownTime t)
        {
            if (t == null) return "null";
            return t.TimeRemaining > 0 ? "active" : "expired";
        }

        // -----------------------------------------------------------------------
        // Property 3: Colony eligibility filtering
        // Feature: colony-daily-build, Property 3
        // **Validates: Requirements 3.1, 3.4, 3.5, 8.1, 8.2**
        // -----------------------------------------------------------------------

        [Test]
        public void Property3_ColonyEligibility_NoStructures_NotEligible()
        {
            var colony = new Colony();
            colony.UUID = Guid.NewGuid().ToString();
            Assert.That(ColonyBuildEligibility.IsEligible(colony, playerContext), Is.False);
        }

        [Test]
        public void Property3_ColonyEligibility_OnlyBuiltStructures_NotEligible()
        {
            var colony = new Colony();
            colony.UUID = Guid.NewGuid().ToString();
            colony.Structures.Add(MakeStructure(false, true));
            colony.Structures.Add(MakeStructure(false, true));
            Assert.That(ColonyBuildEligibility.IsEligible(colony, playerContext), Is.False);
        }

        [Test]
        public void Property3_ColonyEligibility_OneStagedNoBuilding_Eligible()
        {
            var colony = new Colony();
            colony.UUID = Guid.NewGuid().ToString();
            colony.Structures.Add(MakeStructure(true, false));
            colony.Structures.Add(MakeStructure(false, true));
            Assert.That(ColonyBuildEligibility.IsEligible(colony, playerContext), Is.True);
        }

        [Test]
        public void Property3_ColonyEligibility_StagedAndBuilding_NotEligible()
        {
            var colony = new Colony();
            colony.UUID = Guid.NewGuid().ToString();
            colony.Structures.Add(MakeStructure(true, false)); // staged
            colony.Structures.Add(MakeStructure(false, false, MakeActiveTimer())); // building
            Assert.That(ColonyBuildEligibility.IsEligible(colony, playerContext), Is.False);
        }

        [Test]
        public void Property3_ColonyEligibility_MultipleStagedNoBuilding_Eligible()
        {
            var colony = new Colony();
            colony.UUID = Guid.NewGuid().ToString();
            colony.Structures.Add(MakeStructure(true, false));
            colony.Structures.Add(MakeStructure(true, false));
            colony.Structures.Add(MakeStructure(false, true));
            Assert.That(ColonyBuildEligibility.IsEligible(colony, playerContext), Is.True);
        }

        [Test]
        public void Property3_ColonyEligibility_NoStagedWithBuilding_NotEligible()
        {
            var colony = new Colony();
            colony.UUID = Guid.NewGuid().ToString();
            colony.Structures.Add(MakeStructure(false, false, MakeActiveTimer())); // building
            colony.Structures.Add(MakeStructure(false, true)); // built
            Assert.That(ColonyBuildEligibility.IsEligible(colony, playerContext), Is.False);
        }

        [Test]
        public void Property3_ColonyEligibility_ExhaustiveCombinations()
        {
            // Generate colonies with 1-4 structures in various state combinations
            // States: staged, building, built, online (built+online treated same as built for eligibility)
            var states = new[]
            {
                new { Staged = true,  Built = false, Timer = (CountDownTime)null },           // staged
                new { Staged = false, Built = false, Timer = MakeActiveTimer() },              // building
                new { Staged = false, Built = true,  Timer = (CountDownTime)null },            // built
                new { Staged = false, Built = false, Timer = MakeExpiredTimer() },             // expired build
            };

            // Test all pairs of two structures
            for (int i = 0; i < states.Length; i++)
            {
                for (int j = 0; j < states.Length; j++)
                {
                    var colony = new Colony();
                    colony.UUID = Guid.NewGuid().ToString();
                    colony.Structures.Add(MakeStructure(states[i].Staged, states[i].Built, states[i].Timer));
                    colony.Structures.Add(MakeStructure(states[j].Staged, states[j].Built, states[j].Timer));

                    bool hasStaged = states[i].Staged || states[j].Staged;
                    bool hasBuilding = (states[i].Timer != null && states[i].Timer.TimeRemaining > 0 && !states[i].Staged && !states[i].Built)
                                    || (states[j].Timer != null && states[j].Timer.TimeRemaining > 0 && !states[j].Staged && !states[j].Built);
                    bool expected = hasStaged && !hasBuilding;

                    Assert.That(ColonyBuildEligibility.IsEligible(colony, playerContext), Is.EqualTo(expected),
                    $"Eligibility wrong for states[{i}]+states[{j}]");
                }
            }
        }

        // -----------------------------------------------------------------------
        // Property 4: First staged structure selection
        // Feature: colony-daily-build, Property 4
        // **Validates: Requirements 4.2**
        // -----------------------------------------------------------------------

        [Test]
        public void Property4_GetFirstStagedStructure_ReturnsLowestIndex()
        {
            var colony = new Colony();
            colony.UUID = Guid.NewGuid().ToString();

            var built1 = MakeStructure(false, true);
            var staged1 = MakeStructure(true, false);
            var staged2 = MakeStructure(true, false);
            var built2 = MakeStructure(false, true);

            colony.Structures.Add(built1);
            colony.Structures.Add(staged1);
            colony.Structures.Add(staged2);
            colony.Structures.Add(built2);

            var result = ColonyBuildEligibility.GetFirstStagedStructure(colony, playerContext);
            Assert.That(result, Is.SameAs(staged1));
        }

        [Test]
        public void Property4_GetFirstStagedStructure_NoStaged_ReturnsNull()
        {
            var colony = new Colony();
            colony.UUID = Guid.NewGuid().ToString();
            colony.Structures.Add(MakeStructure(false, true));
            colony.Structures.Add(MakeStructure(false, true));

            var result = ColonyBuildEligibility.GetFirstStagedStructure(colony, playerContext);
            Assert.That(result, Is.Null);
        }

        [Test]
        public void Property4_GetFirstStagedStructure_EmptyColony_ReturnsNull()
        {
            var colony = new Colony();
            colony.UUID = Guid.NewGuid().ToString();

            var result = ColonyBuildEligibility.GetFirstStagedStructure(colony, playerContext);
            Assert.That(result, Is.Null);
        }

        [Test]
        public void Property4_GetFirstStagedStructure_VariousPositions()
        {
            // Test that for each position 0-4, if that's the first staged structure,
            // GetFirstStagedStructure returns it
            for (int stagedPos = 0; stagedPos < 5; stagedPos++)
            {
                var colony = new Colony();
                colony.UUID = Guid.NewGuid().ToString();

                ColonyStructure expectedFirst = null;
                for (int i = 0; i < 5; i++)
                {
                    if (i == stagedPos)
                    {
                        var staged = MakeStructure(true, false);
                        colony.Structures.Add(staged);
                        if (expectedFirst == null) expectedFirst = staged;
                    }
                    else
                    {
                        colony.Structures.Add(MakeStructure(false, true));
                    }
                }

                var result = ColonyBuildEligibility.GetFirstStagedStructure(colony, playerContext);
                Assert.That(result, Is.SameAs(expectedFirst),
                    $"Expected first staged at position {stagedPos}");
            }
        }
    }
}
