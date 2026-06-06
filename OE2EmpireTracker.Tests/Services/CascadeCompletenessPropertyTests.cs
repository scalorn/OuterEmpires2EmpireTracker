// <copyright file="CascadeCompletenessPropertyTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Linq;
using FsCheck;
using NUnit.Framework;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Property-based tests for cascade completeness.
    /// Validates that the colony list cascading logic produces exactly
    /// 4 work items per colony (summary, buildings, warehouse, workers).
    /// </summary>
    [TestFixture]
    public class CascadeCompletenessPropertyTests
    {
        // -----------------------------------------------------------------------
        // Property 7: Cascading Completeness (Colonies)
        // For every colony in the colony list response, exactly 4 cascade items
        // are enqueued: ColonySummary, ColonyBuildings, ColonyWarehouse, ColonyWorkers.
        // **Validates: Requirements 2.2**
        // -----------------------------------------------------------------------

        /// <summary>
        /// Generates a list of colony IDs (simulating a colony list API response).
        /// Each colony ID is a positive integer representing the colony's game API identifier.
        /// </summary>
        /// <returns>A generator producing lists of 0 to 50 colony IDs.</returns>
        private static Gen<List<int>> ColonyIdListGen()
        {
            return from count in Gen.Choose(0, 50)
                   from ids in Gen.ListOf(count, Gen.Choose(1, 99999))
                   select ids.ToList();
        }

        /// <summary>
        /// Oracle function replicating the colony list cascading logic.
        /// For each colony in the list, produces exactly 4 cascade labels:
        /// ColonySummary:{id}, ColonyBuildings:{id}, ColonyWarehouse:{id}, ColonyWorkers:{id}.
        /// </summary>
        /// <param name="colonyIds">The list of colony IDs from the colony list response.</param>
        /// <returns>The list of cascade work item labels that should be produced.</returns>
        private static List<string> ComputeExpectedCascades(List<int> colonyIds)
        {
            var cascades = new List<string>();
            foreach (int id in colonyIds)
            {
                cascades.Add("ColonySummary:" + id);
                cascades.Add("ColonyBuildings:" + id);
                cascades.Add("ColonyWarehouse:" + id);
                cascades.Add("ColonyWorkers:" + id);
            }

            return cascades;
        }

        /// <summary>
        /// Property: For any list of N colonies, the cascade count is exactly 4*N.
        /// Each colony produces exactly one summary, one buildings, one warehouse, and one workers item.
        /// </summary>
        /// <returns>The FsCheck property to verify.</returns>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property CascadeCount_Equals_FourTimesColonyCount()
        {
            return Prop.ForAll(
                Arb.From(ColonyIdListGen()),
                colonyIds =>
                {
                    var cascades = ComputeExpectedCascades(colonyIds);

                    int expectedCount = 4 * colonyIds.Count;
                    int actualCount = cascades.Count;

                    return (actualCount == expectedCount)
                        .Label(
                            "Expected cascade count=" + expectedCount
                            + " (4*" + colonyIds.Count + ")"
                            + ", actual=" + actualCount);
                });
        }

        /// <summary>
        /// Property: For any list of N colonies, each colony ID appears in exactly
        /// 4 cascade labels (one per cascade type).
        /// </summary>
        /// <returns>The FsCheck property to verify.</returns>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property EachColony_HasExactlyFourCascadeTypes()
        {
            return Prop.ForAll(
                Arb.From(ColonyIdListGen()),
                colonyIds =>
                {
                    var cascades = ComputeExpectedCascades(colonyIds);

                    foreach (int id in colonyIds)
                    {
                        string suffix = ":" + id;
                        var matching = cascades.Where(c => c.EndsWith(suffix)).ToList();

                        if (matching.Count != 4)
                        {
                            return false.Label(
                                "Colony " + id + " has " + matching.Count
                                + " cascade items, expected 4");
                        }

                        bool hasSummary = matching.Any(c => c == "ColonySummary:" + id);
                        bool hasBuildings = matching.Any(c => c == "ColonyBuildings:" + id);
                        bool hasWarehouse = matching.Any(c => c == "ColonyWarehouse:" + id);
                        bool hasWorkers = matching.Any(c => c == "ColonyWorkers:" + id);

                        if (!hasSummary || !hasBuildings || !hasWarehouse || !hasWorkers)
                        {
                            return false.Label(
                                "Colony " + id + " missing cascade type:"
                                + " summary=" + hasSummary
                                + " buildings=" + hasBuildings
                                + " warehouse=" + hasWarehouse
                                + " workers=" + hasWorkers);
                        }
                    }

                    return true.Label("OK");
                });
        }
    }
}
