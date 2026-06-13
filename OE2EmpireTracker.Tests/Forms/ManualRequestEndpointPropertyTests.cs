// <copyright file="ManualRequestEndpointPropertyTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

// Feature: api-manual-requests, Property 1: Endpoint-to-Field Visibility Mapping

using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using NUnit.Framework;
using OE2EmpireTracker.Forms.GameApiStatus;

namespace OE2EmpireTracker.Tests.Forms
{
    /// <summary>
    /// Property-based tests for ManualRequestEndpoint metadata correctness.
    /// Feature: api-manual-requests
    /// </summary>
    [TestFixture]
    public class ManualRequestEndpointPropertyTests
    {
        // ---------------------------------------------------------------
        // Property 1: Endpoint-to-Field Visibility Mapping
        // For any endpoint in ManualRequestEndpoint.All, verify that:
        // - It has a valid EndpointCategory value
        // - Parameterless endpoints have null IdLabel
        // - SingleId endpoints have non-null IdLabel
        // - LocationDetail endpoints have non-null IdLabel
        // - MarketView endpoints have null IdLabel
        // - MarketCompetitors endpoints have null IdLabel
        // **Validates: Requirements 1.3, 2.1, 2.2**
        // ---------------------------------------------------------------

        private static Gen<ManualRequestEndpoint> EndpointGen()
        {
            var allEndpoints = ManualRequestEndpoint.All;
            return from index in Gen.Choose(0, allEndpoints.Count - 1)
                   select allEndpoints[index];
        }

        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property Every_Endpoint_Has_Valid_Category()
        {
            return Prop.ForAll(
                Arb.From(EndpointGen()),
                endpoint =>
                {
                    var validCategories = Enum.GetValues(typeof(EndpointCategory))
                        .Cast<EndpointCategory>()
                        .ToList();
                    return validCategories.Contains(endpoint.Category);
                });
        }

        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property Parameterless_Endpoints_Have_Null_IdLabel()
        {
            var parameterlessGen = from endpoint in EndpointGen()
                                   where endpoint.Category == EndpointCategory.Parameterless
                                   select endpoint;

            return Prop.ForAll(
                Arb.From(parameterlessGen),
                endpoint => endpoint.IdLabel == null);
        }

        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property SingleId_Endpoints_Have_NonNull_IdLabel()
        {
            var singleIdGen = from endpoint in EndpointGen()
                              where endpoint.Category == EndpointCategory.SingleId
                              select endpoint;

            return Prop.ForAll(
                Arb.From(singleIdGen),
                endpoint => endpoint.IdLabel != null);
        }

        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property LocationDetail_Endpoints_Have_NonNull_IdLabel()
        {
            var locationDetailGen = from endpoint in EndpointGen()
                                    where endpoint.Category == EndpointCategory.LocationDetail
                                    select endpoint;

            return Prop.ForAll(
                Arb.From(locationDetailGen),
                endpoint => endpoint.IdLabel != null);
        }

        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property MarketView_Endpoints_Have_Null_IdLabel()
        {
            var marketViewGen = from endpoint in EndpointGen()
                                where endpoint.Category == EndpointCategory.MarketView
                                select endpoint;

            return Prop.ForAll(
                Arb.From(marketViewGen),
                endpoint => endpoint.IdLabel == null);
        }

        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property MarketCompetitors_Endpoints_Have_Null_IdLabel()
        {
            var marketCompetitorsGen = from endpoint in EndpointGen()
                                       where endpoint.Category == EndpointCategory.MarketCompetitors
                                       select endpoint;

            return Prop.ForAll(
                Arb.From(marketCompetitorsGen),
                endpoint => endpoint.IdLabel == null);
        }

        // ---------------------------------------------------------------
        // Feature: api-manual-requests, Property 4: Endpoint-to-Method Routing
        // For any endpoint in ManualRequestEndpoint.All, verify that
        // the routing dictionary/switch has a matching entry.
        // **Validates: Requirements 4.5**
        // ---------------------------------------------------------------

        /// <summary>
        /// The set of all endpoint display names that have routing entries
        /// in FormGameApiStatus.RouteRequestAsync switch statement.
        /// </summary>
        private static readonly HashSet<string> RoutedEndpoints = new HashSet<string>(StringComparer.Ordinal)
        {
            "Character",
            "Character Skills",
            "Colony List",
            "Colony Buildings",
            "Colony Warehouse",
            "Colony Workers",
            "Colony Summary",
            "Banking Balance",
            "Banking Transactions",
            "Accepted Jobs",
            "Asset Locations",
            "Location Detail",
            "Blueprint",
            "Survey",
            "Crate",
            "Kill Mail List",
            "Kill Mail Detail",
            "Ship Configuration",
            "Ship Cargo",
            "Mail List",
            "Mail Detail",
            "Market Listings",
            "Market Prices",
            "Market Items",
            "Market Ship Components",
            "Market Buy Orders",
            "Market Sell Orders",
            "Market Buy Competitors",
            "Market Sell Competitors",
        };

        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property Every_Endpoint_Has_Routing_Entry()
        {
            return Prop.ForAll(
                Arb.From(EndpointGen()),
                endpoint => RoutedEndpoints.Contains(endpoint.DisplayName));
        }

        [Test]
        public void Routing_Set_Covers_All_Endpoints_Exactly()
        {
            var allDisplayNames = ManualRequestEndpoint.All
                .Select(e => e.DisplayName)
                .ToList();

            var missingFromRouting = allDisplayNames
                .Where(name => !RoutedEndpoints.Contains(name))
                .ToList();

            var extraInRouting = RoutedEndpoints
                .Where(name => !allDisplayNames.Contains(name))
                .ToList();

            Assert.That(
                missingFromRouting,
                Is.Empty,
                "Endpoints missing from routing: " + string.Join(", ", missingFromRouting));

            Assert.That(
                extraInRouting,
                Is.Empty,
                "Extra entries in routing set not in ManualRequestEndpoint.All: " + string.Join(", ", extraInRouting));
        }
    }
}
