using NUnit.Framework;
using OE2EmpireTracker.Services;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OE2EmpireTracker.Tests.Baseline
{
    [TestFixture]
    public class ColonyBuildCompletionTests
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
            t.TimeRemaining = 0;
            // Force it to be expired by setting EndTime in the past
            t.EndTime = DateTime.Now.AddSeconds(-10);
            return t;
        }

        private static CountDownTime MakeActiveTimer(long secondsRemaining = 3600)
        {
            var t = new CountDownTime();
            t.TimeRemaining = secondsRemaining;
            return t;
        }

        // -----------------------------------------------------------------------
        // Property 6: Build completion in ProcessColony
        // Feature: colony-daily-build, Property 6
        // **Validates: Requirements 7.1, 7.2**
        // -----------------------------------------------------------------------

        [Test]
        public void Property6_ExpiredBuildTimers_SetBuiltAndClearTimer()
        {
            // Generate colonies with various combinations of expired and active timers
            int[] expiredCounts = { 1, 2, 3 };
            int[] activeCounts = { 0, 1, 2 };

            foreach (int numExpired in expiredCounts)
            {
                foreach (int numActive in activeCounts)
                {
                    var colony = new Colony();
                    colony.UUID = Guid.NewGuid().ToString();

                    var expiredStructures = new List<ColonyStructure>();
                    var activeStructures = new List<ColonyStructure>();

                    for (int i = 0; i < numExpired; i++)
                    {
                        var s = MakeStructure(false, false, MakeExpiredTimer());
                        colony.Structures.Add(s);
                        expiredStructures.Add(s);
                    }

                    for (int i = 0; i < numActive; i++)
                    {
                        var s = MakeStructure(false, false, MakeActiveTimer());
                        colony.Structures.Add(s);
                        activeStructures.Add(s);
                    }

                    colony.ProcessColony();

                    // Expired timers: Built=true, BuildCompletionTime=null
                    foreach (var s in expiredStructures)
                    {
                        var vm = new ColonyStructureViewModel(s, playerContext);
                        Assert.That(vm.IsBuilt, Is.True,
                    $"Expired structure should be Built (expired={numExpired}, active={numActive})");
                        Assert.That(s.BuildCompletionTime, Is.Null,
                            $"Expired structure BuildCompletionTime should be null");
                    }

                    // Active timers: unchanged
                    foreach (var s in activeStructures)
                    {
                        var vm = new ColonyStructureViewModel(s, playerContext);
                        Assert.That(vm.IsBuilt, Is.False,
                    $"Active structure should NOT be Built");
                        Assert.That(s.BuildCompletionTime, Is.Not.Null,
                    $"Active structure BuildCompletionTime should remain");
                        Assert.That(s.BuildCompletionTime.TimeRemaining, Is.GreaterThan(0),
                    $"Active structure should still have time remaining");
                    }
                }
            }
        }

        // -----------------------------------------------------------------------
        // Unit tests — edge cases (Task 2.3)
        // -----------------------------------------------------------------------

        [Test]
        public void ProcessColony_ExpiredBuildCompletionTime_SetsBuiltTrue()
        {
            var colony = new Colony();
            colony.UUID = Guid.NewGuid().ToString();
            var structure = MakeStructure(false, false, MakeExpiredTimer());
            colony.Structures.Add(structure);

            colony.ProcessColony();

            var vm = new ColonyStructureViewModel(structure, playerContext);
            Assert.That(vm.IsBuilt, Is.True);
            Assert.That(structure.BuildCompletionTime, Is.Null);
        }

        [Test]
        public void ProcessColony_ActiveBuildCompletionTime_Unchanged()
        {
            var colony = new Colony();
            colony.UUID = Guid.NewGuid().ToString();
            var structure = MakeStructure(false, false, MakeActiveTimer());
            colony.Structures.Add(structure);

            colony.ProcessColony();

            var vm = new ColonyStructureViewModel(structure, playerContext);
            Assert.That(vm.IsBuilt, Is.False);
            Assert.That(structure.BuildCompletionTime, Is.Not.Null);
        }

        [Test]
        public void ProcessColony_NoStructures_DoesNotThrow()
        {
            var colony = new Colony();
            colony.UUID = Guid.NewGuid().ToString();

            Assert.DoesNotThrow(() => colony.ProcessColony());
        }

        [Test]
        public void ProcessColony_BuildCompletionRunsBeforeMining()
        {
            // A structure with an expired build timer should be marked Built
            // before the mining/refining loop runs. This means a newly-built
            // mining rig with a ProcessCompletionTime could process in the same call.
            var colony = new Colony();
            colony.UUID = Guid.NewGuid().ToString();

            // Create a mining rig blueprint
            var bp = new OE2EmpireTracker.Models.Blueprint("TestMiningRig");
            bp.UUID = Guid.NewGuid().ToString();
            bp.BluePrintType = BlueprintTypes.MiningRig;
            playerContext.blueprintList.Add(bp);

            // Create a survey
            var survey = new Survey();
            survey.UUID = Guid.NewGuid().ToString();
            survey.PlanetName = "TestPlanet";
            survey.SurveyID = "S1";
            survey.Resources = new Dictionary<string, SurveyResource>
            {
                { "TestOre", new SurveyResource { Resource = "TestOre", Purity = "Low", Amount = "50" } }
            };
            playerContext.surveyList.Add(survey);

            var structure = new ColonyStructure();
            structure.UUID = Guid.NewGuid().ToString();
            structure.FlatpackBlueprintUUID = bp.UUID;
            structure.MiningSurvey = survey.UUID;
            structure.MiningSurveyResource = "TestOre";
            // Start as building with expired timer
            structure.Properties.setProperty(GameConstants.PropBuilt, false);
            structure.Properties.setProperty(GameConstants.PropStaged, false);
            structure.BuildCompletionTime = MakeExpiredTimer();

            // Also set up a process timer with 1 interval passed
            var processTimer = new CountDownTime();
            processTimer.StartRepeating(3600);
            processTimer.StartTime = DateTime.Now.AddSeconds(-3600);
            structure.ProcessCompletionTime = processTimer;

            colony.Structures.Add(structure);
            colony.ProcessColony();

            // Build completion should have run first, setting Built=true
            var vm = new ColonyStructureViewModel(structure, playerContext);
            Assert.That(vm.IsBuilt, Is.True,
                    "Structure should be marked as built");
            Assert.That(structure.BuildCompletionTime, Is.Null, "BuildCompletionTime should be cleared");

            // Mining should have processed (the structure is now built)
            var items = colony.Items.FindResource("TestOre", "Low");
            Assert.That(items.Count, Is.EqualTo(1),
                    "Mining should have produced ore");
            Assert.That(items[0].Quantity, Is.GreaterThan(0),
                    "Mined quantity should be > 0");
        }
    }
}
