using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
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
    }
}
