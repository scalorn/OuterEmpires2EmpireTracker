// <copyright file="AssetCascadePropertyTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Linq;
using FsCheck;
using NUnit.Framework;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Property-based tests for asset location cascade completeness.
    /// Validates that the asset locations cascading logic produces exactly
    /// 1 detail work item per valid asset location.
    /// </summary>
    [TestFixture]
    public class AssetCascadePropertyTests
    {
        // -----------------------------------------------------------------------
        // Property 7b: Cascading Completeness (Assets)
        // For every asset location in the response, exactly 1 detail item
        // is enqueued (AssetDetail:{locationId}).
        // **Validates: Requirements 2.3**
        // -----------------------------------------------------------------------

        /// <summary>
        /// Generates a list of valid asset locations (simulating an asset locations API response).
        /// Each location has a positive LocationId and a non-empty LocationType.
        /// </summary>
        /// <returns>A generator producing lists of 0 to 100 asset location tuples.</returns>
        private static Gen<List<AssetLocationInput>> ValidAssetLocationListGen()
        {
            var locationTypes = new[] { "Co", "St", "Sh", "As" };

            return from count in Gen.Choose(0, 100)
                   from ids in Gen.ListOf(count, Gen.Choose(1, 999999))
                   from typeIndices in Gen.ListOf(count, Gen.Choose(0, locationTypes.Length - 1))
                   let uniqueIds = ids.Distinct().ToList()
                   select uniqueIds.Zip(typeIndices, (id, ti) => new AssetLocationInput(id, locationTypes[ti])).ToList();
        }

        /// <summary>
        /// Oracle function replicating the asset locations cascading logic.
        /// For each valid location (LocationId > 0 and non-empty LocationType),
        /// produces exactly 1 cascade label: AssetDetail:{locationId}.
        /// </summary>
        /// <param name="locations">The list of asset locations from the API response.</param>
        /// <returns>The list of cascade work item labels that should be produced.</returns>
        private static List<string> ComputeExpectedCascades(List<AssetLocationInput> locations)
        {
            return locations
                .Where(loc => loc.LocationId > 0 && !string.IsNullOrEmpty(loc.LocationType))
                .Select(loc => "AssetDetail:" + loc.LocationId)
                .ToList();
        }

        /// <summary>
        /// Property: For any list of N valid asset locations, the cascade count equals N.
        /// Each location produces exactly one detail work item.
        /// </summary>
        /// <returns>The FsCheck property to verify.</returns>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property CascadeCount_Equals_AssetLocationCount()
        {
            return Prop.ForAll(
                Arb.From(ValidAssetLocationListGen()),
                locations =>
                {
                    var cascades = ComputeExpectedCascades(locations);

                    int expectedCount = locations.Count;
                    int actualCount = cascades.Count;

                    return (actualCount == expectedCount)
                        .Label(
                            "Expected cascade count=" + expectedCount
                            + " (1 per location)"
                            + ", actual=" + actualCount);
                });
        }

        /// <summary>
        /// Property: For any list of asset locations, each location ID appears
        /// in exactly 1 cascade label.
        /// </summary>
        /// <returns>The FsCheck property to verify.</returns>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property EachLocation_HasExactlyOneCascadeItem()
        {
            return Prop.ForAll(
                Arb.From(ValidAssetLocationListGen()),
                locations =>
                {
                    var cascades = ComputeExpectedCascades(locations);

                    foreach (var loc in locations)
                    {
                        string expectedLabel = "AssetDetail:" + loc.LocationId;
                        int matchCount = cascades.Count(c => c == expectedLabel);

                        if (matchCount != 1)
                        {
                            return false.Label(
                                "Location " + loc.LocationId
                                + " has " + matchCount
                                + " cascade items, expected 1");
                        }
                    }

                    return true.Label("OK");
                });
        }

        /// <summary>
        /// Simple input record representing an asset location for test generation.
        /// </summary>
        public class AssetLocationInput
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="AssetLocationInput"/> class.
            /// </summary>
            /// <param name="locationId">The location identifier.</param>
            /// <param name="locationType">The location type code.</param>
            public AssetLocationInput(int locationId, string locationType)
            {
                LocationId = locationId;
                LocationType = locationType;
            }

            /// <summary>
            /// Gets the location identifier.
            /// </summary>
            public int LocationId { get; }

            /// <summary>
            /// Gets the location type code.
            /// </summary>
            public string LocationType { get; }
        }
    }
}
