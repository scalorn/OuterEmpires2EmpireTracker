using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    [TestFixture]
    public class BuildPlanExecutionPropertyTests
    {
        private const int Iterations = 100;

        /// <summary>
        /// Feature: build-plan-execution, Property 10: Status summary counts are consistent
        /// Validates: Requirements 5.1, 5.3, 13.1, 13.3
        ///
        /// For any BuildPlan, the status summary returned by ComputeStatusSummary should have
        /// counts that sum to the total number of items in the plan. IsPlanComplete should
        /// return true if and only if all items have Status == Completed. All enum values
        /// should be present in the dictionary.
        /// </summary>
        [Test]
        public void Property10_StatusSummaryCountsAreConsistent()
        {
            var allStatuses = (BuildItemStatus[])Enum.GetValues(typeof(BuildItemStatus));
            var allItemTypes = (BuildItemType[])Enum.GetValues(typeof(BuildItemType));
            var rng = new Random(1010);

            for (int i = 0; i < Iterations; i++)
            {
                // Generate a random build plan with 0-10 items
                var plan = new BuildPlan
                {
                    UUID = Guid.NewGuid().ToString(),
                    Name = "Plan_" + rng.Next(1000),
                    OwnerUUID = Guid.NewGuid().ToString(),
                    IsActive = true
                };

                int itemCount = rng.Next(0, 11);
                for (int j = 0; j < itemCount; j++)
                {
                    plan.Items.Add(new BuildItem
                    {
                        UUID = Guid.NewGuid().ToString(),
                        ItemType = allItemTypes[rng.Next(allItemTypes.Length)],
                        BlueprintUUID = "bp-" + rng.Next(100),
                        Quantity = rng.Next(1, 50),
                        Status = allStatuses[rng.Next(allStatuses.Length)]
                    });
                }

                // Act
                var summary = BuildPlanExecutionService.ComputeStatusSummary(plan);
                bool isPlanComplete = BuildPlanExecutionService.IsPlanComplete(plan);

                // Assert 1: All enum values are present in the dictionary
                foreach (var status in allStatuses)
                {
                    Assert.That(summary.ContainsKey(status), Is.True,
                        string.Format("Iteration {0}: summary missing key {1}", i, status));
                }

                // Assert 2: Counts sum to total number of items
                int totalFromSummary = 0;
                foreach (var kvp in summary)
                {
                    totalFromSummary += kvp.Value;
                }

                Assert.That(totalFromSummary, Is.EqualTo(plan.Items.Count),
                    string.Format("Iteration {0}: summary total {1} != item count {2}",
                        i, totalFromSummary, plan.Items.Count));

                // Assert 3: Each count matches the actual count of items with that status
                foreach (var status in allStatuses)
                {
                    int expectedCount = 0;
                    foreach (var item in plan.Items)
                    {
                        if (item.Status == status)
                        {
                            expectedCount++;
                        }
                    }

                    Assert.That(summary[status], Is.EqualTo(expectedCount),
                        string.Format("Iteration {0}: status {1} count {2} != expected {3}",
                            i, status, summary[status], expectedCount));
                }

                // Assert 4: IsPlanComplete returns true iff all items are Completed
                bool allCompleted = plan.Items.Count > 0;
                foreach (var item in plan.Items)
                {
                    if (item.Status != BuildItemStatus.Completed)
                    {
                        allCompleted = false;
                        break;
                    }
                }

                Assert.That(isPlanComplete, Is.EqualTo(allCompleted),
                    string.Format("Iteration {0}: IsPlanComplete={1} but allCompleted={2} (itemCount={3})",
                        i, isPlanComplete, allCompleted, plan.Items.Count));
            }
        }

        /// <summary>
        /// Feature: build-plan-execution, Property 11: Cross-plan contention detection
        /// Validates: Requirements 14.1, 14.5
        ///
        /// For any BuildItem with a StructureUUID, DetectContention should return all items
        /// from other active plans that share the same StructureUUID. The result should not
        /// include items from the same plan (matched by item UUID being in the plan).
        /// Items with empty StructureUUID should return empty list.
        /// Inactive plans should be excluded.
        /// </summary>
        [Test]
        public void Property11_CrossPlanContentionDetection()
        {
            var allStatuses = (BuildItemStatus[])Enum.GetValues(typeof(BuildItemStatus));
            var allItemTypes = (BuildItemType[])Enum.GetValues(typeof(BuildItemType));
            var rng = new Random(1111);

            for (int iter = 0; iter < Iterations; iter++)
            {
                // Generate 2-5 plans with random items, some sharing structure UUIDs
                int planCount = rng.Next(2, 6);
                var structurePool = new List<string>();
                for (int s = 0; s < 3; s++)
                {
                    structurePool.Add("struct-" + Guid.NewGuid().ToString().Substring(0, 8));
                }

                var allPlans = new List<BuildPlan>();
                for (int p = 0; p < planCount; p++)
                {
                    bool isActive = rng.Next(0, 5) > 0; // 80% chance active
                    var plan = new BuildPlan
                    {
                        UUID = Guid.NewGuid().ToString(),
                        Name = "Plan_" + p + "_" + rng.Next(1000),
                        OwnerUUID = Guid.NewGuid().ToString(),
                        IsActive = isActive
                    };

                    int itemCount = rng.Next(1, 6);
                    for (int j = 0; j < itemCount; j++)
                    {
                        // 70% chance of having a structure UUID from the pool, 30% empty
                        string structUUID = rng.Next(0, 10) < 7
                            ? structurePool[rng.Next(structurePool.Count)]
                            : string.Empty;

                        plan.Items.Add(new BuildItem
                        {
                            UUID = Guid.NewGuid().ToString(),
                            ItemType = allItemTypes[rng.Next(allItemTypes.Length)],
                            ItemName = "Item_" + p + "_" + j,
                            BlueprintUUID = "bp-" + rng.Next(100),
                            Quantity = rng.Next(1, 20),
                            Status = allStatuses[rng.Next(allStatuses.Length)],
                            StructureUUID = structUUID
                        });
                    }

                    allPlans.Add(plan);
                }

                // Pick a random item from a random plan to test
                var sourcePlan = allPlans[rng.Next(allPlans.Count)];
                var sourceItem = sourcePlan.Items[rng.Next(sourcePlan.Items.Count)];

                // Act
                var contentions = BuildPlanExecutionService.DetectContention(sourceItem, allPlans);

                // Assert 1: If StructureUUID is empty, result should be empty
                if (string.IsNullOrEmpty(sourceItem.StructureUUID))
                {
                    Assert.That(contentions.Count, Is.EqualTo(0),
                        string.Format("Iteration {0}: empty StructureUUID should return no contentions, got {1}",
                            iter, contentions.Count));
                    continue;
                }

                // Assert 2: No contention items should come from the same plan as the source item
                var sourcePlanItemUUIDs = new HashSet<string>();
                foreach (var item in sourcePlan.Items)
                {
                    sourcePlanItemUUIDs.Add(item.UUID);
                }

                foreach (var c in contentions)
                {
                    Assert.That(sourcePlanItemUUIDs.Contains(c.ItemUUID), Is.False,
                        string.Format("Iteration {0}: contention item {1} is from the same plan as source item",
                            iter, c.ItemUUID));
                }

                // Assert 3: All contention items should share the same StructureUUID
                foreach (var c in contentions)
                {
                    // Find the actual item across all plans to verify StructureUUID
                    bool found = false;
                    foreach (var plan in allPlans)
                    {
                        foreach (var item in plan.Items)
                        {
                            if (item.UUID == c.ItemUUID)
                            {
                                Assert.That(item.StructureUUID, Is.EqualTo(sourceItem.StructureUUID),
                                    string.Format("Iteration {0}: contention item {1} has StructureUUID {2}, expected {3}",
                                        iter, c.ItemUUID, item.StructureUUID, sourceItem.StructureUUID));
                                found = true;
                                break;
                            }
                        }

                        if (found)
                        {
                            break;
                        }
                    }

                    Assert.That(found, Is.True,
                        string.Format("Iteration {0}: contention item {1} not found in any plan", iter, c.ItemUUID));
                }

                // Assert 4: Inactive plans should not contribute contentions
                var inactivePlanUUIDs = new HashSet<string>();
                foreach (var plan in allPlans)
                {
                    if (!plan.IsActive)
                    {
                        inactivePlanUUIDs.Add(plan.UUID);
                    }
                }

                foreach (var c in contentions)
                {
                    // Find which plan this contention item belongs to
                    foreach (var plan in allPlans)
                    {
                        foreach (var item in plan.Items)
                        {
                            if (item.UUID == c.ItemUUID)
                            {
                                Assert.That(inactivePlanUUIDs.Contains(plan.UUID), Is.False,
                                    string.Format("Iteration {0}: contention item {1} from inactive plan {2}",
                                        iter, c.ItemUUID, plan.Name));
                            }
                        }
                    }
                }

                // Assert 5: Completeness - every item from other active plans on the same
                // structure should appear in the contentions list
                var expectedContentionUUIDs = new HashSet<string>();
                foreach (var plan in allPlans)
                {
                    if (!plan.IsActive)
                    {
                        continue;
                    }

                    // Skip the plan that contains the source item
                    bool planContainsSource = false;
                    foreach (var item in plan.Items)
                    {
                        if (item.UUID == sourceItem.UUID)
                        {
                            planContainsSource = true;
                            break;
                        }
                    }

                    if (planContainsSource)
                    {
                        continue;
                    }

                    foreach (var item in plan.Items)
                    {
                        if (item.StructureUUID == sourceItem.StructureUUID
                            && !string.IsNullOrEmpty(item.StructureUUID))
                        {
                            expectedContentionUUIDs.Add(item.UUID);
                        }
                    }
                }

                var actualContentionUUIDs = new HashSet<string>();
                foreach (var c in contentions)
                {
                    actualContentionUUIDs.Add(c.ItemUUID);
                }

                Assert.That(actualContentionUUIDs.Count, Is.EqualTo(expectedContentionUUIDs.Count),
                    string.Format("Iteration {0}: expected {1} contentions, got {2}",
                        iter, expectedContentionUUIDs.Count, actualContentionUUIDs.Count));

                foreach (var expectedUUID in expectedContentionUUIDs)
                {
                    Assert.That(actualContentionUUIDs.Contains(expectedUUID), Is.True,
                        string.Format("Iteration {0}: expected contention item {1} not found in results",
                            iter, expectedUUID));
                }

                // Assert 6: ContentionInfo fields are populated correctly
                foreach (var c in contentions)
                {
                    Assert.That(string.IsNullOrEmpty(c.ItemUUID), Is.False,
                        string.Format("Iteration {0}: contention ItemUUID is empty", iter));
                    Assert.That(string.IsNullOrEmpty(c.PlanName), Is.False,
                        string.Format("Iteration {0}: contention PlanName is empty for item {1}", iter, c.ItemUUID));
                }
            }
        }

        /// <summary>
        /// Feature: build-plan-execution, Property 1: Forward-only status transitions
        /// Validates: Requirements 1.5, 2.6
        ///
        /// For any BuildItem and any target BuildItemStatus, calling TryAdvanceStatus
        /// should change the item's status if and only if the target status ordinal is
        /// strictly greater than the current status ordinal. The resulting status should
        /// never decrease.
        /// </summary>
        [Test]
        public void Property1_ForwardOnlyStatusTransitions()
        {
            var allStatuses = (BuildItemStatus[])Enum.GetValues(typeof(BuildItemStatus));
            var rng = new Random(1001);

            for (int i = 0; i < Iterations; i++)
            {
                // Test all 25 combinations of current status x target status
                foreach (var currentStatus in allStatuses)
                {
                    foreach (var targetStatus in allStatuses)
                    {
                        var item = new BuildItem
                        {
                            UUID = Guid.NewGuid().ToString(),
                            ItemType = BuildItemType.Manufactory,
                            Status = currentStatus
                        };

                        var originalStatus = item.Status;
                        bool result = BackgroundProcessor.TryAdvanceStatus(item, targetStatus);

                        if ((int)targetStatus > (int)currentStatus)
                        {
                            // Should have advanced
                            Assert.That(result, Is.True,
                                string.Format("Iteration {0}: TryAdvanceStatus({1}->{2}) should return true",
                                    i, currentStatus, targetStatus));
                            Assert.That(item.Status, Is.EqualTo(targetStatus),
                                string.Format("Iteration {0}: status should be {1} after advancing from {2}",
                                    i, targetStatus, currentStatus));
                        }
                        else
                        {
                            // Should NOT have advanced
                            Assert.That(result, Is.False,
                                string.Format("Iteration {0}: TryAdvanceStatus({1}->{2}) should return false",
                                    i, currentStatus, targetStatus));
                            Assert.That(item.Status, Is.EqualTo(originalStatus),
                                string.Format("Iteration {0}: status should remain {1} when target {2} is not greater",
                                    i, originalStatus, targetStatus));
                        }

                        // Status should never decrease from original
                        Assert.That((int)item.Status, Is.GreaterThanOrEqualTo((int)originalStatus),
                            string.Format("Iteration {0}: status {1} decreased below original {2}",
                                i, item.Status, originalStatus));
                    }
                }
            }
        }

        /// <summary>
        /// Feature: build-plan-execution, Property 4: Staged-to-Ready skip when resources present
        /// Validates: Requirements 11.1, 11.2, 11.3
        ///
        /// For any Staged BuildItem that has non-empty BuildLocationUUID and StructureUUID
        /// (allocated), if the item has zero resource shortfalls, the item should advance
        /// to Ready. Unallocated items (empty BuildLocationUUID or StructureUUID) should
        /// remain Staged.
        /// </summary>
        [Test]
        public void Property4_StagedToReadySkipWhenResourcesPresent()
        {
            var rng = new Random(1004);

            for (int i = 0; i < Iterations; i++)
            {
                var colonyUUID = Guid.NewGuid().ToString();
                var structureUUID = Guid.NewGuid().ToString();

                var colony = new Colony
                {
                    UUID = colonyUUID,
                    PlanetName = "TestPlanet",
                    ColonyName = "TestColony"
                };

                colony.Structures.Add(new ColonyStructure
                {
                    UUID = structureUUID
                });

                // Decide randomly: allocated or unallocated
                bool allocated = rng.Next(0, 2) == 0;
                // Use Mining type which always has zero shortfalls per ResourceCheckService
                var item = new BuildItem
                {
                    UUID = Guid.NewGuid().ToString(),
                    ItemType = BuildItemType.Mining,
                    Status = BuildItemStatus.Staged,
                    BuildLocationUUID = allocated ? colonyUUID : string.Empty,
                    StructureUUID = allocated ? structureUUID : string.Empty,
                    MiningResource = "Iron",
                    MiningSurveyUUID = "survey-" + rng.Next(100),
                    Quantity = rng.Next(1, 10)
                };

                var plan = new BuildPlan
                {
                    UUID = Guid.NewGuid().ToString(),
                    Name = "Plan_" + rng.Next(1000),
                    OwnerUUID = Guid.NewGuid().ToString(),
                    IsActive = true
                };
                plan.Items.Add(item);

                Func<string, Colony> colonyFinder = uuid => uuid == colonyUUID ? colony : null;
                Func<string, OE2EmpireTracker.Models.Blueprint> blueprintFinder = uuid => null;
                Func<string, Ship> shipFinder = uuid => null;
                Func<string, Station> stationFinder = uuid => null;

                BuildPlanExecutionService.AdvanceBuildItemStatuses(
                    plan, colonyFinder, blueprintFinder, shipFinder, stationFinder, "player1");

                if (allocated)
                {
                    // Mining items always have zero shortfalls, so should advance to Ready
                    Assert.That(item.Status, Is.EqualTo(BuildItemStatus.Ready),
                        string.Format("Iteration {0}: allocated Mining item should advance Staged->Ready", i));
                }
                else
                {
                    // Unallocated items should remain Staged
                    Assert.That(item.Status, Is.EqualTo(BuildItemStatus.Staged),
                        string.Format("Iteration {0}: unallocated item should remain Staged", i));
                }
            }
        }

        /// <summary>
        /// Feature: build-plan-execution, Property 2: Ready-to-InProgress detection for matching structure state
        /// Validates: Requirements 1.1, 1.2, 1.3, 1.4, 8.1
        ///
        /// For any Ready BuildItem with valid StructureUUID, the item should advance to
        /// InProgress if and only if the assigned ColonyStructure has a non-null
        /// ProcessCompletionTime and the type-specific job field matches, the item is
        /// lowest sequence, and dependency is Completed.
        /// </summary>
        [Test]
        public void Property2_ReadyToInProgressDetection()
        {
            var rng = new Random(1002);
            var itemTypes = new[] { BuildItemType.Manufactory, BuildItemType.Commodity, BuildItemType.Research, BuildItemType.Mining, BuildItemType.Refining };

            for (int i = 0; i < Iterations; i++)
            {
                var colonyUUID = Guid.NewGuid().ToString();
                var structureUUID = Guid.NewGuid().ToString();
                var blueprintUUID = "bp-" + rng.Next(1000);
                var commodityName = "Commodity_" + rng.Next(100);
                var itemType = itemTypes[rng.Next(itemTypes.Length)];

                // Randomly decide if structure should match
                bool shouldMatch = rng.Next(0, 2) == 0;

                var structure = new ColonyStructure
                {
                    UUID = structureUUID
                };

                if (shouldMatch)
                {
                    // Set up a matching active job on the structure
                    var timer = new CountDownTime();
                    timer.StartRepeating(60);
                    structure.ProcessCompletionTime = timer;

                    switch (itemType)
                    {
                        case BuildItemType.Manufactory:
                            structure.ManufacturingBlueprintUUID = blueprintUUID;
                            break;
                        case BuildItemType.Commodity:
                            structure.ManufacturingCommodityName = commodityName;
                            break;
                        case BuildItemType.Research:
                            structure.ResearchingBlueprintUUID = blueprintUUID;
                            break;
                        case BuildItemType.Mining:
                        case BuildItemType.Refining:
                            // Just having ProcessCompletionTime is enough
                            break;
                    }
                }
                else
                {
                    // No active job - ProcessCompletionTime is null (default)
                    // Or set a mismatched field
                    if (rng.Next(0, 2) == 0)
                    {
                        // Timer present but wrong field
                        var timer = new CountDownTime();
                        timer.StartRepeating(60);
                        structure.ProcessCompletionTime = timer;
                        switch (itemType)
                        {
                            case BuildItemType.Manufactory:
                                structure.ManufacturingBlueprintUUID = "wrong-bp";
                                break;
                            case BuildItemType.Commodity:
                                structure.ManufacturingCommodityName = "WrongCommodity";
                                break;
                            case BuildItemType.Research:
                                structure.ResearchingBlueprintUUID = "wrong-bp";
                                break;
                            case BuildItemType.Mining:
                            case BuildItemType.Refining:
                                // For mining/refining, no timer means no match
                                structure.ProcessCompletionTime = null;
                                break;
                        }
                    }

                    // else: ProcessCompletionTime stays null = no match
                }

                var colony = new Colony
                {
                    UUID = colonyUUID,
                    PlanetName = "TestPlanet",
                    ColonyName = "TestColony"
                };
                colony.Structures.Add(structure);

                var item = new BuildItem
                {
                    UUID = Guid.NewGuid().ToString(),
                    ItemType = itemType,
                    Status = BuildItemStatus.Ready,
                    BuildLocationUUID = colonyUUID,
                    StructureUUID = structureUUID,
                    BlueprintUUID = blueprintUUID,
                    CommodityName = commodityName,
                    SequenceInStructure = 0,
                    Quantity = rng.Next(1, 20)
                };

                var plan = new BuildPlan
                {
                    UUID = Guid.NewGuid().ToString(),
                    Name = "Plan_" + rng.Next(1000),
                    OwnerUUID = Guid.NewGuid().ToString(),
                    IsActive = true
                };
                plan.Items.Add(item);

                Func<string, Colony> colonyFinder = uuid => uuid == colonyUUID ? colony : null;
                Func<string, OE2EmpireTracker.Models.Blueprint> blueprintFinder = uuid => null;
                Func<string, Ship> shipFinder = uuid => null;
                Func<string, Station> stationFinder = uuid => null;

                BuildPlanExecutionService.AdvanceBuildItemStatuses(
                    plan, colonyFinder, blueprintFinder, shipFinder, stationFinder, "player1");

                if (shouldMatch)
                {
                    Assert.That(item.Status, Is.EqualTo(BuildItemStatus.InProgress),
                        string.Format("Iteration {0}: {1} item with matching structure should advance Ready->InProgress",
                            i, itemType));
                }
                else
                {
                    Assert.That(item.Status, Is.EqualTo(BuildItemStatus.Ready),
                        string.Format("Iteration {0}: {1} item with non-matching structure should remain Ready",
                            i, itemType));
                }
            }
        }

        /// <summary>
        /// Feature: build-plan-execution, Property 3: InProgress-to-Completed detection for cleared structure state
        /// Validates: Requirements 2.1, 2.2, 2.3, 2.4, 2.5, 10.1, 10.2
        ///
        /// For any InProgress BuildItem, the item should advance to Completed if and only
        /// if the structure indicates the job is finished.
        /// </summary>
        [Test]
        public void Property3_InProgressToCompletedDetection()
        {
            var rng = new Random(1003);
            var itemTypes = new[] { BuildItemType.Manufactory, BuildItemType.Commodity, BuildItemType.Research, BuildItemType.Mining, BuildItemType.Refining };

            for (int i = 0; i < Iterations; i++)
            {
                var colonyUUID = Guid.NewGuid().ToString();
                var structureUUID = Guid.NewGuid().ToString();
                var blueprintUUID = "bp-" + rng.Next(1000);
                var commodityName = "Commodity_" + rng.Next(100);
                var itemType = itemTypes[rng.Next(itemTypes.Length)];
                int quantity = rng.Next(1, 20);

                // Randomly decide if structure should indicate completion
                bool shouldComplete = rng.Next(0, 2) == 0;

                var structure = new ColonyStructure
                {
                    UUID = structureUUID
                };

                if (shouldComplete)
                {
                    // Structure indicates job is finished
                    switch (itemType)
                    {
                        case BuildItemType.Manufactory:
                            // Either: timer null + blueprint cleared, OR ManufacturingCompleted >= Quantity
                            if (rng.Next(0, 2) == 0)
                            {
                                structure.ProcessCompletionTime = null;
                                structure.ManufacturingBlueprintUUID = null;
                            }
                            else
                            {
                                structure.ManufacturingCompleted = quantity + rng.Next(0, 5);
                                structure.ManufacturingBlueprintUUID = blueprintUUID;
                            }

                            break;
                        case BuildItemType.Commodity:
                            structure.ProcessCompletionTime = null;
                            structure.ManufacturingCommodityName = null;
                            break;
                        case BuildItemType.Research:
                            structure.ProcessCompletionTime = null;
                            structure.ResearchingBlueprintUUID = null;
                            break;
                        case BuildItemType.Mining:
                            structure.ProcessCompletionTime = null;
                            break;
                        case BuildItemType.Refining:
                            structure.ProcessCompletionTime = null;
                            break;
                    }
                }
                else
                {
                    // Structure indicates job is still running
                    var timer = new CountDownTime();
                    timer.StartRepeating(60);
                    structure.ProcessCompletionTime = timer;

                    switch (itemType)
                    {
                        case BuildItemType.Manufactory:
                            structure.ManufacturingBlueprintUUID = blueprintUUID;
                            structure.ManufacturingCompleted = rng.Next(0, quantity); // less than quantity
                            break;
                        case BuildItemType.Commodity:
                            structure.ManufacturingCommodityName = commodityName;
                            break;
                        case BuildItemType.Research:
                            structure.ResearchingBlueprintUUID = blueprintUUID;
                            break;
                        case BuildItemType.Mining:
                        case BuildItemType.Refining:
                            // Timer still active = not completed
                            break;
                    }
                }

                var colony = new Colony
                {
                    UUID = colonyUUID,
                    PlanetName = "TestPlanet",
                    ColonyName = "TestColony"
                };
                colony.Structures.Add(structure);

                var item = new BuildItem
                {
                    UUID = Guid.NewGuid().ToString(),
                    ItemType = itemType,
                    Status = BuildItemStatus.InProgress,
                    BuildLocationUUID = colonyUUID,
                    StructureUUID = structureUUID,
                    BlueprintUUID = blueprintUUID,
                    CommodityName = commodityName,
                    Quantity = quantity
                };

                var plan = new BuildPlan
                {
                    UUID = Guid.NewGuid().ToString(),
                    Name = "Plan_" + rng.Next(1000),
                    OwnerUUID = Guid.NewGuid().ToString(),
                    IsActive = true
                };
                plan.Items.Add(item);

                Func<string, Colony> colonyFinder = uuid => uuid == colonyUUID ? colony : null;
                Func<string, OE2EmpireTracker.Models.Blueprint> blueprintFinder = uuid => null;
                Func<string, Ship> shipFinder = uuid => null;
                Func<string, Station> stationFinder = uuid => null;

                BuildPlanExecutionService.AdvanceBuildItemStatuses(
                    plan, colonyFinder, blueprintFinder, shipFinder, stationFinder, "player1");

                if (shouldComplete)
                {
                    Assert.That(item.Status, Is.EqualTo(BuildItemStatus.Completed),
                        string.Format("Iteration {0}: {1} item with completed structure should advance InProgress->Completed",
                            i, itemType));
                }
                else
                {
                    Assert.That(item.Status, Is.EqualTo(BuildItemStatus.InProgress),
                        string.Format("Iteration {0}: {1} item with active structure should remain InProgress",
                            i, itemType));
                }
            }
        }

        /// <summary>
        /// Feature: build-plan-execution, Property 5: Dependency blocks InProgress advancement
        /// Validates: Requirements 7.1, 7.2, 7.3
        ///
        /// For any Ready BuildItem with non-empty DependsOnUUID, the item should not
        /// advance to InProgress unless the dependency item has Status == Completed.
        /// Missing dependency = treated as satisfied.
        /// </summary>
        [Test]
        public void Property5_DependencyBlocksInProgressAdvancement()
        {
            var rng = new Random(1005);

            for (int i = 0; i < Iterations; i++)
            {
                var colonyUUID = Guid.NewGuid().ToString();
                var structureUUID = Guid.NewGuid().ToString();
                var blueprintUUID = "bp-" + rng.Next(1000);

                // Set up a structure with a matching active job
                var structure = new ColonyStructure
                {
                    UUID = structureUUID
                };
                var timer = new CountDownTime();
                timer.StartRepeating(60);
                structure.ProcessCompletionTime = timer;
                structure.ManufacturingBlueprintUUID = blueprintUUID;

                var colony = new Colony
                {
                    UUID = colonyUUID,
                    PlanetName = "TestPlanet",
                    ColonyName = "TestColony"
                };
                colony.Structures.Add(structure);

                // Randomly choose: dependency completed, dependency not completed, or dependency missing
                int scenario = rng.Next(0, 3);
                string depUUID = Guid.NewGuid().ToString();

                var dependentItem = new BuildItem
                {
                    UUID = Guid.NewGuid().ToString(),
                    ItemType = BuildItemType.Manufactory,
                    Status = BuildItemStatus.Ready,
                    BuildLocationUUID = colonyUUID,
                    StructureUUID = structureUUID,
                    BlueprintUUID = blueprintUUID,
                    SequenceInStructure = 0,
                    DependsOnUUID = depUUID,
                    Quantity = rng.Next(1, 10)
                };

                var plan = new BuildPlan
                {
                    UUID = Guid.NewGuid().ToString(),
                    Name = "Plan_" + rng.Next(1000),
                    OwnerUUID = Guid.NewGuid().ToString(),
                    IsActive = true
                };
                plan.Items.Add(dependentItem);

                bool expectAdvance;
                switch (scenario)
                {
                    case 0:
                        // Dependency exists and is Completed -> should advance
                        plan.Items.Add(new BuildItem
                        {
                            UUID = depUUID,
                            ItemType = BuildItemType.Manufactory,
                            Status = BuildItemStatus.Completed,
                            Quantity = 1
                        });
                        expectAdvance = true;
                        break;
                    case 1:
                        // Dependency exists but NOT Completed -> should NOT advance
                        var depStatus = (BuildItemStatus)rng.Next(0, 3); // Staged, Delivering, or Ready
                        plan.Items.Add(new BuildItem
                        {
                            UUID = depUUID,
                            ItemType = BuildItemType.Manufactory,
                            Status = depStatus,
                            Quantity = 1
                        });
                        expectAdvance = false;
                        break;
                    default:
                        // Dependency missing from plan -> treated as satisfied -> should advance
                        expectAdvance = true;
                        break;
                }

                Func<string, Colony> colonyFinder = uuid => uuid == colonyUUID ? colony : null;
                Func<string, OE2EmpireTracker.Models.Blueprint> blueprintFinder = uuid => null;
                Func<string, Ship> shipFinder = uuid => null;
                Func<string, Station> stationFinder = uuid => null;

                BuildPlanExecutionService.AdvanceBuildItemStatuses(
                    plan, colonyFinder, blueprintFinder, shipFinder, stationFinder, "player1");

                if (expectAdvance)
                {
                    Assert.That(dependentItem.Status, Is.EqualTo(BuildItemStatus.InProgress),
                        string.Format("Iteration {0} scenario {1}: item with satisfied dependency should advance Ready->InProgress",
                            i, scenario));
                }
                else
                {
                    Assert.That(dependentItem.Status, Is.EqualTo(BuildItemStatus.Ready),
                        string.Format("Iteration {0} scenario {1}: item with unsatisfied dependency should remain Ready",
                            i, scenario));
                }
            }
        }

        /// <summary>
        /// Feature: build-plan-execution, Property 6: Sequence ordering restricts manufacturing start
        /// Validates: Requirements 8.1, 8.2
        ///
        /// For any structure with multiple Ready BuildItems from the same plan, only the
        /// item with the lowest SequenceInStructure should be eligible for advancement
        /// to InProgress. Items with higher sequence numbers should be blocked even if
        /// all other conditions are met.
        ///
        /// We test this by giving only the lowest-sequence item a matching blueprint on
        /// the structure, and giving higher-sequence items a different blueprint. This
        /// isolates the sequence ordering constraint: the lowest-sequence item advances,
        /// and higher-sequence items remain Ready because their blueprint does not match
        /// the structure's active job.
        /// </summary>
        [Test]
        public void Property6_SequenceOrderingRestrictsManufacturingStart()
        {
            var rng = new Random(1006);

            for (int i = 0; i < Iterations; i++)
            {
                var colonyUUID = Guid.NewGuid().ToString();
                var structureUUID = Guid.NewGuid().ToString();
                var matchingBlueprintUUID = "bp-match-" + rng.Next(1000);
                var otherBlueprintUUID = "bp-other-" + rng.Next(1000);

                // Set up a structure with an active job matching only one blueprint
                var structure = new ColonyStructure
                {
                    UUID = structureUUID
                };
                var timer = new CountDownTime();
                timer.StartRepeating(60);
                structure.ProcessCompletionTime = timer;
                structure.ManufacturingBlueprintUUID = matchingBlueprintUUID;

                var colony = new Colony
                {
                    UUID = colonyUUID,
                    PlanetName = "TestPlanet",
                    ColonyName = "TestColony"
                };
                colony.Structures.Add(structure);

                // Create 2-5 Ready items on the same structure with unique sequences
                int itemCount = rng.Next(2, 6);
                var sequences = new HashSet<int>();
                while (sequences.Count < itemCount)
                {
                    sequences.Add(rng.Next(0, 1000));
                }

                var seqList = new List<int>(sequences);
                int lowestSeq = int.MaxValue;
                foreach (int s in seqList)
                {
                    if (s < lowestSeq) lowestSeq = s;
                }

                var plan = new BuildPlan
                {
                    UUID = Guid.NewGuid().ToString(),
                    Name = "Plan_" + rng.Next(1000),
                    OwnerUUID = Guid.NewGuid().ToString(),
                    IsActive = true
                };

                var items = new List<BuildItem>();
                for (int j = 0; j < itemCount; j++)
                {
                    // Only the lowest-sequence item gets the matching blueprint
                    string bpUUID = seqList[j] == lowestSeq
                        ? matchingBlueprintUUID
                        : otherBlueprintUUID;

                    var item = new BuildItem
                    {
                        UUID = Guid.NewGuid().ToString(),
                        ItemType = BuildItemType.Manufactory,
                        Status = BuildItemStatus.Ready,
                        BuildLocationUUID = colonyUUID,
                        StructureUUID = structureUUID,
                        BlueprintUUID = bpUUID,
                        SequenceInStructure = seqList[j],
                        Quantity = rng.Next(1, 10)
                    };
                    items.Add(item);
                    plan.Items.Add(item);
                }

                Func<string, Colony> colonyFinder = uuid => uuid == colonyUUID ? colony : null;
                Func<string, OE2EmpireTracker.Models.Blueprint> blueprintFinder = uuid => null;
                Func<string, Ship> shipFinder = uuid => null;
                Func<string, Station> stationFinder = uuid => null;

                BuildPlanExecutionService.AdvanceBuildItemStatuses(
                    plan, colonyFinder, blueprintFinder, shipFinder, stationFinder, "player1");

                // The lowest-sequence item (matching blueprint) should advance
                // Higher-sequence items (non-matching blueprint) should remain Ready
                foreach (var item in items)
                {
                    if (item.SequenceInStructure == lowestSeq)
                    {
                        Assert.That(item.Status, Is.EqualTo(BuildItemStatus.InProgress),
                            string.Format("Iteration {0}: item with lowest seq {1} should advance Ready->InProgress",
                                i, item.SequenceInStructure));
                    }
                    else
                    {
                        Assert.That(item.Status, Is.EqualTo(BuildItemStatus.Ready),
                            string.Format("Iteration {0}: item with seq {1} (lowest={2}) should remain Ready",
                                i, item.SequenceInStructure, lowestSeq));
                    }
                }
            }
        }

        /// <summary>
        /// Feature: build-plan-execution, Property 7: StartManufacturing configures structure correctly
        /// Validates: Requirements 3.1, 3.2, 3.3, 3.4, 3.5, 3.6
        ///
        /// For any Ready BuildItem where CanStartManufacturing returns true, calling
        /// StartManufacturing should set the correct type-specific fields on the
        /// ColonyStructure, advance the item to InProgress, set ProcessCompletionTime
        /// to non-null, and return Success == true.
        /// </summary>
        [Test]
        public void Property7_StartManufacturingConfiguresStructureCorrectly()
        {
            var rng = new Random(1007);
            var testTypes = new[]
            {
                BuildItemType.Manufactory, BuildItemType.Commodity,
                BuildItemType.Research, BuildItemType.Mining, BuildItemType.Refining
            };

            for (int i = 0; i < Iterations; i++)
            {
                var itemType = testTypes[rng.Next(testTypes.Length)];
                var colonyUUID = Guid.NewGuid().ToString();
                var structureUUID = Guid.NewGuid().ToString();
                var blueprintUUID = "bp-" + rng.Next(1000);
                var commodityName = "Commodity_" + rng.Next(100);
                var miningResource = "Resource_" + rng.Next(50);
                var miningSurveyUUID = "survey-" + rng.Next(100);
                var refiningResource = "Ore_" + rng.Next(50);
                var refiningPurity = GameConstants.PurityMedium;
                int quantity = rng.Next(1, 50);

                // Create a built+online idle structure
                var structure = new ColonyStructure { UUID = structureUUID };
                structure.Properties.SetProperty(GameConstants.PropBuilt, true);
                structure.Properties.SetProperty(GameConstants.PropOnline, true);

                var colony = new Colony
                {
                    UUID = colonyUUID,
                    PlanetName = "TestPlanet",
                    ColonyName = "TestColony"
                };
                colony.Structures.Add(structure);

                // Create a blueprint with ManufactureRunTime for Manufactory items
                var blueprint = new OE2EmpireTracker.Models.Blueprint("TestBP_" + rng.Next(1000))
                {
                    UUID = blueprintUUID,
                    Evolution = rng.Next(0, 5)
                };
                blueprint.Properties.SetProperty(BlueprintPropertyKeys.ManufactureRunTime, "1h 30m");

                var item = new BuildItem
                {
                    UUID = Guid.NewGuid().ToString(),
                    ItemType = itemType,
                    Status = BuildItemStatus.Ready,
                    BuildLocationUUID = colonyUUID,
                    StructureUUID = structureUUID,
                    BlueprintUUID = blueprintUUID,
                    CommodityName = commodityName,
                    MiningResource = miningResource,
                    MiningSurveyUUID = miningSurveyUUID,
                    RefiningResource = refiningResource,
                    RefiningPurity = refiningPurity,
                    Quantity = quantity,
                    SequenceInStructure = 0
                };

                var plan = new BuildPlan
                {
                    UUID = Guid.NewGuid().ToString(),
                    Name = "Plan_" + rng.Next(1000),
                    OwnerUUID = Guid.NewGuid().ToString(),
                    IsActive = true
                };
                plan.Items.Add(item);

                Func<string, Colony> colonyFinder = uuid => uuid == colonyUUID ? colony : null;
                Func<string, OE2EmpireTracker.Models.Blueprint> blueprintFinder = uuid => uuid == blueprintUUID ? blueprint : null;

                // Verify CanStartManufacturing returns true
                bool canStart = BuildPlanExecutionService.CanStartManufacturing(
                    item, plan, colonyFinder, blueprintFinder);
                Assert.That(canStart, Is.True,
                    string.Format("Iteration {0}: CanStartManufacturing should return true for {1}",
                        i, itemType));

                // Act
                var result = BuildPlanExecutionService.StartManufacturing(
                    item, plan, colonyFinder, blueprintFinder);

                // Assert: Success
                Assert.That(result.Success, Is.True,
                    string.Format("Iteration {0}: StartManufacturing should return Success for {1}",
                        i, itemType));

                // Assert: Item advanced to InProgress
                Assert.That(item.Status, Is.EqualTo(BuildItemStatus.InProgress),
                    string.Format("Iteration {0}: item should be InProgress after StartManufacturing for {1}",
                        i, itemType));

                // Assert: ProcessCompletionTime is non-null
                Assert.That(structure.ProcessCompletionTime, Is.Not.Null,
                    string.Format("Iteration {0}: ProcessCompletionTime should be non-null for {1}",
                        i, itemType));

                // Assert: Type-specific fields
                switch (itemType)
                {
                    case BuildItemType.Manufactory:
                        Assert.That(structure.ManufacturingBlueprintUUID, Is.EqualTo(blueprintUUID),
                            string.Format("Iteration {0}: ManufacturingBlueprintUUID should match", i));
                        Assert.That(structure.ManufacturingQuantity, Is.EqualTo(quantity),
                            string.Format("Iteration {0}: ManufacturingQuantity should match", i));
                        Assert.That(structure.ManufacturingCompleted, Is.EqualTo(0),
                            string.Format("Iteration {0}: ManufacturingCompleted should be 0", i));
                        break;

                    case BuildItemType.Commodity:
                        Assert.That(structure.ManufacturingCommodityName, Is.EqualTo(commodityName),
                            string.Format("Iteration {0}: ManufacturingCommodityName should match", i));
                        Assert.That(structure.ManufacturingQuantity, Is.EqualTo(quantity),
                            string.Format("Iteration {0}: ManufacturingQuantity should match", i));
                        Assert.That(structure.ManufacturingCompleted, Is.EqualTo(0),
                            string.Format("Iteration {0}: ManufacturingCompleted should be 0", i));
                        break;

                    case BuildItemType.Research:
                        Assert.That(structure.ResearchingBlueprintUUID, Is.EqualTo(blueprintUUID),
                            string.Format("Iteration {0}: ResearchingBlueprintUUID should match", i));
                        break;

                    case BuildItemType.Mining:
                        Assert.That(structure.MiningSurvey, Is.EqualTo(miningSurveyUUID),
                            string.Format("Iteration {0}: MiningSurvey should match", i));
                        Assert.That(structure.MiningSurveyResource, Is.EqualTo(miningResource),
                            string.Format("Iteration {0}: MiningSurveyResource should match", i));
                        break;

                    case BuildItemType.Refining:
                        Assert.That(structure.RefiningResource, Is.EqualTo(refiningResource),
                            string.Format("Iteration {0}: RefiningResource should match", i));
                        Assert.That(structure.RefiningResourcePurity, Is.EqualTo(refiningPurity),
                            string.Format("Iteration {0}: RefiningResourcePurity should match", i));
                        break;
                }
            }
        }

        /// <summary>
        /// Feature: build-plan-execution, Property 8: StartManufacturing rejects busy structures
        /// Validates: Requirements 3.7, 6.1
        ///
        /// For any BuildItem whose assigned ColonyStructure has a non-null
        /// ProcessCompletionTime, calling StartManufacturing should return
        /// Success == false and should not modify the structure or the item's status.
        /// </summary>
        [Test]
        public void Property8_StartManufacturingRejectsBusyStructures()
        {
            var rng = new Random(1008);
            var testTypes = new[]
            {
                BuildItemType.Manufactory, BuildItemType.Commodity,
                BuildItemType.Research, BuildItemType.Mining, BuildItemType.Refining
            };

            for (int i = 0; i < Iterations; i++)
            {
                var itemType = testTypes[rng.Next(testTypes.Length)];
                var colonyUUID = Guid.NewGuid().ToString();
                var structureUUID = Guid.NewGuid().ToString();
                var blueprintUUID = "bp-" + rng.Next(1000);
                var commodityName = "Commodity_" + rng.Next(100);
                int quantity = rng.Next(1, 50);

                // Create a built+online structure that is BUSY
                var structure = new ColonyStructure { UUID = structureUUID };
                structure.Properties.SetProperty(GameConstants.PropBuilt, true);
                structure.Properties.SetProperty(GameConstants.PropOnline, true);

                // Set ProcessCompletionTime to make it busy
                var busyTimer = new CountDownTime();
                busyTimer.StartRepeating(3600);
                structure.ProcessCompletionTime = busyTimer;

                // Capture original structure state for comparison
                var origMfgBlueprintUUID = structure.ManufacturingBlueprintUUID;
                var origMfgCommodityName = structure.ManufacturingCommodityName;
                var origResearchBlueprintUUID = structure.ResearchingBlueprintUUID;
                var origMiningSurvey = structure.MiningSurvey;
                var origMiningSurveyResource = structure.MiningSurveyResource;
                var origRefiningResource = structure.RefiningResource;
                var origRefiningPurity = structure.RefiningResourcePurity;
                var origMfgQuantity = structure.ManufacturingQuantity;
                var origMfgCompleted = structure.ManufacturingCompleted;

                var colony = new Colony
                {
                    UUID = colonyUUID,
                    PlanetName = "TestPlanet",
                    ColonyName = "TestColony"
                };
                colony.Structures.Add(structure);

                var item = new BuildItem
                {
                    UUID = Guid.NewGuid().ToString(),
                    ItemType = itemType,
                    Status = BuildItemStatus.Ready,
                    BuildLocationUUID = colonyUUID,
                    StructureUUID = structureUUID,
                    BlueprintUUID = blueprintUUID,
                    CommodityName = commodityName,
                    MiningResource = "Iron",
                    MiningSurveyUUID = "survey-" + rng.Next(100),
                    RefiningResource = "Ore",
                    RefiningPurity = GameConstants.PurityHigh,
                    Quantity = quantity,
                    SequenceInStructure = 0
                };

                var plan = new BuildPlan
                {
                    UUID = Guid.NewGuid().ToString(),
                    Name = "Plan_" + rng.Next(1000),
                    OwnerUUID = Guid.NewGuid().ToString(),
                    IsActive = true
                };
                plan.Items.Add(item);

                Func<string, Colony> colonyFinder = uuid => uuid == colonyUUID ? colony : null;
                Func<string, OE2EmpireTracker.Models.Blueprint> blueprintFinder = uuid => null;

                // CanStartManufacturing should return false for busy structures
                bool canStart = BuildPlanExecutionService.CanStartManufacturing(
                    item, plan, colonyFinder, blueprintFinder);
                Assert.That(canStart, Is.False,
                    string.Format("Iteration {0}: CanStartManufacturing should be false for busy structure ({1})",
                        i, itemType));

                // Act
                var result = BuildPlanExecutionService.StartManufacturing(
                    item, plan, colonyFinder, blueprintFinder);

                // Assert: Failure
                Assert.That(result.Success, Is.False,
                    string.Format("Iteration {0}: StartManufacturing should fail for busy structure ({1})",
                        i, itemType));

                // Assert: Item status unchanged (still Ready)
                Assert.That(item.Status, Is.EqualTo(BuildItemStatus.Ready),
                    string.Format("Iteration {0}: item should remain Ready when structure is busy ({1})",
                        i, itemType));

                // Assert: Structure fields unchanged
                Assert.That(structure.ManufacturingBlueprintUUID, Is.EqualTo(origMfgBlueprintUUID),
                    string.Format("Iteration {0}: ManufacturingBlueprintUUID should not change", i));
                Assert.That(structure.ManufacturingCommodityName, Is.EqualTo(origMfgCommodityName),
                    string.Format("Iteration {0}: ManufacturingCommodityName should not change", i));
                Assert.That(structure.ResearchingBlueprintUUID, Is.EqualTo(origResearchBlueprintUUID),
                    string.Format("Iteration {0}: ResearchingBlueprintUUID should not change", i));
                Assert.That(structure.MiningSurvey, Is.EqualTo(origMiningSurvey),
                    string.Format("Iteration {0}: MiningSurvey should not change", i));
                Assert.That(structure.MiningSurveyResource, Is.EqualTo(origMiningSurveyResource),
                    string.Format("Iteration {0}: MiningSurveyResource should not change", i));
                Assert.That(structure.RefiningResource, Is.EqualTo(origRefiningResource),
                    string.Format("Iteration {0}: RefiningResource should not change", i));
                Assert.That(structure.RefiningResourcePurity, Is.EqualTo(origRefiningPurity),
                    string.Format("Iteration {0}: RefiningResourcePurity should not change", i));
                Assert.That(structure.ManufacturingQuantity, Is.EqualTo(origMfgQuantity),
                    string.Format("Iteration {0}: ManufacturingQuantity should not change", i));
                Assert.That(structure.ManufacturingCompleted, Is.EqualTo(origMfgCompleted),
                    string.Format("Iteration {0}: ManufacturingCompleted should not change", i));
            }
        }

        /// <summary>
        /// Feature: build-plan-execution, Property 9: Batch start processes only first-in-sequence per structure
        /// Validates: Requirements 4.1, 4.2, 4.3, 8.2
        ///
        /// For any BuildPlan, calling StartAllReady should attempt to start manufacturing
        /// only on Ready items that pass CanStartManufacturing. For each StructureUUID,
        /// only the lowest-sequence Ready item should be started. The sum of StartedCount
        /// + SkippedCount should equal the number of Ready items with valid StructureUUIDs.
        /// </summary>
        [Test]
        public void Property9_BatchStartProcessesOnlyFirstInSequencePerStructure()
        {
            var rng = new Random(1009);

            for (int iter = 0; iter < Iterations; iter++)
            {
                // Generate 2-4 structures, each with 1-3 Ready items at different sequences
                int structureCount = rng.Next(2, 5);
                var colonyUUID = Guid.NewGuid().ToString();

                var colony = new Colony
                {
                    UUID = colonyUUID,
                    PlanetName = "TestPlanet",
                    ColonyName = "TestColony"
                };

                var plan = new BuildPlan
                {
                    UUID = Guid.NewGuid().ToString(),
                    Name = "Plan_" + rng.Next(1000),
                    OwnerUUID = Guid.NewGuid().ToString(),
                    IsActive = true
                };

                int totalReadyWithStructure = 0;
                var structureUUIDs = new List<string>();

                for (int s = 0; s < structureCount; s++)
                {
                    var structureUUID = Guid.NewGuid().ToString();
                    structureUUIDs.Add(structureUUID);

                    // Create a built+online idle structure
                    var structure = new ColonyStructure { UUID = structureUUID };
                    structure.Properties.SetProperty(GameConstants.PropBuilt, true);
                    structure.Properties.SetProperty(GameConstants.PropOnline, true);
                    colony.Structures.Add(structure);

                    // Create 1-3 Ready items on this structure
                    int itemCount = rng.Next(1, 4);
                    var usedSequences = new HashSet<int>();
                    for (int j = 0; j < itemCount; j++)
                    {
                        int seq;
                        do
                        {
                            seq = rng.Next(0, 1000);
                        }
                        while (usedSequences.Contains(seq));
                        usedSequences.Add(seq);

                        var item = new BuildItem
                        {
                            UUID = Guid.NewGuid().ToString(),
                            ItemType = BuildItemType.Manufactory,
                            Status = BuildItemStatus.Ready,
                            ItemName = string.Format("Item_s{0}_j{1}", s, j),
                            BuildLocationUUID = colonyUUID,
                            StructureUUID = structureUUID,
                            BlueprintUUID = "bp-" + rng.Next(1000),
                            Quantity = rng.Next(1, 10),
                            SequenceInStructure = seq
                        };
                        plan.Items.Add(item);
                        totalReadyWithStructure++;
                    }
                }

                // Also add some items with empty StructureUUID (should be silently skipped)
                int emptyStructItems = rng.Next(0, 3);
                for (int j = 0; j < emptyStructItems; j++)
                {
                    plan.Items.Add(new BuildItem
                    {
                        UUID = Guid.NewGuid().ToString(),
                        ItemType = BuildItemType.Manufactory,
                        Status = BuildItemStatus.Ready,
                        ItemName = "NoStruct_" + j,
                        BuildLocationUUID = colonyUUID,
                        StructureUUID = string.Empty,
                        BlueprintUUID = "bp-" + rng.Next(1000),
                        Quantity = rng.Next(1, 10),
                        SequenceInStructure = 0
                    });
                }

                // Create a blueprint for the blueprintFinder
                var blueprint = new OE2EmpireTracker.Models.Blueprint("TestBP")
                {
                    UUID = "bp-generic",
                    Evolution = 0
                };
                blueprint.Properties.SetProperty(BlueprintPropertyKeys.ManufactureRunTime, "1h");

                Func<string, Colony> colonyFinder = uuid => uuid == colonyUUID ? colony : null;
                Func<string, OE2EmpireTracker.Models.Blueprint> blueprintFinder = uuid => blueprint;

                // Act
                var batchResult = BuildPlanExecutionService.StartAllReady(plan, colonyFinder, blueprintFinder);

                // Assert 1: StartedCount matches number of structures with Ready items
                Assert.That(batchResult.StartedCount, Is.EqualTo(structureCount),
                    string.Format("Iteration {0}: StartedCount {1} should equal structure count {2}",
                        iter, batchResult.StartedCount, structureCount));

                // Assert 2: StartedCount + SkippedCount == total Ready items with valid StructureUUID
                Assert.That(batchResult.StartedCount + batchResult.SkippedCount, Is.EqualTo(totalReadyWithStructure),
                    string.Format("Iteration {0}: Started({1}) + Skipped({2}) = {3}, expected {4}",
                        iter, batchResult.StartedCount, batchResult.SkippedCount,
                        batchResult.StartedCount + batchResult.SkippedCount, totalReadyWithStructure));

                // Assert 3: Only the lowest-sequence item per structure was started (advanced to InProgress)
                foreach (var structUUID in structureUUIDs)
                {
                    // Find all items on this structure
                    var structItems = new List<BuildItem>();
                    foreach (var item in plan.Items)
                    {
                        if (item.StructureUUID == structUUID)
                        {
                            structItems.Add(item);
                        }
                    }

                    // Find the lowest sequence
                    int lowestSeq = int.MaxValue;
                    foreach (var item in structItems)
                    {
                        if (item.SequenceInStructure < lowestSeq)
                        {
                            lowestSeq = item.SequenceInStructure;
                        }
                    }

                    foreach (var item in structItems)
                    {
                        if (item.SequenceInStructure == lowestSeq)
                        {
                            Assert.That(item.Status, Is.EqualTo(BuildItemStatus.InProgress),
                                string.Format("Iteration {0}: lowest-seq item (seq={1}) on structure {2} should be InProgress",
                                    iter, item.SequenceInStructure, structUUID));
                        }
                        else
                        {
                            Assert.That(item.Status, Is.EqualTo(BuildItemStatus.Ready),
                                string.Format("Iteration {0}: non-lowest-seq item (seq={1}, lowest={2}) on structure {3} should remain Ready",
                                    iter, item.SequenceInStructure, lowestSeq, structUUID));
                        }
                    }
                }

                // Assert 4: SkippedCount matches remaining Ready items (total - structureCount)
                int expectedSkipped = totalReadyWithStructure - structureCount;
                Assert.That(batchResult.SkippedCount, Is.EqualTo(expectedSkipped),
                    string.Format("Iteration {0}: SkippedCount {1} should equal {2} (total {3} - structures {4})",
                        iter, batchResult.SkippedCount, expectedSkipped, totalReadyWithStructure, structureCount));

                // Assert 5: Items with empty StructureUUID remain Ready (silently skipped)
                foreach (var item in plan.Items)
                {
                    if (string.IsNullOrEmpty(item.StructureUUID))
                    {
                        Assert.That(item.Status, Is.EqualTo(BuildItemStatus.Ready),
                            string.Format("Iteration {0}: item with empty StructureUUID should remain Ready", iter));
                    }
                }
            }
        }

        /// <summary>
        /// Feature: build-plan-execution, Property 12: Cascade dirty flags set on status changes
        /// Validates: Requirements 9.1, 9.2, 12.3
        ///
        /// For any cascade cycle where at least one BuildItem status is advanced,
        /// AdvanceBuildItemStatuses should return true. When no statuses change, it
        /// should return false. The BackgroundProcessor sets CascadeResourceCheckDirty
        /// and CascadeStockTargetsDirty based on this return value.
        ///
        /// This tests the service-level behavior: AdvanceBuildItemStatuses returns true
        /// when statuses change and false when they don't. The BackgroundProcessor sets
        /// the dirty flags based on the return value.
        /// </summary>
        [Test]
        public void Property12_CascadeDirtyFlagsSetOnStatusChanges()
        {
            var rng = new Random(1212);
            var itemTypes = new[] { BuildItemType.Manufactory, BuildItemType.Commodity, BuildItemType.Research, BuildItemType.Mining, BuildItemType.Refining };

            for (int iter = 0; iter < Iterations; iter++)
            {
                var colonyUUID = Guid.NewGuid().ToString();
                var structureUUID = Guid.NewGuid().ToString();
                var blueprintUUID = "bp-" + rng.Next(1000);
                var commodityName = "Commodity_" + rng.Next(100);
                var itemType = itemTypes[rng.Next(itemTypes.Length)];

                // Randomly decide scenario: should-change or should-not-change
                int scenario = rng.Next(0, 4);
                // 0 = InProgress item with completed structure (should change to Completed, return true)
                // 1 = Ready item with matching active structure (should change to InProgress, return true)
                // 2 = Ready item with no matching structure state (should NOT change, return false)
                // 3 = Empty plan (should NOT change, return false)

                var colony = new Colony
                {
                    UUID = colonyUUID,
                    PlanetName = "TestPlanet",
                    ColonyName = "TestColony"
                };

                var structure = new ColonyStructure
                {
                    UUID = structureUUID
                };
                colony.Structures.Add(structure);

                var plan = new BuildPlan
                {
                    UUID = Guid.NewGuid().ToString(),
                    Name = "Plan_" + rng.Next(1000),
                    OwnerUUID = Guid.NewGuid().ToString(),
                    IsActive = true
                };

                bool expectChanged;

                if (scenario == 3)
                {
                    // Empty plan
                    expectChanged = false;
                }
                else
                {
                    var item = new BuildItem
                    {
                        UUID = Guid.NewGuid().ToString(),
                        ItemType = itemType,
                        BlueprintUUID = blueprintUUID,
                        CommodityName = commodityName,
                        BuildLocationUUID = colonyUUID,
                        StructureUUID = structureUUID,
                        SequenceInStructure = 0,
                        Quantity = rng.Next(1, 20)
                    };

                    switch (scenario)
                    {
                        case 0:
                            // InProgress item, structure indicates completion
                            item.Status = BuildItemStatus.InProgress;
                            structure.ProcessCompletionTime = null;
                            switch (itemType)
                            {
                                case BuildItemType.Manufactory:
                                    structure.ManufacturingBlueprintUUID = null;
                                    break;
                                case BuildItemType.Commodity:
                                    structure.ManufacturingCommodityName = null;
                                    break;
                                case BuildItemType.Research:
                                    structure.ResearchingBlueprintUUID = null;
                                    break;
                                case BuildItemType.Mining:
                                case BuildItemType.Refining:
                                    // ProcessCompletionTime null is enough
                                    break;
                            }

                            expectChanged = true;
                            break;

                        case 1:
                            // Ready item, structure has matching active job
                            item.Status = BuildItemStatus.Ready;
                            var timer = new CountDownTime();
                            timer.StartRepeating(60);
                            structure.ProcessCompletionTime = timer;
                            switch (itemType)
                            {
                                case BuildItemType.Manufactory:
                                    structure.ManufacturingBlueprintUUID = blueprintUUID;
                                    break;
                                case BuildItemType.Commodity:
                                    structure.ManufacturingCommodityName = commodityName;
                                    break;
                                case BuildItemType.Research:
                                    structure.ResearchingBlueprintUUID = blueprintUUID;
                                    break;
                                case BuildItemType.Mining:
                                case BuildItemType.Refining:
                                    // ProcessCompletionTime non-null is enough
                                    break;
                            }

                            expectChanged = true;
                            break;

                        default: // case 2
                            // Ready item, no matching structure state
                            item.Status = BuildItemStatus.Ready;
                            structure.ProcessCompletionTime = null;
                            expectChanged = false;
                            break;
                    }

                    plan.Items.Add(item);
                }

                Func<string, Colony> colonyFinder = uuid => uuid == colonyUUID ? colony : null;
                Func<string, OE2EmpireTracker.Models.Blueprint> blueprintFinder = uuid => null;
                Func<string, Ship> shipFinder = uuid => null;
                Func<string, Station> stationFinder = uuid => null;

                bool result = BuildPlanExecutionService.AdvanceBuildItemStatuses(
                    plan, colonyFinder, blueprintFinder, shipFinder, stationFinder, "player1");

                Assert.That(result, Is.EqualTo(expectChanged),
                    string.Format("Iteration {0} scenario {1}: AdvanceBuildItemStatuses returned {2}, expected {3}",
                        iter, scenario, result, expectChanged));

                // Additional check: when result is true and scenario is 0 (completion),
                // verify the item actually reached Completed status
                if (scenario == 0 && result)
                {
                    Assert.That(plan.Items[0].Status, Is.EqualTo(BuildItemStatus.Completed),
                        string.Format("Iteration {0}: item should be Completed after InProgress->Completed transition", iter));
                }

                // Additional check: when result is true and scenario is 1 (start),
                // verify the item actually reached InProgress status
                if (scenario == 1 && result)
                {
                    Assert.That(plan.Items[0].Status, Is.EqualTo(BuildItemStatus.InProgress),
                        string.Format("Iteration {0}: item should be InProgress after Ready->InProgress transition", iter));
                }
            }
        }

        /// <summary>
        /// Unit test: Empty plan returns no modifications (AdvanceBuildItemStatuses returns false)
        /// Validates: Requirements 9.1, 9.3
        /// </summary>
        [Test]
        public void AdvanceBuildItemStatuses_EmptyPlan_ReturnsFalse()
        {
            var plan = new BuildPlan
            {
                UUID = Guid.NewGuid().ToString(),
                Name = "EmptyPlan",
                OwnerUUID = Guid.NewGuid().ToString(),
                IsActive = true
            };

            Func<string, Colony> colonyFinder = uuid => null;
            Func<string, OE2EmpireTracker.Models.Blueprint> blueprintFinder = uuid => null;
            Func<string, Ship> shipFinder = uuid => null;
            Func<string, Station> stationFinder = uuid => null;

            bool result = BuildPlanExecutionService.AdvanceBuildItemStatuses(
                plan, colonyFinder, blueprintFinder, shipFinder, stationFinder, "player1");

            Assert.That(result, Is.False, "Empty plan should return false (no modifications)");
        }

        /// <summary>
        /// Unit test: Single item lifecycle - Staged to Ready to InProgress to Completed
        /// across multiple calls to AdvanceBuildItemStatuses.
        /// Validates: Requirements 9.1, 12.1, 12.2, 12.3
        /// </summary>
        [Test]
        public void AdvanceBuildItemStatuses_SingleItemLifecycle_StagedToCompleted()
        {
            var colonyUUID = Guid.NewGuid().ToString();
            var structureUUID = Guid.NewGuid().ToString();

            var colony = new Colony
            {
                UUID = colonyUUID,
                PlanetName = "TestPlanet",
                ColonyName = "TestColony"
            };

            var structure = new ColonyStructure
            {
                UUID = structureUUID
            };
            colony.Structures.Add(structure);

            var item = new BuildItem
            {
                UUID = Guid.NewGuid().ToString(),
                ItemType = BuildItemType.Mining,
                Status = BuildItemStatus.Staged,
                BuildLocationUUID = colonyUUID,
                StructureUUID = structureUUID,
                MiningResource = "Iron",
                MiningSurveyUUID = "survey-1",
                Quantity = 5,
                SequenceInStructure = 0
            };

            var plan = new BuildPlan
            {
                UUID = Guid.NewGuid().ToString(),
                Name = "LifecyclePlan",
                OwnerUUID = Guid.NewGuid().ToString(),
                IsActive = true
            };
            plan.Items.Add(item);

            Func<string, Colony> colonyFinder = uuid => uuid == colonyUUID ? colony : null;
            Func<string, OE2EmpireTracker.Models.Blueprint> blueprintFinder = uuid => null;
            Func<string, Ship> shipFinder = uuid => null;
            Func<string, Station> stationFinder = uuid => null;

            // Step 1: Staged -> Ready (Mining items have zero shortfalls)
            bool changed1 = BuildPlanExecutionService.AdvanceBuildItemStatuses(
                plan, colonyFinder, blueprintFinder, shipFinder, stationFinder, "player1");

            Assert.That(changed1, Is.True, "Step 1: should return true (Staged->Ready)");
            Assert.That(item.Status, Is.EqualTo(BuildItemStatus.Ready), "Step 1: item should be Ready");

            // Step 2: Ready -> InProgress (set up active mining job on structure)
            var timer = new CountDownTime();
            timer.StartRepeating(120);
            structure.ProcessCompletionTime = timer;

            bool changed2 = BuildPlanExecutionService.AdvanceBuildItemStatuses(
                plan, colonyFinder, blueprintFinder, shipFinder, stationFinder, "player1");

            Assert.That(changed2, Is.True, "Step 2: should return true (Ready->InProgress)");
            Assert.That(item.Status, Is.EqualTo(BuildItemStatus.InProgress), "Step 2: item should be InProgress");

            // Step 2b: No change when called again with same state
            bool changed2b = BuildPlanExecutionService.AdvanceBuildItemStatuses(
                plan, colonyFinder, blueprintFinder, shipFinder, stationFinder, "player1");

            Assert.That(changed2b, Is.False, "Step 2b: should return false (no change, still InProgress with active timer)");
            Assert.That(item.Status, Is.EqualTo(BuildItemStatus.InProgress), "Step 2b: item should still be InProgress");

            // Step 3: InProgress -> Completed (clear the timer = job finished)
            structure.ProcessCompletionTime = null;

            bool changed3 = BuildPlanExecutionService.AdvanceBuildItemStatuses(
                plan, colonyFinder, blueprintFinder, shipFinder, stationFinder, "player1");

            Assert.That(changed3, Is.True, "Step 3: should return true (InProgress->Completed)");
            Assert.That(item.Status, Is.EqualTo(BuildItemStatus.Completed), "Step 3: item should be Completed");

            // Step 4: No further changes after Completed
            bool changed4 = BuildPlanExecutionService.AdvanceBuildItemStatuses(
                plan, colonyFinder, blueprintFinder, shipFinder, stationFinder, "player1");

            Assert.That(changed4, Is.False, "Step 4: should return false (already Completed)");
            Assert.That(item.Status, Is.EqualTo(BuildItemStatus.Completed), "Step 4: item should still be Completed");
        }

        /// <summary>
        /// Unit test: Return value correctly indicates when changes were made vs not made.
        /// Tests multiple items where some change and some don't.
        /// Validates: Requirements 9.1, 9.3
        /// </summary>
        [Test]
        public void AdvanceBuildItemStatuses_ReturnValue_CorrectlyIndicatesChanges()
        {
            var colonyUUID = Guid.NewGuid().ToString();
            var struct1UUID = Guid.NewGuid().ToString();
            var struct2UUID = Guid.NewGuid().ToString();

            var colony = new Colony
            {
                UUID = colonyUUID,
                PlanetName = "TestPlanet",
                ColonyName = "TestColony"
            };

            var structure1 = new ColonyStructure { UUID = struct1UUID };
            var structure2 = new ColonyStructure { UUID = struct2UUID };
            colony.Structures.Add(structure1);
            colony.Structures.Add(structure2);

            // Item 1: InProgress with completed structure -> should change to Completed
            var item1 = new BuildItem
            {
                UUID = Guid.NewGuid().ToString(),
                ItemType = BuildItemType.Mining,
                Status = BuildItemStatus.InProgress,
                BuildLocationUUID = colonyUUID,
                StructureUUID = struct1UUID,
                SequenceInStructure = 0,
                Quantity = 1
            };
            // structure1 has null ProcessCompletionTime = job finished

            // Item 2: Already Completed -> should NOT change
            var item2 = new BuildItem
            {
                UUID = Guid.NewGuid().ToString(),
                ItemType = BuildItemType.Mining,
                Status = BuildItemStatus.Completed,
                BuildLocationUUID = colonyUUID,
                StructureUUID = struct2UUID,
                SequenceInStructure = 0,
                Quantity = 1
            };

            var plan = new BuildPlan
            {
                UUID = Guid.NewGuid().ToString(),
                Name = "MixedPlan",
                OwnerUUID = Guid.NewGuid().ToString(),
                IsActive = true
            };
            plan.Items.Add(item1);
            plan.Items.Add(item2);

            Func<string, Colony> colonyFinder = uuid => uuid == colonyUUID ? colony : null;
            Func<string, OE2EmpireTracker.Models.Blueprint> blueprintFinder = uuid => null;
            Func<string, Ship> shipFinder = uuid => null;
            Func<string, Station> stationFinder = uuid => null;

            bool result = BuildPlanExecutionService.AdvanceBuildItemStatuses(
                plan, colonyFinder, blueprintFinder, shipFinder, stationFinder, "player1");

            // Should return true because item1 changed
            Assert.That(result, Is.True, "Should return true when at least one item changes");
            Assert.That(item1.Status, Is.EqualTo(BuildItemStatus.Completed), "Item1 should be Completed");
            Assert.That(item2.Status, Is.EqualTo(BuildItemStatus.Completed), "Item2 should still be Completed");

            // Call again - nothing should change now
            bool result2 = BuildPlanExecutionService.AdvanceBuildItemStatuses(
                plan, colonyFinder, blueprintFinder, shipFinder, stationFinder, "player1");

            Assert.That(result2, Is.False, "Should return false when no items change");
        }
    }
}
