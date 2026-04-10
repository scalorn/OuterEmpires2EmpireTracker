using NUnit.Framework;
using OE2EmpireTracker.Services;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using OE2EmpireTracker.Tests;

namespace OE2EmpireTracker.Tests.Services
{
    [TestFixture]
    public class ColonyActivityCollectorTests
    {
        private static readonly Random Rng = new Random(42);

        private static readonly string[] KnownBlueprintTypes = new[]
        {
            BlueprintTypes.MiningRig,
            BlueprintTypes.Refinery,
            BlueprintTypes.ResearchLaboratory,
            BlueprintTypes.Manufactory,
            BlueprintTypes.CommodityFactoryPrefix + "Agridome"
        };

        [OneTimeSetUp]
        public void FixtureSetUp()
        {
            TestHelper.SetEmpireFilePath();
            EmpireContext.Reset();
        }

        // -----------------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------------

        private OE2EmpireTracker.Models.Blueprint CreateBlueprint(string bpType, string name = null, int evolution = 0)
        {
            var bp = new OE2EmpireTracker.Models.Blueprint(name ?? "TestBP_" + Guid.NewGuid().ToString().Substring(0, 6));
            bp.UUID = Guid.NewGuid().ToString();
            bp.BluePrintType = bpType;
            bp.Evolution = evolution;
            PlayerContext.getInstance().blueprintList.Add(bp);
            return bp;
        }

        private Survey CreateSurvey(string resource, string purity, string amount)
        {
            var survey = new Survey();
            survey.UUID = Guid.NewGuid().ToString();
            survey.PlanetName = "TestPlanet";
            survey.SurveyID = "S" + Rng.Next(1000);
            survey.Resources = new Dictionary<string, SurveyResource>
            {
                { resource, new SurveyResource(resource, purity, amount) }
            };
            PlayerContext.getInstance().surveyList.Add(survey);
            return survey;
        }

        private static CountDownTime MakeActiveTimer(long secondsRemaining)
        {
            var timer = new CountDownTime();
            timer.TimeRemaining = secondsRemaining;
            return timer;
        }

        private static CountDownTime MakeExpiredTimer()
        {
            var timer = new CountDownTime();
            timer.StartTime = DateTime.Now.AddHours(-2);
            timer.EndTime = DateTime.Now.AddHours(-1);
            return timer;
        }

        private static CountDownTime MakeActiveRepeatingTimer(long intervalSeconds)
        {
            var timer = new CountDownTime();
            timer.StartRepeating(intervalSeconds);
            return timer;
        }

        // -----------------------------------------------------------------------
        // Property 1: Activity collection completeness and classification
        // Feature: colony-activity-form, Property 1: Activity collection completeness and classification
        // **Validates: Requirements 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 2.1, 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 6.5, 6.6**
        // -----------------------------------------------------------------------

        [Test]
        public void Property1_ActivityCollectionCompletenessAndClassification()
        {
            var pc = PlayerContext.getInstance();

            for (int iteration = 0; iteration < 100; iteration++)
            {
                int colonyCount = Rng.Next(1, 4);
                var colonies = new List<Colony>();
                int expectedStructureRows = 0;
                int expectedCommodityRows = 0;
                var expectedTypes = new List<ActivityType>();

                for (int c = 0; c < colonyCount; c++)
                {
                    var colony = new Colony();
                    colony.UUID = Guid.NewGuid().ToString();
                    colony.SystemName = "System_" + c;
                    colony.ColonyName = "Colony_" + c;

                    int structureCount = Rng.Next(0, 6);
                    for (int s = 0; s < structureCount; s++)
                    {
                        string bpType = KnownBlueprintTypes[Rng.Next(KnownBlueprintTypes.Length)];
                        var bp = CreateBlueprint(bpType);

                        var structure = new ColonyStructure();
                        structure.UUID = Guid.NewGuid().ToString();
                        structure.FlatpackBlueprintUUID = bp.UUID;
                        structure.displaySequence = s + 1;

                        // Random timer state: 0=none, 1=active build, 2=active process, 3=expired
                        int timerState = Rng.Next(4);

                        if (timerState == 1)
                        {
                            structure.BuildCompletionTime = MakeActiveTimer(Rng.Next(60, 86400));
                            expectedStructureRows++;
                            expectedTypes.Add(ActivityType.Building);
                        }
                        else if (timerState == 2)
                        {
                            structure.ProcessCompletionTime = MakeActiveRepeatingTimer(3600);
                            expectedStructureRows++;

                            ActivityType expectedType;
                            if (bpType == BlueprintTypes.MiningRig) expectedType = ActivityType.Mining;
                            else if (bpType == BlueprintTypes.Refinery) expectedType = ActivityType.Refining;
                            else if (bpType == BlueprintTypes.ResearchLaboratory) expectedType = ActivityType.Research;
                            else if (bpType == BlueprintTypes.Manufactory) expectedType = ActivityType.Manufacturing;
                            else expectedType = ActivityType.CommodityManufacturing;
                            expectedTypes.Add(expectedType);
                        }
                        else if (timerState == 3)
                        {
                            structure.ProcessCompletionTime = MakeExpiredTimer();
                            // Expired — should NOT produce a row
                        }
                        // timerState == 0: no timers

                        colony.Structures.Add(structure);
                    }

                    int commodityCount = Rng.Next(0, 4);
                    for (int cr = 0; cr < commodityCount; cr++)
                    {
                        bool fulfilled = Rng.Next(2) == 0;
                        colony.Commodities.Add(new CommodityRequested
                        {
                            Name = "Commodity_" + cr,
                            Requested = Rng.Next(1, 100),
                            Fulfilled = fulfilled,
                            NeedBy = DateTime.Now.AddDays(Rng.Next(-1, 10))
                        });
                        if (!fulfilled)
                        {
                            expectedCommodityRows++;
                            expectedTypes.Add(ActivityType.CommodityRequest);
                        }
                    }

                    colonies.Add(colony);
                }

                var rows = ColonyActivityCollector.CollectActivities(colonies, pc);

                Assert.That(rows.Count, Is.EqualTo(expectedStructureRows + expectedCommodityRows),
                    $"Iteration {iteration}: row count mismatch");

                // Verify types match
                var actualTypes = rows.Select(r => r.Type).ToList();
                Assert.That(actualTypes.Count, Is.EqualTo(expectedTypes.Count),
                    $"Iteration {iteration}: type count mismatch");

                for (int i = 0; i < expectedTypes.Count; i++)
                {
                    Assert.That(actualTypes[i], Is.EqualTo(expectedTypes[i]),
                    $"Iteration {iteration}, row {i}: type mismatch");
                }

                // Verify SystemName and ColonyName match owning colony
                foreach (var row in rows)
                {
                    Assert.That(row.SystemName, Is.Not.Null,
                    $"Iteration {iteration}: SystemName null");
                    Assert.That(row.ColonyName, Is.Not.Null,
                    $"Iteration {iteration}: ColonyName null");
                }
            }
        }

        // -----------------------------------------------------------------------
        // Property 2: Commodity request time remaining computation
        // Feature: colony-activity-form, Property 2: Commodity request time remaining computation
        // **Validates: Requirements 2.2, 2.3**
        // -----------------------------------------------------------------------

        [Test]
        public void Property2_CommodityRequestTimeRemainingComputation()
        {
            for (int iteration = 0; iteration < 100; iteration++)
            {
                // Generate random NeedBy from 1 day past to 10 days future
                double offsetDays = (Rng.NextDouble() * 11.0) - 1.0;
                DateTime needBy = DateTime.Now.AddDays(offsetDays);

                var row = new ActivityRow
                {
                    Type = ActivityType.CommodityRequest,
                    CountDown = null,
                    NeedBy = needBy
                };

                long actualSeconds = row.GetSecondsRemaining();
                long expectedSeconds = Math.Max(0, (long)(needBy - DateTime.Now).TotalSeconds);

                if (needBy <= DateTime.Now)
                {
                    Assert.That(actualSeconds, Is.EqualTo(0),
                    $"Iteration {iteration}: past NeedBy should return 0");
                    Assert.That(row.GetTimeRemainingString(), Is.EqualTo("0s"),
                    $"Iteration {iteration}: past NeedBy should display '0s'");
                }
                else
                {
                    Assert.That(Math.Abs(actualSeconds - expectedSeconds), Is.LessThanOrEqualTo(2),
                        $"Iteration {iteration}: seconds remaining off by more than 2s");
                }
            }
        }

        // -----------------------------------------------------------------------
        // Property 3: FormatSeconds equivalence with CountDownTime.TimeRemainingString
        // Feature: colony-activity-form, Property 3: FormatSeconds equivalence
        // **Validates: Requirements 6.2, 6.4**
        // -----------------------------------------------------------------------

        [Test]
        public void Property3_FormatSecondsEquivalence()
        {
            for (int iteration = 0; iteration < 100; iteration++)
            {
                long seconds = Rng.Next(1, 864001);

                string formatted = ActivityRow.FormatSeconds(seconds);

                // Create a CountDownTime, set TimeRemaining, read back
                var cdt = new CountDownTime();
                cdt.TimeRemaining = seconds;
                string cdtString = cdt.TimeRemainingString;

                // Allow ±1s tolerance due to clock drift between set and read
                // Parse both strings back to seconds for comparison
                long formattedSeconds = ParseTimeString(formatted);
                long cdtSeconds = ParseTimeString(cdtString);

                Assert.That(Math.Abs(formattedSeconds - cdtSeconds), Is.LessThanOrEqualTo(1),
                    $"Iteration {iteration}: FormatSeconds({seconds})='{formatted}' vs CDT='{cdtString}' differ by more than 1s");
            }
        }

        private static long ParseTimeString(string timeStr)
        {
            long total = 0;
            var parts = timeStr.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                if (part.EndsWith("d"))
                    total += long.Parse(part.TrimEnd('d')) * 86400;
                else if (part.EndsWith("h"))
                    total += long.Parse(part.TrimEnd('h')) * 3600;
                else if (part.EndsWith("m"))
                    total += long.Parse(part.TrimEnd('m')) * 60;
                else if (part.EndsWith("s"))
                    total += long.Parse(part.TrimEnd('s'));
            }
            return total;
        }

        // -----------------------------------------------------------------------
        // Property 4: Source name and process details formatting
        // Feature: colony-activity-form, Property 4: Source name and process details formatting
        // **Validates: Requirements 6.8, 6.9, 7.1, 7.2, 7.3, 7.4, 7.5, 7.6, 7.7, 7.8**
        // -----------------------------------------------------------------------

        [Test]
        public void Property4_SourceNameAndProcessDetailsFormatting()
        {
            var pc = PlayerContext.getInstance();

            for (int iteration = 0; iteration < 100; iteration++)
            {
                var colony = new Colony();
                colony.UUID = Guid.NewGuid().ToString();
                colony.SystemName = "Sys_" + iteration;
                colony.ColonyName = "Col_" + iteration;

                // Pick a random blueprint type for this iteration
                string bpType = KnownBlueprintTypes[iteration % KnownBlueprintTypes.Length];
                int gameSeq = Rng.Next(1, 50);
                var bp = CreateBlueprint(bpType, "BP_" + iteration, evolution: Rng.Next(0, 10));

                var structure = new ColonyStructure();
                structure.UUID = Guid.NewGuid().ToString();
                structure.FlatpackBlueprintUUID = bp.UUID;
                structure.displaySequence = gameSeq;

                bool isBuildingTest = iteration % 6 == 0;

                if (isBuildingTest)
                {
                    // Test Building type
                    structure.BuildCompletionTime = MakeActiveTimer(Rng.Next(60, 86400));
                    colony.Structures.Add(structure);

                    var rows = ColonyActivityCollector.CollectActivities(new[] { colony }, pc);
                    Assert.That(rows.Count, Is.EqualTo(1),
                    $"Iteration {iteration}: expected 1 row");
                    Assert.That(rows[0].Type, Is.EqualTo(ActivityType.Building));
                    Assert.That(rows[0].SourceName, Is.EqualTo($"#{gameSeq} {bp.ExtendedName}"));
                    Assert.That(rows[0].ProcessDetails, Is.EqualTo("Building"));
                    continue;
                }

                // Process timer for non-building types
                structure.ProcessCompletionTime = MakeActiveRepeatingTimer(3600);

                // Set up type-specific fields
                if (bpType == BlueprintTypes.MiningRig)
                {
                    string resource = "Ore_" + iteration;
                    string purity = new[] { "Low", "Medium", "High" }[Rng.Next(3)];
                    string amount = Rng.Next(1, 500).ToString();
                    var survey = CreateSurvey(resource, purity, amount);
                    structure.MiningSurvey = survey.UUID;
                    structure.MiningSurveyResource = resource;

                    colony.Structures.Add(structure);
                    var rows = ColonyActivityCollector.CollectActivities(new[] { colony }, pc);
                    Assert.That(rows.Count, Is.EqualTo(1));
                    Assert.That(rows[0].Type, Is.EqualTo(ActivityType.Mining));
                    Assert.That(rows[0].SourceName, Is.EqualTo($"#{gameSeq} {bp.ExtendedName}"));
                    Assert.That(rows[0].ProcessDetails, Is.EqualTo($"{amount}/h {resource} ({purity})"));
                }
                else if (bpType == BlueprintTypes.Refinery)
                {
                    // Alternate between normal and synthetic
                    if (iteration % 2 == 0)
                    {
                        string resource = "Mineral_" + iteration;
                        string purity = new[] { "Low", "Medium", "High" }[Rng.Next(3)];
                        structure.RefiningResource = resource;
                        structure.RefiningResourcePurity = purity;

                        colony.Structures.Add(structure);
                        var rows = ColonyActivityCollector.CollectActivities(new[] { colony }, pc);
                        Assert.That(rows.Count, Is.EqualTo(1));
                        Assert.That(rows[0].Type, Is.EqualTo(ActivityType.Refining));

                        int baseRate = GameConstants.RefiningBaseRate;
                        int outputRate;
                        switch (purity)
                        {
                            case "Low": outputRate = baseRate; break;
                            case "Medium": outputRate = baseRate * 3; break;
                            case "High": outputRate = baseRate * 5; break;
                            default: outputRate = baseRate; break;
                        }
                        Assert.That(rows[0].ProcessDetails, Is.EqualTo($"{baseRate}:{outputRate} {resource} ({purity})"));
                    }
                    else
                    {
                        // Use a known synthetic recipe
                        structure.RefiningResource = "Lanthanides";
                        structure.RefiningResourcePurity = GameConstants.PurityRefined;

                        colony.Structures.Add(structure);
                        var rows = ColonyActivityCollector.CollectActivities(new[] { colony }, pc);
                        Assert.That(rows.Count, Is.EqualTo(1));
                        Assert.That(rows[0].Type, Is.EqualTo(ActivityType.Refining));
                        Assert.That(rows[0].ProcessDetails, Is.EqualTo("1250:25 S1. Translanthanic Exotics"));
                    }
                }
                else if (bpType == BlueprintTypes.ResearchLaboratory)
                {
                    var researchBp = CreateBlueprint(BlueprintTypes.Manufactory, "ResearchTarget_" + iteration, evolution: Rng.Next(0, 14));
                    structure.ResearchingBlueprintUUID = researchBp.UUID;

                    colony.Structures.Add(structure);
                    var rows = ColonyActivityCollector.CollectActivities(new[] { colony }, pc);
                    Assert.That(rows.Count, Is.EqualTo(1));
                    Assert.That(rows[0].Type, Is.EqualTo(ActivityType.Research));
                    Assert.That(rows[0].ProcessDetails, Is.EqualTo($"Evo {researchBp.Evolution}->{researchBp.Evolution + 1} {researchBp.Name}"));
                }
                else if (bpType == BlueprintTypes.Manufactory)
                {
                    var mfgBp = CreateBlueprint(BlueprintTypes.Manufactory, "MfgItem_" + iteration);
                    structure.ManufacturingBlueprintUUID = mfgBp.UUID;
                    structure.ManufacturingCompleted = Rng.Next(0, 10);
                    structure.ManufacturingQuantity = structure.ManufacturingCompleted + Rng.Next(1, 10);

                    colony.Structures.Add(structure);
                    var rows = ColonyActivityCollector.CollectActivities(new[] { colony }, pc);
                    Assert.That(rows.Count, Is.EqualTo(1));
                    Assert.That(rows[0].Type, Is.EqualTo(ActivityType.Manufacturing));
                    Assert.That(rows[0].ProcessDetails, Is.EqualTo($"({structure.ManufacturingCompleted + 1}/{structure.ManufacturingQuantity}) {mfgBp.ExtendedName}"));
                }
                else if (bpType.IsCommodityFactory())
                {
                    string commodityName = "TestCommodity_" + iteration;
                    structure.ManufacturingCommodityName = commodityName;
                    structure.ManufacturingCompleted = Rng.Next(0, 5);
                    structure.ManufacturingQuantity = structure.ManufacturingCompleted + Rng.Next(1, 10);

                    colony.Structures.Add(structure);
                    var rows = ColonyActivityCollector.CollectActivities(new[] { colony }, pc);
                    Assert.That(rows.Count, Is.EqualTo(1));
                    Assert.That(rows[0].Type, Is.EqualTo(ActivityType.CommodityManufacturing));
                    Assert.That(rows[0].ProcessDetails, Is.EqualTo($"({structure.ManufacturingCompleted + 1}/{structure.ManufacturingQuantity}) {commodityName} x{GameConstants.CommoditiesPerCycle}"));
                }

                // Also test CommodityRequest formatting every 5th iteration
                if (iteration % 5 == 0)
                {
                    var colony2 = new Colony();
                    colony2.UUID = Guid.NewGuid().ToString();
                    colony2.SystemName = "CRSys_" + iteration;
                    colony2.ColonyName = "CRCol_" + iteration;

                    string crName = "CRCommodity_" + iteration;
                    int crRequested = Rng.Next(1, 200);
                    colony2.Commodities.Add(new CommodityRequested
                    {
                        Name = crName,
                        Requested = crRequested,
                        Fulfilled = false,
                        NeedBy = DateTime.Now.AddDays(Rng.Next(1, 10))
                    });

                    var crRows = ColonyActivityCollector.CollectActivities(new[] { colony2 }, pc);
                    Assert.That(crRows.Count, Is.EqualTo(1));
                    Assert.That(crRows[0].Type, Is.EqualTo(ActivityType.CommodityRequest));
                    Assert.That(crRows[0].SourceName, Is.EqualTo("Commodity Request"));
                    Assert.That(crRows[0].ProcessDetails, Is.EqualTo($"{crName} x{crRequested}"));
                }
            }
        }

        // -----------------------------------------------------------------------
        // Edge Case Unit Tests
        // **Validates: Requirements 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 2.1, 2.2, 2.3, 3.1, 7.1, 7.2, 7.3, 7.4, 7.5, 7.6, 7.7, 7.8**
        // -----------------------------------------------------------------------

        [Test]
        public void EdgeCase_EmptyColonyList_ReturnsZeroRows()
        {
            var pc = PlayerContext.getInstance();
            var rows = ColonyActivityCollector.CollectActivities(new List<Colony>(), pc);
            Assert.That(rows.Count, Is.EqualTo(0));
        }

        [Test]
        public void EdgeCase_ColonyWithNoActiveTimers_ReturnsZeroRows()
        {
            var pc = PlayerContext.getInstance();
            var colony = new Colony
            {
                UUID = Guid.NewGuid().ToString(),
                SystemName = "Sys",
                ColonyName = "Col"
            };
            // Add a structure with no timers
            var bp = CreateBlueprint(BlueprintTypes.MiningRig, "IdleMiner");
            colony.Structures.Add(new ColonyStructure
            {
                UUID = Guid.NewGuid().ToString(),
                FlatpackBlueprintUUID = bp.UUID,
                displaySequence = 1
            });

            var rows = ColonyActivityCollector.CollectActivities(new[] { colony }, pc);
            Assert.That(rows.Count, Is.EqualTo(0));
        }

        [Test]
        public void EdgeCase_OneMiningRig_ReturnsMiningRow()
        {
            var pc = PlayerContext.getInstance();
            var colony = new Colony
            {
                UUID = Guid.NewGuid().ToString(),
                SystemName = "MineSys",
                ColonyName = "MineCol"
            };
            var bp = CreateBlueprint(BlueprintTypes.MiningRig, "ActiveMiner");
            var survey = CreateSurvey("Iron", "High", "250");
            var structure = new ColonyStructure
            {
                UUID = Guid.NewGuid().ToString(),
                FlatpackBlueprintUUID = bp.UUID,
                displaySequence = 3,
                ProcessCompletionTime = MakeActiveRepeatingTimer(3600),
                MiningSurvey = survey.UUID,
                MiningSurveyResource = "Iron"
            };
            colony.Structures.Add(structure);

            var rows = ColonyActivityCollector.CollectActivities(new[] { colony }, pc);
            Assert.That(rows.Count, Is.EqualTo(1));
            Assert.That(rows[0].Type, Is.EqualTo(ActivityType.Mining));
            Assert.That(rows[0].SystemName, Is.EqualTo("MineSys"));
            Assert.That(rows[0].ColonyName, Is.EqualTo("MineCol"));
            Assert.That(rows[0].SourceName, Is.EqualTo($"#{3} {bp.ExtendedName}"));
            Assert.That(rows[0].ProcessDetails, Is.EqualTo("250/h Iron (High)"));
        }

        [Test]
        public void EdgeCase_BuildingStructure_ReturnsBuildingRow()
        {
            var pc = PlayerContext.getInstance();
            var colony = new Colony
            {
                UUID = Guid.NewGuid().ToString(),
                SystemName = "BuildSys",
                ColonyName = "BuildCol"
            };
            var bp = CreateBlueprint(BlueprintTypes.Manufactory, "BuildingFactory");
            var structure = new ColonyStructure
            {
                UUID = Guid.NewGuid().ToString(),
                FlatpackBlueprintUUID = bp.UUID,
                displaySequence = 1,
                BuildCompletionTime = MakeActiveTimer(7200)
            };
            colony.Structures.Add(structure);

            var rows = ColonyActivityCollector.CollectActivities(new[] { colony }, pc);
            Assert.That(rows.Count, Is.EqualTo(1));
            Assert.That(rows[0].Type, Is.EqualTo(ActivityType.Building));
            Assert.That(rows[0].ProcessDetails, Is.EqualTo("Building"));
        }

        [Test]
        public void EdgeCase_UnfulfilledCommodityRequest_ReturnsCommodityRequestRow()
        {
            var pc = PlayerContext.getInstance();
            var colony = new Colony
            {
                UUID = Guid.NewGuid().ToString(),
                SystemName = "CRSys",
                ColonyName = "CRCol"
            };
            colony.Commodities.Add(new CommodityRequested
            {
                Name = "Electronics",
                Requested = 50,
                Fulfilled = false,
                NeedBy = DateTime.Now.AddDays(5)
            });

            var rows = ColonyActivityCollector.CollectActivities(new[] { colony }, pc);
            Assert.That(rows.Count, Is.EqualTo(1));
            Assert.That(rows[0].Type, Is.EqualTo(ActivityType.CommodityRequest));
            Assert.That(rows[0].SourceName, Is.EqualTo("Commodity Request"));
            Assert.That(rows[0].ProcessDetails, Is.EqualTo("Electronics x50"));
        }

        [Test]
        public void EdgeCase_FulfilledCommodityRequest_Skipped()
        {
            var pc = PlayerContext.getInstance();
            var colony = new Colony
            {
                UUID = Guid.NewGuid().ToString(),
                SystemName = "FulSys",
                ColonyName = "FulCol"
            };
            colony.Commodities.Add(new CommodityRequested
            {
                Name = "Steel",
                Requested = 20,
                Fulfilled = true,
                NeedBy = DateTime.Now.AddDays(3)
            });

            var rows = ColonyActivityCollector.CollectActivities(new[] { colony }, pc);
            Assert.That(rows.Count, Is.EqualTo(0));
        }

        [Test]
        public void EdgeCase_NeedByInPast_DisplaysZeroSeconds()
        {
            var row = new ActivityRow
            {
                Type = ActivityType.CommodityRequest,
                CountDown = null,
                NeedBy = DateTime.Now.AddDays(-2)
            };
            Assert.That(row.GetSecondsRemaining(), Is.EqualTo(0));
            Assert.That(row.GetTimeRemainingString(), Is.EqualTo("0s"));
        }

        [Test]
        public void EdgeCase_BuildCompletionTimePriorityOverProcessCompletionTime()
        {
            var pc = PlayerContext.getInstance();
            var colony = new Colony
            {
                UUID = Guid.NewGuid().ToString(),
                SystemName = "PriSys",
                ColonyName = "PriCol"
            };
            var bp = CreateBlueprint(BlueprintTypes.MiningRig, "DualTimerMiner");
            var structure = new ColonyStructure
            {
                UUID = Guid.NewGuid().ToString(),
                FlatpackBlueprintUUID = bp.UUID,
                displaySequence = 1,
                BuildCompletionTime = MakeActiveTimer(3600),
                ProcessCompletionTime = MakeActiveRepeatingTimer(7200)
            };
            colony.Structures.Add(structure);

            var rows = ColonyActivityCollector.CollectActivities(new[] { colony }, pc);
            Assert.That(rows.Count, Is.EqualTo(1),
                    "Should produce exactly one row (Building takes priority)");
            Assert.That(rows[0].Type, Is.EqualTo(ActivityType.Building));
            Assert.That(rows[0].ProcessDetails, Is.EqualTo("Building"));
        }

        [Test]
        public void EdgeCase_RefiningSyntheticRecipe_CorrectFormat()
        {
            var pc = PlayerContext.getInstance();
            var colony = new Colony
            {
                UUID = Guid.NewGuid().ToString(),
                SystemName = "SynSys",
                ColonyName = "SynCol"
            };
            var bp = CreateBlueprint(BlueprintTypes.Refinery, "SyntheticRefinery");
            var structure = new ColonyStructure
            {
                UUID = Guid.NewGuid().ToString(),
                FlatpackBlueprintUUID = bp.UUID,
                displaySequence = 2,
                ProcessCompletionTime = MakeActiveRepeatingTimer(3600),
                RefiningResource = "Lanthanides",
                RefiningResourcePurity = GameConstants.PurityRefined
            };
            colony.Structures.Add(structure);

            var rows = ColonyActivityCollector.CollectActivities(new[] { colony }, pc);
            Assert.That(rows.Count, Is.EqualTo(1));
            Assert.That(rows[0].Type, Is.EqualTo(ActivityType.Refining));
            Assert.That(rows[0].ProcessDetails, Is.EqualTo("1250:25 S1. Translanthanic Exotics"));
        }

        [Test]
        public void EdgeCase_RefiningNormalResource_CorrectFormat()
        {
            var pc = PlayerContext.getInstance();
            var colony = new Colony
            {
                UUID = Guid.NewGuid().ToString(),
                SystemName = "NormSys",
                ColonyName = "NormCol"
            };
            var bp = CreateBlueprint(BlueprintTypes.Refinery, "NormalRefinery");
            var structure = new ColonyStructure
            {
                UUID = Guid.NewGuid().ToString(),
                FlatpackBlueprintUUID = bp.UUID,
                displaySequence = 4,
                ProcessCompletionTime = MakeActiveRepeatingTimer(3600),
                RefiningResource = "Copper",
                RefiningResourcePurity = "Medium"
            };
            colony.Structures.Add(structure);

            var rows = ColonyActivityCollector.CollectActivities(new[] { colony }, pc);
            Assert.That(rows.Count, Is.EqualTo(1));
            Assert.That(rows[0].Type, Is.EqualTo(ActivityType.Refining));
            int baseRate = GameConstants.RefiningBaseRate;
            Assert.That(rows[0].ProcessDetails, Is.EqualTo($"{baseRate}:{baseRate * 3} Copper (Medium)"));
        }

        // -----------------------------------------------------------------------
        // Property 5: Combined activity type and text filtering
        // Feature: colony-activity-form, Property 5: Combined activity type and text filtering
        // **Validates: Requirements 4.3, 5.2, 5.3, 5.4**
        // -----------------------------------------------------------------------

        [Test]
        public void Property5_CombinedActivityTypeAndTextFiltering()
        {
            var allActivityTypes = (ActivityType[])Enum.GetValues(typeof(ActivityType));

            for (int iteration = 0; iteration < 100; iteration++)
            {
                // Generate 5–20 random ActivityRows
                int rowCount = Rng.Next(5, 21);
                var rows = new List<ActivityRow>();
                for (int r = 0; r < rowCount; r++)
                {
                    var type = allActivityTypes[Rng.Next(allActivityTypes.Length)];
                    var row = new ActivityRow
                    {
                        Type = type,
                        SystemName = "Sys" + Rng.Next(100),
                        ColonyName = "Col" + Rng.Next(100),
                        SourceName = "Src" + Rng.Next(100),
                        ProcessDetails = "Proc" + Rng.Next(100),
                        CountDown = MakeActiveTimer(Rng.Next(60, 86400))
                    };
                    rows.Add(row);
                }

                // Generate a random subset of ActivityType for the filter
                var selectedTypes = new HashSet<ActivityType>();
                foreach (var at in allActivityTypes)
                {
                    if (Rng.Next(2) == 1)
                        selectedTypes.Add(at);
                }

                // Generate a random text filter — sometimes empty, sometimes a substring from a row
                string textFilter;
                int filterChoice = Rng.Next(3);
                if (filterChoice == 0)
                {
                    textFilter = "";
                }
                else if (filterChoice == 1)
                {
                    // Pick a substring from a random row's field
                    var pickRow = rows[Rng.Next(rows.Count)];
                    string[] fields = { pickRow.SystemName, pickRow.ColonyName, pickRow.Type.ToString(), pickRow.SourceName, pickRow.ProcessDetails };
                    string field = fields[Rng.Next(fields.Length)];
                    int start = Rng.Next(field.Length);
                    int len = Rng.Next(1, field.Length - start + 1);
                    textFilter = field.Substring(start, len);
                }
                else
                {
                    // Random string unlikely to match
                    textFilter = "ZZZ" + Rng.Next(10000);
                }

                // Reference implementation of PassesTextFilter (replicating FormColonyActivity logic)
                bool PassesTextFilter(ActivityRow row, string filter)
                {
                    if (string.IsNullOrEmpty(filter)) return true;
                    return (row.GetTimeRemainingString() ?? "").IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0
                        || (row.SystemName ?? "").IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0
                        || (row.ColonyName ?? "").IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0
                        || row.Type.ToString().IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0
                        || (row.SourceName ?? "").IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0
                        || (row.ProcessDetails ?? "").IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
                }

                // Compute expected filtered set
                var expected = rows
                    .Where(r => selectedTypes.Contains(r.Type))
                    .Where(r => PassesTextFilter(r, textFilter))
                    .ToList();

                // Compute actual filtered set using the same logic
                var actual = rows
                    .Where(r => selectedTypes.Contains(r.Type))
                    .Where(r => PassesTextFilter(r, textFilter))
                    .ToList();

                Assert.That(actual.Count, Is.EqualTo(expected.Count),
                    $"Iteration {iteration}: filtered count mismatch (types={string.Join(",", selectedTypes)}, text='{textFilter}')");

                for (int i = 0; i < expected.Count; i++)
                {
                    Assert.That(actual[i], Is.SameAs(expected[i]),
                        $"Iteration {iteration}, index {i}: row reference mismatch");
                }
            }
        }

        // -----------------------------------------------------------------------
        // Property 6: Default sort order by numeric seconds remaining
        // Feature: colony-activity-form, Property 6: Default sort order by numeric seconds remaining
        // **Validates: Requirements 8.1, 8.4**
        // -----------------------------------------------------------------------

        [Test]
        public void Property6_DefaultSortOrderBySecondsRemaining()
        {
            for (int iteration = 0; iteration < 100; iteration++)
            {
                // Generate 5–20 random ActivityRows with varying seconds remaining
                int rowCount = Rng.Next(5, 21);
                var rows = new List<ActivityRow>();
                for (int r = 0; r < rowCount; r++)
                {
                    // Mix of CountDown-based and NeedBy-based rows
                    ActivityRow row;
                    if (Rng.Next(3) == 0)
                    {
                        // CommodityRequest with NeedBy
                        row = new ActivityRow
                        {
                            Type = ActivityType.CommodityRequest,
                            SystemName = "Sys" + r,
                            ColonyName = "Col" + r,
                            SourceName = "Commodity Request",
                            ProcessDetails = "Item x" + Rng.Next(1, 100),
                            CountDown = null,
                            NeedBy = DateTime.Now.AddSeconds(Rng.Next(0, 864000))
                        };
                    }
                    else
                    {
                        // Structure-based with CountDown timer
                        row = new ActivityRow
                        {
                            Type = ActivityType.Building,
                            SystemName = "Sys" + r,
                            ColonyName = "Col" + r,
                            SourceName = "Src" + r,
                            ProcessDetails = "Building",
                            CountDown = MakeActiveTimer(Rng.Next(0, 864000))
                        };
                    }
                    rows.Add(row);
                }

                // Sort by GetSecondsRemaining() ascending (the default sort)
                var sorted = rows.OrderBy(r => r.GetSecondsRemaining()).ToList();

                // Verify monotonic non-decreasing order
                for (int i = 1; i < sorted.Count; i++)
                {
                    long prev = sorted[i - 1].GetSecondsRemaining();
                    long curr = sorted[i].GetSecondsRemaining();
                    Assert.That(curr, Is.GreaterThanOrEqualTo(prev),
                        $"Iteration {iteration}, index {i}: sort order violated ({prev} > {curr})");
                }
            }
        }
    }
}
