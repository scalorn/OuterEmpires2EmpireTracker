// <copyright file="ColonyMergeServiceWorkersTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using NUnit.Framework;
using OE2EmpireTracker.Common.Client.Generated;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Unit tests for ColonyMergeService.MergeWorkers.
    /// Validates: Req 12, Criteria 2-5 (commodity mapping, Delivered logic, empty list, change detection).
    /// Validates: Req 13, Criteria 1-3 (attitude, workforce overview, wages).
    /// </summary>
    [TestFixture]
    public class ColonyMergeServiceWorkersTests
    {
        private Colony _colony;

        [SetUp]
        public void SetUp()
        {
            _colony = new Colony
            {
                UUID = "colony-workers-test-001",
                OwnerUUID = "owner-uuid-001",
                ColonyName = "Test Colony",
                WorkerCurrentAttitude = 50,
                BlueCollarAllocated = 0,
                BlueCollarUnallocated = 0,
                WhiteCollarAllocated = 0,
                WhiteCollarUnallocated = 0,
                SpecialistAllocated = 0,
                SpecialistUnallocated = 0,
                WageLevel = 100,
            };
        }

        // -------------------------------------------------------------------
        // Test 1: MergeWorkers maps commodity demands to CommodityRequested list
        // Validates: Req 12.2
        // -------------------------------------------------------------------

        [Test]
        public void MergeWorkers_MapsCommodityDemandsToCommodityRequestedList()
        {
            var needBy = new DateTime(2025, 3, 15, 10, 0, 0, DateTimeKind.Utc);
            var apiWorkers = new ColonyWorkers
            {
                WorkerCurrentAttitude = 50,
                WorkforceCommodityDemands = new List<ColonyCommodityDemand>
                {
                    new ColonyCommodityDemand
                    {
                        TypeName = "Food Rations",
                        Amount = 100,
                        RequiredBy = new DateTimeOffset(needBy),
                        Fulfilled = false,
                    },
                    new ColonyCommodityDemand
                    {
                        TypeName = "Luxury Goods",
                        Amount = 50,
                        RequiredBy = new DateTimeOffset(needBy.AddDays(1)),
                        Fulfilled = true,
                    },
                },
            };

            bool changed = ColonyMergeService.MergeWorkers(apiWorkers, _colony);

            Assert.That(changed, Is.True);
            Assert.That(_colony.Commodities.Count, Is.EqualTo(2));

            Assert.That(_colony.Commodities[0].Name, Is.EqualTo("Food Rations"));
            Assert.That(_colony.Commodities[0].Requested, Is.EqualTo(100));
            Assert.That(_colony.Commodities[0].NeedBy, Is.EqualTo(needBy));
            Assert.That(_colony.Commodities[0].Fulfilled, Is.False);

            Assert.That(_colony.Commodities[1].Name, Is.EqualTo("Luxury Goods"));
            Assert.That(_colony.Commodities[1].Requested, Is.EqualTo(50));
            Assert.That(_colony.Commodities[1].NeedBy, Is.EqualTo(needBy.AddDays(1)));
            Assert.That(_colony.Commodities[1].Fulfilled, Is.True);
        }

        // -------------------------------------------------------------------
        // Test 2: MergeWorkers sets Delivered=Amount when Fulfilled=true
        // Validates: Req 12.3
        // -------------------------------------------------------------------

        [Test]
        public void MergeWorkers_SetsDeliveredToAmountWhenFulfilledTrue()
        {
            var needBy = new DateTime(2025, 3, 15, 10, 0, 0, DateTimeKind.Utc);
            var apiWorkers = new ColonyWorkers
            {
                WorkerCurrentAttitude = 50,
                WorkforceCommodityDemands = new List<ColonyCommodityDemand>
                {
                    new ColonyCommodityDemand
                    {
                        TypeName = "Fulfilled Item",
                        Amount = 200,
                        RequiredBy = new DateTimeOffset(needBy),
                        Fulfilled = true,
                    },
                    new ColonyCommodityDemand
                    {
                        TypeName = "Unfulfilled Item",
                        Amount = 75,
                        RequiredBy = new DateTimeOffset(needBy),
                        Fulfilled = false,
                    },
                },
            };

            ColonyMergeService.MergeWorkers(apiWorkers, _colony);

            Assert.That(_colony.Commodities[0].Delivered, Is.EqualTo(200));
            Assert.That(_colony.Commodities[1].Delivered, Is.EqualTo(0));
        }

        // -------------------------------------------------------------------
        // Test 3: MergeWorkers clears Commodities on empty demand list
        // Validates: Req 12.4
        // -------------------------------------------------------------------

        [Test]
        public void MergeWorkers_ClearsCommoditiesOnEmptyDemandList()
        {
            _colony.Commodities = new List<CommodityRequested>
            {
                new CommodityRequested
                {
                    Name = "Old Demand",
                    Requested = 50,
                    Delivered = 0,
                    NeedBy = new DateTime(2025, 4, 1, 12, 0, 0, DateTimeKind.Utc),
                    Fulfilled = false,
                },
            };

            var apiWorkers = new ColonyWorkers
            {
                WorkerCurrentAttitude = 50,
                WorkforceCommodityDemands = new List<ColonyCommodityDemand>(),
            };

            bool changed = ColonyMergeService.MergeWorkers(apiWorkers, _colony);

            Assert.That(changed, Is.True);
            Assert.That(_colony.Commodities.Count, Is.EqualTo(0));
        }

        // -------------------------------------------------------------------
        // Test 4: MergeWorkers returns false when data is identical
        // Validates: Req 12.5
        // -------------------------------------------------------------------

        [Test]
        public void MergeWorkers_ReturnsFalseWhenDataIsIdentical()
        {
            var needBy = new DateTime(2025, 3, 15, 10, 0, 0, DateTimeKind.Utc);

            _colony.WorkerCurrentAttitude = 80;
            _colony.BlueCollarAllocated = 10;
            _colony.BlueCollarUnallocated = 5;
            _colony.WhiteCollarAllocated = 3;
            _colony.WhiteCollarUnallocated = 2;
            _colony.SpecialistAllocated = 1;
            _colony.SpecialistUnallocated = 0;
            _colony.WageLevel = 120;
            _colony.Commodities = new List<CommodityRequested>
            {
                new CommodityRequested
                {
                    Name = "Food Rations",
                    Requested = 100,
                    NeedBy = needBy,
                    Fulfilled = false,
                    Delivered = 0,
                },
            };

            var apiWorkers = new ColonyWorkers
            {
                WorkerCurrentAttitude = 80,
                WorkforceOverview = new ColonyWorkforceOverview
                {
                    BlueCollarAllocated = 10,
                    BlueCollarUnallocated = 5,
                    WhiteCollarAllocated = 3,
                    WhiteCollarUnallocated = 2,
                    SpecialistAllocated = 1,
                    SpecialistUnallocated = 0,
                },
                Wages = new ColonyWages
                {
                    CurrentWagePercentage = 120,
                },
                WorkforceCommodityDemands = new List<ColonyCommodityDemand>
                {
                    new ColonyCommodityDemand
                    {
                        TypeName = "Food Rations",
                        Amount = 100,
                        RequiredBy = new DateTimeOffset(needBy),
                        Fulfilled = false,
                    },
                },
            };

            bool changed = ColonyMergeService.MergeWorkers(apiWorkers, _colony);

            Assert.That(changed, Is.False);
        }

        // -------------------------------------------------------------------
        // Test 5: MergeWorkers updates WorkerCurrentAttitude
        // Validates: Req 13.1
        // -------------------------------------------------------------------

        [Test]
        public void MergeWorkers_UpdatesWorkerCurrentAttitude()
        {
            _colony.WorkerCurrentAttitude = 50;

            var apiWorkers = new ColonyWorkers
            {
                WorkerCurrentAttitude = 95,
                WorkforceCommodityDemands = new List<ColonyCommodityDemand>(),
            };

            bool changed = ColonyMergeService.MergeWorkers(apiWorkers, _colony);

            Assert.That(changed, Is.True);
            Assert.That(_colony.WorkerCurrentAttitude, Is.EqualTo(95));
        }

        // -------------------------------------------------------------------
        // Test 6: MergeWorkers updates workforce allocation counts
        // Validates: Req 13.2
        // -------------------------------------------------------------------

        [Test]
        public void MergeWorkers_UpdatesWorkforceAllocationCounts()
        {
            var apiWorkers = new ColonyWorkers
            {
                WorkerCurrentAttitude = 50,
                WorkforceOverview = new ColonyWorkforceOverview
                {
                    BlueCollarAllocated = 15,
                    BlueCollarUnallocated = 3,
                    WhiteCollarAllocated = 8,
                    WhiteCollarUnallocated = 2,
                    SpecialistAllocated = 4,
                    SpecialistUnallocated = 1,
                },
                WorkforceCommodityDemands = new List<ColonyCommodityDemand>(),
            };

            bool changed = ColonyMergeService.MergeWorkers(apiWorkers, _colony);

            Assert.That(changed, Is.True);
            Assert.That(_colony.BlueCollarAllocated, Is.EqualTo(15));
            Assert.That(_colony.BlueCollarUnallocated, Is.EqualTo(3));
            Assert.That(_colony.WhiteCollarAllocated, Is.EqualTo(8));
            Assert.That(_colony.WhiteCollarUnallocated, Is.EqualTo(2));
            Assert.That(_colony.SpecialistAllocated, Is.EqualTo(4));
            Assert.That(_colony.SpecialistUnallocated, Is.EqualTo(1));
        }

        // -------------------------------------------------------------------
        // Test 7: MergeWorkers updates WageLevel from CurrentWagePercentage
        // Validates: Req 13.3
        // -------------------------------------------------------------------

        [Test]
        public void MergeWorkers_UpdatesWageLevelFromCurrentWagePercentage()
        {
            _colony.WageLevel = 100;

            var apiWorkers = new ColonyWorkers
            {
                WorkerCurrentAttitude = 50,
                Wages = new ColonyWages
                {
                    CurrentWagePercentage = 150,
                },
                WorkforceCommodityDemands = new List<ColonyCommodityDemand>(),
            };

            bool changed = ColonyMergeService.MergeWorkers(apiWorkers, _colony);

            Assert.That(changed, Is.True);
            Assert.That(_colony.WageLevel, Is.EqualTo(150));
        }
    }
}
